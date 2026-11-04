# fix_complete.ps1 - COMPLETE REWRITE of Invoice files
$ProjectPath = 'D:\PROG\Accounting Prog\Techtoon Accounting C+'
$ViewsDir = Join-Path $ProjectPath 'Techtoon.UI\Views'

if (-not (Test-Path $ViewsDir)) { Write-Host 'ERROR' -ForegroundColor Red; exit 1 }

Write-Host '========================================' -ForegroundColor Cyan
Write-Host '  Techtoon - COMPLETE REWRITE' -ForegroundColor Cyan
Write-Host '========================================' -ForegroundColor Cyan

# حذف الملفات المكررة
Get-ChildItem -Path $ViewsDir -Filter 'InvoiceEdit*' | Where-Object { $_.Name -match '\(1\)' } | Remove-Item -Force -ErrorAction SilentlyContinue

# ===== InvoiceEditViewModel.cs (UTF-8 with BOM) =====
$vmPath = Join-Path $ViewsDir 'InvoiceEditViewModel.cs'
if (Test-Path $vmPath) { Copy-Item $vmPath ($vmPath + '.bak_new') -Force }
$vm = @'
// =====================================================================
// InvoiceEditViewModel.cs  -  COMPLETE REWRITE
// يعمل مع الأعمدة الفعلية في قاعدة البيانات
// =====================================================================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    // ==================================================================
    //  صنف نتيجة البحث عن الأصناف
    // ==================================================================
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SellPrice { get; set; }
        public decimal CurrentStock { get; set; }
        public string SearchDisplay
        {
            get
            {
                string b = string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]";
                string s = CurrentStock > 0 ? "  (stock: " + CurrentStock.ToString("0.##") + ")" : "";
                return (Name ?? "") + b + s;
            }
        }
    }

    // ==================================================================
    //  صف بند الفاتورة
    // ==================================================================
    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;

        public InvoiceLineItem(List<ItemSearchResult> allItems)
        {
            _allItems = allItems ?? new List<ItemSearchResult>();
        }

        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged("SearchText");
                OnPropertyChanged("FilteredItems");
            }
        }

        // قائمة منسدلة مصفاة حسب النص المكتوب
        public List<ItemSearchResult> FilteredItems
        {
            get
            {
                string q = (_searchText ?? "").Trim().ToLower();
                if (q.Length == 0) return _allItems.Take(50).ToList();
                return _allItems
                    .Where(it =>
                        (it.Name != null && it.Name.ToLower().Contains(q)) ||
                        (it.Barcode != null && it.Barcode.ToLower().Contains(q)))
                    .Take(20)
                    .ToList();
            }
        }

        public int ItemId { get; set; }
        public string Unit { get; set; }

        private string _itemName = "";
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; OnPropertyChanged("ItemName"); }
        }

        private string _barcode = "";
        public string Barcode
        {
            get { return _barcode; }
            set { _barcode = value; OnPropertyChanged("Barcode"); }
        }

        private decimal _quantity = 1;
        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value; OnPropertyChanged("Quantity"); Recalc(); }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get { return _price; }
            set { _price = value; OnPropertyChanged("Price"); Recalc(); }
        }

        private decimal _discount = 0;
        public decimal Discount
        {
            get { return _discount; }
            set { _discount = value; OnPropertyChanged("Discount"); Recalc(); }
        }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); }
        }

        private decimal _stockAvailable = 0;
        public decimal StockAvailable
        {
            get { return _stockAvailable; }
            set { _stockAvailable = value; OnPropertyChanged("StockAvailable"); }
        }

        public bool HasItem { get { return ItemId > 0; } }

        private void Recalc()
        {
            Total = Math.Round(_quantity * _price - _discount, 2);
        }

        public void SelectItem(ItemSearchResult it)
        {
            if (it == null) return;
            ItemId = it.Id;
            ItemName = it.Name;
            Barcode = it.Barcode;
            Price = it.SellPrice;
            StockAvailable = it.CurrentStock;
            Quantity = 1;
            SearchText = it.Name;
            OnPropertyChanged("HasItem");
            OnPropertyChanged("FilteredItems");
        }
    }

    // ==================================================================
    //  صف الدفع
    // ==================================================================
    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }

        private string _method = "cash";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); }
        }

        private decimal _amount = 0;
        public decimal Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged("Amount"); }
        }

        private string _reference = "";
        public string Reference
        {
            get { return _reference; }
            set { _reference = value; OnPropertyChanged("Reference"); }
        }

        private DateTime _paymentDate = DateTime.Now;
        public DateTime PaymentDate
        {
            get { return _paymentDate; }
            set { _paymentDate = value; OnPropertyChanged("PaymentDate"); }
        }
    }

    // ==================================================================
    //  خيارات الطرف والمخزن
    // ==================================================================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class WarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    // ==================================================================
    //  ViewModel الرئيسي
    // ==================================================================
    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;
        public int Id { get; set; }
        public bool DialogResult { get; set; }
        public bool PrintAfterSave { get; set; }

        // معلومات الفاتورة
        private string _invoiceType = "sales";
        public string InvoiceType
        {
            get { return _invoiceType; }
            set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); }
        }
        public string InvoiceTypeDisplay
        {
            get { return (_invoiceType ?? "") == "purchase" ? "Purchase" : "Sales"; }
        }

        private string _serialNumber = "";
        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; OnPropertyChanged("SerialNumber"); }
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get { return _invoiceDate; }
            set { _invoiceDate = value; OnPropertyChanged("InvoiceDate"); }
        }

        private int _partyId = 0;
        public int PartyId
        {
            get { return _partyId; }
            set { _partyId = value; OnPropertyChanged("PartyId"); }
        }

        private int _warehouseId = 0;
        public int WarehouseId
        {
            get { return _warehouseId; }
            set { _warehouseId = value; OnPropertyChanged("WarehouseId"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }

        // القوائم
        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }
        private List<ItemSearchResult> _allItems;

        // الحسابات
        private decimal _subtotal = 0;
        public decimal Subtotal { get { return _subtotal; } set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); } }
        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }

        private decimal _discountAmount = 0;
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set { _discountAmount = value; OnPropertyChanged("DiscountAmount"); RefreshTotals(); }
        }

        private decimal _total = 0;
        public decimal Total { get { return _total; } set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalDisplay { get { return _total.ToString("N2"); } }

        private decimal _totalPaid = 0;
        public decimal TotalPaid { get { return _totalPaid; } set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }

        public decimal Remaining { get { return Math.Round(_total - _totalPaid, 2); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = "";
        public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }

        private bool _isBusy = false;
        public bool IsBusy { get { return _isBusy; } set { _isBusy = value; OnPropertyChanged("IsBusy"); } }

        // الأوامر
        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public InvoiceEditViewModel()
        {
            Lines = new ObservableCollection<InvoiceLineItem>();
            Payments = new ObservableCollection<InvoicePaymentRow>();
            Parties = new ObservableCollection<PartyOption>();
            Warehouses = new ObservableCollection<WarehouseOption>();
            _allItems = new List<ItemSearchResult>();

            AddLineCommand = new UiRelayCommand(_ => AddLine());
            DeleteLineCommand = new UiRelayCommand(p => { if (p is InvoiceLineItem li) Lines.Remove(li); RefreshTotals(); });
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment());
            DeletePaymentCommand = new UiRelayCommand(p => { if (p is InvoicePaymentRow pr) Payments.Remove(pr); RefreshTotals(); });
            SaveCommand = new UiRelayCommand(_ => { _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });

            Payments.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InvoicePaymentRow r in e.NewItems)
                    {
                        r.PropertyChanged += (s2, e2) => { if (e2.PropertyName == "Amount") RefreshTotals(); };
                    }
                }
            };
        }

        // ==================================================================
        //  سلسلة الاتصال المباشرة (Neon PostgreSQL)
        // ==================================================================
        public static string GetConnectionString()
        {
            return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";
        }

        // ==================================================================
        //  تحميل البيانات الأولية
        // ==================================================================
        public async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "Loading data...";
            try
            {
                _allItems = LoadItems();
                System.Windows.MessageBox.Show("Items loaded: " + _allItems.Count, "Debug");
                LoadParties();
                LoadWarehouses();
                SerialNumber = GenerateSerial(InvoiceType);
                AddLine();
                if (Payments.Count == 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0, PaymentDate = DateTime.Now });
                }
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "Error: " + ex.Message;
                System.Windows.MessageBox.Show("LoadAsync Error:\n" + ex.Message, "Error");
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  تحميل الأصناف (بالأعمدة الفعلية)
        // ==================================================================
        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            string cs = GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                string sql = @"SELECT id, name, barcode, 
                    COALESCE(sell_price, 0) AS sell_price,
                    COALESCE(opening_quantity, 0) AS opening_qty
                    FROM items 
                    ORDER BY name LIMIT 500";
                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new ItemSearchResult
                        {
                            Id = r.GetInt32(0),
                            Name = r.IsDBNull(1) ? "" : r.GetString(1),
                            Barcode = r.IsDBNull(2) ? "" : r.GetString(2),
                            SellPrice = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3)),
                            CurrentStock = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4))
                        });
                    }
                }
            }
            return list;
        }

        // ==================================================================
        //  تحميل الأطراف
        // ==================================================================
        private void LoadParties()
        {
            Parties.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, name, type FROM parties ORDER BY name LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Parties.Add(new PartyOption
                            {
                                Id = r.GetInt32(0),
                                Name = r.IsDBNull(1) ? "" : r.GetString(1),
                                Type = r.IsDBNull(2) ? "" : r.GetString(2)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { StatusMessage = "Parties error: " + ex.Message; }
        }

        // ==================================================================
        //  تحميل المخازن
        // ==================================================================
        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, name FROM warehouses WHERE COALESCE(is_active, true) = true ORDER BY name LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Warehouses.Add(new WarehouseOption
                            {
                                Id = r.GetInt32(0),
                                Name = r.IsDBNull(1) ? "" : r.GetString(1)
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { StatusMessage = "Warehouses error: " + ex.Message; }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                    int year = DateTime.Now.Year;
                    string likePattern = prefix + "-" + year + "-%";
                    string sql = @"SELECT invoice_number FROM invoices 
                        WHERE invoice_number LIKE @p 
                        ORDER BY invoice_number DESC LIMIT 1";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("p", likePattern);
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value)
                        {
                            string last = o.ToString();
                            int dashIdx = last.LastIndexOf('-');
                            if (dashIdx >= 0 && int.TryParse(last.Substring(dashIdx + 1), out int seq))
                            {
                                return prefix + "-" + year + "-" + (seq + 1).ToString("D4");
                            }
                        }
                    }
                }
            }
            catch { }
            string pf = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        public void AddLine()
        {
            var line = new InvoiceLineItem(_allItems);
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Quantity" || e.PropertyName == "Price" ||
                    e.PropertyName == "Discount" || e.PropertyName == "Total")
                    RefreshTotals();
            };
            Lines.Add(line);
            RefreshTotals();
        }

        public void AddPayment()
        {
            decimal rem = Remaining;
            Payments.Add(new InvoicePaymentRow
            {
                Method = "cash",
                Amount = rem > 0 ? rem : 0,
                PaymentDate = DateTime.Now
            });
            RefreshTotals();
        }

        public void RefreshTotals()
        {
            decimal sub = 0;
            foreach (var line in Lines) sub += line.Total;
            Subtotal = Math.Round(sub, 2);
            Total = Math.Round(_subtotal - _discountAmount, 2);

            decimal paid = 0;
            foreach (var p in Payments) paid += p.Amount;
            TotalPaid = Math.Round(paid, 2);

            OnPropertyChanged("Remaining");
            OnPropertyChanged("RemainingDisplay");
        }

        // ==================================================================
        //  تحميل فاتورة موجودة
        // ==================================================================
        public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
        {
            IsBusy = true;
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string sql = @"SELECT id, invoice_number, date, type, party_id, warehouse_id, 
                        notes, subtotal, discount_amount, total, paid_amount
                        FROM invoices WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                Id = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                SerialNumber = r.IsDBNull(1) ? "" : r.GetString(1);
                                InvoiceDate = r.IsDBNull(2) ? DateTime.Now : r.GetDateTime(2);
                                InvoiceType = r.IsDBNull(3) ? "sales" : r.GetString(3);
                                PartyId = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                                WarehouseId = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                                Notes = r.IsDBNull(6) ? "" : r.GetString(6);
                                Subtotal = r.IsDBNull(7) ? 0 : Convert.ToDecimal(r.GetValue(7));
                                DiscountAmount = r.IsDBNull(8) ? 0 : Convert.ToDecimal(r.GetValue(8));
                                Total = r.IsDBNull(9) ? 0 : Convert.ToDecimal(r.GetValue(9));
                                TotalPaid = r.IsDBNull(10) ? 0 : Convert.ToDecimal(r.GetValue(10));
                            }
                        }
                    }

                    Lines.Clear();
                    string lineSql = @"SELECT il.item_id, il.quantity, il.price, il.discount, il.total, i.name, i.barcode
                        FROM invoice_lines il 
                        LEFT JOIN items i ON il.item_id = i.id
                        WHERE il.invoice_id = @id";
                    using (var cmd = new NpgsqlCommand(lineSql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                var line = new InvoiceLineItem(_allItems);
                                line.ItemId = r.IsDBNull(0) ? 0 : r.GetInt32(0);
                                line.Quantity = r.IsDBNull(1) ? 1 : Convert.ToDecimal(r.GetValue(1));
                                line.Price = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2));
                                line.Discount = r.IsDBNull(3) ? 0 : Convert.ToDecimal(r.GetValue(3));
                                line.Total = r.IsDBNull(4) ? 0 : Convert.ToDecimal(r.GetValue(4));
                                line.ItemName = r.IsDBNull(5) ? "" : r.GetString(5);
                                line.Barcode = r.IsDBNull(6) ? "" : r.GetString(6);
                                line.SearchText = line.ItemName;
                                Lines.Add(line);
                            }
                        }
                    }

                    Payments.Clear();
                    string paySql = @"SELECT id, method, amount, reference, paid_at
                        FROM invoice_payments WHERE invoice_id = @id ORDER BY id";
                    using (var cmd = new NpgsqlCommand(paySql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                Payments.Add(new InvoicePaymentRow
                                {
                                    Id = r.IsDBNull(0) ? 0 : r.GetInt32(0),
                                    Method = r.IsDBNull(1) ? "cash" : r.GetString(1),
                                    Amount = r.IsDBNull(2) ? 0 : Convert.ToDecimal(r.GetValue(2)),
                                    Reference = r.IsDBNull(3) ? "" : r.GetString(3),
                                    PaymentDate = r.IsDBNull(4) ? DateTime.Now : r.GetDateTime(4)
                                });
                            }
                        }
                    }
                }
                if (Lines.Count == 0) AddLine();
                RefreshTotals();
            }
            catch (Exception ex)
            {
                StatusMessage = "Load error: " + ex.Message;
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  حفظ الفاتورة
        // ==================================================================
        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "Saving...";
            try
            {
                RefreshTotals();
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();

                    if (Id > 0)
                    {
                        string upd = @"UPDATE invoices SET 
                            invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid,
                            notes=@nt, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa,
                            updated_at=NOW() WHERE id=@id";
                        using (var cmd = new NpgsqlCommand(upd, conn))
                        {
                            cmd.Parameters.AddWithValue("id", Id);
                            cmd.Parameters.AddWithValue("sn", SerialNumber);
                            cmd.Parameters.AddWithValue("dt", InvoiceDate);
                            cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                            cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("nt", (object)Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal);
                            cmd.Parameters.AddWithValue("da", DiscountAmount);
                            cmd.Parameters.AddWithValue("tt", Total);
                            cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string ins = @"INSERT INTO invoices 
                            (invoice_number, date, type, party_id, warehouse_id, notes, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at)
                            VALUES (@sn, @dt, @tp, @pid, @wid, @nt, @st, @da, @tt, @pa, @status, NOW(), NOW()) RETURNING id";
                        using (var cmd = new NpgsqlCommand(ins, conn))
                        {
                            cmd.Parameters.AddWithValue("sn", SerialNumber);
                            cmd.Parameters.AddWithValue("dt", InvoiceDate);
                            cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                            cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("nt", (object)Notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal);
                            cmd.Parameters.AddWithValue("da", DiscountAmount);
                            cmd.Parameters.AddWithValue("tt", Total);
                            cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.Parameters.AddWithValue("status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }

                    // حفظ البنود
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var line in Lines)
                    {
                        if (line.ItemId <= 0) continue;
                        string lineSql = @"INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total)
                            VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)";
                        using (var cmd = new NpgsqlCommand(lineSql, conn))
                        {
                            cmd.Parameters.AddWithValue("inv", Id);
                            cmd.Parameters.AddWithValue("iid", line.ItemId);
                            cmd.Parameters.AddWithValue("qty", line.Quantity);
                            cmd.Parameters.AddWithValue("prc", line.Price);
                            cmd.Parameters.AddWithValue("dsc", line.Discount);
                            cmd.Parameters.AddWithValue("tot", line.Total);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // حفظ الدفعات
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                    foreach (var pay in Payments)
                    {
                        if (pay.Amount <= 0) continue;
                        string paySql = @"INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at)
                            VALUES (@inv, @mth, @amt, @ref, @pdt)";
                        using (var cmd = new NpgsqlCommand(paySql, conn))
                        {
                            cmd.Parameters.AddWithValue("inv", Id);
                            cmd.Parameters.AddWithValue("mth", pay.Method);
                            cmd.Parameters.AddWithValue("amt", pay.Amount);
                            cmd.Parameters.AddWithValue("ref", (object)pay.Reference ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("pdt", pay.PaymentDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                StatusMessage = "Saved! ID: " + Id;
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "Save error: " + ex.Message;
                System.Windows.MessageBox.Show("Save Error:\n" + ex.Message, "Error");
            }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
'@
[System.IO.File]::WriteAllText($vmPath, $vm, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditViewModel.cs" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml (ASCII) =====
$xamlPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml'
if (Test-Path $xamlPath) { Copy-Item $xamlPath ($xamlPath + '.bak_new') -Force }
$xaml = @'
<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200"
        WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft"
        Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="Hdr" TargetType="TextBlock">
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="Margin" Value="0,0,6,0"/>
            <Setter Property="VerticalAlignment" Value="Center"/>
        </Style>
        <Style x:Key="Inp" TargetType="TextBox">
            <Setter Property="Padding" Value="6,4"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="BorderBrush" Value="#B0BEC5"/>
            <Setter Property="BorderThickness" Value="1"/>
        </Style>
        <Style x:Key="AddBtn" TargetType="Button">
            <Setter Property="Background" Value="#26A69A"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="14,6"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="SaveBtn" TargetType="Button">
            <Setter Property="Background" Value="#00897B"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="FontWeight" Value="Bold"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Padding" Value="24,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
        <Style x:Key="CancelBtn" TargetType="Button">
            <Setter Property="Background" Value="#ECEFF1"/>
            <Setter Property="Foreground" Value="#37474F"/>
            <Setter Property="FontWeight" Value="SemiBold"/>
            <Setter Property="FontSize" Value="13"/>
            <Setter Property="Padding" Value="18,10"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
        </Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel>
                        <TextBlock x:Name="HeaderTitle" FontSize="18" FontWeight="Bold" Foreground="#00695C"/>
                        <TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/>
                    </StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/>
                        <ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="150"/><ColumnDefinition Width="18"/>
                        <ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/>
                    </Grid.ColumnDefinitions>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" Style="{StaticResource Inp}" IsReadOnly="True"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" Style="{StaticResource Hdr}"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" Style="{StaticResource Hdr}"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>

        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="*"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Width="340">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo"
                                              ItemsSource="{Binding FilteredItems}"
                                              DisplayMemberPath="SearchDisplay"
                                              IsEditable="True"
                                              Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                                              StaysOpenOnEdit="True"
                                              SelectionChanged="ItemSearchCombo_SelectionChanged"
                                              Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTextColumn x:Name="ColUnit" Width="70" Binding="{Binding Unit}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Width="90">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Width="100">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Width="80">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle>
                                <Style TargetType="TextBlock">
                                    <Setter Property="HorizontalAlignment" Value="Right"/>
                                    <Setter Property="FontWeight" Value="SemiBold"/>
                                    <Setter Property="Foreground" Value="#00695C"/>
                                </Style>
                            </DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddPay" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="140" CellEditEnding="PaymentsGrid_CellEditEnding">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColPayMethod" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="PayMethodCombo" SelectedValue="{Binding Method, UpdateSourceTrigger=PropertyChanged}" SelectedValuePath="Tag" Padding="4,2"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayAmount" Width="140">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <TextBox Text="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Padding="4,3" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColPayRef" Width="180" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="150">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <DatePicker SelectedDate="{Binding PaymentDate}" FontSize="12"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <Button Content="X" Background="Transparent" BorderThickness="0" FontSize="14" Cursor="Hand" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*"/><ColumnDefinition Width="300"/>
                    </Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center">
                        <TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/>
                    </StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*"/><ColumnDefinition Width="110"/>
                        </Grid.ColumnDefinitions>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/>
                        </Grid.RowDefinitions>
                        <TextBlock x:Name="LblSubtotal" Grid.Row="0" Grid.Column="0" FontSize="13" Foreground="#546E7A" Padding="0,2"/>
                        <TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding SubtotalDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#37474F" TextAlignment="Left" Padding="0,2"/>
                        <StackPanel Grid.Row="1" Grid.Column="0" Orientation="Horizontal">
                            <TextBlock x:Name="LblDisc" FontSize="13" Foreground="#546E7A" VerticalAlignment="Center"/>
                            <TextBox Text="{Binding DiscountAmount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Width="80" Margin="6,0,0,0" Padding="3,2" FontSize="12" BorderBrush="#E0E0E0" TextAlignment="Right"/>
                        </StackPanel>
                        <TextBlock Grid.Row="1" Grid.Column="1" Text="{Binding DiscountAmount, StringFormat=N2}" FontSize="13" Foreground="#E53935" TextAlignment="Left"/>
                        <TextBlock x:Name="LblTotal" Grid.Row="2" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#00695C" Padding="0,2"/>
                        <TextBlock Grid.Row="2" Grid.Column="1" Text="{Binding TotalDisplay}" FontSize="15" FontWeight="Bold" Foreground="#00695C" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblPaid" Grid.Row="3" Grid.Column="0" FontSize="13" Foreground="#2E7D32" Padding="0,2"/>
                        <TextBlock Grid.Row="3" Grid.Column="1" Text="{Binding TotalPaidDisplay}" FontSize="13" FontWeight="SemiBold" Foreground="#2E7D32" TextAlignment="Left" Padding="0,2"/>
                        <TextBlock x:Name="LblRemaining" Grid.Row="4" Grid.Column="0" FontSize="14" FontWeight="Bold" Foreground="#E65100" Padding="0,2"/>
                        <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding RemainingDisplay}" FontSize="14" FontWeight="Bold" Foreground="#E65100" TextAlignment="Left" Padding="0,2"/>
                    </Grid>
                </Grid>
                <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Left" FlowDirection="LeftToRight" Margin="0,8,0,0">
                    <Button x:Name="BtnSave" Style="{StaticResource SaveBtn}" Margin="0,0,8,0" Command="{Binding SaveCommand}"/>
                    <Button x:Name="BtnCancel" Style="{StaticResource CancelBtn}" Command="{Binding CancelCommand}"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</Window>
'@
[System.IO.File]::WriteAllText($xamlPath, $xaml, [System.Text.ASCIIEncoding]::new())
Write-Host "  OK: InvoiceEditWindow.xaml" -ForegroundColor Green

# ===== InvoiceEditWindow.xaml.cs (UTF-8 with BOM) =====
$csPath = Join-Path $ViewsDir 'InvoiceEditWindow.xaml.cs'
if (Test-Path $csPath) { Copy-Item $csPath ($csPath + '.bak_new') -Force }
$cs = @'
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoiceEditWindow : Window
    {
        private readonly InvoiceEditViewModel _vm;
        private readonly int? _editId;

        public InvoiceEditWindow(int? editId = null)
        {
            InitializeComponent();
            _editId = editId;
            _vm = new InvoiceEditViewModel();
            if (editId.HasValue) _vm.Id = editId.Value;
            DataContext = _vm;
            _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // ضبط كل النصوص العربية من الكود (Unicode escape - مستحيل أن تظهر مكسورة)
            SetArabicTexts();
            FillTypeCombo();
            FillPaymentMethods();
            await _vm.LoadAsync();
            if (_editId.HasValue) await _vm.LoadFromInvoiceAsync(_editId.Value);
        }

        private void SetArabicTexts()
        {
            // كل النصوص العربية بـ Unicode escape
            try { Title = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { HeaderIcon.Text = "\uD83E\uDDF9"; } catch { } // 🧾
            try { HeaderTitle.Text = "\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // فاتورة
            try { LblSerial.Text = "\u0627\u0644\u0631\u0642\u0645:"; } catch { } // الرقم:
            try { LblDate.Text = "\u0627\u0644\u062A\u0627\u0631\u064A\u062E:"; } catch { } // التاريخ:
            try { LblType.Text = "\u0627\u0644\u0646\u0648\u0639:"; } catch { } // النوع:
            try { LblParty.Text = "\u0627\u0644\u0637\u0631\u0641:"; } catch { } // الطرف:
            try { LblWarehouse.Text = "\u0627\u0644\u0645\u062E\u0632\u0646:"; } catch { } // المخزن:
            try { LblItems.Text = "\u0628\u0646\u0648\u062F \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // بنود الفاتورة
            try { BtnAddLine.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641"; } catch { } // + إضافة صنف
            try { ColItem.Header = "\u0635\u0646\u0641 (\u0627\u0643\u062A\u0628 \u0644\u0644\u0628\u062D\u062B)"; } catch { } // صنف (اكتب للبحث)
            try { ColBarcode.Header = "\u0628\u0627\u0631\u0643\u0648\u062F"; } catch { } // باركود
            try { ColUnit.Header = "\u0627\u0644\u0648\u062D\u062F\u0629"; } catch { } // الوحدة
            try { ColQty.Header = "\u0627\u0644\u0643\u0645\u064A\u0629"; } catch { } // الكمية
            try { ColPrice.Header = "\u0627\u0644\u0633\u0639\u0631"; } catch { } // السعر
            try { ColDisc.Header = "\u062E\u0635\u0645"; } catch { } // خصم
            try { ColTotal.Header = "\u0625\u062C\u0645\u0627\u0644\u064A"; } catch { } // إجمالي
            try { ColDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblPayments.Text = "\u0637\u0631\u0642 \u0627\u0644\u062F\u0641\u0639 \u0627\u0644\u0645\u062A\u0639\u062F\u062F\u0629"; } catch { } // طرق الدفع المتعددة
            try { BtnAddPay.Content = "+ \u0625\u0636\u0627\u0641\u0629 \u062F\u0641\u0639\u0629"; } catch { } // + إضافة دفعة
            try { ColPayMethod.Header = "\u0637\u0631\u064A\u0642\u0629 \u0627\u0644\u062F\u0641\u0639"; } catch { } // طريقة الدفع
            try { ColPayAmount.Header = "\u0627\u0644\u0645\u0628\u0644\u063A"; } catch { } // المبلغ
            try { ColPayRef.Header = "\u0645\u0631\u062C\u0639"; } catch { } // مرجع
            try { ColPayDate.Header = "\u062A\u0627\u0631\u064A\u062E"; } catch { } // تاريخ
            try { ColPayDel.Header = "\u062D\u0630\u0641"; } catch { } // حذف
            try { LblSubtotal.Text = "\u0627\u0644\u0645\u062C\u0645\u0648\u0639:"; } catch { } // المجموع:
            try { LblDisc.Text = "\u062E\u0635\u0645:"; } catch { } // خصم:
            try { LblTotal.Text = "\u0625\u062C\u0645\u0627\u0644\u064A:"; } catch { } // إجمالي:
            try { LblPaid.Text = "\u0627\u0644\u0645\u062F\u0641\u0648\u0639:"; } catch { } // المدفوع:
            try { LblRemaining.Text = "\u0627\u0644\u0645\u062A\u0628\u0642\u064A:"; } catch { } // المتبقي:
            try { BtnSave.Content = "\uD83D\uDCBE \u062D\u0641\u0638 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629"; } catch { } // 💾 حفظ الفاتورة
            try { BtnCancel.Content = "\u2716 \u0625\u0644\u063A\u0627\u0621"; } catch { } // ✖ إلغاء
        }

        private void FillTypeCombo()
        {
            try
            {
                CmbType.Items.Clear();
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0628\u064A\u0639\u0627\u062A", Tag = "sales" }); // مبيعات
                CmbType.Items.Add(new ComboBoxItem { Content = "\u0645\u0634\u062A\u0631\u064A\u0627\u062A", Tag = "purchase" }); // مشتريات
            }
            catch { }
        }

        private void FillPaymentMethods()
        {
            // سيتم تعبئة عند تحميل كل صف في PaymentsGrid
        }

        // اختيار صنف من القائمة المنسدلة
        private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item)
                {
                    if (cb.DataContext is InvoiceLineItem line)
                    {
                        line.SelectItem(item);
                    }
                }
            }
            catch { }
        }

        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line);
            int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last)
            {
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow)
                {
                    var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag);
                    if (next != null) { next.Focus(); next.SelectAll(); }
                }
            }
            else if (idx == last)
            {
                _vm.AddLineCommand.Execute(null);
                LinesGrid.UpdateLayout();
                if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow)
                {
                    var cb = FindDescendant<ComboBox>(newRow, x => x.Name == "ItemSearchCombo");
                    cb?.Focus();
                }
            }
            e.Handled = true;
        }

        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T matched && predicate(matched)) return matched;
                var deep = FindDescendant(child, predicate);
                if (deep != null) return deep;
            }
            return null;
        }

        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals();
        }
    }
}
'@
[System.IO.File]::WriteAllText($csPath, $cs, [System.Text.UTF8Encoding]::new($true))
Write-Host "  OK: InvoiceEditWindow.xaml.cs" -ForegroundColor Green

# ===== appsettings.json =====
$appsettings = '{"ConnectionStrings":{"DefaultConnection":"Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"}}'
$ap = Join-Path $ProjectPath 'Techtoon.UI\appsettings.json'
[System.IO.File]::WriteAllText($ap, $appsettings, [System.Text.UTF8Encoding]::new($false))
$bd = Join-Path $ProjectPath 'Techtoon.UI\bin\Debug\net10.0-windows'
if (Test-Path $bd) { Copy-Item $ap (Join-Path $bd 'appsettings.json') -Force }
Write-Host "  OK: appsettings.json" -ForegroundColor Green

# ===== تنظيف obj =====
$objDir = Join-Path $ProjectPath 'Techtoon.UI\obj\Debug\net10.0-windows'
if (Test-Path $objDir) {
    Get-ChildItem -Path $objDir -Filter 'InvoiceEdit*' | Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "  Cleaned obj" -ForegroundColor Green
}

# ===== البناء =====
Write-Host ''
Write-Host '===== Build =====' -ForegroundColor Cyan
dotnet build (Join-Path $ProjectPath 'Techtoon.UI') 2>&1 | Select-Object -Last 8
Write-Host ''
Write-Host '========================================' -ForegroundColor Cyan
Write-Host "  DONE! Run: dotnet run --project Techtoon.UI" -ForegroundColor Green
Write-Host '========================================' -ForegroundColor Cyan