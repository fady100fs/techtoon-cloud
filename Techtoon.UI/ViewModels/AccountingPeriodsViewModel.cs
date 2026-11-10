using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>سطر فترة محاسبية.</summary>
    public class AccountingPeriodRow : ViewModelBase
    {
        public int Id { get; set; }
        public string PeriodName { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsFiscalYear { get; set; }
        public string Status { get; set; } = "";
        public DateTime? ClosingDate { get; set; }
        public string ClosingNotes { get; set; } = "";

        public string StartDateDisplay => StartDate == DateTime.MinValue ? "" : StartDate.ToString("yyyy-MM-dd");
        public string EndDateDisplay => EndDate == DateTime.MinValue ? "" : EndDate.ToString("yyyy-MM-dd");
        public string FiscalYearDisplay => IsFiscalYear ? "نعم" : "لا";
        public string StatusDisplay => AccountingPeriodStatus.Display(Status);
        public string ClosingDateDisplay => ClosingDate.HasValue ? ClosingDate.Value.ToString("yyyy-MM-dd HH:mm") : "";
        public bool IsClosed => AccountingPeriodStatus.IsClosed(Status);
    }

    /// <summary>
    /// A4: الفترات المحاسبية — إنشاء/تعديل/إغلاق/حذف + قفل الترحيل في الفترات المغلقة.
    /// </summary>
    public class AccountingPeriodsViewModel : ViewModelBase
    {
        public ObservableCollection<AccountingPeriodRow> Periods { get; } = new();

        private AccountingPeriodRow _selectedItem;
        public AccountingPeriodRow SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { if (SetProperty(ref _searchText, value)) LoadPeriods(); }
        }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand DeleteCommand { get; }

        public AccountingPeriodsViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => LoadPeriods());
            AddCommand = new UiRelayCommand(_ => OpenEditWindow(0));
            EditCommand = new UiRelayCommand(_ => OpenEditWindow(SelectedItem?.Id ?? 0), _ => SelectedItem != null);
            CloseCommand = new UiRelayCommand(_ => ClosePeriod(), _ => SelectedItem != null && !SelectedItem.IsClosed);
            DeleteCommand = new UiRelayCommand(_ => DeletePeriod(), _ => SelectedItem != null);
            LoadPeriods();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public void LoadPeriods()
        {
            int keepId = SelectedItem?.Id ?? 0;
            Periods.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT id, period_name, start_date, end_date,
                                     COALESCE(is_fiscal_year, FALSE), status,
                                     closing_date, COALESCE(closing_notes,'')
                              FROM accounting_periods";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " WHERE period_name ILIKE @q";
                sql += " ORDER BY start_date DESC, id DESC LIMIT 500";

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");

                using var r = cmd.ExecuteReader();
                while (r.Read())
                    Periods.Add(new AccountingPeriodRow
                    {
                        Id = r.GetInt32(0),
                        PeriodName = r.IsDBNull(1) ? "" : r.GetString(1),
                        StartDate = DbValue.AsDateTime(r.GetValue(2)),
                        EndDate = DbValue.AsDateTime(r.GetValue(3)),
                        IsFiscalYear = r.IsDBNull(4) ? false : r.GetBoolean(4),
                        Status = r.IsDBNull(5) ? "" : r.GetString(5),
                        ClosingDate = r.IsDBNull(6) ? (DateTime?)null : DbValue.AsDateTime(r.GetValue(6)),
                        ClosingNotes = r.IsDBNull(7) ? "" : r.GetString(7)
                    });

                StatusMessage = "تم التحميل: " + Periods.Count + " فترة";
                if (keepId > 0)
                {
                    var keep = Periods.FirstOrDefault(p => p.Id == keepId);
                    if (keep != null) SelectedItem = keep;
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.AccountingPeriodEditWindow(id);
                if (win.ShowDialog() == true) LoadPeriods();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>إغلاق الفترة: status='closed' + closing_date + closing_notes + closed_by + سجل تدقيق.</summary>
        private void ClosePeriod()
        {
            if (SelectedItem == null) return;
            if (SelectedItem.IsClosed)
            {
                System.Windows.MessageBox.Show("الفترة '" + SelectedItem.PeriodName + "' مغلقة بالفعل.", "معلومة",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }
            try
            {
                var win = new Views.AccountingPeriodCloseWindow(SelectedItem.Id, SelectedItem.PeriodName);
                if (win.ShowDialog() == true) LoadPeriods();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>الحذف مسموح فقط إذا كانت الفترة ما زالت مفتوحة.</summary>
        private void DeletePeriod()
        {
            if (SelectedItem == null) return;
            if (SelectedItem.IsClosed)
            {
                System.Windows.MessageBox.Show(
                    "لا يمكن حذف الفترة '" + SelectedItem.PeriodName + "' لأنها مغلقة.\nالفترة المغلقة يجب أن تبقى كسجل للأرشفة.",
                    "ممنوع", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            if (System.Windows.MessageBox.Show("تأكيد حذف الفترة '" + SelectedItem.PeriodName + "'؟",
                    "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();

                // إعادة قراءة الحالة داخل معاملة حتى لا يُحذف ما أُغلق في اللحظة الأخيرة
                string status;
                using (var chk = new NpgsqlCommand("SELECT status FROM accounting_periods WHERE id=@id FOR UPDATE", conn, tx))
                {
                    chk.Parameters.AddWithValue("id", SelectedItem.Id);
                    var o = chk.ExecuteScalar();
                    if (o == null || o == DBNull.Value)
                    {
                        tx.Rollback();
                        StatusMessage = "الفترة غير موجودة";
                        LoadPeriods();
                        return;
                    }
                    status = o.ToString();
                }

                if (AccountingPeriodStatus.IsClosed(status))
                {
                    tx.Rollback();
                    System.Windows.MessageBox.Show("لا يمكن الحذف: الفترة '" + SelectedItem.PeriodName + "' مغلقة الآن.",
                        "ممنوع", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    LoadPeriods();
                    return;
                }

                using (var cmd = new NpgsqlCommand("DELETE FROM accounting_periods WHERE id = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("id", SelectedItem.Id);
                    cmd.ExecuteNonQuery();
                }
                tx.Commit();

                AuditService.Log("DELETE", "AccountingPeriod", SelectedItem.Id, "حذف فترة محاسبية: " + SelectedItem.PeriodName);
                StatusMessage = "تم الحذف بنجاح";
                LoadPeriods();
            }
            catch (Npgsql.PostgresException px)
            {
                System.Windows.MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
