# fix_voucher.py - إضافة ربط الفواتير لنافذة سند القبض/الصرف
import os, re

FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Pages\PaymentsPage.xaml.cs"

with open(FILE, "r", encoding="utf-8-sig") as f:
    content = f.read()

# 1) إضافة صنف InvoiceOption إن لم يوجد
if "class InvoiceOption" not in content:
    insert_class = '''
    public class InvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string PartyName { get; set; }
        public string Type { get; set; }
        public string Display
        {
            get
            {
                string t = (Type == "sales") ? "مبيعات" : (Type == "purchase" ? "مشتريات" : Type);
                return InvoiceNumber + " | " + t + " | " + PartyName + " | متبقي: " + Remaining.ToString("N2");
            }
        }
    }

'''
    idx = content.find("public partial class PaymentsPage")
    if idx > 0:
        content = content[:idx] + insert_class + content[idx:]
        print("OK: Added InvoiceOption class")

# 2) في دالة OpenEditor، أضف حقل اختيار الفاتورة قبل var sp = new StackPanel
if "cmbInvoice" not in content:
    # ابحث عن سطر إنشاء الأزرار
    marker = "var sp = new StackPanel { Orientation = Orientation.Horizontal"
    idx = content.find(marker)
    if idx > 0:
        # ابحث عن السطر السابق (نهاية AddTreasury)
        # أضف حقل الفاتورة قبل sp
        invoice_field = '''
            // حقل اختيار الفاتورة المرتبطة
            g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var lbInv = new TextBlock { Text = "فاتورة مرتبطة:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(lbInv, r);
            Grid.SetColumn(lbInv, 0);
            g.Children.Add(lbInv);
            var cmbInvoice = new ComboBox { MinHeight = 30, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 3, 10, 3) };
            Grid.SetRow(cmbInvoice, r);
            Grid.SetColumn(cmbInvoice, 1);
            g.Children.Add(cmbInvoice);
            r++;

            // قائمة الفواتير غير المدفوعة
            var unpaidInvoices = new List<InvoiceOption>();
            try
            {
                using (var con = new NpgsqlConnection(GetConnectionString()))
                {
                    con.Open();
                    string sql = @"SELECT i.id, i.invoice_number, i.total, i.paid_amount, COALESCE(p.name, '') AS party_name, i.type
                        FROM invoices i LEFT JOIN parties p ON i.party_id = p.id
                        WHERE i.paid_amount < i.total ORDER BY i.id DESC LIMIT 200";
                    using (var cmd = new NpgsqlCommand(sql, con))
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            var opt = new InvoiceOption
                            {
                                Id = rdr.IsDBNull(0) ? 0 : rdr.GetInt32(0),
                                InvoiceNumber = rdr.IsDBNull(1) ? "" : rdr.GetString(1),
                                Total = rdr.IsDBNull(2) ? 0 : Convert.ToDecimal(rdr.GetValue(2)),
                                Paid = rdr.IsDBNull(3) ? 0 : Convert.ToDecimal(rdr.GetValue(3)),
                                PartyName = rdr.IsDBNull(4) ? "" : rdr.GetString(4),
                                Type = rdr.IsDBNull(5) ? "" : rdr.GetString(5)
                            };
                            unpaidInvoices.Add(opt);
                            cmbInvoice.Items.Add(new ComboBoxItem { Content = opt.Display, Tag = opt });
                        }
                    }
                }
            }
            catch { }

            // عند اختيار فاتورة، يُملأ المبلغ والطرف تلقائياً
            cmbInvoice.SelectionChanged += (s3, e3) =>
            {
                try
                {
                    if (cmbInvoice.SelectedItem is ComboBoxItem item && item.Tag is InvoiceOption inv)
                    {
                        if (string.IsNullOrWhiteSpace(tbAmount.Text) || tbAmount.Text == "0")
                            tbAmount.Text = inv.Remaining.ToString(CultureInfo.InvariantCulture);
                        if (string.IsNullOrWhiteSpace(tbParty.Text))
                            tbParty.Text = inv.PartyName;
                        if (inv.Type == "sales") cmbType.SelectedIndex = 0;
                        else if (inv.Type == "purchase") cmbType.SelectedIndex = 1;
                    }
                }
                catch { }
            };

'''
        content = content[:idx] + invoice_field + "            " + content[idx:]
        print("OK: Added invoice ComboBox to OpenEditor")

# 3) في دالة الحفظ، أضف تحديث paid_amount للفاتورة المرتبطة
if "linkedInvoice" not in content:
    # ابحث عن سطر vals[nc] = tbNotes.Text.Trim();
    marker = 'if (nc != null && !string.IsNullOrWhiteSpace(tbNotes.Text)) vals[nc] = tbNotes.Text.Trim();'
    idx = content.find(marker)
    if idx > 0:
        # ابحث عن السطر التالي
        end_line = content.find("\n", idx) + 1
        # أضف كود ربط الفاتورة
        link_code = '''
                    // ربط الفاتورة المرتبطة
                    InvoiceOption selectedInv = null;
                    if (cmbInvoice.SelectedItem is ComboBoxItem item && item.Tag is InvoiceOption inv)
                    {
                        selectedInv = inv;
                        var ic = Pick(_cols, "invoice_id", "invoiceid", "doc_id", "docid");
                        if (ic != null) vals[ic] = inv.Id;
                    }

'''
        content = content[:end_line] + link_code + content[end_line:]
        print("OK: Added invoice link to save logic")

    # أضف تحديث paid_amount بعد InsertRow/UpdateRow
    marker2 = "else UpdateRow(con, table, editId, snapshot, colsSnapshot);"
    idx2 = content.find(marker2)
    if idx2 > 0:
        end_line2 = content.find("\n", idx2) + 1
        update_code = '''
                            // تحديث paid_amount للفاتورة المرتبطة
                            if (selectedInv != null && amt > 0)
                            {
                                try
                                {
                                    string updSql = "UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid";
                                    using (var upCmd = new NpgsqlCommand(updSql, con))
                                    {
                                        upCmd.Parameters.AddWithValue("amt", amt);
                                        upCmd.Parameters.AddWithValue("pid", selectedInv.Id);
                                        upCmd.ExecuteNonQuery();
                                    }
                                }
                                catch { }
                            }

'''
        content = content[:end_line2] + update_code + content[end_line2:]
        print("OK: Added paid_amount update logic")

with open(FILE, "w", encoding="utf-8-sig") as f:
    f.write(content)

print()
print("=" * 60)
print("  DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 60)