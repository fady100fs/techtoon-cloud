using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class DashboardMetric : ViewModelBase
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string Subtitle { get; set; }
    }

    public class SalesChartData : ViewModelBase
    {
        public string Label { get; set; }
        public decimal Sales { get; set; }
        public decimal Purchases { get; set; }
        public double BarWidth { get; set; }
    }

    public class TopItemData : ViewModelBase
    {
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
    }

    public class OverdueInvoiceData : ViewModelBase
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }
    public string PartyName { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining { get { return Total - Paid; } }
    public int DaysOverdue { get; set; }
    public string DueDateDisplay { get { return DueDate.ToString("yyyy-MM-dd"); } }
    public string TotalDisplay { get { return Total.ToString("N2"); } }
    public string PaidDisplay { get { return Paid.ToString("N2"); } }
    public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    public string DaysOverdueDisplay { get { return DaysOverdue + " يوم"; } }
}

 public class DashboardViewModel : ViewModelBase
    {
        public ObservableCollection<DashboardMetric> Metrics { get; set; }
        public ObservableCollection<SalesChartData> DailySales { get; set; }
        public ObservableCollection<SalesChartData> MonthlySales { get; set; }
        public ObservableCollection<TopItemData> TopItems { get; set; }
         public ObservableCollection<TopItemData> LowStockItems { get; set; }
        public ObservableCollection<OverdueInvoiceData> OverdueInvoices { get; set; }
        private int _overdueCount = 0;
        public int OverdueCount { get { return _overdueCount; } set { _overdueCount = value; OnPropertyChanged("OverdueCount"); OnPropertyChanged("HasOverdueAlert"); OnPropertyChanged("OverdueAlertMessage"); } }
        private decimal _overdueAmount = 0;
        public decimal OverdueAmount { get { return _overdueAmount; } set { _overdueAmount = value; OnPropertyChanged("OverdueAmount"); OnPropertyChanged("OverdueAlertMessage"); } }
        public bool HasOverdueAlert { get { return _overdueCount > 0; } }
        public string OverdueAlertMessage { get { if (_overdueCount == 0) return ""; return "🔔 تنبيه: " + _overdueCount + " فاتورة متأخرة بقيمة " + _overdueAmount.ToString("N2"); } }

        private static void LogErr(string section, Exception ex)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"DASHBOARD ERROR in {section}: {ex.Message}");
                System.Windows.MessageBox.Show($"خطأ في {section}: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            } catch { }
        }

        private decimal _maxMonthlySales = 1;
        public decimal MaxMonthlySales
        {
            get { return _maxMonthlySales; }
            set { _maxMonthlySales = value; OnPropertyChanged("MaxMonthlySales"); }
        }

        private string _statusMessage = "";
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }

        private int _lowStockCount = 0;
        public int LowStockCount { get { return _lowStockCount; } set { _lowStockCount = value; OnPropertyChanged("LowStockCount"); OnPropertyChanged("HasLowStockAlert"); OnPropertyChanged("LowStockAlertMessage"); } }

        public bool HasLowStockAlert { get { return _lowStockCount > 0; } }
        public string LowStockAlertMessage
        {
            get
            {
                if (_lowStockCount == 0) return "";
                return "\u26A0\uFE0F \u062A\u0646\u0628\u064A\u0647: " + _lowStockCount + " \u0635\u0646\u0641 \u0628\u062D\u062F \u0627\u0644\u0623\u062F\u0646\u0649 \u0644\u0644\u0645\u062E\u0632\u0648\u0646";
            }
        }

        public ICommand RefreshCommand { get; set; }

        public DashboardViewModel()
        {
            Metrics = new ObservableCollection<DashboardMetric>();
            DailySales = new ObservableCollection<SalesChartData>();
            MonthlySales = new ObservableCollection<SalesChartData>();
            TopItems = new ObservableCollection<TopItemData>();
            LowStockItems = new ObservableCollection<TopItemData>();
            OverdueInvoices = new ObservableCollection<OverdueInvoiceData>();
            RefreshCommand = new UiRelayCommand(_ => LoadDashboard());
            LoadDashboard();
        }

        public static string GetCS()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        }

        public void LoadDashboard()
        {
            Metrics.Clear();
            DailySales.Clear();
            MonthlySales.Clear();
            TopItems.Clear();
            LowStockItems.Clear();
            OverdueInvoices.Clear();
            OverdueCount = 0;
            OverdueAmount = 0;
            LowStockCount = 0;

            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();

                    // 1) مبيعات اليوم
                    decimal todaySales = 0;
                    int todayCount = 0;
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT COALESCE(SUM(total),0), COUNT(*) 
                        FROM invoices WHERE type='sales' AND DATE(date) = CURRENT_DATE", conn))
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) { todaySales = Convert.ToDecimal(r.GetValue(0)); todayCount = r.GetInt32(1); }
                    
                    Metrics.Add(new DashboardMetric { Title = "\u0645\u0628\u064A\u0639\u0627\u062A \u0627\u0644\u064A\u0648\u0645", Value = todaySales.ToString("N2"), Icon = "\uD83D\uDCB0", Color = "#00897B", Subtitle = todayCount + " \u0641\u0627\u062A\u0648\u0631\u0629" });

                    // 2) التحصيلات
                    decimal collections = 0;
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE payment_type='receipt' AND DATE(date)=CURRENT_DATE", conn))
                        collections = Convert.ToDecimal(cmd.ExecuteScalar());
                    Metrics.Add(new DashboardMetric { Title = "\u0627\u0644\u062A\u062D\u0635\u064A\u0644\u0627\u062A", Value = collections.ToString("N2"), Icon = "\uD83D\uDCB5", Color = "#1565C0", Subtitle = "\u0627\u0644\u064A\u0648\u0645" });

                    // 3) الديون المستحقة (عملاء)
                    decimal receivables = 0;
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total-paid_amount),0) FROM invoices WHERE type='sales' AND paid_amount<total", conn))
                        receivables = Convert.ToDecimal(cmd.ExecuteScalar());
                    Metrics.Add(new DashboardMetric { Title = "\u062F\u064A\u0648\u0646 \u0627\u0644\u0639\u0645\u0644\u0627\u0621", Value = receivables.ToString("N2"), Icon = "\uD83D\uDCB3", Color = "#E65100", Subtitle = "\u0645\u0633\u062A\u062D\u0642\u0629" });

                    // 4) المصروفات
                    decimal expenses = 0;
                    using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(amount),0) FROM expenses WHERE DATE(date)=CURRENT_DATE", conn))
                        expenses = Convert.ToDecimal(cmd.ExecuteScalar());
                    Metrics.Add(new DashboardMetric { Title = "\u0627\u0644\u0645\u0635\u0627\u0631\u064A\u0641 \u0627\u0644\u064A\u0648\u0645", Value = expenses.ToString("N2"), Icon = "\uD83D\uDCB8", Color = "#C62828", Subtitle = "\u0645\u0635\u0631\u0648\u0641" });

                    // 5) قيمة المخزون
                    decimal stockValue = 0;
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT COALESCE(SUM(
                            i.opening_quantity * COALESCE(i.avg_cost_price, i.cost_price, 0) +
                            COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id), 0) * COALESCE(i.avg_cost_price, i.cost_price, 0)
                        ), 0)
                        FROM items i WHERE i.is_kit IS NOT TRUE", conn))
                        stockValue = Convert.ToDecimal(cmd.ExecuteScalar());
                    Metrics.Add(new DashboardMetric { Title = "\u0642\u064A\u0645\u0629 \u0627\u0644\u0645\u062E\u0632\u0648\u0646", Value = stockValue.ToString("N2"), Icon = "\uD83D\uDCE6", Color = "#6A1B9A", Subtitle = "\u062A\u0643\u0644\u0641\u0629" });

                    // 6) المبيعات الشهرية (آخر 6 أشهر)
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT TO_CHAR(date, 'YYYY-MM') as month, COALESCE(SUM(total),0)
                        FROM invoices WHERE type='sales' AND date >= CURRENT_DATE - INTERVAL '6 months'
                        GROUP BY month ORDER BY month", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            MonthlySales.Add(new SalesChartData { Label = r.GetString(0), Sales = Convert.ToDecimal(r.GetValue(1)) });
                        // حساب أقصى مبيعات شهرية
                        if (MonthlySales.Count > 0)
                            MaxMonthlySales = MonthlySales.Max(m => m.Sales);
                        if (MaxMonthlySales <= 0) MaxMonthlySales = 1;
                        // حساب عرض الشريط لكل شهر (مسبقًا - آمن في الـ layout)
                        const double maxBarWidth = 400.0;
                        foreach (var item in MonthlySales)
                        {
                            double ratio = (double)(item.Sales / MaxMonthlySales);
                            item.BarWidth = Math.Max(10, ratio * maxBarWidth);
                        }

                    // 7) أفضل 5 أصناف
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT i.name, SUM(il.quantity) as qty, SUM(il.total) as total
                        FROM invoice_lines il
                        JOIN invoices inv ON il.invoice_id = inv.id
                        JOIN items i ON il.item_id = i.id
                        WHERE inv.type='sales' AND inv.date >= CURRENT_DATE - INTERVAL '30 days'
                        GROUP BY i.name ORDER BY total DESC LIMIT 5", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            TopItems.Add(new TopItemData { Name = r.GetString(0), Quantity = Convert.ToDecimal(r.GetValue(1)), Total = Convert.ToDecimal(r.GetValue(2)) });

                    // 8) الأصناف منخفضة المخزون
                    using (var cmd = new NpgsqlCommand(@"
                        SELECT i.name, i.min_stock,
                            i.opening_quantity + COALESCE((
                                SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END)
                                FROM inventory_movements WHERE item_id=i.id
                            ), 0) as stock
                        FROM items i
                        WHERE i.min_stock IS NOT NULL AND i.min_stock > 0 AND i.is_kit IS NOT TRUE
                        ORDER BY stock ASC LIMIT 10", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            decimal stock = Convert.ToDecimal(r.GetValue(2));
                            decimal min = Convert.ToDecimal(r.GetValue(1));
                            if (stock <= min)
                            {
                                LowStockItems.Add(new TopItemData { Name = r.GetString(0), Quantity = stock, Total = min });
                                LowStockCount++;
                            }
                        }

                    // Load overdue invoices
                    try
                    {
                        using (var cmd = new NpgsqlCommand("SELECT i.id, i.invoice_number, COALESCE(p.name, ''), i.due_date, i.total, i.paid_amount FROM invoices i LEFT JOIN parties p ON i.party_id = p.id WHERE i.type = 'sales' AND i.status <> 'cancelled' AND i.due_date IS NOT NULL AND i.due_date < CURRENT_DATE AND i.paid_amount < i.total ORDER BY i.due_date ASC LIMIT 20", conn))
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                var total = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4);
                                var paid = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5);
                                OverdueInvoices.Add(new OverdueInvoiceData {
                                    Id = r.GetInt32(0),
                                    InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1),
                                    PartyName = r.IsDBNull(2) ? "" : r.GetString(2),
                                    DueDate = r.GetDateTime(3),
                                    Total = total, Paid = paid,
                                    DaysOverdue = (int)(DateTime.Now - r.GetDateTime(3)).TotalDays
                                });
                                OverdueCount++;
                                OverdueAmount += total - paid;
                            }
                    }
                    catch (Exception exOv) { System.Diagnostics.Debug.WriteLine("Overdue: " + exOv.Message); }
                    StatusMessage = "\u062A\u0645 \u0627\u0644\u062A\u062D\u0645\u064A\u0644";
                }
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; System.Windows.MessageBox.Show("LOAD DASHBOARD ERROR:\n\n" + ex.ToString(), "Dashboard Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
