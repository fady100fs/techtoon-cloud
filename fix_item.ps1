// =====================================================================
// InvoiceEditViewModel.cs  —  Techtoon Accounting C+
// الإصدار المصحح: بحث فوري ذكي + دفع متعدد الطرق
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
    //  صنف يساعد على عرض صف البند داخل شبكة الفاتورة
    //  يحتوي على بحث فوري: عند كتابة أي حرف أو رقم يظهر ما يقاربه
    // ==================================================================
    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;

        public InvoiceLineItem(List<ItemSearchResult> allItems)
        {
            _allItems = allItems ?? new List<ItemSearchResult>();
            FilteredItems = new ObservableCollection<ItemSearchResult>();
        }

        // ---- البحث الفوري ----
        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value ?? "";
                OnPropertyChanged("SearchText");
                ApplySearch();
            }
        }

        private void ApplySearch()
        {
            string q = (_searchText ?? "").Trim();
            FilteredItems.Clear();
            if (q.Length == 0)
            {
                IsPopupOpen = false;
                return;
            }

            // ابحث بالاسم أو الباركود أو SKU
            var matches = _allItems
                .Where(it =>
                    (it.Name != null && it.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Barcode != null && it.Barcode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (it.Sku != null && it.Sku.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                .Take(15)
                .ToList();

            foreach (var m in matches) FilteredItems.Add(m);
            IsPopupOpen = matches.Count > 0;
            SearchSelectedIndex = matches.Count > 0 ? 0 : -1;
        }

        public ObservableCollection<ItemSearchResult> FilteredItems { get; set; }

        private int _searchSelectedIndex = -1;
        public int SearchSelectedIndex
        {
            get { return _searchSelectedIndex; }
            set { _searchSelectedIndex = value; OnPropertyChanged("SearchSelectedIndex"); }
        }

        private bool _isPopupOpen = false;
        public bool IsPopupOpen
        {
            get { return _isPopupOpen; }
            set { _isPopupOpen = value; OnPropertyChanged("IsPopupOpen"); }
        }

        // ---- بيانات البند المحدد ----
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
            set
            {
                _quantity = value;
                OnPropertyChanged("Quantity");
                Recalc();
            }
        }

        private decimal _price = 0;
        public decimal Price
        {
            get { return _price; }
            set
            {
                _price = value;
                OnPropertyChanged("Price");
                Recalc();
            }
        }

        private decimal _discount = 0;
        public decimal Discount
        {
            get { return _discount; }
            set
            {
                _discount = value;
                OnPropertyChanged("Discount");
                Recalc();
            }
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

        // ---- اختيار صنف من نتائج البحث ----
        public void SelectItem(ItemSearchResult it)
        {
            if (it == null) return;
            ItemId = it.Id;
            ItemName = it.Name;
            Barcode = it.Barcode;
            Unit = it.Unit;
            Price = it.SalePrice;
            StockAvailable = it.CurrentStock;
            Quantity = 1;
            IsPopupOpen = false;
            SearchText = it.Name;
            OnPropertyChanged("HasItem");
        }

        public void SelectByIndex()
        {
            if (SearchSelectedIndex >= 0 && SearchSelectedIndex < FilteredItems.Count)
            {
                SelectItem(FilteredItems[SearchSelectedIndex]);
            }
        }
    }

    // ==================================================================
    //  صف الدفع - لطرق الدفع المتعددة في الفاتورة
    // ==================================================================
    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }

        private string _method = "cash";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); OnPropertyChanged("MethodDisplay"); }
        }

        public string MethodDisplay
        {
            get
            {
                switch ((_method ?? "").ToLowerInvariant())
                {
                    case "cash": return "نقدي";
                    case "card": return "بطاقة";
                    case "bank": return "تحويل بنكي";
                    case "check": return "شيك";
                    case "credit": return "آجل";
                    default: return _method;
                }
            }
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

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }
    }

    // ==================================================================
    //  صنف مبسط للأصناف
    // ==================================================================
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public string Sku { get; set; }
        public string Unit { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal CurrentStock { get; set; }
        public decimal TaxRate { get; set; }
        public bool IsActive { get; set; }

        public string SearchDisplay
        {
            get
            {
                string b = string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]";
                string s = string.IsNullOrEmpty(Sku) ? "" : " (" + Sku + ")";
                string stk = CurrentStock > 0 ? "  مخزون: " + CurrentStock.ToString("0.##") : "";
                return (Name ?? "") + b + s + stk;
            }
        }
    }

    // ==================================================================
    //  ViewModel الرئيسي لنافذة تعديل الفاتورة
    // ==================================================================
    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;

        // ---- معلومات الفاتورة ----
        public int Id { get; set; }
        public bool DialogResult { get; set; }
        public bool PrintAfterSave { get; set; }

        private string _invoiceType = "sales";
        public string InvoiceType
        {
            get { return _invoiceType; }
            set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); }
        }

        public string InvoiceTypeDisplay
        {
            get
            {
                return (_invoiceType ?? "") == "purchase" ? "مشتريات" : "مبيعات";
            }
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

        private string _partyName = "";
        public string PartyName
        {
            get { return _partyName; }
            set { _partyName = value; OnPropertyChanged("PartyName"); }
        }

        private int _warehouseId = 0;
        public int WarehouseId
        {
            get { return _warehouseId; }
            set { _warehouseId = value; OnPropertyChanged("WarehouseId"); }
        }

        private string _warehouseName = "";
        public string WarehouseName
        {
            get { return _warehouseName; }
            set { _warehouseName = value; OnPropertyChanged("WarehouseName"); }
        }

        private string _notes = "";
        public string Notes
        {
            get { return _notes; }
            set { _notes = value; OnPropertyChanged("Notes"); }
        }

        // ---- وضع الدفع ----
        private string _paymentMode = "full";
        public string PaymentMode
        {
            get { return _paymentMode; }
            set
            {
                _paymentMode = value;
                OnPropertyChanged("PaymentMode");
                OnPropertyChanged("ShowPaymentsSection");
                OnPaymentModeChanged();
            }
        }

        // قسم الدفع متعدد الطرق يظهر دائماً
        public bool ShowPaymentsSection
        {
            get { return true; }
        }

        // ---- بنود الفاتورة ----
        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<ItemSearchResult> AllItemsList { get; set; }
        private List<ItemSearchResult> _allItems;

        // ---- قائمة الأطراف والمخازن ----
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }

        // ---- الدفعات المتعددة ----
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }

        // ---- الحسابات ----
        private decimal _subtotal = 0;
        public decimal Subtotal
        {
            get { return _subtotal; }
            set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); }
        }

        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }

        private decimal _discountAmount = 0;
        public decimal DiscountAmount
        {
            get { return _discountAmount; }
            set
            {
                _discountAmount = value;
                OnPropertyChanged("DiscountAmount");
                RefreshTotals();
            }
        }

        private decimal _discountPercent = 0;
        public decimal DiscountPercent
        {
            get { return _discountPercent; }
            set
            {
                _discountPercent = value;
                OnPropertyChanged("DiscountPercent");
                // احسب خصم مبلغ من النسبة
                if (_discountPercent > 0)
                {
                    DiscountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
                }
            }
        }

        private decimal _taxAmount = 0;
        public decimal TaxAmount
        {
            get { return _taxAmount; }
            set { _taxAmount = value; OnPropertyChanged("TaxAmount"); OnPropertyChanged("TaxDisplay"); }
        }

        public string TaxDisplay { get { return _taxAmount.ToString("N2"); } }

        private decimal _total = 0;
        public decimal Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalDisplay { get { return _total.ToString("N2"); } }

        private decimal _totalPaid = 0;
        public decimal TotalPaid
        {
            get { return _totalPaid; }
            set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); }
        }

        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }

        public decimal Remaining
        {
            get { return Math.Round(_total - _totalPaid, 2); }
        }

        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; OnPropertyChanged("StatusMessage"); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy"); }
        }

        // ---- الأوامر ----
        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand SaveAndPrintCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        // ==================================================================
        public InvoiceEditViewModel()
        {
            Lines = new ObservableCollection<InvoiceLineItem>();
            AllItemsList = new ObservableCollection<ItemSearchResult>();
            _allItems = new List<ItemSearchResult>();
            Parties = new ObservableCollection<PartyOption>();
            Warehouses = new ObservableCollection<WarehouseOption>();
            Payments = new ObservableCollection<InvoicePaymentRow>();

            AddLineCommand = new UiRelayCommand(_ => AddLine());
            DeleteLineCommand = new UiRelayCommand(p =>
            {
                if (p is InvoiceLineItem li) Lines.Remove(li);
                RefreshTotals();
            });
            AddPaymentCommand = new UiRelayCommand(_ => AddPayment());
            DeletePaymentCommand = new UiRelayCommand(p =>
            {
                if (p is InvoicePaymentRow pr) Payments.Remove(pr);
                RefreshTotals();
            });

            // تحديث الإجماليات فور تغيير أي مبلغ دفع
            Payments.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (InvoicePaymentRow r in e.NewItems)
                    {
                        r.PropertyChanged += (s2, e2) =>
                        {
                            if (e2.PropertyName == "Amount" || e2.PropertyName == "Method")
                                RefreshTotals();
                        };
                    }
                }
            };
            SaveCommand = new UiRelayCommand(_ => { PrintAfterSave = false; _ = SaveAsync(); }, _ => !IsBusy);
            SaveAndPrintCommand = new UiRelayCommand(_ => { PrintAfterSave = true; _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });
        }

        // ==================================================================
        //  اتصال قاعدة البيانات
        // ==================================================================
        public static string GetConnectionString()
        {
            try
            {
                string env = Environment.GetEnvironmentVariable("TECHTOON_PG_CONNECTION");
                if (!string.IsNullOrWhiteSpace(env) && env.Contains("Host=")) return env;
            }
            catch { }

            // القيمة الافتراضية (Neon PostgreSQL) - تُستخدم كأساس دائماً
            string defaultCs = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true";

            // اقرأ من appsettings.json باستخدام JSON parser صحيح
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] paths = new string[]
                {
                    Path.Combine(baseDir, "appsettings.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "appsettings.json"),
                    Path.Combine(baseDir, "..", "..", "..", "..", "Techtoon.UI", "appsettings.json"),
                };
                foreach (string path in paths)
                {
                    if (!File.Exists(path)) continue;
                    string json = File.ReadAllText(path);
                    string cs = ExtractConnectionStringFromJson(json);
                    // تحقق من صحة السلسلة قبل إرجاعها
                    if (!string.IsNullOrWhiteSpace(cs) && cs.Contains("Host=") && cs.Contains("Database="))
                        return cs;
                }
            }
            catch { }

            // ارجع القيمة الافتراضية الآمنة
            return defaultCs;
        }

        // ==================================================================
        //  استخراج سلسلة الاتصال من JSON بشكل صحيح
        // ==================================================================
        private static string ExtractConnectionStringFromJson(string json)
        {
            // الطريقة 1: استخدام System.Text.Json (مضمن في .NET 10)
            try
            {
                using (var doc = System.Text.Json.JsonDocument.Parse(json))
                {
                    string result = ExtractFromJsonElement(doc.RootElement);
                    if (!string.IsNullOrEmpty(result)) return result;
                }
            }
            catch { }

            // الطريقة 2: regex احتياطية - تبحث عن أي نص يحتوي Host=
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(json, @"""([^""\\]*(?:\\.[^""\\]*)*Host=[^""\\]*(?:\\.[^""\\]*)*)""");
                if (match.Success) return match.Groups[1].Value.Replace("\\\"", "\"");
            }
            catch { }

            return null;
        }

        private static string ExtractFromJsonElement(System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    string name = prop.Name.ToLowerInvariant();
                    // إذا كان المفتاح يحتوي connection وقيمته نص يحتوي Host=
                    if ((name.Contains("connection") || name == "db" || name == "database" || name == "postgres" || name == "neon")
                        && prop.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        string val = prop.Value.GetString();
                        if (!string.IsNullOrEmpty(val) && val.Contains("Host="))
                            return val;
                    }
                    // ابحث في القيم المتداخلة
                    var nested = ExtractFromJsonElement(prop.Value);
                    if (nested != null) return nested;
                }
            }
            else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var nested = ExtractFromJsonElement(item);
                    if (nested != null) return nested;
                }
            }
            return null;
        }

        // ==================================================================
        //  تحميل البيانات الأولية
        // ==================================================================
        public new async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            try
            {
                _allItems = LoadItems();
                AllItemsList.Clear();
                foreach (var it in _allItems) AllItemsList.Add(it);

                LoadParties();
                LoadWarehouses();

                // رقم تسلسلي جديد
                SerialNumber = GenerateSerial(InvoiceType);

                // صف بند فارغ
                AddLine();

                // دفع افتراضي
                if (Payments.Count == 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0, PaymentDate = DateTime.Now });
                }

                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            string cs = GetConnectionString();
            using (var conn = new NpgsqlConnection(cs))
            {
                conn.Open();
                // تحقق من وجود الجدول
                string table = FindTable(conn, new string[] { "items", "products", "inventory_items" });
                if (string.IsNullOrEmpty(table)) return list;

                string nameCol = FindColumn(conn, table, new string[] { "name", "item_name", "title", "description" });
                string barCol = FindColumn(conn, table, new string[] { "barcode", "bar_code", "code", "ean" });
                string skuCol = FindColumn(conn, table, new string[] { "sku", "item_code", "code" });
                string priceCol = FindColumn(conn, table, new string[] { "sale_price", "selling_price", "price", "unit_price" });
                string unitCol = FindColumn(conn, table, new string[] { "unit", "uom" });
                string stockCol = FindColumn(conn, table, new string[] { "current_stock", "stock", "quantity", "qty_on_hand" });

                string sql = "SELECT id";
                if (!string.IsNullOrEmpty(nameCol)) sql += ", \"" + nameCol + "\"";
                if (!string.IsNullOrEmpty(barCol)) sql += ", \"" + barCol + "\"";
                if (!string.IsNullOrEmpty(skuCol)) sql += ", \"" + skuCol + "\"";
                if (!string.IsNullOrEmpty(priceCol)) sql += ", \"" + priceCol + "\"";
                if (!string.IsNullOrEmpty(unitCol)) sql += ", \"" + unitCol + "\"";
                sql += " FROM \"" + table + "\"";
                string where = " WHERE ";
                if (!string.IsNullOrEmpty(nameCol)) where += "\"" + nameCol + "\" IS NOT NULL";
                else where += "1=1";
                // فلترة الأصناف النشطة إن وجد العمود
                string actCol = FindColumn(conn, table, new string[] { "is_active", "active", "enabled" });
                if (!string.IsNullOrEmpty(actCol)) where += " AND (\"" + actCol + "\" IS NULL OR \"" + actCol + "\" = true)";
                sql += where + " ORDER BY ";
                sql += !string.IsNullOrEmpty(nameCol) ? "\"" + nameCol + "\"" : "id";
                sql += " LIMIT 500";

                using (var cmd = new NpgsqlCommand(sql, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        var it = new ItemSearchResult();
                        it.Id = r.GetInt32(0);
                        int col = 1;
                        if (!string.IsNullOrEmpty(nameCol)) { it.Name = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(barCol)) { it.Barcode = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(skuCol)) { it.Sku = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        if (!string.IsNullOrEmpty(priceCol)) { it.SalePrice = r.IsDBNull(col) ? 0 : (decimal)r.GetValue(col); col++; }
                        if (!string.IsNullOrEmpty(unitCol)) { it.Unit = r.IsDBNull(col) ? "" : r.GetString(col); col++; }
                        list.Add(it);
                    }
                }
            }
            return list;
        }

        private void LoadParties()
        {
            Parties.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "parties", "customers_suppliers", "contacts", "accounts" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "party_name", "title", "account_name" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 500";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "warehouses", "stores", "locations" });
                    if (string.IsNullOrEmpty(table)) return;
                    string nameCol = FindColumn(conn, table, new string[] { "name", "warehouse_name", "title" });
                    if (string.IsNullOrEmpty(nameCol)) return;
                    string sql = "SELECT id, \"" + nameCol + "\" FROM \"" + table + "\" ORDER BY \"" + nameCol + "\" LIMIT 100";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1) });
                        }
                    }
                }
            }
            catch { }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) return "";
                    string serialCol = FindColumn(conn, table, new string[] { "serial_number", "invoice_number", "number", "code" });
                    string typeCol = FindColumn(conn, table, new string[] { "invoice_type", "type", "kind" });
                    string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                    int year = DateTime.Now.Year;
                    string likePattern = prefix + "-" + year + "-%";
                    string sql;
                    if (!string.IsNullOrEmpty(serialCol))
                    {
                        if (!string.IsNullOrEmpty(typeCol))
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY \"" + serialCol + "\" DESC LIMIT 1";
                        }
                        else
                        {
                            sql = "SELECT \"" + serialCol + "\" FROM \"" + table + "\" WHERE \"" + serialCol + "\" LIKE @p ORDER BY id DESC LIMIT 1";
                        }
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
            }
            catch { }
            string pf = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
            return pf + "-" + DateTime.Now.Year + "-0001";
        }

        // ==================================================================
        //  أوامر البنود والدفعات
        // ==================================================================
        public void AddLine()
        {
            var line = new InvoiceLineItem(_allItems);
            line.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "Quantity" || e.PropertyName == "Price" ||
                    e.PropertyName == "Discount" || e.PropertyName == "Total")
                {
                    RefreshTotals();
                }
            };
            Lines.Add(line);
            RefreshTotals();
        }

        public void AddPayment()
        {
            decimal remaining = Remaining;
            Payments.Add(new InvoicePaymentRow
            {
                Method = "cash",
                Amount = remaining > 0 ? remaining : 0,
                PaymentDate = DateTime.Now
            });
            RefreshTotals();
        }

        private void OnPaymentModeChanged()
        {
            // عند تغيير وضع الدفع
            if (_paymentMode == "full" && Total > 0 && Payments.Count == 0)
            {
                Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                RefreshTotals();
            }
            else if (_paymentMode == "none")
            {
                Payments.Clear();
                RefreshTotals();
            }
        }

        // ==================================================================
        //  إعادة حساب الإجماليات
        // ==================================================================
        public void RefreshTotals()
        {
            decimal sub = 0;
            foreach (var line in Lines)
            {
                sub += line.Total;
            }
            Subtotal = Math.Round(sub, 2);

            // إذا كان خصم بالنسبة، أعد حسابه
            if (_discountPercent > 0 && _discountAmount == 0)
            {
                _discountAmount = Math.Round(_subtotal * _discountPercent / 100m, 2);
            }

            decimal afterDiscount = _subtotal - _discountAmount;
            TaxAmount = Math.Round(afterDiscount * 0m, 2); // الضريب=0 حالياً
            Total = Math.Round(afterDiscount + _taxAmount, 2);

            // مجموع المدفوع
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
            StatusMessage = "جاري تحميل الفاتورة...";
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table)) { StatusMessage = "جدول الفواتير غير موجود"; return; }

                    string[] cols = GetColumns(conn, table);
                    string selCols = "";
                    foreach (string c in new string[] { "id", "serial_number", "invoice_number", "number", "invoice_date", "date", "invoice_type", "type", "party_id", "warehouse_id", "notes", "subtotal", "discount_amount", "tax_amount", "total", "paid_amount" })
                    {
                        if (cols.Contains(c)) selCols += "\"" + c + "\", ";
                    }
                    selCols = selCols.TrimEnd(',', ' ');
                    if (selCols.Length == 0) { StatusMessage = "لا توجد أعمدة"; return; }

                    string sql = "SELECT " + selCols + " FROM \"" + table + "\" WHERE id = @id";
                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("id", invoiceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                int col = 0;
                                for (int i = 0; i < r.FieldCount; i++)
                                {
                                    string fname = r.GetName(i).ToLowerInvariant();
                                    object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                    switch (fname)
                                    {
                                        case "id": Id = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "serial_number":
                                        case "invoice_number":
                                        case "number": SerialNumber = val == null ? "" : val.ToString(); break;
                                        case "invoice_date":
                                        case "date": InvoiceDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                        case "invoice_type":
                                        case "type": InvoiceType = val == null ? "sales" : val.ToString(); break;
                                        case "party_id": PartyId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "warehouse_id": WarehouseId = val == null ? 0 : Convert.ToInt32(val); break;
                                        case "notes": Notes = val == null ? "" : val.ToString(); break;
                                        case "subtotal": Subtotal = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "discount_amount": DiscountAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "tax_amount": TaxAmount = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "total": Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                        case "paid_amount": TotalPaid = val == null ? 0 : Convert.ToDecimal(val); break;
                                    }
                                }
                            }
                        }
                    }

                    // تحميل البنود
                    Lines.Clear();
                    string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
                    if (!string.IsNullOrEmpty(lineTable))
                    {
                        string lc = "SELECT * FROM \"" + lineTable + "\" WHERE invoice_id = @id";
                        try
                        {
                            using (var cmd = new NpgsqlCommand(lc, conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var line = new InvoiceLineItem(_allItems);
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "item_id": line.ItemId = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "item_name":
                                                case "name": line.ItemName = val == null ? "" : val.ToString(); break;
                                                case "barcode": line.Barcode = val == null ? "" : val.ToString(); break;
                                                case "unit": line.Unit = val == null ? "" : val.ToString(); break;
                                                case "quantity":
                                                case "qty": line.Quantity = val == null ? 1 : Convert.ToDecimal(val); break;
                                                case "price":
                                                case "unit_price": line.Price = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "discount": line.Discount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "total":
                                                case "line_total": line.Total = val == null ? 0 : Convert.ToDecimal(val); break;
                                            }
                                        }
                                        Lines.Add(line);
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // تحميل الدفعات
                    Payments.Clear();
                    string payTable = FindTable(conn, new string[] { "invoice_payments" });
                    if (!string.IsNullOrEmpty(payTable))
                    {
                        try
                        {
                            using (var cmd = new NpgsqlCommand("SELECT * FROM \"" + payTable + "\" WHERE invoice_id = @id ORDER BY id", conn))
                            {
                                cmd.Parameters.AddWithValue("id", invoiceId);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        var row = new InvoicePaymentRow();
                                        for (int i = 0; i < r.FieldCount; i++)
                                        {
                                            string fn = r.GetName(i).ToLowerInvariant();
                                            object val = r.IsDBNull(i) ? null : r.GetValue(i);
                                            switch (fn)
                                            {
                                                case "id": row.Id = val == null ? 0 : Convert.ToInt32(val); break;
                                                case "method": row.Method = val == null ? "cash" : val.ToString(); break;
                                                case "amount": row.Amount = val == null ? 0 : Convert.ToDecimal(val); break;
                                                case "reference": row.Reference = val == null ? "" : val.ToString(); break;
                                                case "payment_date":
                                                case "date": row.PaymentDate = val == null ? DateTime.Now : Convert.ToDateTime(val); break;
                                                case "notes": row.Notes = val == null ? "" : val.ToString(); break;
                                            }
                                        }
                                        Payments.Add(row);
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (Lines.Count == 0) AddLine();
                if (Payments.Count == 0 && Total > 0)
                {
                    Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = Total, PaymentDate = DateTime.Now });
                }
                RefreshTotals();
                StatusMessage = "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        // ==================================================================
        //  حفظ الفاتورة
        // ==================================================================
        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري الحفظ...";
            try
            {
                RefreshTotals();

                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();

                    string table = FindTable(conn, new string[] { "invoices" });
                    if (string.IsNullOrEmpty(table))
                    {
                        // أنشئ الجدول إن لم يكن موجوداً
                        CreateInvoicesTable(conn);
                        table = "invoices";
                    }

                    string[] cols = GetColumns(conn, table);
                    if (Id > 0)
                    {
                        // تحديث
                        string setParts = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddSet(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                setParts += "\"" + col + "\" = @" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddSet("serial_number", "sn", SerialNumber);
                        AddSet("invoice_date", "dt", InvoiceDate);
                        AddSet("invoice_type", "tp", InvoiceType);
                        AddSet("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddSet("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddSet("notes", "nt", Notes);
                        AddSet("subtotal", "st", Subtotal);
                        AddSet("discount_amount", "da", DiscountAmount);
                        AddSet("tax_amount", "tx", TaxAmount);
                        AddSet("total", "tt", Total);
                        AddSet("paid_amount", "pa", TotalPaid);
                        AddSet("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddSet("updated_at", "ua", DateTime.Now);

                        setParts = setParts.TrimEnd(',', ' ');
                        if (setParts.Length > 0)
                        {
                            string sql = "UPDATE \"" + table + "\" SET " + setParts + " WHERE id = @id";
                            using (var cmd = new NpgsqlCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("id", Id);
                                foreach (var kv in paramDict)
                                {
                                    cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    else
                    {
                        // إدخال جديد
                        string colNames = "";
                        string paramNames = "";
                        var paramDict = new Dictionary<string, object>();
                        void AddInsert(string col, string param, object val)
                        {
                            if (cols.Contains(col))
                            {
                                colNames += "\"" + col + "\", ";
                                paramNames += "@" + param + ", ";
                                paramDict[param] = val ?? DBNull.Value;
                            }
                        }
                        AddInsert("serial_number", "sn", SerialNumber);
                        AddInsert("invoice_date", "dt", InvoiceDate);
                        AddInsert("invoice_type", "tp", InvoiceType);
                        AddInsert("party_id", "pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                        AddInsert("warehouse_id", "wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                        AddInsert("notes", "nt", Notes);
                        AddInsert("subtotal", "st", Subtotal);
                        AddInsert("discount_amount", "da", DiscountAmount);
                        AddInsert("tax_amount", "tx", TaxAmount);
                        AddInsert("total", "tt", Total);
                        AddInsert("paid_amount", "pa", TotalPaid);
                        AddInsert("status", "status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                        AddInsert("created_at", "ca", DateTime.Now);
                        AddInsert("updated_at", "ua", DateTime.Now);

                        colNames = colNames.TrimEnd(',', ' ');
                        paramNames = paramNames.TrimEnd(',', ' ');
                        string sql = "INSERT INTO \"" + table + "\" (" + colNames + ") VALUES (" + paramNames + ") RETURNING id";
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            foreach (var kv in paramDict)
                            {
                                cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                            }
                            var o = cmd.ExecuteScalar();
                            if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }

                    // احفظ البنود
                    SaveLines(conn, Id);
                    // احفظ الدفعات
                    SavePayments(conn, Id);
                }

                StatusMessage = "تم الحفظ بنجاح - رقم الفاتورة: " + SerialNumber;
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ عند الحفظ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void SaveLines(NpgsqlConnection conn, int invoiceId)
        {
            string lineTable = FindTable(conn, new string[] { "invoice_lines", "invoice_items", "invoice_details" });
            if (string.IsNullOrEmpty(lineTable))
            {
                CreateInvoiceLinesTable(conn);
                lineTable = "invoice_lines";
            }

            // احذف البنود القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + lineTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, lineTable);
            foreach (var line in Lines)
            {
                if (line.ItemId <= 0 && string.IsNullOrEmpty(line.ItemName)) continue;
                string colNames = "invoice_id";
                string paramNames = "@inv";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("item_id", "iid", line.ItemId > 0 ? (object)line.ItemId : DBNull.Value);
                AddCol("item_name", "inm", line.ItemName);
                AddCol("barcode", "bc", line.Barcode);
                AddCol("unit", "un", line.Unit);
                AddCol("quantity", "qty", line.Quantity);
                AddCol("price", "prc", line.Price);
                AddCol("discount", "dsc", line.Discount);
                AddCol("total", "tot", line.Total);

                string sql = "INSERT INTO \"" + lineTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void SavePayments(NpgsqlConnection conn, int invoiceId)
        {
            string payTable = FindTable(conn, new string[] { "invoice_payments" });
            if (string.IsNullOrEmpty(payTable))
            {
                CreateInvoicePaymentsTable(conn);
                payTable = "invoice_payments";
            }

            // احذف الدفعات القديمة
            using (var cmd = new NpgsqlCommand("DELETE FROM \"" + payTable + "\" WHERE invoice_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("id", invoiceId);
                cmd.ExecuteNonQuery();
            }

            string[] cols = GetColumns(conn, payTable);
            foreach (var pay in Payments)
            {
                if (pay.Amount <= 0) continue;
                string colNames = "invoice_id, method, amount";
                string paramNames = "@inv, @mth, @amt";
                var paramDict = new Dictionary<string, object>();
                paramDict["inv"] = invoiceId;
                paramDict["mth"] = pay.Method;
                paramDict["amt"] = pay.Amount;

                void AddCol(string col, string param, object val)
                {
                    if (cols.Contains(col))
                    {
                        colNames += ", \"" + col + "\"";
                        paramNames += ", @" + param;
                        paramDict[param] = val ?? DBNull.Value;
                    }
                }
                AddCol("reference", "ref", pay.Reference);
                AddCol("payment_date", "pdt", pay.PaymentDate);
                AddCol("notes", "nt", pay.Notes);

                string sql = "INSERT INTO \"" + payTable + "\" (" + colNames + ") VALUES (" + paramNames + ")";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    foreach (var kv in paramDict) cmd.Parameters.AddWithValue(kv.Key, kv.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ==================================================================
        //  دوال مساعدة
        // ==================================================================
        private static string FindTable(NpgsqlConnection conn, string[] candidates)
        {
            using (var cmd = new NpgsqlCommand("select table_name from information_schema.tables where table_schema = current_schema()", conn))
            using (var r = cmd.ExecuteReader())
            {
                var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (r.Read()) tables.Add(r.GetString(0));
                r.Close();
                foreach (string c in candidates)
                {
                    if (tables.Contains(c)) return c;
                }
                // بحث جزئي
                foreach (string c in candidates)
                {
                    foreach (string t in tables)
                    {
                        if (t.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return t;
                    }
                }
            }
            return null;
        }

        private static string FindColumn(NpgsqlConnection conn, string table, string[] candidates)
        {
            string[] cols = GetColumns(conn, table);
            foreach (string c in candidates)
            {
                if (cols.Contains(c, StringComparer.OrdinalIgnoreCase)) return c;
            }
            foreach (string c in candidates)
            {
                foreach (string col in cols)
                {
                    if (col.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0) return col;
                }
            }
            return null;
        }

        private static string[] GetColumns(NpgsqlConnection conn, string table)
        {
            var list = new List<string>();
            using (var cmd = new NpgsqlCommand("select lower(column_name) from information_schema.columns where table_schema = current_schema() and lower(table_name) = @t", conn))
            {
                cmd.Parameters.AddWithValue("t", table.ToLowerInvariant());
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read()) list.Add(r.GetString(0));
                }
            }
            return list.ToArray();
        }

        private static void CreateInvoicesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoices (
                id SERIAL PRIMARY KEY,
                serial_number TEXT,
                invoice_date TIMESTAMP DEFAULT NOW(),
                invoice_type TEXT DEFAULT 'sales',
                party_id INTEGER,
                warehouse_id INTEGER,
                cash_box_id INTEGER,
                notes TEXT,
                subtotal NUMERIC(14,2) DEFAULT 0,
                discount_amount NUMERIC(14,2) DEFAULT 0,
                tax_amount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0,
                paid_amount NUMERIC(14,2) DEFAULT 0,
                status TEXT DEFAULT 'draft',
                created_at TIMESTAMP DEFAULT NOW(),
                updated_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoiceLinesTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_lines (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                item_id INTEGER,
                item_name TEXT,
                barcode TEXT,
                unit TEXT,
                quantity NUMERIC(14,3) DEFAULT 0,
                price NUMERIC(14,2) DEFAULT 0,
                discount NUMERIC(14,2) DEFAULT 0,
                total NUMERIC(14,2) DEFAULT 0
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }

        private static void CreateInvoicePaymentsTable(NpgsqlConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS invoice_payments (
                id SERIAL PRIMARY KEY,
                invoice_id INTEGER NOT NULL,
                method TEXT NOT NULL DEFAULT 'cash',
                amount NUMERIC(14,2) NOT NULL DEFAULT 0,
                reference TEXT,
                cash_box_id INTEGER,
                bank_account_id INTEGER,
                payment_date TIMESTAMP DEFAULT NOW(),
                notes TEXT,
                created_at TIMESTAMP DEFAULT NOW()
            )";
            using (var cmd = new NpgsqlCommand(sql, conn)) cmd.ExecuteNonQuery();
        }
    }

    // ==================================================================
    //  أصناف مساعدة للقوائم المنسدلة
    // ==================================================================
    public class PartyOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }

    public class WarehouseOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() { return Name ?? ""; }
    }
}
