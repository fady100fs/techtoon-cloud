using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Npgsql;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    public class SalesReportItem
    {
        public DateTime Date { get; set; }
        public string InvoiceNumber { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }

    public class InventoryReportItem
    {
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public decimal StockQty { get; set; }
        public decimal AvgCost { get; set; }
        public decimal SellPrice { get; set; }
        public decimal StockValue { get; set; }
    }

    public class ReportsViewModel : ViewModelBase
    {
        private readonly string _connectionString =
            "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;" +
            "Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;" +
            "SSL Mode=Require;Trust Server Certificate=true";

        private DateTime _fromDate = DateTime.Today.AddDays(-30);
        private DateTime _toDate = DateTime.Today;
        private string _reportType = "sales";
        private string _periodLabel = "آخر 30 يوم";

        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        public string ReportType
        {
            get => _reportType;
            set => SetProperty(ref _reportType, value);
        }

        public string PeriodLabel
        {
            get => _periodLabel;
            set => SetProperty(ref _periodLabel, value);
        }

        public ObservableCollection<SalesReportItem> SalesData { get; } = new ObservableCollection<SalesReportItem>();
        public ObservableCollection<InventoryReportItem> InventoryData { get; } = new ObservableCollection<InventoryReportItem>();

        public UiRelayCommand SearchCommand { get; }
        public UiRelayCommand TodayCommand { get; }
        public UiRelayCommand WeekCommand { get; }
        public UiRelayCommand MonthCommand { get; }
        public UiRelayCommand QuarterCommand { get; }
        public UiRelayCommand YearCommand { get; }
        public UiRelayCommand ExportPdfCommand { get; }

        public ReportsViewModel()
        {
            SearchCommand = new UiRelayCommand(_ => LoadReport(), null);
            TodayCommand = new UiRelayCommand(_ => SetPeriod("today"), null);
            WeekCommand = new UiRelayCommand(_ => SetPeriod("week"), null);
            MonthCommand = new UiRelayCommand(_ => SetPeriod("month"), null);
            QuarterCommand = new UiRelayCommand(_ => SetPeriod("quarter"), null);
            YearCommand = new UiRelayCommand(_ => SetPeriod("year"), null);
            ExportPdfCommand = new UiRelayCommand(_ => ExportPdf(), null);
            LoadReport();
        }

        private void SetPeriod(string period)
        {
            var today = DateTime.Today;
            switch (period)
            {
                case "today":
                    FromDate = today;
                    ToDate = today;
                    PeriodLabel = "اليوم";
                    break;
                case "week":
                    FromDate = today.AddDays(-7);
                    ToDate = today;
                    PeriodLabel = "آخر أسبوع";
                    break;
                case "month":
                    FromDate = today.AddDays(-30);
                    ToDate = today;
                    PeriodLabel = "آخر 30 يوم";
                    break;
                case "quarter":
                    FromDate = today.AddMonths(-3);
                    ToDate = today;
                    PeriodLabel = "آخر ربع سنة";
                    break;
                case "year":
                    FromDate = today.AddYears(-1);
                    ToDate = today;
                    PeriodLabel = "آخر سنة";
                    break;
            }
            LoadReport();
        }

        public void LoadReport()
        {
            if (ReportType == "sales")
            {
                LoadSalesReport();
            }
            else
            {
                LoadInventoryReport();
            }
        }

        private void LoadSalesReport()
        {
            SalesData.Clear();
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT i.date, i.invoice_number, p.name, i.total, i.paid_amount " +
                    "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " +
                    "WHERE i.date BETWEEN @from AND @to " +
                    "AND i.status <> 'cancelled' ORDER BY i.date", conn))
                {
                    cmd.Parameters.AddWithValue("@from", FromDate);
                    cmd.Parameters.AddWithValue("@to", ToDate.AddDays(1));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var total = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                            var paid = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                            SalesData.Add(new SalesReportItem
                            {
                                Date = reader.GetDateTime(0),
                                InvoiceNumber = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                PartyName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Total = total,
                                Paid = paid,
                                Remaining = total - paid
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ تحميل التقرير: " + ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        private void LoadInventoryReport()
        {
            InventoryData.Clear();
            NpgsqlConnection conn = null;
            try
            {
                conn = new NpgsqlConnection(_connectionString);
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT name, barcode, opening_quantity, avg_cost_price, sell_price " +
                    "FROM items WHERE is_active = TRUE ORDER BY name", conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var qty = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
                            var cost = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3);
                            var sell = reader.IsDBNull(4) ? 0m : (decimal)reader.GetDouble(4);
                            InventoryData.Add(new InventoryReportItem
                            {
                                ItemName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                                Barcode = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                StockQty = qty,
                                AvgCost = cost,
                                SellPrice = sell,
                                StockValue = qty * cost
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ تحميل الجرد: " + ex.Message);
            }
            finally
            {
                if (conn != null) conn.Close();
            }
        }

        private void ExportPdf()
        {
            try
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    FileName = ReportType == "sales"
                        ? $"SalesReport_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf"
                        : $"InventoryReport_{DateTime.Now:yyyyMMdd}.pdf"
                };
                if (dlg.ShowDialog() != true) return;

                var svc = new QuestPdfReportService(_connectionString);
                string path;
                if (ReportType == "sales")
                    path = svc.ExportSalesReport(FromDate, ToDate, dlg.FileName);
                else
                    path = svc.ExportInventoryReport(dlg.FileName);

                MessageBox.Show("تم التصدير بنجاح:\n" + path, "تصدير PDF",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ التصدير: " + ex.Message);
            }
        }
    }
}



