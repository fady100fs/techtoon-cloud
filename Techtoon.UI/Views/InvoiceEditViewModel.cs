using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public class ItemSearchResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SellPrice { get; set; }
        public string SearchDisplay { get { return (Name ?? "") + (string.IsNullOrEmpty(Barcode) ? "" : " [" + Barcode + "]"); } }
    }

    public class InvoiceLineItem : ViewModelBase
    {
        private List<ItemSearchResult> _allItems;
        public InvoiceLineItem(List<ItemSearchResult> allItems) { _allItems = allItems ?? new List<ItemSearchResult>(); }

        private string _searchText = "";
        public string SearchText
        {
            get { return _searchText; }
            set { _searchText = value; OnPropertyChanged("SearchText"); OnPropertyChanged("FilteredItems"); }
        }

        public List<ItemSearchResult> FilteredItems
        {
            get
            {
                string q = (_searchText ?? "").Trim().ToLower();
                if (q.Length == 0) return _allItems.Take(50).ToList();
                return _allItems.Where(it => (it.Name ?? "").ToLower().Contains(q) || (it.Barcode ?? "").ToLower().Contains(q)).Take(20).ToList();
            }
        }

        public int ItemId { get; set; }
        private string _itemName = ""; public string ItemName { get { return _itemName; } set { _itemName = value; OnPropertyChanged("ItemName"); } }
        private string _barcode = ""; public string Barcode { get { return _barcode; } set { _barcode = value; OnPropertyChanged("Barcode"); } }
        private decimal _quantity = 1; public decimal Quantity { get { return _quantity; } set { _quantity = value; OnPropertyChanged("Quantity"); Recalc(); } }
        private decimal _price = 0; public decimal Price { get { return _price; } set { _price = value; OnPropertyChanged("Price"); Recalc(); } }
        private decimal _discount = 0; public decimal Discount { get { return _discount; } set { _discount = value; OnPropertyChanged("Discount"); Recalc(); } }
        private decimal _total = 0; public decimal Total { get { return _total; } set { _total = value; OnPropertyChanged("Total"); } }
        public void Recalc() { Total = Math.Round(_quantity * _price - _discount, 2); }
        public void SelectItem(ItemSearchResult it) { if (it == null) return; ItemId = it.Id; ItemName = it.Name; Barcode = it.Barcode; Price = it.SellPrice; Quantity = 1; SearchText = it.Name; OnPropertyChanged("FilteredItems"); }
    }

    public class InvoicePaymentRow : ViewModelBase
    {
        public int Id { get; set; }
        private string _method = "cash"; public string Method { get { return _method; } set { _method = value; OnPropertyChanged("Method"); } }
        private decimal _amount = 0; public decimal Amount { get { return _amount; } set { _amount = value; OnPropertyChanged("Amount"); } }
        private string _reference = ""; public string Reference { get { return _reference; } set { _reference = value; OnPropertyChanged("Reference"); } }
        private DateTime _paymentDate = DateTime.Now; public DateTime PaymentDate { get { return _paymentDate; } set { _paymentDate = value; OnPropertyChanged("PaymentDate"); } }
    }

    public class PartyOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() { return Name ?? ""; } }
    public class WarehouseOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() { return Name ?? ""; } }

    public class InvoiceEditViewModel : ViewModelBase
    {
        public Action RequestClose;
        public int Id { get; set; }
        public bool DialogResult { get; set; }

        private string _invoiceType = "sales";
        public string InvoiceType { get { return _invoiceType; } set { _invoiceType = value; OnPropertyChanged("InvoiceType"); OnPropertyChanged("InvoiceTypeDisplay"); } }
        public string InvoiceTypeDisplay { get { return (_invoiceType ?? "") == "purchase" ? "Purchase" : "Sales"; } }

        private string _serialNumber = "";
        public string SerialNumber { get { return _serialNumber; } set { _serialNumber = value; OnPropertyChanged("SerialNumber"); } }
        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate { get { return _invoiceDate; } set { _invoiceDate = value; OnPropertyChanged("InvoiceDate"); } }
        private int _partyId = 0; public int PartyId { get { return _partyId; } set { _partyId = value; OnPropertyChanged("PartyId"); } }
        private int _warehouseId = 0; public int WarehouseId { get { return _warehouseId; } set { _warehouseId = value; OnPropertyChanged("WarehouseId"); } }

        public ObservableCollection<InvoiceLineItem> Lines { get; set; }
        public ObservableCollection<InvoicePaymentRow> Payments { get; set; }
        public ObservableCollection<PartyOption> Parties { get; set; }
        public ObservableCollection<WarehouseOption> Warehouses { get; set; }
        private List<ItemSearchResult> _allItems;

        private decimal _subtotal = 0; public decimal Subtotal { get { return _subtotal; } set { _subtotal = value; OnPropertyChanged("Subtotal"); OnPropertyChanged("SubtotalDisplay"); } }
        public string SubtotalDisplay { get { return _subtotal.ToString("N2"); } }
        private decimal _discountAmount = 0;
        public decimal DiscountAmount { get { return _discountAmount; } set { _discountAmount = value; OnPropertyChanged("DiscountAmount"); RefreshTotals(); } }
        private decimal _total = 0; public decimal Total { get { return _total; } set { _total = value; OnPropertyChanged("Total"); OnPropertyChanged("TotalDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalDisplay { get { return _total.ToString("N2"); } }
        private decimal _totalPaid = 0; public decimal TotalPaid { get { return _totalPaid; } set { _totalPaid = value; OnPropertyChanged("TotalPaid"); OnPropertyChanged("TotalPaidDisplay"); OnPropertyChanged("Remaining"); } }
        public string TotalPaidDisplay { get { return _totalPaid.ToString("N2"); } }
        public decimal Remaining { get { return Math.Round(_total - _totalPaid, 2); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }

        private string _statusMessage = ""; public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }
        private bool _isBusy = false;
        private bool _isSaving = false; public bool IsBusy { get { return _isBusy; } set { _isBusy = value; OnPropertyChanged("IsBusy"); } }

        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand PayFullCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand PrintA4Command { get; set; }
        public ICommand PrintThermal58Command { get; set; }
        public ICommand PrintThermal80Command { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand SaveDraftCommand { get; set; }
        public ICommand CancelInvoiceCommand { get; set; }
        public ICommand AddByBarcodeCommand { get; set; }

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
            PayFullCommand = new UiRelayCommand(_ => PayFull());
            SaveCommand = new UiRelayCommand(_ => { _ = SaveAsync(); }, _ => !IsBusy);
            PrintA4Command = new UiRelayCommand(_ => PrintA4(), _ => Id > 0 && !IsBusy);
            PrintThermal58Command = new UiRelayCommand(_ => PrintThermal58(), _ => Id > 0 && !IsBusy);
            PrintThermal80Command = new UiRelayCommand(_ => PrintThermal80(), _ => Id > 0 && !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });
            SaveDraftCommand = new UiRelayCommand(_ => { _ = SaveAsDraftAsync(); }, _ => !IsBusy);
            CancelInvoiceCommand = new UiRelayCommand(_ => { _ = CancelInvoiceAsync(); }, _ => Id > 0 && !IsBusy);
            AddByBarcodeCommand = new UiRelayCommand(p => { _ = AddByBarcodeAsync(p?.ToString() ?? ""); });
        }

        public static string GetConnectionString() { return "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"; }

        public async System.Threading.Tasks.Task LoadAsync()
        {
            IsBusy = true;
            try
            {
                _allItems = LoadItems();
                LoadParties(); LoadWarehouses();
                SerialNumber = GenerateSerial(InvoiceType);
                AddLine();
                if (Payments.Count == 0) Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = 0 });
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private List<ItemSearchResult> LoadItems()
        {
            var list = new List<ItemSearchResult>();
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, name, barcode, COALESCE(sell_price,0) FROM items ORDER BY name LIMIT 500", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new ItemSearchResult { Id = r.GetInt32(0), Name = r.IsDBNull(1) ? "" : r.GetString(1), Barcode = r.IsDBNull(2) ? "" : r.GetString(2), SellPrice = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3) });
            }
            return list;
        }

        private void LoadParties()
        {
            Parties.Clear();
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties ORDER BY name LIMIT 500", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) Parties.Add(new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        }

        private void LoadWarehouses()
        {
            Warehouses.Clear();
            using (var conn = new NpgsqlConnection(GetConnectionString()))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT id, name FROM warehouses ORDER BY name LIMIT 100", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) Warehouses.Add(new WarehouseOption { Id = r.GetInt32(0), Name = r.GetString(1) });
            }
        }

        private string GenerateSerial(string type)
        {
            try
            {
                string prefix = (type ?? "sales") == "purchase" ? "PUR" : "SAL";
                int year = DateTime.Now.Year;
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT invoice_number FROM invoices WHERE invoice_number LIKE @p ORDER BY invoice_number DESC LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("p", prefix + "-" + year + "-%");
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) { string last = o.ToString(); int d = last.LastIndexOf('-'); if (d >= 0 && int.TryParse(last.Substring(d + 1), out int seq)) return prefix + "-" + year + "-" + (seq + 1).ToString("D4"); }
                    }
                }
            }
            catch { }
            return ((type ?? "sales") == "purchase" ? "PUR" : "SAL") + "-" + DateTime.Now.Year + "-0001";
        }

        public void AddLine() { var line = new InvoiceLineItem(_allItems); line.PropertyChanged += (s, e) => { if (e.PropertyName == "Quantity" || e.PropertyName == "Price" || e.PropertyName == "Discount" || e.PropertyName == "Total") RefreshTotals(); }; Lines.Add(line); RefreshTotals(); }
        public void AddPayment() { decimal rem = Remaining; Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = rem > 0 ? rem : 0, PaymentDate = DateTime.Now }); RefreshTotals(); }
        public void PayFull() { decimal r = Remaining; if (r <= 0) return; Payments.Add(new InvoicePaymentRow { Method = "cash", Amount = r, PaymentDate = DateTime.Now }); RefreshTotals(); }

        public void RefreshTotals()
        {
            decimal sub = 0; foreach (var l in Lines) sub += l.Total; Subtotal = Math.Round(sub, 2);
            Total = Math.Round(_subtotal - _discountAmount, 2);
            decimal paid = 0; foreach (var p in Payments) paid += p.Amount; TotalPaid = Math.Round(paid, 2);
            OnPropertyChanged("Remaining"); OnPropertyChanged("RemainingDisplay");
        }

        public async System.Threading.Tasks.Task LoadFromInvoiceAsync(int invoiceId)
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

        
        private bool Validate()
        {
            if (PartyId <= 0)
            {
                StatusMessage = "\u0627\u062E\u062A\u0631 \u0627\u0644\u0637\u0631\u0641";
                System.Windows.MessageBox.Show("\u0627\u0644\u0631\u062C\u0627\u0621 \u0627\u062E\u062A\u064A\u0627\u0631 \u0627\u0644\u0637\u0631\u0641", "\u062A\u062D\u0642\u0642");
                return false;
            }
            if (Lines.Count == 0 || Lines.All(l => l.ItemId <= 0))
            {
                StatusMessage = "\u0623\u0636\u0641 \u0628\u0646\u0648\u062F";
                System.Windows.MessageBox.Show("\u0627\u0644\u0631\u062C\u0627\u0621 \u0625\u0636\u0627\u0641\u0629 \u0635\u0646\u0641", "\u062A\u062D\u0642\u0642");
                return false;
            }
            foreach (var line in Lines)
            {
                if (line.ItemId > 0 && line.Quantity <= 0)
                {
                    StatusMessage = "\u0627\u0644\u0643\u0645\u064A\u0629 \u063A\u064A\u0631 \u0635\u062D\u064A\u062D\u0629";
                    return false;
                }
            }
            return true;
        }

        public async System.Threading.Tasks.Task<bool> AddByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;
            barcode = barcode.Trim();
            var item = _allItems.FirstOrDefault(it => (it.Barcode ?? "").Equals(barcode, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                StatusMessage = "\u0644\u0627 \u064A\u0648\u062C\u062F \u0635\u0646\u0641 \u0628\u0627\u0631\u0643\u0648\u062F: " + barcode;
                System.Windows.MessageBox.Show("\u0644\u0627 \u064A\u0648\u062C\u062F \u0635\u0646\u0641 \u0628\u0627\u0631\u0643\u0648\u062F: " + barcode, "\u062A\u0646\u0628\u064A\u0647");
                return false;
            }
            var existing = Lines.FirstOrDefault(l => l.ItemId == item.Id);
            if (existing != null)
            {
                existing.Quantity += 1;
                RefreshTotals();
                StatusMessage = "\u0632\u064A\u0648\u062F\u062A \u0627\u0644\u0643\u0645\u064A\u0629: " + item.Name;
            }
            else
            {
                var line = new InvoiceLineItem(_allItems);
                line.SelectItem(item);
                Lines.Add(line);
                RefreshTotals();
                StatusMessage = "\u0623\u0636\u064A\u0641: " + item.Name;
            }
            await System.Threading.Tasks.Task.CompletedTask;
            return true;
        }

        public async System.Threading.Tasks.Task SaveAsDraftAsync()
        {
            if (_isSaving) return;
            if (Lines.Count == 0 || Lines.All(l => l.ItemId <= 0))
            {
                System.Windows.MessageBox.Show("\u0644\u0627 \u064A\u0645\u0643\u0646 \u062D\u0641\u0638 \u0645\u0633\u0648\u062F\u0629 \u0641\u0627\u0631\u063A\u0629", "\u062A\u062D\u0642\u0642");
                return;
            }
            _isSaving = true;
            IsBusy = true;
            try
            {
                RefreshTotals();
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var tran = conn.BeginTransaction())
                    {
                        try
                        {
                            if (Id > 0)
                            {
                                using (var cmd = new NpgsqlCommand("UPDATE invoices SET invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa, currency_id=1, status='draft', currency_id=1, updated_at=NOW() WHERE id=@id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("id", Id);
                                    cmd.Parameters.AddWithValue("sn", SerialNumber);
                                    cmd.Parameters.AddWithValue("dt", InvoiceDate);
                                    cmd.Parameters.AddWithValue("tp", InvoiceType);
                                    cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("st", Subtotal);
                                    cmd.Parameters.AddWithValue("da", DiscountAmount);
                                    cmd.Parameters.AddWithValue("tt", Total);
                                    cmd.Parameters.AddWithValue("pa", TotalPaid);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                using (var cmd = new NpgsqlCommand("INSERT INTO invoices (invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at) VALUES (@sn, @dt, @tp, @pid, @wid, @st, @da, @tt, @pa, 'draft', 1, NOW(), NOW()) RETURNING id", conn, tran))
                                {
                                    cmd.Parameters.AddWithValue("sn", SerialNumber);
                                    cmd.Parameters.AddWithValue("dt", InvoiceDate);
                                    cmd.Parameters.AddWithValue("tp", InvoiceType);
                                    cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                                    cmd.Parameters.AddWithValue("st", Subtotal);
                                    cmd.Parameters.AddWithValue("da", DiscountAmount);
                                    cmd.Parameters.AddWithValue("tt", Total);
                                    cmd.Parameters.AddWithValue("pa", TotalPaid);
                                    var o = cmd.ExecuteScalar();
                                    if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                                }
                            }
                            using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn, tran)) { cmd.Parameters.AddWithValue("id", Id); cmd.ExecuteNonQuery(); }
                            foreach (var line in Lines)
                            {
                                if (line.ItemId <= 0) continue;
                                using (var cmd = new NpgsqlCommand("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total) VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)", conn, tran))
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
                            tran.Commit();
                            StatusMessage = "\u062A\u0645 \u0627\u0644\u062D\u0641\u0638 \u0643\u0645\u0633\u0648\u062F\u0629 - ID: " + Id;
                            DialogResult = true;
                            RequestClose?.Invoke();
                        }
                        catch { try { tran.Rollback(); } catch { } throw; }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "\u062E\u0637\u0623: " + ex.Message;
                System.Windows.MessageBox.Show("\u062E\u0637\u0623 \u062D\u0641\u0638 \u0627\u0644\u0645\u0633\u0648\u062F\u0629:\n" + ex.Message, "\u062E\u0637\u0623");
            }
            finally { IsBusy = false; _isSaving = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

        public async System.Threading.Tasks.Task CancelInvoiceAsync()
        {
            if (Id <= 0) return;
            var result = System.Windows.MessageBox.Show(
                "\u0647\u0644 \u062A\u0631\u064A\u062F \u0625\u0644\u063A\u0627\u0621 \u0647\u0630\u0647 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629\u061F",
                "\u062A\u0623\u0643\u064A\u062F",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            IsBusy = true;
            try
            {
                string cs = GetConnectionString();
                using (var conn = new NpgsqlConnection(cs))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("UPDATE invoices SET status='cancelled', updated_at=NOW() WHERE id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", Id);
                        cmd.ExecuteNonQuery();
                    }
                }
                StatusMessage = "\u062A\u0645 \u0625\u0644\u063A\u0627\u0621 \u0627\u0644\u0641\u0627\u062A\u0648\u0631\u0629";
                DialogResult = true;
                RequestClose?.Invoke();
            }
            catch (Exception ex) { StatusMessage = "\u062E\u0637\u0623: " + ex.Message; }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }

public async System.Threading.Tasks.Task SaveAsync()
        {
            if (_isSaving) return;
            if (!Validate()) return;
            _isSaving = true;
            IsBusy = true; StatusMessage = "Saving...";
            try
            {
                RefreshTotals();
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    if (Id > 0)
                    {
                        using (var cmd = new NpgsqlCommand("UPDATE invoices SET invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa, currency_id=1, updated_at=NOW() WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", Id); cmd.Parameters.AddWithValue("sn", SerialNumber); cmd.Parameters.AddWithValue("dt", InvoiceDate); cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value); cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal); cmd.Parameters.AddWithValue("da", DiscountAmount); cmd.Parameters.AddWithValue("tt", Total); cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand("INSERT INTO invoices (invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, currency_id, created_at, updated_at) VALUES (@sn, @dt, @tp, @pid, @wid, @st, @da, @tt, @pa, @status, 1, NOW(), NOW()) RETURNING id", conn))
                        {
                            cmd.Parameters.AddWithValue("sn", SerialNumber); cmd.Parameters.AddWithValue("dt", InvoiceDate); cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value); cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal); cmd.Parameters.AddWithValue("da", DiscountAmount); cmd.Parameters.AddWithValue("tt", Total); cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.Parameters.AddWithValue("status", TotalPaid >= Total ? "paid" : (TotalPaid > 0 ? "partial" : "unpaid"));
                            var o = cmd.ExecuteScalar(); if (o != null && o != DBNull.Value) Id = Convert.ToInt32(o);
                        }
                    }
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_lines WHERE invoice_id=@id", conn)) { cmd.Parameters.AddWithValue("id", Id); cmd.ExecuteNonQuery(); }
                    foreach (var line in Lines) { if (line.ItemId <= 0) continue; using (var cmd = new NpgsqlCommand("INSERT INTO invoice_lines (invoice_id, item_id, quantity, price, discount, total) VALUES (@inv, @iid, @qty, @prc, @dsc, @tot)", conn)) { cmd.Parameters.AddWithValue("inv", Id); cmd.Parameters.AddWithValue("iid", line.ItemId); cmd.Parameters.AddWithValue("qty", line.Quantity); cmd.Parameters.AddWithValue("prc", line.Price); cmd.Parameters.AddWithValue("dsc", line.Discount); cmd.Parameters.AddWithValue("tot", line.Total); cmd.ExecuteNonQuery(); } }
                    using (var cmd = new NpgsqlCommand("DELETE FROM invoice_payments WHERE invoice_id=@id", conn)) { cmd.Parameters.AddWithValue("id", Id); cmd.ExecuteNonQuery(); }
                    foreach (var pay in Payments) { if (pay.Amount <= 0) continue; using (var cmd = new NpgsqlCommand("INSERT INTO invoice_payments (invoice_id, method, amount, reference, paid_at) VALUES (@inv, @mth, @amt, @ref, @pdt)", conn)) { cmd.Parameters.AddWithValue("inv", Id); cmd.Parameters.AddWithValue("mth", pay.Method); cmd.Parameters.AddWithValue("amt", pay.Amount); cmd.Parameters.AddWithValue("ref", (object)pay.Reference ?? DBNull.Value); cmd.Parameters.AddWithValue("pdt", pay.PaymentDate); cmd.ExecuteNonQuery(); } }
                }
                StatusMessage = "Saved! ID: " + Id; DialogResult = true; RequestClose?.Invoke();
            }
            catch (Exception ex) { StatusMessage = "Save error: " + ex.Message; }
            finally { IsBusy = false; }
            await System.Threading.Tasks.Task.CompletedTask;
        }
        private void PrintThermal58()
        {
            try
            {
                if (Id <= 0) { System.Windows.MessageBox.Show("احفظ الفاتورة أولًا قبل الطباعة"); return; }
                Techtoon.UI.Services.PrintService.PrintInvoiceThermal58(Id);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("خطأ الطباعة: " + ex.Message); }
        }

        private void PrintThermal80()
        {
            try
            {
                if (Id <= 0) { System.Windows.MessageBox.Show("احفظ الفاتورة أولًا قبل الطباعة"); return; }
                Techtoon.UI.Services.PrintService.PrintInvoiceThermal80(Id);
            }
            catch (Exception ex) { System.Windows.MessageBox.Show("خطأ الطباعة: " + ex.Message); }
        }

        private void PrintA4()
        {
            try
            {
                if (Id <= 0)
                {
                    System.Windows.MessageBox.Show("&#x627;&#x62D;&#x641;&#x638; &#x627;&#x644;&#x641;&#x627;&#x62A;&#x648;&#x631;&#x629; &#x623;&#x648;&#x644;&#x627;&#x64B; &#x642;&#x628;&#x644; &#x627;&#x644;&#x637;&#x628;&#x627;&#x639;&#x629;");
                    return;
                }
                Techtoon.UI.Services.PrintService.PreviewInvoiceA4(Id);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("&#x62E;&#x637;&#x623; &#x627;&#x644;&#x637;&#x628;&#x627;&#x639;&#x629;: " + ex.Message);
            }
        }
    }
}



