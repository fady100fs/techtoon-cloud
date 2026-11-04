using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Techtoon.UI.Services
{
    public class SalesReportRow
    {
        public DateTime Date { get; set; }
        public string InvoiceNumber { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }

    public class InventoryReportRow
    {
        public string ItemName { get; set; }
        public string Barcode { get; set; }
        public decimal StockQty { get; set; }
        public decimal AvgCost { get; set; }
        public decimal SellPrice { get; set; }
        public decimal StockValue { get; set; }
    }

    public class QuestPdfReportService
    {
        private readonly string _connectionString;

        public QuestPdfReportService(string connectionString)
        {
            _connectionString = connectionString;
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
        }

        public string ExportSalesReport(DateTime fromDate, DateTime toDate, string outputPath)
        {
            var rows = LoadSalesData(fromDate, toDate);
            var total = 0m;
            var totalPaid = 0m;
            foreach (var r in rows) { total += r.Total; totalPaid += r.Paid; }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Tahoma"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Techtoon Accounting").FontSize(18).Bold();
                        col.Item().Text("تقرير المبيعات").FontSize(14).Bold();
                        col.Item().Text($"الفترة: {fromDate:yyyy-MM-dd} إلى {toDate:yyyy-MM-dd}").FontSize(10);
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("التاريخ");
                                header.Cell().Element(CellStyle).Text("رقم الفاتورة");
                                header.Cell().Element(CellStyle).Text("العميل");
                                header.Cell().Element(CellStyle).AlignRight().Text("الإجمالي");
                                header.Cell().Element(CellStyle).AlignRight().Text("المدفوع");
                                header.Cell().Element(CellStyle).AlignRight().Text("المتبقي");
                            });
                            foreach (var r in rows)
                            {
                                table.Cell().Element(CellStyle).Text(r.Date.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text(r.InvoiceNumber ?? "");
                                table.Cell().Element(CellStyle).Text(r.PartyName ?? "");
                                table.Cell().Element(CellStyle).AlignRight().Text(r.Total.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(r.Paid.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(r.Remaining.ToString("0.00"));
                            }
                            static IContainer CellStyle(IContainer c) => c
                                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(4);
                        });

                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Text($"إجمالي المبيعات: {total:0.00}").Bold();
                            row.RelativeItem().Text($"إجمالي المحصل: {totalPaid:0.00}").Bold();
                            row.RelativeItem().Text($"المتبقي: {(total - totalPaid):0.00}").Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("صفحة ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span($"  |  أنشئ في: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        public string ExportInventoryReport(string outputPath)
        {
            var rows = LoadInventoryData();
            var totalValue = 0m;
            foreach (var r in rows) totalValue += r.StockValue;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Tahoma"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Techtoon Accounting").FontSize(18).Bold();
                        col.Item().Text("تقرير الجرد").FontSize(14).Bold();
                        col.Item().Text($"بتاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10);
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2.5f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1.2f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("الصنف");
                                header.Cell().Element(CellStyle).Text("الباركود");
                                header.Cell().Element(CellStyle).AlignRight().Text("الكمية");
                                header.Cell().Element(CellStyle).AlignRight().Text("متوسط التكلفة");
                                header.Cell().Element(CellStyle).AlignRight().Text("سعر البيع");
                                header.Cell().Element(CellStyle).AlignRight().Text("قيمة المخزون");
                            });
                            foreach (var r in rows)
                            {
                                table.Cell().Element(CellStyle).Text(r.ItemName ?? "");
                                table.Cell().Element(CellStyle).Text(r.Barcode ?? "");
                                table.Cell().Element(CellStyle).AlignRight().Text(r.StockQty.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(r.AvgCost.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(r.SellPrice.ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(r.StockValue.ToString("0.00"));
                            }
                            static IContainer CellStyle(IContainer c) => c
                                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(4);
                        });

                        col.Item().PaddingTop(10).Text($"إجمالي قيمة المخزون: {totalValue:0.00}").Bold().FontSize(12);
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("صفحة ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span($"  |  أنشئ في: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        private List<SalesReportRow> LoadSalesData(DateTime from, DateTime to)
        {
            var rows = new List<SalesReportRow>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT i.date, i.invoice_number, p.name, i.total, i.paid_amount " +
                "FROM invoices i LEFT JOIN parties p ON i.party_id = p.id " +
                "WHERE i.date BETWEEN @from AND @to " +
                "AND i.status <> 'cancelled' " +
                "ORDER BY i.date", conn);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to.AddDays(1));
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var total = reader.IsDBNull(3) ? 0m : reader.GetDecimal(3);
                var paid = reader.IsDBNull(4) ? 0m : reader.GetDecimal(4);
                rows.Add(new SalesReportRow
                {
                    Date = reader.GetDateTime(0),
                    InvoiceNumber = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    PartyName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Total = total,
                    Paid = paid,
                    Remaining = total - paid
                });
            }
            return rows;
        }

        private List<InventoryReportRow> LoadInventoryData()
        {
            var rows = new List<InventoryReportRow>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "SELECT name, barcode, opening_quantity, avg_cost_price, sell_price " +
                "FROM items WHERE is_active = TRUE ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var qty = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
                var cost = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3);
                var sell = reader.IsDBNull(4) ? 0m : (decimal)reader.GetDouble(4);
                rows.Add(new InventoryReportRow
                {
                    ItemName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                    Barcode = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    StockQty = qty,
                    AvgCost = cost,
                    SellPrice = sell,
                    StockValue = qty * cost
                });
            }
            return rows;
        }
    }
}



