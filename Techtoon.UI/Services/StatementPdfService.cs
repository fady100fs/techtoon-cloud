using System;
using System.Collections.Generic;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Techtoon.UI.Services
{
    public class StatementPdfService
    {
        public StatementPdfService(string connectionString)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.UseSystemFonts = true;
        }

        public void Export(int partyId, string partyName, DateTime from, DateTime to,
            decimal openingBalance, List<dynamic> rows, string outputPath)
        {
            decimal totalDebit = 0, totalCredit = 0;
            decimal runningBalance = openingBalance;
            foreach (dynamic r in rows)
            {
                totalDebit += r.Debit;
                totalCredit += r.Credit;
                r.Balance = runningBalance + r.Debit - r.Credit;
                runningBalance = r.Balance;
            }

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
                        col.Item().Text("كشف حساب").FontSize(14).Bold();
                        col.Item().Text($"الطرف: {partyName}").FontSize(12).Bold();
                        col.Item().Text($"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}").FontSize(10);
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(5);

                        // Summary
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"رصيد افتتاحي: {openingBalance:0.00}").Bold();
                            row.RelativeItem().Text($"إجمالي مدين: {totalDebit:0.00}").Bold();
                            row.RelativeItem().Text($"إجمالي دائن: {totalCredit:0.00}").Bold();
                            row.RelativeItem().Text($"الرصيد النهائي: {runningBalance:0.00}").Bold();
                        });

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(0.8f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2.5f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                                c.RelativeColumn(1f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("التاريخ");
                                header.Cell().Element(CellStyle).Text("النوع");
                                header.Cell().Element(CellStyle).Text("المرجع");
                                header.Cell().Element(CellStyle).Text("ملاحظات");
                                header.Cell().Element(CellStyle).AlignRight().Text("مدين");
                                header.Cell().Element(CellStyle).AlignRight().Text("دائن");
                                header.Cell().Element(CellStyle).AlignRight().Text("الرصيد");
                            });
                            foreach (dynamic r in rows)
                            {
                                table.Cell().Element(CellStyle).Text(((DateTime)r.Date).ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text((string)r.Type);
                                table.Cell().Element(CellStyle).Text((string)r.Reference);
                                table.Cell().Element(CellStyle).Text((string)r.Description);
                                table.Cell().Element(CellStyle).AlignRight().Text(((decimal)r.Debit).ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(((decimal)r.Credit).ToString("0.00"));
                                table.Cell().Element(CellStyle).AlignRight().Text(((decimal)r.Balance).ToString("0.00"));
                            }
                            static IContainer CellStyle(IContainer c) => c
                                .BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(3);
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("صفحة ").FontSize(9).FontColor(Colors.Grey.Medium);
                        t.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        t.Span($"  |  أنشئ في: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(outputPath);
        }
    }
}
