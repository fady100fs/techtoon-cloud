using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>سطر عرض في قائمة القيود اليدوية.</summary>
    public class JournalEntryRow : ViewModelBase
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string ReferenceType { get; set; } = "";
        public int LineCount { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }

        public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.005m;
        public string DateDisplay => Date == default ? "" : Date.ToString("dd/MM/yyyy");
        public string TotalDebitDisplay => TotalDebit.ToString("N2");
        public string TotalCreditDisplay => TotalCredit.ToString("N2");
        public string BalancedDisplay => IsBalanced ? "متوازن" : "غير متوازن";
        public string ReferenceTypeDisplay => ReferenceType switch
        {
            "manual" => "يدوي",
            "invoice" => "فاتورة",
            "payment" => "سند",
            "" => "-",
            _ => ReferenceType
        };
    }

    /// <summary>سطر في ميزان المراجعة (حساب واحد).</summary>
    public class TrialBalanceRow
    {
        public int AccountId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        public decimal Balance => Debit - Credit;
        public string DebitDisplay => Debit.ToString("N2");
        public string CreditDisplay => Credit.ToString("N2");
        public string BalanceDisplay => Balance.ToString("N2");
        public string TypeDisplay => Type switch
        {
            "ASSET" => "أصول",
            "LIABILITY" => "خصوم",
            "EQUITY" => "حقوق ملكية",
            "REVENUE" => "إيرادات",
            "EXPENSE" => "مصروفات",
            _ => Type
        };
    }

    /// <summary>
    /// GAP B2 — القيود المحاسبية اليدوية + ميزان المراجعة.
    /// كل العمليات تتم على قاعدة البيانات الحقيقية (journal_entries / journal_lines).
    /// </summary>
    public class JournalEntriesViewModel : ViewModelBase
    {
        public ObservableCollection<JournalEntryRow> Entries { get; } = new();
        public ObservableCollection<TrialBalanceRow> TrialBalanceRows { get; } = new();

        private JournalEntryRow _selectedEntry;
        public JournalEntryRow SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetProperty(ref _selectedEntry, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool HasSelection => SelectedEntry != null;

        private DateTime _fromDate = DateTime.Today.AddMonths(-1);
        public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }

        private DateTime _toDate = DateTime.Today;
        public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private decimal _trialTotalDebit;
        public decimal TrialTotalDebit
        {
            get => _trialTotalDebit;
            set { if (SetProperty(ref _trialTotalDebit, value)) { OnPropertyChanged(nameof(TrialTotalDebitDisplay)); OnPropertyChanged(nameof(IsTrialBalanced)); OnPropertyChanged(nameof(TrialBalanceStatusDisplay)); } }
        }

        private decimal _trialTotalCredit;
        public decimal TrialTotalCredit
        {
            get => _trialTotalCredit;
            set { if (SetProperty(ref _trialTotalCredit, value)) { OnPropertyChanged(nameof(TrialTotalCreditDisplay)); OnPropertyChanged(nameof(IsTrialBalanced)); OnPropertyChanged(nameof(TrialBalanceStatusDisplay)); } }
        }

        public string TrialTotalDebitDisplay => TrialTotalDebit.ToString("N2");
        public string TrialTotalCreditDisplay => TrialTotalCredit.ToString("N2");
        public bool IsTrialBalanced => Math.Abs(TrialTotalDebit - TrialTotalCredit) < 0.005m;
        public string TrialBalanceStatusDisplay => IsTrialBalanced ? "القيد متوازن" : "القيد غير متوازن";

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand ViewCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand RunTrialBalanceCommand { get; }

        public JournalEntriesViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => LoadEntries());
            AddCommand = new UiRelayCommand(_ => OpenEditor(null, false));
            EditCommand = new UiRelayCommand(_ => OpenEditor(SelectedEntry?.Id, false), _ => SelectedEntry != null);
            ViewCommand = new UiRelayCommand(_ => OpenEditor(SelectedEntry?.Id, true), _ => SelectedEntry != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteEntry(), _ => SelectedEntry != null);
            RunTrialBalanceCommand = new UiRelayCommand(_ => LoadTrialBalance());
            LoadEntries();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        private static decimal ToDecimal(object? value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            if (value is decimal d) return d;
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>تحميل قائمة القيود مع عدد البنود ومجموع المدين/الدائن.</summary>
        public void LoadEntries()
        {
            Entries.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT je.id,
                                     je.date,
                                     COALESCE(je.description, ''),
                                     COALESCE(je.reference_type, ''),
                                     COUNT(jl.id),
                                     COALESCE(SUM(jl.debit), 0),
                                     COALESCE(SUM(jl.credit), 0)
                              FROM journal_entries je
                              LEFT JOIN journal_lines jl ON jl.entry_id = je.id
                              GROUP BY je.id, je.date, je.description, je.reference_type
                              ORDER BY je.date DESC, je.id DESC
                              LIMIT 1000";
                using var cmd = new NpgsqlCommand(sql, conn);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Entries.Add(new JournalEntryRow
                    {
                        Id = r.GetInt32(0),
                        Date = DbValue.AsDateTime(r.GetValue(1)),
                        Description = r.IsDBNull(2) ? "" : r.GetString(2),
                        ReferenceType = r.IsDBNull(3) ? "" : r.GetString(3),
                        LineCount = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4)),
                        TotalDebit = ToDecimal(r.GetValue(5)),
                        TotalCredit = ToDecimal(r.GetValue(6))
                    });
                }
                StatusMessage = "تم تحميل " + Entries.Count + " قيد محاسبي";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditor(int? entryId, bool readOnly)
        {
            try
            {
                var win = new Views.JournalEntryEditWindow(entryId, readOnly);
                win.ShowDialog();
                LoadEntries();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>حذف القيد وكل بنوده داخل معاملة واحدة (transaction).</summary>
        private void DeleteEntry()
        {
            if (SelectedEntry == null) return;
            int id = SelectedEntry.Id;
            var confirm = MessageBox.Show(
                "تأكيد حذف القيد رقم " + id + " وجميع بنوده؟",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                using (var delLines = new NpgsqlCommand("DELETE FROM journal_lines WHERE entry_id = @id", conn, tx))
                {
                    delLines.Parameters.AddWithValue("@id", id);
                    delLines.ExecuteNonQuery();
                }
                using (var delEntry = new NpgsqlCommand("DELETE FROM journal_entries WHERE id = @id", conn, tx))
                {
                    delEntry.Parameters.AddWithValue("@id", id);
                    delEntry.ExecuteNonQuery();
                }
                tx.Commit();
                AuditService.Log("DELETE", "JournalEntry", id, "Deleted manual journal entry and its lines");
                StatusMessage = "تم حذف القيد رقم " + id;
                LoadEntries();
            }
            catch (Npgsql.PostgresException px)
            {
                MessageBox.Show("خطأ في قاعدة البيانات: " + px.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>ميزان المراجعة: مجموع (مدين - دائن) لكل حساب عن الفترة المحددة.</summary>
        public void LoadTrialBalance()
        {
            TrialBalanceRows.Clear();
            TrialTotalDebit = 0m;
            TrialTotalCredit = 0m;

            if (FromDate.Date > ToDate.Date)
            {
                StatusMessage = "تاريخ البداية يجب أن يكون قبل تاريخ النهاية.";
                return;
            }

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT a.id,
                                     COALESCE(a.code, ''),
                                     COALESCE(a.name, ''),
                                     COALESCE(a.type, ''),
                                     COALESCE(SUM(jl.debit), 0),
                                     COALESCE(SUM(jl.credit), 0)
                              FROM journal_lines jl
                              JOIN journal_entries je ON je.id = jl.entry_id
                              JOIN accounts a ON a.id = jl.account_id
                              WHERE je.date >= CAST(@from AS timestamp)
                                AND je.date < CAST(@to AS timestamp) + INTERVAL '1 day'
                              GROUP BY a.id, a.code, a.name, a.type
                              ORDER BY a.code, a.name";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@from", FromDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@to", ToDate.ToString("yyyy-MM-dd"));

                decimal totalDebit = 0m, totalCredit = 0m;
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var debit = ToDecimal(r.GetValue(4));
                        var credit = ToDecimal(r.GetValue(5));
                        totalDebit += debit;
                        totalCredit += credit;
                        TrialBalanceRows.Add(new TrialBalanceRow
                        {
                            AccountId = r.GetInt32(0),
                            Code = r.IsDBNull(1) ? "" : r.GetString(1),
                            Name = r.IsDBNull(2) ? "" : r.GetString(2),
                            Type = r.IsDBNull(3) ? "" : r.GetString(3),
                            Debit = debit,
                            Credit = credit
                        });
                    }
                }
                TrialTotalDebit = totalDebit;
                TrialTotalCredit = totalCredit;
                StatusMessage = "تم تحميل ميزان المراجعة — " + TrialBalanceRows.Count + " حساب";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }
    }
}
