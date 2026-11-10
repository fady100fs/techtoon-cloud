using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Licensing;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    // ================= صف اشتراك =================
    public class SubscriptionRow : ViewModelBase
    {
        public int Id { get; set; }
        public int PartyId { get; set; }
        public string PartyName { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public decimal Amount { get; set; }
        public string Frequency { get; set; } = "monthly";
        public DateTime StartDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "active";
        public decimal NextDueAmount { get; set; }
        public string NextDueDate { get; set; } = "";
        public int OverdueCount { get; set; }
        public string Notes { get; set; } = "";

        public string AmountDisplay => Amount.ToString("N2");
        public string FrequencyDisplay => Frequency == "yearly" ? "سنوي" : "شهري";
        public string StatusDisplay => Status switch
        {
            "active" => "نشط",
            "paused" => "موقوف",
            "cancelled" => "ملغي",
            "completed" => "منتهي",
            _ => Status
        };
        public string NextDueDisplay => string.IsNullOrEmpty(NextDueDate) ? "—" : NextDueDate;
        public string OverdueDisplay => OverdueCount > 0 ? OverdueCount + " متأخرة" : "—";
    }

    // ================= فترة (دفعة) =================
    public class SubscriptionPeriodRow : ViewModelBase
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public int PeriodNumber { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal PaidAmount { get; set; }
        public string Status { get; set; } = "due";
        public string PartyName { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public decimal Remaining => Amount - PaidAmount;

        public string DueDateDisplay => DueDate.ToString("yyyy-MM-dd");
        public string AmountDisplay => Amount.ToString("N2");
        public string PaidDisplay => PaidAmount.ToString("N2");
        public string RemainingDisplay => Remaining.ToString("N2");
        public string StatusDisplay => Status switch
        {
            "paid" => "مسددة",
            "overdue" => "متأخرة",
            "cancelled" => "ملغاة",
            _ => "مستحقة"
        };
    }

    // ================= العميل (للاختيار) =================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    /// <summary>
    /// P5: نظام الاشتراكات — خدمة يشتريها العميل ويجدّد بمبلغ دوري (شهري/سنوي).
    /// تُولَّد الفترات تلقائيًا، ويُسجَّل سند الدفع لكل فترة (تُسدد الفترة)،
    /// وما لم يُسدد يظهر إنذار في لوحة التحكم.
    /// </summary>
    public class SubscriptionsViewModel : ViewModelBase
    {
        public ObservableCollection<SubscriptionRow> Subscriptions { get; } = new();
        public ObservableCollection<SubscriptionPeriodRow> Periods { get; } = new();
        public ObservableCollection<PartyOption> Parties { get; } = new();

        private SubscriptionRow _selectedSubscription;
        public SubscriptionRow SelectedSubscription { get => _selectedSubscription; set { SetProperty(ref _selectedSubscription, value); _ = LoadPeriodsAsync(); } }
        private SubscriptionPeriodRow _selectedPeriod;
        public SubscriptionPeriodRow SelectedPeriod { get => _selectedPeriod; set => SetProperty(ref _selectedPeriod, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        private string _periodsTitle = "فترات الاشتراك";
        public string PeriodsTitle { get => _periodsTitle; set => SetProperty(ref _periodsTitle, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand PayPeriodCommand { get; }
        public ICommand RefreshPeriodsCommand { get; }

        public SubscriptionsViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            AddCommand = new UiRelayCommand(_ => OpenEditor(0));
            EditCommand = new UiRelayCommand(_ => OpenEditor(SelectedSubscription?.Id ?? 0), _ => SelectedSubscription != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteSubscription(), _ => SelectedSubscription != null);
            PauseCommand = new UiRelayCommand(_ => TogglePause(), _ => SelectedSubscription != null);
            PayPeriodCommand = new UiRelayCommand(_ => PayPeriod(), _ => SelectedPeriod != null);
            RefreshPeriodsCommand = new UiRelayCommand(_ => _ = LoadPeriodsAsync());
            _ = LoadAsync();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        /// <summary>P5: أعمدة DATE تُعيدها Npgsql بصيغة DateOnly — نحولها لتاريخ/وقت موحّد.</summary>
        public static DateTime ToDateTime(object? value) => value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => DateTime.MinValue
        };

        // ================= تحميل =================
        public async Task LoadAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();

                // العملاء
                Parties.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties WHERE COALESCE(is_active, TRUE) ORDER BY name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) });

                // الاشتراكات + الفترة التالية + عدد المتأخرات
                Subscriptions.Clear();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT s.id, s.party_id, COALESCE(p.name,''), COALESCE(s.service_name,''),
                           COALESCE(s.amount,0), COALESCE(s.frequency,'monthly'), COALESCE(s.start_date, CURRENT_DATE),
                           COALESCE(s.status,'active'), COALESCE(s.notes,''),
                           (SELECT MIN(sp.due_date) FROM subscription_periods sp
                             WHERE sp.subscription_id = s.id AND sp.status IN ('due','overdue') AND sp.paid_amount < sp.amount),
                           (SELECT COALESCE(SUM(sp.amount - sp.paid_amount),0) FROM subscription_periods sp
                             WHERE sp.subscription_id = s.id AND sp.status IN ('due','overdue') AND sp.paid_amount < sp.amount),
                           (SELECT COUNT(*) FROM subscription_periods sp
                             WHERE sp.subscription_id = s.id AND sp.status IN ('due','overdue') AND sp.paid_amount < sp.amount AND sp.due_date < CURRENT_DATE)
                    FROM subscriptions s
                    LEFT JOIN parties p ON p.id = s.party_id
                    ORDER BY s.created_at DESC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        Subscriptions.Add(new SubscriptionRow
                        {
                            Id = r.GetInt32(0),
                            PartyId = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                            PartyName = r.GetString(2),
                            ServiceName = r.GetString(3),
                            Amount = Convert.ToDecimal(r.GetValue(4)),
                            Frequency = r.GetString(5),
                            StartDate = r.IsDBNull(6) ? DateTime.Today : ToDateTime(r.GetValue(6)),
                            Status = r.GetString(7),
                            Notes = r.GetString(8),
                            NextDueDate = r.IsDBNull(9) ? "" : ToDateTime(r.GetValue(9)).ToString("yyyy-MM-dd"),
                            NextDueAmount = Convert.ToDecimal(r.GetValue(10)),
                            OverdueCount = Convert.ToInt32(r.GetValue(11))
                        });

                // تحديث حالات المتأخرات
                await UpdateOverdueFlagsAsync(conn);

                StatusMessage = "تم التحميل: " + Subscriptions.Count + " اشتراك";
                await LoadPeriodsAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private static async Task UpdateOverdueFlagsAsync(NpgsqlConnection conn)
        {
            try
            {
                using var cmd = new NpgsqlCommand(@"UPDATE subscription_periods SET status='overdue'
                    WHERE status='due' AND due_date < CURRENT_DATE AND paid_amount < amount", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { }
        }

        public async Task LoadPeriodsAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                Periods.Clear();
                int subId = SelectedSubscription?.Id ?? 0;
                using var cmd = new NpgsqlCommand(@"
                    SELECT sp.id, sp.subscription_id, sp.period_number, sp.due_date, COALESCE(sp.amount,0),
                           COALESCE(sp.paid_amount,0), COALESCE(sp.status,'due'),
                           COALESCE(p.name,''), COALESCE(s.service_name,'')
                    FROM subscription_periods sp
                    JOIN subscriptions s ON s.id = sp.subscription_id
                    LEFT JOIN parties p ON p.id = s.party_id
                    WHERE (@sid = 0 OR sp.subscription_id = @sid)
                    ORDER BY sp.due_date ASC LIMIT 500", conn);
                cmd.Parameters.AddWithValue("@sid", subId);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    Periods.Add(new SubscriptionPeriodRow
                    {
                        Id = r.GetInt32(0),
                        SubscriptionId = r.GetInt32(1),
                        PeriodNumber = Convert.ToInt32(r.GetValue(2)),
                        DueDate = ToDateTime(r.GetValue(3)),
                        Amount = Convert.ToDecimal(r.GetValue(4)),
                        PaidAmount = Convert.ToDecimal(r.GetValue(5)),
                        Status = r.GetString(6),
                        PartyName = r.GetString(7),
                        ServiceName = r.GetString(8)
                    });
                PeriodsTitle = subId > 0 ? "فترات: " + (SelectedSubscription?.PartyName ?? "") : "كل الفترات";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        // ================= إضافة / تعديل =================
        private void OpenEditor(int id)
        {
            var win = new Views.SubscriptionEditWindow(id, this);
            win.ShowDialog();
            _ = LoadAsync();
        }

        private void TogglePause()
        {
            if (SelectedSubscription == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                var newStatus = SelectedSubscription.Status == "paused" ? "active" : "paused";
                using var cmd = new NpgsqlCommand("UPDATE subscriptions SET status=@s WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("s", newStatus);
                cmd.Parameters.AddWithValue("id", SelectedSubscription.Id);
                cmd.ExecuteNonQuery();
                StatusMessage = newStatus == "paused" ? "تم إيقاف الاشتراك مؤقتًا" : "تم تفعيل الاشتراك";
                _ = LoadAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void DeleteSubscription()
        {
            if (SelectedSubscription == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var chk = new NpgsqlCommand("SELECT COUNT(*) FROM subscription_periods WHERE subscription_id=@id AND (paid_amount > 0 OR status='paid')", conn);
                chk.Parameters.AddWithValue("id", SelectedSubscription.Id);
                if (Convert.ToInt64(chk.ExecuteScalar()) > 0)
                {
                    StatusMessage = "لا يمكن الحذف: توجد فترات مسددة — استخدم الإيقاف بدلًا من الحذف.";
                    return;
                }
                if (System.Windows.MessageBox.Show("حذف اشتراك '" + SelectedSubscription.ServiceName + "'؟", "تأكيد", System.Windows.MessageBoxButton.YesNo) != System.Windows.MessageBoxResult.Yes) return;
                using var tx = conn.BeginTransaction();
                using (var d1 = new NpgsqlCommand("DELETE FROM subscription_periods WHERE subscription_id=@id", conn, tx)) { d1.Parameters.AddWithValue("id", SelectedSubscription.Id); d1.ExecuteNonQuery(); }
                using (var d2 = new NpgsqlCommand("DELETE FROM subscriptions WHERE id=@id", conn, tx)) { d2.Parameters.AddWithValue("id", SelectedSubscription.Id); d2.ExecuteNonQuery(); }
                tx.Commit();
                AuditService.Log("DELETE", "Subscription", SelectedSubscription.Id, "Deleted subscription: " + SelectedSubscription.ServiceName);
                StatusMessage = "تم حذف الاشتراك";
                _ = LoadAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        // ================= سداد فترة (إنشاء سند دفع) =================
        public void PayPeriod()
        {
            if (SelectedPeriod == null) return;
            if (SelectedPeriod.Remaining <= 0) { StatusMessage = "هذه الفترة مسددة بالكامل."; return; }
            var win = new Views.SubscriptionPaymentWindow(SelectedPeriod);
            if (win.ShowDialog() == true)
            {
                StatusMessage = "تم تسجيل السداد";
                _ = LoadAsync();
            }
        }
    }
}
