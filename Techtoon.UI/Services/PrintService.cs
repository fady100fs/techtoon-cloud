using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Npgsql;

namespace Techtoon.UI.Services
{
    public static class PrintService
    {
        public static string GetCS() { return Techtoon.UI.Services.SecureConnectionManager.GetConnectionString(); }

        public static void PrintInvoiceThermal58(int invoiceId) { PrintInvoiceThermal(invoiceId, 230, "Thermal 58mm"); }
        public static void PrintInvoiceThermal80(int invoiceId) { PrintInvoiceThermal(invoiceId, 320, "Thermal 80mm"); }

        private static void PrintInvoiceThermal(int invoiceId, double width, string desc)
        {
            var data = LoadInvoiceData(invoiceId);
            if (data == null) return;
            var doc = BuildThermalFlowDoc(data, width);
            PrintFlowDoc(doc, desc + " #" + invoiceId);
        }

                public static void PreviewInvoiceA4(int invoiceId)
        {
            try
            {
                var data = LoadInvoiceData(invoiceId);
                if (data == null) return;
                var doc = BuildA4FlowDoc(data);
                doc.PagePadding = new Thickness(40);

                // FlowDocumentScrollViewer لعرض الفاتورة (أبسط وأخف)
                var viewer = new System.Windows.Controls.FlowDocumentScrollViewer
                {
                    Document = doc,
                    FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
                    FlowDirection = System.Windows.FlowDirection.RightToLeft,
                    IsToolBarVisible = true,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto
                };

                // زر طباعة واضح في الأسفل
                var printBtn = new System.Windows.Controls.Button
                {
                    Content = "🖨️ طباعة",
                    Width = 160,
                    Height = 44,
                    FontSize = 16,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Background = System.Windows.Media.Brushes.DarkGreen,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 10, 12, 10),
                    Padding = new Thickness(20, 0, 20, 0)
                };
                printBtn.Click += (s, e) =>
                {
                    try
                    {
                        var dlg = new System.Windows.Controls.PrintDialog();
                        if (dlg.ShowDialog() == true)
                        {
                            var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)doc).DocumentPaginator;
                            paginator.PageSize = new System.Windows.Size(816, 1056); // A4 at 96 DPI
                            dlg.PrintDocument(paginator, "فاتورة #" + invoiceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show("خطأ الطباعة: " + ex.Message);
                    }
                };

                var closeBtn = new System.Windows.Controls.Button
                {
                    Content = "إغلاق",
                    Width = 120,
                    Height = 44,
                    FontSize = 14,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Background = System.Windows.Media.Brushes.LightGray,
                    Foreground = System.Windows.Media.Brushes.DarkRed,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                closeBtn.Click += (s, e) => {
                    var win = System.Windows.Window.GetWindow(printBtn);
                    if (win != null) win.Close();
                };

                var toolbar = new System.Windows.Controls.StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Background = System.Windows.Media.Brushes.WhiteSmoke
                };
                toolbar.Children.Add(printBtn);
                toolbar.Children.Add(closeBtn);

                // Grid: Row 0 = viewer, Row 1 = toolbar
                var grid = new System.Windows.Controls.Grid();
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition 
                    { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition 
                    { Height = System.Windows.GridLength.Auto });
                System.Windows.Controls.Grid.SetRow(viewer, 0);
                System.Windows.Controls.Grid.SetRow(toolbar, 1);
                grid.Children.Add(viewer);
                grid.Children.Add(toolbar);

                var previewWin = new System.Windows.Window
                {
                    Title = "معاينة الفاتورة",
                    Content = grid,
                    Width = 850,
                    Height = 1100,
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen,
                    FlowDirection = System.Windows.FlowDirection.RightToLeft,
                    FontFamily = new System.Windows.Media.FontFamily("Tahoma"),
                    Background = System.Windows.Media.Brushes.LightGray
                };

                previewWin.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("خطأ المعاينة: " + ex.Message);
            }
        }
public static void PrintInvoiceA4(int invoiceId)
        {
            var data = LoadInvoiceData(invoiceId);
            if (data == null) return;
            var doc = BuildA4FlowDoc(data);
            PrintFlowDoc(doc, "A4 Invoice #" + invoiceId);
        }

        public static void PrintPaymentReceipt(int paymentId, string paymentType)
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT voucher_no, date, party_name, amount, method, notes FROM payments WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", paymentId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return;
                            string title = paymentType == "receipt" ? "\u0633\u0646\u062F \u0642\u0628\u0636" : "\u0633\u0646\u062F \u0635\u0631\u0641";
                            var doc = new FlowDocument
                            {
                                PageWidth = 320, PageHeight = 600,
                                FontFamily = new FontFamily("Tahoma"), FontSize = 11,
                                FlowDirection = FlowDirection.RightToLeft
                            };
                            doc.Blocks.Add(new Paragraph(new Run(title)) { FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) });
                            doc.Blocks.Add(new Paragraph(new Run(new string('-', 30))) { TextAlignment = TextAlignment.Center });
                            doc.Blocks.Add(new Paragraph(new Run("\u0631\u0642\u0645 \u0627\u0644\u0633\u0646\u062F: " + (r.IsDBNull(0) ? "" : r.GetString(0)))) { TextAlignment = TextAlignment.Right });
                            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u062A\u0627\u0631\u064A\u062E: " + (r.IsDBNull(1) ? DateTime.Now : r.GetDateTime(1)).ToString("yyyy-MM-dd HH:mm"))) { TextAlignment = TextAlignment.Right });
                            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0637\u0631\u0641: " + (r.IsDBNull(2) ? "" : r.GetString(2)))) { TextAlignment = TextAlignment.Right });
                            doc.Blocks.Add(new Paragraph(new Run(new string('-', 30))) { TextAlignment = TextAlignment.Center });
                            decimal amt = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3);
                            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0645\u0628\u0644\u063A: " + amt.ToString("N2"))) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
                            doc.Blocks.Add(new Paragraph(new Run("\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639: " + (r.IsDBNull(4) ? "cash" : r.GetString(4)))) { TextAlignment = TextAlignment.Right });
                            doc.Blocks.Add(new Paragraph(new Run(new string('-', 30))) { TextAlignment = TextAlignment.Center });
                            doc.Blocks.Add(new Paragraph(new Run("\u0634\u0643\u0631\u0627\u064B \u0644\u062A\u0639\u0627\u0645\u0644\u0643\u0645 \u0645\u0639\u0646\u0627")) { TextAlignment = TextAlignment.Center });
                            PrintFlowDoc(doc, "Receipt #" + paymentId);
                        }
                    }
                }
            }
            catch { }
        }

        public static void PrintStatement(int partyId, DateTime fromDate, DateTime toDate)
        {
            var doc = new FlowDocument
            {
                PageWidth = 780, PageHeight = 1100,
                FontFamily = new FontFamily("Tahoma"), FontSize = 11,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run("\u0643\u0634\u0641 \u062D\u0633\u0627\u0628")) { FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 15) });
            doc.Blocks.Add(new Paragraph(new Run($"\u0645\u0646: {fromDate:yyyy-MM-dd}  \u0625\u0644\u0649: {toDate:yyyy-MM-dd}")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 15) });
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(250) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.RowGroups.Add(new TableRowGroup());
            var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)) };
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u062A\u0627\u0631\u064A\u062E"));
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u0645\u0631\u062C\u0639"));
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u0628\u064A\u0627\u0646"));
            header.Cells.Add(CreateHeaderCell("\u0645\u062F\u064A\u0646"));
            header.Cells.Add(CreateHeaderCell("\u062F\u0627\u0626\u0646"));
            table.RowGroups[0].Rows.Add(header);
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT invoice_number, date, total FROM invoices WHERE party_id=@pid AND date BETWEEN @from AND @to ORDER BY date", conn))
                    {
                        cmd.Parameters.AddWithValue("pid", partyId);
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                var row = new TableRow();
                                row.Cells.Add(CreateCell(r.GetDateTime(1).ToString("yyyy-MM-dd")));
                                row.Cells.Add(CreateCell(r.IsDBNull(0) ? "" : r.GetString(0)));
                                row.Cells.Add(CreateCell("\u0641\u0627\u062A\u0648\u0631\u0629"));
                                row.Cells.Add(CreateCell(r.IsDBNull(2) ? "0" : ((decimal)r.GetDouble(2)).ToString("N2")));
                                row.Cells.Add(CreateCell(""));
                                table.RowGroups[0].Rows.Add(row);
                            }
                    }
                    using (var cmd = new NpgsqlCommand("SELECT voucher_no, date, amount FROM payments WHERE party_id=@pid AND date BETWEEN @from AND @to ORDER BY date", conn))
                    {
                        cmd.Parameters.AddWithValue("pid", partyId);
                        cmd.Parameters.AddWithValue("from", fromDate);
                        cmd.Parameters.AddWithValue("to", toDate);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                var row = new TableRow();
                                row.Cells.Add(CreateCell(r.GetDateTime(1).ToString("yyyy-MM-dd")));
                                row.Cells.Add(CreateCell(r.IsDBNull(0) ? "" : r.GetString(0)));
                                row.Cells.Add(CreateCell("\u062F\u0641\u0639\u0629"));
                                row.Cells.Add(CreateCell(""));
                                row.Cells.Add(CreateCell(r.IsDBNull(2) ? "0" : ((decimal)r.GetDouble(2)).ToString("N2")));
                                table.RowGroups[0].Rows.Add(row);
                            }
                    }
                }
            }
            catch { }
            doc.Blocks.Add(table);
            PrintFlowDoc(doc, "Statement");
        }

        public static void PrintInventoryReport()
        {
            var doc = new FlowDocument
            {
                PageWidth = 780, PageHeight = 1100,
                FontFamily = new FontFamily("Tahoma"), FontSize = 11,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run("\u062A\u0642\u0631\u064A\u0631 \u0627\u0644\u0645\u062E\u0632\u0648\u0646")) { FontSize = 20, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 15) });
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            table.Columns.Add(new TableColumn { Width = new GridLength(250) });
            table.Columns.Add(new TableColumn { Width = new GridLength(120) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(80) });
            table.Columns.Add(new TableColumn { Width = new GridLength(100) });
            table.RowGroups.Add(new TableRowGroup());
            var header = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)) };
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u0635\u0646\u0641"));
            header.Cells.Add(CreateHeaderCell("\u0628\u0627\u0631\u0643\u0648\u062F"));
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u0631\u0635\u064A\u062F"));
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u062D\u062F"));
            header.Cells.Add(CreateHeaderCell("\u0627\u0644\u0642\u064A\u0645\u0629"));
            table.RowGroups[0].Rows.Add(header);
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT i.name, i.barcode, i.opening_quantity + COALESCE((SELECT SUM(CASE WHEN type IN ('in','opening') THEN quantity WHEN type='out' THEN -quantity WHEN type='adjustment' THEN quantity ELSE 0 END) FROM inventory_movements WHERE item_id=i.id), 0), COALESCE(i.min_stock,0), COALESCE(i.avg_cost_price, i.cost_price, 0) FROM items i WHERE i.is_kit IS NOT TRUE ORDER BY i.name", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            var row = new TableRow();
                            row.Cells.Add(CreateCell(r.GetString(0)));
                            row.Cells.Add(CreateCell(r.IsDBNull(1) ? "" : r.GetString(1)));
                            decimal stock = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2);
                            row.Cells.Add(CreateCell(stock.ToString("N2")));
                            row.Cells.Add(CreateCell(r.IsDBNull(3) ? "0" : ((decimal)r.GetDouble(3)).ToString("N2")));
                            decimal cost = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4);
                            row.Cells.Add(CreateCell((stock * cost).ToString("N2")));
                            table.RowGroups[0].Rows.Add(row);
                        }
                }
            }
            catch { }
            doc.Blocks.Add(table);
            PrintFlowDoc(doc, "Inventory Report");
        }

        private static FlowDocument BuildThermalFlowDoc(InvoicePrintData data, double width)
        {
            var doc = new FlowDocument
            {
                PageWidth = width, PageHeight = 1200,
                FontFamily = new FontFamily("Tahoma"), FontSize = 10,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run("Techtoon Accounting")) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 5) });
            doc.Blocks.Add(new Paragraph(new Run(data.InvoiceNumber)) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 5) });
            doc.Blocks.Add(new Paragraph(new Run(new string('-', 25))) { TextAlignment = TextAlignment.Center });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u062A\u0627\u0631\u064A\u062E: " + data.Date.ToString("yyyy-MM-dd HH:mm"))) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0637\u0631\u0641: " + data.PartyName)) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run(new string('-', 25))) { TextAlignment = TextAlignment.Center });
            foreach (var item in data.Items)
            {
                doc.Blocks.Add(new Paragraph(new Run(item.Name)) { TextAlignment = TextAlignment.Right });
                doc.Blocks.Add(new Paragraph(new Run($"  {item.Quantity:N2} x {item.Price:N2} = {item.Total:N2}")) { TextAlignment = TextAlignment.Right });
            }
            doc.Blocks.Add(new Paragraph(new Run(new string('-', 25))) { TextAlignment = TextAlignment.Center });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0645\u062C\u0645\u0648\u0639: " + data.Subtotal.ToString("N2"))) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("\u062E\u0635\u0645: " + data.Discount.ToString("N2"))) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0625\u062C\u0645\u0627\u0644\u064A: " + data.Total.ToString("N2"))) { FontSize = 12, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0645\u062F\u0641\u0648\u0639: " + data.Paid.ToString("N2"))) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run("\u0627\u0644\u0645\u062A\u0628\u0642\u064A: " + (data.Total - data.Paid).ToString("N2"))) { TextAlignment = TextAlignment.Right });
            doc.Blocks.Add(new Paragraph(new Run(new string('-', 25))) { TextAlignment = TextAlignment.Center });
            doc.Blocks.Add(new Paragraph(new Run("\u0634\u0643\u0631\u0627\u064B \u0644\u062A\u0639\u0627\u0645\u0644\u0643\u0645 \u0645\u0639\u0646\u0627")) { TextAlignment = TextAlignment.Center });
            return doc;
        }

        private static FlowDocument BuildA4FlowDoc(InvoicePrintData data)
        {
            var doc = new FlowDocument
            {
                PageWidth = 780, PageHeight = 1100,
                FontFamily = new FontFamily("Tahoma"), FontSize = 12,
                FlowDirection = FlowDirection.RightToLeft
            };
            doc.Blocks.Add(new Paragraph(new Run("\u0641\u0627\u062A\u0648\u0631\u0629 \u0645\u0628\u064A\u0639\u0627\u062A")) { FontSize = 24, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(Color.FromRgb(0, 105, 92)), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 20) });
            var infoTable = new Table { CellSpacing = 5 };
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(120) });
            infoTable.Columns.Add(new TableColumn { Width = new GridLength(250) });
            infoTable.RowGroups.Add(new TableRowGroup());
            var infoRow = new TableRow();
            infoRow.Cells.Add(CreateCell("\u0631\u0642\u0645 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629:", true));
            infoRow.Cells.Add(CreateCell(data.InvoiceNumber));
            infoTable.RowGroups[0].Rows.Add(infoRow);
            doc.Blocks.Add(infoTable);
            var itemsTable = new Table { CellSpacing = 0, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(250) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(80) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(80) });
            itemsTable.Columns.Add(new TableColumn { Width = new GridLength(100) });
            itemsTable.RowGroups.Add(new TableRowGroup());
            var headerRow = new TableRow { Background = new SolidColorBrush(Color.FromRgb(0, 137, 123)) };
            headerRow.Cells.Add(CreateHeaderCell("\u0627\u0644\u0635\u0646\u0641"));
            headerRow.Cells.Add(CreateHeaderCell("\u0627\u0644\u0643\u0645\u064A\u0629"));
            headerRow.Cells.Add(CreateHeaderCell("\u0627\u0644\u0633\u0639\u0631"));
            headerRow.Cells.Add(CreateHeaderCell("\u0625\u062C\u0645\u0627\u0644\u064A"));
            itemsTable.RowGroups[0].Rows.Add(headerRow);
            foreach (var item in data.Items)
            {
                var row = new TableRow();
                row.Cells.Add(CreateCell(item.Name));
                row.Cells.Add(CreateCell(item.Quantity.ToString("N2")));
                row.Cells.Add(CreateCell(item.Price.ToString("N2")));
                row.Cells.Add(CreateCell(item.Total.ToString("N2")));
                itemsTable.RowGroups[0].Rows.Add(row);
            }
            doc.Blocks.Add(itemsTable);
            doc.Blocks.Add(new Paragraph(new Run($"\u0627\u0644\u0645\u062C\u0645\u0648\u0639: {data.Subtotal:N2}    \u062E\u0635\u0645: {data.Discount:N2}    \u0625\u062C\u0645\u0627\u0644\u064A: {data.Total:N2}")) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 20, 0, 0) });
            return doc;
        }

        private static void PrintFlowDoc(FlowDocument doc, string description)
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true)
            {
                doc.PageWidth = dialog.PrintableAreaWidth;
                doc.PageHeight = dialog.PrintableAreaHeight;
                var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                dialog.PrintDocument(paginator, description);
            }
        }

        private static InvoicePrintData LoadInvoiceData(int invoiceId)
        {
            try
            {
                var data = new InvoicePrintData();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT i.invoice_number, i.date, COALESCE(p.name,''), i.subtotal, i.discount_amount, i.total, i.paid_amount, i.status FROM invoices i LEFT JOIN parties p ON i.party_id=p.id WHERE i.id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                            if (r.Read())
                            {
                                data.InvoiceNumber = r.IsDBNull(0) ? "" : r.GetString(0);
                                data.Date = r.IsDBNull(1) ? DateTime.Now : r.GetDateTime(1);
                                data.PartyName = r.IsDBNull(2) ? "" : r.GetString(2);
                                data.Subtotal = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3);
                                data.Discount = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4);
                                data.Total = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5);
                                data.Paid = r.IsDBNull(6) ? 0 : (decimal)r.GetDouble(6);
                                data.Status = r.IsDBNull(7) ? "" : r.GetString(7);
                            }
                    }
                    using (var cmd = new NpgsqlCommand("SELECT il.quantity, il.price, il.total, COALESCE(i.name,''), COALESCE(i.barcode,'') FROM invoice_lines il LEFT JOIN items i ON il.item_id=i.id WHERE il.invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                data.Items.Add(new InvoiceItemPrint { Quantity = r.IsDBNull(0) ? 0 : (decimal)r.GetDouble(0), Price = r.IsDBNull(1) ? 0 : (decimal)r.GetDouble(1), Total = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2), Name = r.IsDBNull(3) ? "" : r.GetString(3), Barcode = r.IsDBNull(4) ? "" : r.GetString(4) });
                    }
                }
                return data;
            }
            catch { return null; }
        }

        private static TableCell CreateCell(string text, bool bold = false)
        {
            var cell = new TableCell(new Paragraph(new Run(text)));
            cell.BorderBrush = Brushes.LightGray;
            cell.BorderThickness = new Thickness(0.5);
            cell.Padding = new Thickness(5);
            if (bold) cell.FontWeight = FontWeights.Bold;
            return cell;
        }

        private static TableCell CreateHeaderCell(string text)
        {
            var cell = new TableCell(new Paragraph(new Run(text)));
            cell.BorderBrush = Brushes.White;
            cell.BorderThickness = new Thickness(0.5);
            cell.Padding = new Thickness(5);
            cell.FontWeight = FontWeights.Bold;
            cell.Foreground = Brushes.White;
            return cell;
        }
    }

    public class InvoicePrintData
    {
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string PartyName { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public string Status { get; set; }
        public List<InvoiceItemPrint> Items { get; set; } = new List<InvoiceItemPrint>();
    }

    public class InvoiceItemPrint
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
    }
}