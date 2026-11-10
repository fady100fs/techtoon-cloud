using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Npgsql;
using Techtoon.Core.Data;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels
{
    /// <summary>P5: خيار صنف لقوالب الفواتير المتكررة.</summary>
    public class ItemOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal Price { get; set; }
        public override string ToString() => Name;
    }

    public class RecurringTemplateRow : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int PartyId { get; set; }
        public string PartyName { get; set; } = "";
        public string InvoiceType { get; set; } = "sales";
        public string Frequency { get; set; } = "monthly";
        public decimal DiscountPercentage { get; set; }
        public decimal TaxRate { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime NextRunDate { get; set; } = DateTime.Today;
        public bool IsActive { get; set; } = true;
        public int LinesCount { get; set; }
        public int RunsCount { get; set; }

        public string FrequencyDisplay => Frequency switch
        {
            "daily" => "يومي",
            "weekly" => "أسبوعي",
            "quarterly" => "ربع سنوي",
            "yearly" => "سنوي",
            _ => "شهري"
        };
        public string TypeDisplay => InvoiceType == "purchase" ? "شراء" : "بيع";
        public string NextRunDisplay => NextRunDate.ToString("yyyy-MM-dd");
        public string StatusDisplay => IsActive ? "نشط" : "موقوف";
        public string LinesDisplay => LinesCount + " صنف";
    }

    public class RecurringLineRow : ViewModelBase
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public string Barcode { get; set; } = "";
        public decimal Quantity { get; set; } = 1;
        public decimal Price { get; set; }
        public decimal Total => Math.Round(Quantity * Price, 2);
        public string QuantityDisplay => Quantity.ToString("N2");
        public string PriceDisplay => Price.ToString("N2");
        public string TotalDisplay => Total.ToString("N2");
    }

    /// <summary>
    /// P5: الفواتير المتكررة — قالب (عميل + أصناف + دورية) يولّد فواتير فعلية عند الاستحقاق.
    /// </summary>
    public class RecurringInvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<RecurringTemplateRow> Templates { get; } = new();
        public ObservableCollection<RecurringLineRow> Lines { get; } = new();
        public ObservableCollection<PartyOption> Parties { get; } = new();
        public ObservableCollection<ItemOption> Items { get; } = new();

        private RecurringTemplateRow _selectedTemplate;
        public RecurringTemplateRow SelectedTemplate { get => _selectedTemplate; set { SetProperty(ref _selectedTemplate, value); _ = LoadLinesAsync(); } }
        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

        public ICommand RefreshCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ToggleCommand { get; }
        public ICommand GenerateCommand { get; }
        public ICommand GenerateDueCommand { get; }

        public RecurringInvoicesViewModel()
        {
            RefreshCommand = new UiRelayCommand(_ => _ = LoadAsync());
            AddCommand = new UiRelayCommand(_ => OpenEditor(0));
            EditCommand = new UiRelayCommand(_ => OpenEditor(SelectedTemplate?.Id ?? 0), _ => SelectedTemplate != null);
            DeleteCommand = new UiRelayCommand(_ => DeleteTemplate(), _ => SelectedTemplate != null);
            ToggleCommand = new UiRelayCommand(_ => ToggleActive(), _ => SelectedTemplate != null);
            GenerateCommand = new UiRelayCommand(_ => _ = GenerateNowAsync(SelectedTemplate), _ => SelectedTemplate != null);
            GenerateDueCommand = new UiRelayCommand(_ => _ = GenerateDueAsync());
            _ = LoadAsync();
        }

        public static string GetCS() => SecureConnectionManager.GetConnectionString();

        public async Task LoadAsync()
        {
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();

                Parties.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties WHERE COALESCE(is_active,TRUE)=TRUE ORDER BY name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) });

                Items.Clear();
                using (var cmd = new NpgsqlCommand("SELECT id, name, COALESCE(barcode,''), COALESCE(sell_price,0) FROM items WHERE COALESCE(is_active,TRUE) AND COALESCE(is_kit,FALSE)=FALSE ORDER BY name", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        Items.Add(new ItemOption
                        {
                            Id = r.GetInt32(0),
                            Name = r.GetString(1),
                            Barcode = r.GetString(2),
                            Price = Convert.ToDecimal(r.GetValue(3))
                        });

                var keepId = SelectedTemplate?.Id ?? 0;
                Templates.Clear();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT t.id, COALESCE(t.name,''), t.party_id, COALESCE(p.name,''),
                           COALESCE(t.invoice_type,'sales'), COALESCE(t.frequency,'monthly'),
                           COALESCE(t.discount_percentage,0), COALESCE(t.tax_rate,0),
                           COALESCE(t.start_date,CURRENT_DATE), COALESCE(t.next_run_date,CURRENT_DATE),
                           COALESCE(t.is_active,TRUE), COALESCE(t.runs_count,0),
                           (SELECT COUNT(*) FROM recurring_invoice_lines l WHERE l.template_id = t.id)
                    FROM recurring_invoice_templates t
                    LEFT JOIN parties p ON p.id = t.party_id
                    ORDER BY t.next_run_date ASC", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        Templates.Add(new RecurringTemplateRow
                        {
                            Id = r.GetInt32(0),
                            Name = r.GetString(1),
                            PartyId = r.IsDBNull(2) ? 0 : r.GetInt32(2),
                            PartyName = r.GetString(3),
                            InvoiceType = r.GetString(4),
                            Frequency = r.GetString(5),
                            DiscountPercentage = Convert.ToDecimal(r.GetValue(6)),
                            TaxRate = Convert.ToDecimal(r.GetValue(7)),
                            StartDate = DbValue.AsDateTime(r.GetValue(8)),
                            NextRunDate = DbValue.AsDateTime(r.GetValue(9)),
                            IsActive = r.GetBoolean(10),
                            RunsCount = r.GetInt32(11),
                            LinesCount = r.GetInt32(12)
                        });

                if (keepId > 0)
                {
                    var keep = Templates.FirstOrDefault(t => t.Id == keepId);
                    if (keep != null) SelectedTemplate = keep;
                }
                StatusMessage = "تم التحميل: " + Templates.Count + " قالب";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        public async Task LoadLinesAsync()
        {
            Lines.Clear();
            if (SelectedTemplate == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand(@"SELECT l.item_id, COALESCE(i.name,''), COALESCE(i.barcode,''), l.quantity, l.price
                    FROM recurring_invoice_lines l LEFT JOIN items i ON i.id = l.item_id
                    WHERE l.template_id = @t ORDER BY l.id", conn);
                cmd.Parameters.AddWithValue("@t", SelectedTemplate.Id);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    Lines.Add(new RecurringLineRow
                    {
                        ItemId = r.GetInt32(0),
                        ItemName = r.GetString(1),
                        Barcode = r.GetString(2),
                        Quantity = Convert.ToDecimal(r.GetValue(3)),
                        Price = Convert.ToDecimal(r.GetValue(4))
                    });
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void OpenEditor(int id)
        {
            var win = new Views.RecurringInvoiceTemplateWindow(id, this);
            win.ShowDialog();
            _ = LoadAsync();
        }

        private void DeleteTemplate()
        {
            if (SelectedTemplate == null) return;
            if (MessageBox.Show("حذف القالب '" + SelectedTemplate.Name + "'؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var tx = conn.BeginTransaction();
                using (var d1 = new NpgsqlCommand("DELETE FROM recurring_invoice_lines WHERE template_id=@id", conn, tx)) { d1.Parameters.AddWithValue("id", SelectedTemplate.Id); d1.ExecuteNonQuery(); }
                using (var d2 = new NpgsqlCommand("DELETE FROM recurring_invoice_templates WHERE id=@id", conn, tx)) { d2.Parameters.AddWithValue("id", SelectedTemplate.Id); d2.ExecuteNonQuery(); }
                tx.Commit();
                AuditService.Log("DELETE", "RecurringInvoiceTemplate", SelectedTemplate.Id, "Deleted template: " + SelectedTemplate.Name);
                StatusMessage = "تم حذف القالب";
                _ = LoadAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private void ToggleActive()
        {
            if (SelectedTemplate == null) return;
            try
            {
                using var conn = new NpgsqlConnection(GetCS());
                conn.Open();
                using var cmd = new NpgsqlCommand("UPDATE recurring_invoice_templates SET is_active = @a WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("a", !SelectedTemplate.IsActive);
                cmd.Parameters.AddWithValue("id", SelectedTemplate.Id);
                cmd.ExecuteNonQuery();
                AuditService.Log("UPDATE", "RecurringInvoiceTemplate", SelectedTemplate.Id, "Active=" + (!SelectedTemplate.IsActive));
                StatusMessage = SelectedTemplate.IsActive ? "تم تفعيل القالب" : "تم إيقاف القالب";
                _ = LoadAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>توليد فاتورة الآن من القالب (فاتورة حقيقية + سطور + حركة مخزون).</summary>
        public async Task GenerateNowAsync(RecurringTemplateRow template)
        {
            if (template == null) return;
            try
            {
                var lines = await LoadTemplateLinesAsync(template.Id);
                if (lines.Count == 0) { StatusMessage = "لا توجد أصناف في القالب — أضف صنفًا أولًا."; return; }
                var number = await InvoiceService.NextInvoiceNumberAsync(template.InvoiceType);
                var input = new InvoiceInput
                {
                    InvoiceNumber = number,
                    Date = DateTime.Now,
                    Type = template.InvoiceType,
                    PartyId = template.PartyId,
                    DiscountAmount = 0,
                    Status = "unpaid",
                    SourceType = "recurring_invoice",
                    SourceId = template.Id
                };
                decimal sub = 0;
                foreach (var l in lines)
                {
                    decimal total = Math.Round(l.Quantity * l.Price, 2);
                    sub += total;
                    input.Lines.Add(new InvoiceLineInput { ItemId = l.ItemId, Quantity = l.Quantity, Price = l.Price, Discount = 0, Total = total });
                }
                if (template.DiscountPercentage > 0)
                    input.DiscountAmount = Math.Round(sub * template.DiscountPercentage / 100m, 2);

                int invoiceId = await InvoiceService.CreateInvoiceAsync(input);

                // تحديث القالب (آخر تشغيل + التشغيل التالي)
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    await conn.OpenAsync();
                    var next = NextDate(template.NextRunDate, template.Frequency);
                    using var cmd = new NpgsqlCommand(@"UPDATE recurring_invoice_templates
                        SET last_run_date = NOW(), next_run_date = @next, runs_count = COALESCE(runs_count,0) + 1 WHERE id=@id", conn);
                    cmd.Parameters.AddWithValue("@next", next);
                    cmd.Parameters.AddWithValue("@id", template.Id);
                    await cmd.ExecuteNonQueryAsync();
                }

                StatusMessage = "تم إنشاء الفاتورة " + number + " (رقم " + invoiceId + ") من القالب";
                await LoadAsync();
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        /// <summary>توليد كل القوالب المستحقة (حتى اليوم).</summary>
        public async Task GenerateDueAsync()
        {
            try
            {
                var due = Templates.Where(t => t.IsActive && t.NextRunDate.Date <= DateTime.Today).ToList();
                if (due.Count == 0) { StatusMessage = "لا توجد قوالب مستحقة اليوم."; return; }
                int made = 0;
                foreach (var t in due)
                {
                    await GenerateNowAsync(t);
                    made++;
                }
                StatusMessage = "تم توليد " + made + " فاتورة من القوالب المستحقة";
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
        }

        private async Task<List<RecurringLineRow>> LoadTemplateLinesAsync(int templateId)
        {
            var list = new List<RecurringLineRow>();
            using var conn = new NpgsqlConnection(GetCS());
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand(@"SELECT l.item_id, COALESCE(i.name,''), COALESCE(i.barcode,''), l.quantity, l.price
                FROM recurring_invoice_lines l LEFT JOIN items i ON i.id = l.item_id WHERE l.template_id=@t ORDER BY l.id", conn);
            cmd.Parameters.AddWithValue("@t", templateId);
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(new RecurringLineRow
                {
                    ItemId = r.GetInt32(0),
                    ItemName = r.GetString(1),
                    Barcode = r.GetString(2),
                    Quantity = Convert.ToDecimal(r.GetValue(3)),
                    Price = Convert.ToDecimal(r.GetValue(4))
                });
            return list;
        }

        /// <summary>التاريخ التالي وفق الدورية.</summary>
        public static DateTime NextDate(DateTime current, string frequency) => frequency switch
        {
            "daily" => current.AddDays(1),
            "weekly" => current.AddDays(7),
            "quarterly" => current.AddMonths(3),
            "yearly" => current.AddYears(1),
            _ => current.AddMonths(1)
        };
    }
}
