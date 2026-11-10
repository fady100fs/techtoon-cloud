using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>أسماء الشهور بالعربية (تُستخدم في القوائم والتقارير).</summary>
    public static class BudgetMonths
    {
        public static readonly string[] Names =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        public static string Name(int month) => (month >= 1 && month <= 12) ? Names[month - 1] : month.ToString();
    }

    /// <summary>سطر ميزانية في جدول budgets.</summary>
    public class BudgetRow : ViewModelBase
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal BudgetedAmount { get; set; }
        public string Notes { get; set; } = "";

        public string MonthName => BudgetMonths.Name(Month);
        public string PeriodDisplay => MonthName + " " + Year;
        public string AmountDisplay => BudgetedAmount.ToString("N2");
        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : AccountCode + " - " + AccountName;
    }

    /// <summary>سطر في تقرير مقارنة الميزانية بالفعلي (budget مقابل actual).</summary>
    public class BudgetReportRow : ViewModelBase
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public decimal BudgetedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Difference => BudgetedAmount - ActualAmount;

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : AccountCode + " - " + AccountName;
        public string BudgetedDisplay => BudgetedAmount.ToString("N2");
        public string ActualDisplay => ActualAmount.ToString("N2");
        public string DifferenceDisplay => Difference.ToString("N2");
        public string ResultDisplay => Difference < 0 ? "تجاوز الميزانية" : "ضمن الميزانية";
    }

    /// <summary>
    /// B1 — إدارة الميزانيات (budgets): إضافة/تعديل/حذف + تقرير مقارنة الميزانية بالإنفاق الفعلي
    /// المحسوب من جدول expenses عبر expense_categories.account_id.
    /// </summary>
    public class BudgetsViewModel : ViewModelBase
    {
        public ObservableCollection<BudgetRow> Budgets { get; } = new();
        public ObservableCollection<BudgetReportRow> ReportRows { get; } = new();
        public ObservableCollection<int> Months { get; } = new();
        public ObservableCollection<int> Years { get; } = new();

        private BudgetRow? _selectedItem;
        public BudgetRow? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }

        private string _searchText = "";
        public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) LoadBudgets(); } }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        private int _selectedMonth;
        public int SelectedMonth
        {
            get => _selectedMonth;
            set { if (SetProperty(ref _selectedMonth, value) && !_suppressReport) LoadReport(); }
        }

        private int _selectedYear;
        public int SelectedYear
        {
            get => _selectedYear;
            set { if (SetProperty(ref _selectedYear, value) && !_suppressReport) LoadReport(); }
        }

        private string _reportSummary = "";
        public string ReportSummary { get => _reportSummary; set => SetProperty(ref _reportSummary, value); }

        private bool _suppressReport;

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ReportCommand { get; }

        public BudgetsViewModel()
        {
            for (int m = 1; m <= 12; m++) Months.Add(m);
            int currentYear = DateTime.Now.Year;
            for (int y = currentYear + 1; y >= currentYear - 6; y--) Years.Add(y);

            RefreshCommand = new UiRelayCommand(_ => { LoadBudgets(); LoadReport(); });
            AddCommand = new UiRelayCommand(_ => OpenEditWindow(0));
            EditCommand = new UiRelayCommand(_ => OpenEditWindow(SelectedItem?.Id ?? 0), _ => SelectedItem != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteBudget(), _ => SelectedItem != null);
            ReportCommand = new UiRelayCommand(_ => LoadReport());

            _suppressReport = true;
            SelectedMonth = DateTime.Now.Month;
            SelectedYear = DateTime.Now.Year;
            _suppressReport = false;

            LoadBudgets();
            LoadReport();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        /// <summary>قائمة الميزانيات مع اسم الحساب وكوده.</summary>
        public void LoadBudgets()
        {
            int keepId = SelectedItem?.Id ?? 0;
            Budgets.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT b.id, b.account_id, COALESCE(a.code,''), COALESCE(a.name,''),
                                      b.month, b.year, b.budgeted_amount, COALESCE(b.notes,'')
                               FROM budgets b
                               JOIN accounts a ON a.id = b.account_id
                               WHERE 1=1";
                if (!string.IsNullOrWhiteSpace(SearchText))
                    sql += " AND (a.name ILIKE @q OR a.code ILIKE @q OR COALESCE(b.notes,'') ILIKE @q)";
                sql += " ORDER BY b.year DESC, b.month DESC, a.code LIMIT 1000";

                using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(SearchText))
                    cmd.Parameters.AddWithValue("q", "%" + SearchText + "%");

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    Budgets.Add(new BudgetRow
                    {
                        Id = BudgetAssetsDb.ToInt(r.GetValue(0)),
                        AccountId = BudgetAssetsDb.ToInt(r.GetValue(1)),
                        AccountCode = BudgetAssetsDb.ToText(r.GetValue(2)),
                        AccountName = BudgetAssetsDb.ToText(r.GetValue(3)),
                        Month = BudgetAssetsDb.ToInt(r.GetValue(4)),
                        Year = BudgetAssetsDb.ToInt(r.GetValue(5)),
                        BudgetedAmount = BudgetAssetsDb.ToDecimal(r.GetValue(6)),
                        Notes = BudgetAssetsDb.ToText(r.GetValue(7))
                    });
                }
                StatusMessage = "تم تحميل " + Budgets.Count + " ميزانية";
                if (keepId > 0)
                {
                    var keep = Budgets.FirstOrDefault(b => b.Id == keepId);
                    if (keep != null) SelectedItem = keep;
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>
        /// تقرير الميزانية مقابل الإنفاق الفعلي — SQL حقيقي:
        /// SUM(expenses.amount) مجمّعًا حسب expense_categories.account_id لنفس الشهر/السنة.
        /// </summary>
        public void LoadReport()
        {
            ReportRows.Clear();
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                string sql = @"SELECT b.account_id, COALESCE(a.code,''), COALESCE(a.name,''),
                                      b.budgeted_amount, COALESCE(x.actual, 0)
                               FROM budgets b
                               JOIN accounts a ON a.id = b.account_id
                               LEFT JOIN (
                                   SELECT ec.account_id AS account_id, SUM(ex.amount) AS actual
                                   FROM expenses ex
                                   JOIN expense_categories ec ON ec.id = ex.category_id
                                   WHERE EXTRACT(MONTH FROM ex.date) = @m
                                     AND EXTRACT(YEAR FROM ex.date) = @y
                                   GROUP BY ec.account_id
                               ) x ON x.account_id = b.account_id
                               WHERE b.month = @m AND b.year = @y
                               ORDER BY a.code";

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("m", SelectedMonth);
                cmd.Parameters.AddWithValue("y", SelectedYear);

                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        ReportRows.Add(new BudgetReportRow
                        {
                            AccountId = BudgetAssetsDb.ToInt(r.GetValue(0)),
                            AccountCode = BudgetAssetsDb.ToText(r.GetValue(1)),
                            AccountName = BudgetAssetsDb.ToText(r.GetValue(2)),
                            BudgetedAmount = BudgetAssetsDb.ToDecimal(r.GetValue(3)),
                            ActualAmount = BudgetAssetsDb.ToDecimal(r.GetValue(4))
                        });
                    }
                }

                decimal totalBudget = ReportRows.Sum(r => r.BudgetedAmount);
                decimal totalActual = ReportRows.Sum(r => r.ActualAmount);
                ReportSummary = BudgetMonths.Name(SelectedMonth) + " " + SelectedYear
                    + "  |  إجمالي الميزانية: " + totalBudget.ToString("N2")
                    + "  |  إجمالي الفعلي: " + totalActual.ToString("N2")
                    + "  |  الفرق: " + (totalBudget - totalActual).ToString("N2");
            }
            catch (Exception ex) { StatusMessage = "خطأ في تقرير المقارنة: " + ex.Message; }
        }

        private void OpenEditWindow(int id)
        {
            try
            {
                var win = new Views.BudgetEditWindow(id);
                if (win.ShowDialog() == true)
                {
                    StatusMessage = id > 0 ? "تم تعديل الميزانية" : "تمت إضافة الميزانية";
                    LoadBudgets();
                    LoadReport();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ: " + ex.Message, "خطأ",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DeleteBudget()
        {
            var row = SelectedItem;
            if (row == null) return;
            if (System.Windows.MessageBox.Show(
                    "تأكيد حذف ميزانية الحساب (" + row.AccountDisplay + ") عن " + row.PeriodDisplay + "؟",
                    "تأكيد", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning)
                != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("DELETE FROM budgets WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("id", row.Id);
                cmd.ExecuteNonQuery();

                AuditService.Log("DELETE", "Budget", row.Id,
                    "Budget deleted: account=" + row.AccountDisplay + ", period=" + row.PeriodDisplay +
                    ", amount=" + row.BudgetedAmount.ToString("N2"));

                StatusMessage = "تم حذف الميزانية";
                LoadBudgets();
                LoadReport();
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
