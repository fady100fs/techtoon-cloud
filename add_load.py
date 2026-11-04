# add_load.py - إضافة دالة LoadFromInvoiceAsync
import os

FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditViewModel.cs"

with open(FILE, "r", encoding="utf-8-sig") as f:
    content = f.read()

# تحقق إن كانت الدالة موجودة
if "LoadFromInvoiceAsync" in content:
    print("SKIP: LoadFromInvoiceAsync already exists")
else:
    # ابحث عن بداية SaveAsync وأضف الدالة قبلها
    marker = "public async System.Threading.Tasks.Task SaveAsync()"
    idx = content.find(marker)
    
    if idx < 0:
        print("ERROR: SaveAsync marker not found")
    else:
        method = '''public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
        {
            IsBusy = true;
            try
            {
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount FROM invoices WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                Id = r.GetInt32(0);
                                SerialNumber = r.IsDBNull(1) ? "" : r.GetString(1);
                                InvoiceDate = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                                InvoiceType = r.IsDBNull(3) ? "sales" : r.GetString(3);
                                PartyId = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                                WarehouseId = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                                Subtotal = r.IsDBNull(6) ? 0 : (decimal)r.GetDouble(6);
                                DiscountAmount = r.IsDBNull(7) ? 0 : (decimal)r.GetDouble(7);
                                Total = r.IsDBNull(8) ? 0 : (decimal)r.GetDouble(8);
                                TotalPaid = r.IsDBNull(9) ? 0 : (decimal)r.GetDouble(9);
                            }
                        }
                    }
                    Lines.Clear();
                    using (var cmd = new NpgsqlCommand("SELECT il.item_id, il.quantity, il.price, il.discount, il.total, i.name, i.barcode FROM invoice_lines il LEFT JOIN items i ON il.item_id = i.id WHERE il.invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var line = new InvoiceLineItem(_allItems);
                                line.ItemId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                line.Quantity = r.IsDBNull(1) ? 1 : (decimal)r.GetDouble(1);
                                line.Price = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2);
                                line.Discount = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3);
                                line.Total = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4);
                                line.ItemName = r.IsDBNull(5) ? "" : r.GetString(5);
                                line.Barcode = r.IsDBNull(6) ? "" : r.GetString(6);
                                line.SearchText = line.ItemName;
                                Lines.Add(line);
                            }
                        }
                    }
                    Payments.Clear();
                    using (var cmd = new NpgsqlCommand("SELECT method, amount, reference, paid_at FROM invoice_payments WHERE invoice_id=@id ORDER BY id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                Payments.Add(new InvoicePaymentRow
                                {
                                    Method = r.IsDBNull(0) ? "cash" : r.GetString(0),
                                    Amount = r.IsDBNull(1) ? 0 : (decimal)r.GetDouble(1),
                                    Reference = r.IsDBNull(2) ? "" : r.GetString(2),
                                    PaymentDate = r.IsDBNull(3) ? DateTime.Now : r.GetDateTime(3)
                                });
                            }
                        }
                    }
                }
                if (Lines.Count == 0) AddLine();
                RefreshTotals();
            }
            catch (Exception ex) { StatusMessage = "Load error: " + ex.Message; }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        '''
        
        content = content[:idx] + method + content[idx:]
        
        with open(FILE, "w", encoding="utf-8-sig") as f:
            f.write(content)
        print("OK: Added LoadFromInvoiceAsync method")

# أصلح الاستدعاء في xaml.cs
FILE2 = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditWindow.xaml.cs"
with open(FILE2, "r", encoding="utf-8-sig") as f:
    content2 = f.read()

# استبدل أي استدعاء معطوب
content2 = content2.replace(
    "await _vm.LoadFromInvoiceAsync?.Invoke(_editId.Value);",
    "await _vm.LoadFromInvoiceAsync(_editId.Value);"
)
content2 = content2.replace(
    "if (_editId.HasValue) await _vm.LoadFromInvoiceAsync?.Invoke(_editId.Value);",
    "if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);"
)

with open(FILE2, "w", encoding="utf-8-sig") as f:
    f.write(content2)
print("OK: Fixed call in xaml.cs")

print("\nNow run: dotnet build Techtoon.UI")