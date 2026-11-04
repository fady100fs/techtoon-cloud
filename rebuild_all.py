# rebuild_all.py - حذف وإعادة إنشاء كل ملفات الفواتير والمدفوعات
import os, shutil, glob

PROJECT = r"D:\PROG\Accounting Prog\Techtoon Accounting C+"
UI = os.path.join(PROJECT, "Techtoon.UI")
VIEWS = os.path.join(UI, "Views")
VMS = os.path.join(UI, "ViewModels")
PAGES = os.path.join(UI, "Pages")
BACKUP = os.path.join(PROJECT, "_backup_old_files")
CS = "Host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Username=neondb_owner;Password=npg_K9nHVkY5OjRC;Database=neondb;SSL Mode=Require;Trust Server Certificate=true"

print("=" * 60)
print("  REBUILD ALL - Invoices + Payments")
print("=" * 60)

# ============================================================
# 1) إنشاء مجلد backup ونقل كل الملفات القديمة
# ============================================================
os.makedirs(BACKUP, exist_ok=True)

old_files_patterns = [
    os.path.join(VIEWS, "InvoiceEdit*"),
    os.path.join(VIEWS, "Payment*"),
    os.path.join(VIEWS, "InvoicesView*"),
    os.path.join(VMS, "InvoiceEditViewModel*"),
    os.path.join(VMS, "Payment*"),
    os.path.join(VMS, "InvoicesViewModel*"),
    os.path.join(PAGES, "PaymentsPage*"),
    os.path.join(PAGES, "InvoiceEdit*"),
]

print("\n[1/8] Moving old files to backup...")
moved = 0
for pattern in old_files_patterns:
    for f in glob.glob(pattern):
        try:
            shutil.copy2(f, BACKUP)
            os.remove(f)
            print(f"  Removed: {os.path.basename(f)}")
            moved += 1
        except Exception as e:
            print(f"  SKIP: {os.path.basename(f)} - {e}")
print(f"  Total: {moved} files moved to {BACKUP}")
# ============================================================
# 2) إنشاء InvoiceEditViewModel.cs (نظيف - Npgsql)
# ============================================================
print("\n[2/8] Creating InvoiceEditViewModel.cs...")
vm_path = os.path.join(VIEWS, "InvoiceEditViewModel.cs")

vm_content = '''using System;
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
        private bool _isBusy = false; public bool IsBusy { get { return _isBusy; } set { _isBusy = value; OnPropertyChanged("IsBusy"); } }

        public ICommand AddLineCommand { get; set; }
        public ICommand DeleteLineCommand { get; set; }
        public ICommand AddPaymentCommand { get; set; }
        public ICommand DeletePaymentCommand { get; set; }
        public ICommand PayFullCommand { get; set; }
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
            PayFullCommand = new UiRelayCommand(_ => PayFull());
            SaveCommand = new UiRelayCommand(_ => { _ = SaveAsync(); }, _ => !IsBusy);
            CancelCommand = new UiRelayCommand(_ => { DialogResult = false; RequestClose?.Invoke(); });
        }

        public static string GetConnectionString() { return "''' + CS + '''"; }

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

        public async System.Threading.Tasks.Task SaveAsync()
        {
            IsBusy = true; StatusMessage = "Saving...";
            try
            {
                RefreshTotals();
                using (var conn = new NpgsqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    if (Id > 0)
                    {
                        using (var cmd = new NpgsqlCommand("UPDATE invoices SET invoice_number=@sn, date=@dt, type=@tp, party_id=@pid, warehouse_id=@wid, subtotal=@st, discount_amount=@da, total=@tt, paid_amount=@pa, updated_at=NOW() WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("id", Id); cmd.Parameters.AddWithValue("sn", SerialNumber); cmd.Parameters.AddWithValue("dt", InvoiceDate); cmd.Parameters.AddWithValue("tp", InvoiceType);
                            cmd.Parameters.AddWithValue("pid", PartyId > 0 ? (object)PartyId : DBNull.Value); cmd.Parameters.AddWithValue("wid", WarehouseId > 0 ? (object)WarehouseId : DBNull.Value);
                            cmd.Parameters.AddWithValue("st", Subtotal); cmd.Parameters.AddWithValue("da", DiscountAmount); cmd.Parameters.AddWithValue("tt", Total); cmd.Parameters.AddWithValue("pa", TotalPaid);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand("INSERT INTO invoices (invoice_number, date, type, party_id, warehouse_id, subtotal, discount_amount, total, paid_amount, status, created_at, updated_at) VALUES (@sn, @dt, @tp, @pid, @wid, @st, @da, @tt, @pa, @status, NOW(), NOW()) RETURNING id", conn))
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
    }
}
'''
with open(vm_path, "w", encoding="utf-8-sig") as f:
    f.write(vm_content)
print("  OK: InvoiceEditViewModel.cs")
# ============================================================
# 3) إنشاء InvoicesViewModel.cs (قائمة الفواتير)
# ============================================================
print("\n[3/8] Creating InvoicesViewModel.cs...")
ivm_path = os.path.join(VMS, "InvoicesViewModel.cs")

ivm_content = '''using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class InvoiceRow : ViewModelBase
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Status { get; set; }
        public string TypeDisplay { get { return Type == "sales" ? "\\u0645\\u0628\\u064A\\u0639\\u0627\\u062A" : (Type == "purchase" ? "\\u0645\\u0634\\u062A\\u0631\\u064A\\u0627\\u062A" : Type ?? ""); } }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string TotalDisplay { get { return Total.ToString("N2"); } }
        public string PaidDisplay { get { return Paid.ToString("N2"); } }
        public string RemainingDisplay { get { return Remaining.ToString("N2"); } }
    }

    public class InvoicesViewModel : ViewModelBase
    {
        public ObservableCollection<InvoiceRow> Invoices { get; set; }
        private InvoiceRow _selectedInvoice; public InvoiceRow SelectedInvoice { get { return _selectedInvoice; } set { _selectedInvoice = value; OnPropertyChanged("SelectedInvoice"); } }
        private string _statusMessage = ""; public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }
        public ICommand RefreshCommand { get; set; }
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand EditInvoiceCommand { get; set; }
        public ICommand DeleteInvoiceCommand { get; set; }

        public InvoicesViewModel()
        {
            Invoices = new ObservableCollection<InvoiceRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadInvoices());
            NewInvoiceCommand = new UiRelayCommand(_ => OpenNew());
            EditInvoiceCommand = new UiRelayCommand(_ => OpenEdit(), _ => SelectedInvoice != null);
            DeleteInvoiceCommand = new UiRelayCommand(_ => DeleteSelected(), _ => SelectedInvoice != null);
            LoadInvoices();
        }

        public static string GetCS() { return "''' + CS + '''"; }

        public void LoadInvoices()
        {
            Invoices.Clear();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT i.id, i.invoice_number, i.date, i.type, i.total, i.paid_amount, COALESCE(p.name,'') AS party_name, i.status FROM invoices i LEFT JOIN parties p ON i.party_id = p.id ORDER BY i.id DESC LIMIT 200", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            Invoices.Add(new InvoiceRow { Id = r.GetInt32(0), InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1), Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2), Type = r.IsDBNull(3) ? "" : r.GetString(3), Total = r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4), Paid = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5), PartyName = r.IsDBNull(6) ? "" : r.GetString(6), Status = r.IsDBNull(7) ? "" : r.GetString(7) });
                }
                StatusMessage = "Loaded: " + Invoices.Count + " invoices";
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }

        private void OpenNew() { try { var w = new InvoiceEditWindow(null); w.ShowDialog(); LoadInvoices(); } catch { } }
        private void OpenEdit() { try { if (_selectedInvoice == null) return; var w = new InvoiceEditWindow(_selectedInvoice.Id); w.ShowDialog(); LoadInvoices(); } catch { } }
        private void DeleteSelected()
        {
            if (_selectedInvoice == null) return;
            try
            {
                var res = System.Windows.MessageBox.Show("\\u0647\\u0644 \\u062A\\u0631\\u064A\\u062F \\u062D\\u0630\\u0641 \\u0647\\u0630\\u0647 \\u0627\\u0644\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629\\u061F", "\\u062A\\u0623\\u0643\\u064A\\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res != System.Windows.MessageBoxResult.Yes) return;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    foreach (var t in new[] { "invoice_lines", "invoice_payments" }) { using (var c = new NpgsqlCommand("DELETE FROM " + t + " WHERE invoice_id=@id", conn)) { c.Parameters.AddWithValue("id", _selectedInvoice.Id); c.ExecuteNonQuery(); } }
                    using (var c = new NpgsqlCommand("DELETE FROM invoices WHERE id=@id", conn)) { c.Parameters.AddWithValue("id", _selectedInvoice.Id); c.ExecuteNonQuery(); }
                }
                LoadInvoices();
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }
    }
}
'''
with open(ivm_path, "w", encoding="utf-8-sig") as f:
    f.write(ivm_content)
print("  OK: InvoicesViewModel.cs")
# ============================================================
# 4) إنشاء PaymentsViewModel.cs (قائمة المدفوعات/السندات)
# ============================================================
print("\n[4/8] Creating PaymentsViewModel.cs...")
pvm_path = os.path.join(VMS, "PaymentsViewModel.cs")

pvm_content = '''using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Npgsql;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels
{
    public class PaymentRow : ViewModelBase
    {
        public int Id { get; set; }
        public string VoucherNo { get; set; }
        public DateTime Date { get; set; }
        public string PartyName { get; set; }
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Notes { get; set; }
        public string DateDisplay { get { return Date.ToString("yyyy-MM-dd"); } }
        public string AmountDisplay { get { return Amount.ToString("N2"); } }
        public string TypeDisplay { get { return Type == "receipt" ? "\\u0642\\u0628\\u0636" : (Type == "payment" ? "\\u0635\\u0631\\u0641" : Type ?? ""); } }
    }

    public class PaymentsViewModel : ViewModelBase
    {
        public ObservableCollection<PaymentRow> Payments { get; set; }
        private PaymentRow _selectedPayment; public PaymentRow SelectedPayment { get { return _selectedPayment; } set { _selectedPayment = value; OnPropertyChanged("SelectedPayment"); } }
        private string _statusMessage = ""; public string StatusMessage { get { return _statusMessage; } set { _statusMessage = value; OnPropertyChanged("StatusMessage"); } }
        public ICommand RefreshCommand { get; set; }
        public ICommand NewReceiptCommand { get; set; }
        public ICommand NewPaymentCommand { get; set; }
        public ICommand EditCommand { get; set; }
        public ICommand DeleteCommand { get; set; }

        public PaymentsViewModel()
        {
            Payments = new ObservableCollection<PaymentRow>();
            RefreshCommand = new UiRelayCommand(_ => LoadPayments());
            NewReceiptCommand = new UiRelayCommand(_ => OpenEditor("receipt", null));
            NewPaymentCommand = new UiRelayCommand(_ => OpenEditor("payment", null));
            EditCommand = new UiRelayCommand(_ => { if (_selectedPayment != null) OpenEditor(_selectedPayment.Type, _selectedPayment.Id); }, _ => _selectedPayment != null);
            DeleteCommand = new UiRelayCommand(_ => DeletePayment(), _ => _selectedPayment != null);
            LoadPayments();
        }

        public static string GetCS() { return "''' + CS + '''"; }

        public void LoadPayments()
        {
            Payments.Clear();
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, voucher_no, date, party_name, type, amount, method, notes FROM payments ORDER BY id DESC LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            Payments.Add(new PaymentRow { Id = r.GetInt32(0), VoucherNo = r.IsDBNull(1) ? "" : r.GetString(1), Date = r.IsDBNull(2) ? DateTime.MinValue : r.GetDateTime(2), PartyName = r.IsDBNull(3) ? "" : r.GetString(3), Type = r.IsDBNull(4) ? "" : r.GetString(4), Amount = r.IsDBNull(5) ? 0 : (decimal)r.GetDouble(5), Method = r.IsDBNull(6) ? "" : r.GetString(6), Notes = r.IsDBNull(7) ? "" : r.GetString(7) });
                }
                StatusMessage = "Loaded: " + Payments.Count + " payments";
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }

        private void OpenEditor(string type, int? editId) { try { var w = new PaymentEditWindow(type, editId); w.ShowDialog(); LoadPayments(); } catch (Exception ex) { StatusMessage = "Error: " + ex.Message; } }

        private void DeletePayment()
        {
            if (_selectedPayment == null) return;
            try
            {
                var res = System.Windows.MessageBox.Show("\\u0647\\u0644 \\u062A\\u0631\\u064A\\u062F \\u062D\\u0630\\u0641 \\u0647\\u0630\\u0627 \\u0627\\u0644\\u0633\\u0646\\u062F\\u061F", "\\u062A\\u0623\\u0643\\u064A\\u062F", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                if (res != System.Windows.MessageBoxResult.Yes) return;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM payments WHERE id=@id", conn)) { cmd.Parameters.AddWithValue("id", _selectedPayment.Id); cmd.ExecuteNonQuery(); }
                }
                LoadPayments();
            }
            catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
        }
    }
}
'''
with open(pvm_path, "w", encoding="utf-8-sig") as f:
    f.write(pvm_content)
print("  OK: PaymentsViewModel.cs")
# ============================================================
# 5) إنشاء InvoiceEditWindow.xaml + .xaml.cs
# ============================================================
print("\n[5/8] Creating InvoiceEditWindow.xaml + .xaml.cs...")

xaml_inv = '''<Window x:Class="Techtoon.UI.Views.InvoiceEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Invoice" Height="850" Width="1200" WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft" Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Window.Resources>
        <Style x:Key="AddBtn" TargetType="Button"><Setter Property="Background" Value="#26A69A"/><Setter Property="Foreground" Value="White"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="FontSize" Value="13"/><Setter Property="Padding" Value="14,6"/><Setter Property="BorderThickness" Value="0"/><Setter Property="Cursor" Value="Hand"/></Style>
        <Style x:Key="SaveBtn" TargetType="Button"><Setter Property="Background" Value="#00897B"/><Setter Property="Foreground" Value="White"/><Setter Property="FontWeight" Value="Bold"/><Setter Property="FontSize" Value="14"/><Setter Property="Padding" Value="24,10"/><Setter Property="BorderThickness" Value="0"/><Setter Property="Cursor" Value="Hand"/></Style>
        <Style x:Key="CancelBtn" TargetType="Button"><Setter Property="Background" Value="#ECEFF1"/><Setter Property="Foreground" Value="#37474F"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="FontSize" Value="13"/><Setter Property="Padding" Value="18,10"/><Setter Property="BorderThickness" Value="0"/><Setter Property="Cursor" Value="Hand"/></Style>
    </Window.Resources>
    <Grid>
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <Border Grid.Row="0" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                <StackPanel Grid.Column="0" Orientation="Horizontal" VerticalAlignment="Center">
                    <TextBlock x:Name="HeaderIcon" FontSize="28" Margin="0,0,10,0"/>
                    <StackPanel><TextBlock x:Name="HeaderTitle" FontSize="18" FontWeight="Bold" Foreground="#00695C"/><TextBlock Text="{Binding InvoiceTypeDisplay}" FontSize="12" Foreground="#78909C"/></StackPanel>
                </StackPanel>
                <Grid Grid.Column="1" Margin="20,0,0,0">
                    <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="140"/><ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="150"/><ColumnDefinition Width="18"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="120"/></Grid.ColumnDefinitions>
                    <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
                    <TextBlock x:Name="LblSerial" Grid.Row="0" Grid.Column="0" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="SemiBold"/>
                    <TextBox Grid.Row="0" Grid.Column="1" Text="{Binding SerialNumber}" IsReadOnly="True" Padding="6,4" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblDate" Grid.Row="0" Grid.Column="3" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="SemiBold"/>
                    <DatePicker Grid.Row="0" Grid.Column="4" SelectedDate="{Binding InvoiceDate}" FontSize="13"/>
                    <TextBlock x:Name="LblType" Grid.Row="0" Grid.Column="6" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="SemiBold"/>
                    <ComboBox x:Name="CmbType" Grid.Row="0" Grid.Column="7" SelectedValue="{Binding InvoiceType}" SelectedValuePath="Tag" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblParty" Grid.Row="2" Grid.Column="0" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="SemiBold"/>
                    <ComboBox Grid.Row="2" Grid.Column="1" Grid.ColumnSpan="4" ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding PartyId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                    <TextBlock x:Name="LblWarehouse" Grid.Row="2" Grid.Column="6" VerticalAlignment="Center" Margin="0,0,6,0" FontWeight="SemiBold"/>
                    <ComboBox Grid.Row="2" Grid.Column="7" ItemsSource="{Binding Warehouses}" DisplayMemberPath="Name" SelectedValuePath="Id" SelectedValue="{Binding WarehouseId}" Padding="6,4" FontSize="13" BorderBrush="#B0BEC5" BorderThickness="1"/>
                </Grid>
            </Grid>
        </Border>
        <Border Grid.Row="1" Background="White" Margin="10,8,10,4" Padding="8" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblItems" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#37474F" VerticalAlignment="Center"/>
                    <Button x:Name="BtnAddLine" Grid.Column="1" Style="{StaticResource AddBtn}" Command="{Binding AddLineCommand}"/>
                </Grid>
                <DataGrid x:Name="LinesGrid" Grid.Row="1" ItemsSource="{Binding Lines}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MinHeight="180">
                    <DataGrid.Columns>
                        <DataGridTemplateColumn x:Name="ColItem" Width="340">
                            <DataGridTemplateColumn.CellTemplate>
                                <DataTemplate>
                                    <ComboBox x:Name="ItemSearchCombo" ItemsSource="{Binding FilteredItems}" DisplayMemberPath="SearchDisplay" IsEditable="True" Text="{Binding ItemName, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay}" StaysOpenOnEdit="True" SelectionChanged="ItemSearchCombo_SelectionChanged" PreviewKeyDown="ItemSearchCombo_PreviewKeyDown" Padding="4,3" FontSize="13"/>
                                </DataTemplate>
                            </DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColBarcode" Width="110" Binding="{Binding Barcode}" IsReadOnly="True"/>
                        <DataGridTemplateColumn x:Name="ColQty" Width="90">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><TextBox Text="{Binding Quantity, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="qty" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPrice" Width="100">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><TextBox Text="{Binding Price, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="price" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColDisc" Width="80">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><TextBox Text="{Binding Discount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}" Tag="discount" Padding="4,3" BorderBrush="#E0E0E0" KeyDown="Cell_KeyDown"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTextColumn x:Name="ColTotal" Width="110" Binding="{Binding Total, StringFormat=N2}" IsReadOnly="True">
                            <DataGridTextColumn.ElementStyle><Style TargetType="TextBlock"><Setter Property="HorizontalAlignment" Value="Right"/><Setter Property="FontWeight" Value="SemiBold"/><Setter Property="Foreground" Value="#00695C"/></Style></DataGridTextColumn.ElementStyle>
                        </DataGridTextColumn>
                        <DataGridTemplateColumn x:Name="ColDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2" Command="{Binding DataContext.DeleteLineCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border x:Name="PaymentsSection" Grid.Row="2" Background="White" Margin="10,4,10,4" Padding="10" BorderBrush="#80CBC4" BorderThickness="1" CornerRadius="4">
            <Grid>
                <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
                <Grid Grid.Row="0" Margin="0,0,0,6">
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                    <TextBlock x:Name="LblPayments" Grid.Column="0" FontSize="15" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
                    <Button x:Name="BtnPayFull" Grid.Column="1" Background="#FB8C00" Foreground="White" FontWeight="SemiBold" FontSize="13" Padding="14,6" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding PayFullCommand}"/>
                    <Button x:Name="BtnAddPay" Grid.Column="2" Style="{StaticResource AddBtn}" Command="{Binding AddPaymentCommand}"/>
                </Grid>
                <DataGrid x:Name="PaymentsGrid" Grid.Row="1" ItemsSource="{Binding Payments}" AutoGenerateColumns="False" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="All" HorizontalGridLinesBrush="#ECEFF1" VerticalGridLinesBrush="#ECEFF1" BorderBrush="#E0E0E0" BorderThickness="1" Background="White" FontSize="13" MaxHeight="180" CellEditEnding="PaymentsGrid_CellEditEnding" SelectionMode="Single" SelectionUnit="CellOrRowHeader" IsReadOnly="False">
                    <DataGrid.Columns>
                        <DataGridTextColumn x:Name="ColPayMethod" Width="120" Binding="{Binding Method}"/>
                        <DataGridTextColumn x:Name="ColPayAmount" Width="120" Binding="{Binding Amount, UpdateSourceTrigger=PropertyChanged, StringFormat=N2}"/>
                        <DataGridTextColumn x:Name="ColPayRef" Width="160" Binding="{Binding Reference, UpdateSourceTrigger=PropertyChanged}"/>
                        <DataGridTemplateColumn x:Name="ColPayDate" Width="130">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><TextBlock Text="{Binding PaymentDate, StringFormat=yyyy-MM-dd}" Padding="4,0" VerticalAlignment="Center"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                            <DataGridTemplateColumn.CellEditingTemplate><DataTemplate><DatePicker SelectedDate="{Binding PaymentDate, UpdateSourceTrigger=PropertyChanged}"/></DataTemplate></DataGridTemplateColumn.CellEditingTemplate>
                        </DataGridTemplateColumn>
                        <DataGridTemplateColumn x:Name="ColPayDel" Width="60">
                            <DataGridTemplateColumn.CellTemplate><DataTemplate><Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2" Command="{Binding DataContext.DeletePaymentCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                        </DataGridTemplateColumn>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>
        <Border Grid.Row="3" Background="White" Padding="16,10" BorderBrush="#E0E0E0" BorderThickness="0,1,0,0">
            <Grid>
                <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="8"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
                <Grid Grid.Row="0">
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="300"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" VerticalAlignment="Center"><TextBlock x:Name="StatusText" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" TextWrapping="Wrap"/></StackPanel>
                    <Grid Grid.Column="1">
                        <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="110"/></Grid.ColumnDefinitions>
                        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
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
'''
with open(os.path.join(VIEWS, "InvoiceEditWindow.xaml"), "w", encoding="ascii") as f:
    f.write(xaml_inv)

cs_inv = '''using System;
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
        public InvoiceEditWindow(int? editId = null) { InitializeComponent(); _editId = editId; _vm = new InvoiceEditViewModel(); if (editId.HasValue) _vm.Id = editId.Value; DataContext = _vm; _vm.RequestClose += () => { DialogResult = _vm.DialogResult; Close(); }; }

        private async void Window_Loaded(object sender, RoutedEventArgs e) { SetArabicTexts(); FillTypeCombo(); await _vm.LoadAsync(); if (_editId.HasValue) await _vm.LoadFromInvoiceAsync?.Invoke(_editId.Value); }
        private void SetArabicTexts()
        {
            try { Title = "\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629"; } catch { }
            try { HeaderIcon.Text = "\\uD83E\\uDDF9"; } catch { }
            try { HeaderTitle.Text = "\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629"; } catch { }
            try { LblSerial.Text = "\\u0627\\u0644\\u0631\\u0642\\u0645:"; } catch { }
            try { LblDate.Text = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E:"; } catch { }
            try { LblType.Text = "\\u0627\\u0644\\u0646\\u0648\\u0639:"; } catch { }
            try { LblParty.Text = "\\u0627\\u0644\\u0637\\u0631\\u0641:"; } catch { }
            try { LblWarehouse.Text = "\\u0627\\u0644\\u0645\\u062E\\u0632\\u0646:"; } catch { }
            try { LblItems.Text = "\\u0628\\u0646\\u0648\\u062F \\u0627\\u0644\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629"; } catch { }
            try { BtnAddLine.Content = "+ \\u0625\\u0636\\u0627\\u0641\\u0629 \\u0635\\u0646\\u0641"; } catch { }
            try { ColItem.Header = "\\u0635\\u0646\\u0641 (\\u0627\\u0643\\u062A\\u0628 \\u0644\\u0644\\u0628\\u062D\\u062B)"; } catch { }
            try { ColBarcode.Header = "\\u0628\\u0627\\u0631\\u0643\\u0648\\u062F"; } catch { }
            try { ColQty.Header = "\\u0627\\u0644\\u0643\\u0645\\u064A\\u0629"; } catch { }
            try { ColPrice.Header = "\\u0627\\u0644\\u0633\\u0639\\u0631"; } catch { }
            try { ColDisc.Header = "\\u062E\\u0635\\u0645"; } catch { }
            try { ColTotal.Header = "\\u0625\\u062C\\u0645\\u0627\\u0644\\u064A"; } catch { }
            try { ColDel.Header = "\\u062D\\u0630\\u0641"; } catch { }
            try { LblPayments.Text = "\\u0637\\u0631\\u0642 \\u0627\\u0644\\u062F\\u0641\\u0639 \\u0627\\u0644\\u0645\\u062A\\u0639\\u062F\\u062F\\u0629"; } catch { }
            try { BtnAddPay.Content = "+ \\u0625\\u0636\\u0627\\u0641\\u0629 \\u062F\\u0641\\u0639\\u0629"; } catch { }
            try { BtnPayFull.Content = "\\uD83D\\uDCB5 \\u062F\\u0641\\u0639 \\u0628\\u0627\\u0644\\u0643\\u0627\\u0645\\u0644"; } catch { }
            try { ColPayMethod.Header = "\\u0637\\u0631\\u064A\\u0642\\u0629"; } catch { }
            try { ColPayAmount.Header = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A"; } catch { }
            try { ColPayRef.Header = "\\u0645\\u0631\\u062C\\u0639"; } catch { }
            try { ColPayDate.Header = "\\u062A\\u0627\\u0631\\u064A\\u062E"; } catch { }
            try { ColPayDel.Header = "\\u062D\\u0630\\u0641"; } catch { }
            try { LblSubtotal.Text = "\\u0627\\u0644\\u0645\\u062C\\u0645\\u0648\\u0639:"; } catch { }
            try { LblDisc.Text = "\\u062E\\u0635\\u0645:"; } catch { }
            try { LblTotal.Text = "\\u0625\\u062C\\u0645\\u0627\\u0644\\u064A:"; } catch { }
            try { LblPaid.Text = "\\u0627\\u0644\\u0645\\u062F\\u0641\\u0648\\u0639:"; } catch { }
            try { LblRemaining.Text = "\\u0627\\u0644\\u0645\\u062A\\u0628\\u0642\\u064A:"; } catch { }
            try { BtnSave.Content = "\\uD83D\\uDCBE \\u062D\\u0641\\u0638 \\u0627\\u0644\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629"; } catch { }
            try { BtnCancel.Content = "\\u2716 \\u0625\\u0644\\u063A\\u0627\\u0621"; } catch { }
        }
        private void FillTypeCombo() { try { CmbType.Items.Clear(); CmbType.Items.Add(new ComboBoxItem { Content = "\\u0645\\u0628\\u064A\\u0639\\u0627\\u062A", Tag = "sales" }); CmbType.Items.Add(new ComboBoxItem { Content = "\\u0645\\u0634\\u062A\\u0631\\u064A\\u0627\\u062A", Tag = "purchase" }); } catch { } }

        private void ItemSearchCombo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (sender is not ComboBox cb) return;
                if (cb.DataContext is not InvoiceLineItem line) return;
                if (!cb.IsDropDownOpen && (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)) { cb.IsDropDownOpen = true; e.Handled = true; return; }
                switch (e.Key)
                {
                    case Key.Down: if (cb.Items.Count > 0) { cb.SelectedIndex = cb.SelectedIndex < cb.Items.Count - 1 ? cb.SelectedIndex + 1 : 0; e.Handled = true; } break;
                    case Key.Up: if (cb.Items.Count > 0) { cb.SelectedIndex = cb.SelectedIndex > 0 ? cb.SelectedIndex - 1 : cb.Items.Count - 1; e.Handled = true; } break;
                    case Key.Enter:
                        if (cb.IsDropDownOpen && cb.SelectedIndex >= 0 && cb.Items[cb.SelectedIndex] is ItemSearchResult item) { line.SelectItem(item); cb.Text = item.Name; cb.IsDropDownOpen = false; Dispatcher.BeginInvoke(new Action(() => { try { LinesGrid.CommitEdit(DataGridEditingUnit.Cell, true); LinesGrid.CommitEdit(DataGridEditingUnit.Row, true); if (LinesGrid.ItemContainerGenerator.ContainerFromItem(line) is DataGridRow row) { var tb = FindDescendant<TextBox>(row, x => (x.Tag as string) == "qty"); tb?.Focus(); tb?.SelectAll(); } } catch { } }), System.Windows.Threading.DispatcherPriority.Input); }
                        e.Handled = true; break;
                    case Key.Escape: cb.IsDropDownOpen = false; e.Handled = true; break;
                }
            }
            catch { }
        }

        private void ItemSearchCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { try { if (sender is ComboBox cb && cb.SelectedItem is ItemSearchResult item && cb.DataContext is InvoiceLineItem line) { line.SelectItem(item); cb.Text = item.Name; } } catch { } }
        private void Cell_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not TextBox tb) return;
            if (tb.Tag is not string tag) return;
            if (tb.DataContext is not InvoiceLineItem line) return;
            LinesGrid.CommitEdit(DataGridEditingUnit.Row, true);
            int idx = _vm.Lines.IndexOf(line); int last = _vm.Lines.Count - 1;
            if (idx >= 0 && idx < last) { if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(idx + 1) is DataGridRow nextRow) { var next = FindDescendant<TextBox>(nextRow, x => (x.Tag as string) == tag); if (next != null) { next.Focus(); next.SelectAll(); } } }
            else if (idx == last) { _vm.AddLineCommand.Execute(null); LinesGrid.UpdateLayout(); if (LinesGrid.ItemContainerGenerator.ContainerFromIndex(_vm.Lines.Count - 1) is DataGridRow newRow) { var cb = FindDescendant<ComboBox>(newRow, x => x.Name == "ItemSearchCombo"); cb?.Focus(); } }
            e.Handled = true;
        }
        private static T? FindDescendant<T>(DependencyObject? parent, Func<T, bool> predicate) where T : class { if (parent == null) return null; int count = VisualTreeHelper.GetChildrenCount(parent); for (int i = 0; i < count; i++) { var child = VisualTreeHelper.GetChild(parent, i); if (child is T matched && predicate(matched)) return matched; var deep = FindDescendant(child, predicate); if (deep != null) return deep; } return null; }
        private void PaymentsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) { if (e.EditAction == DataGridEditAction.Commit) _vm.RefreshTotals(); }
    }
}
'''
with open(os.path.join(VIEWS, "InvoiceEditWindow.xaml.cs"), "w", encoding="utf-8-sig") as f:
    f.write(cs_inv)
print("  OK: InvoiceEditWindow.xaml + .xaml.cs")
# ============================================================
# 6) إنشاء InvoicesView.xaml + .xaml.cs
# ============================================================
print("\n[6/8] Creating InvoicesView.xaml + .xaml.cs...")

xaml_inv_view = '''<UserControl x:Class="Techtoon.UI.Views.InvoicesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             FlowDirection="RightToLeft" Background="#F5F7FA" FontFamily="Tahoma" Loaded="UserControl_Loaded">
    <Grid Margin="10">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnDelete" Grid.Column="2" Background="#E53935" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding DeleteInvoiceCommand}"/>
            <Button x:Name="BtnNew" Grid.Column="3" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewInvoiceCommand}"/>
        </Grid>
        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgInvoices" ItemsSource="{Binding Invoices}" SelectedItem="{Binding SelectedInvoice}" AutoGenerateColumns="False" IsReadOnly="True" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" Background="White" FontSize="13" MouseDoubleClick="DgInvoices_MouseDoubleClick" KeyDown="DgInvoices_KeyDown">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColNum" Binding="{Binding InvoiceNumber}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="100"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColTotal" Binding="{Binding TotalDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColPaid" Binding="{Binding PaidDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColRem" Binding="{Binding RemainingDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColStatus" Binding="{Binding Status}" Width="90"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'''
with open(os.path.join(VIEWS, "InvoicesView.xaml"), "w", encoding="ascii") as f:
    f.write(xaml_inv_view)

cs_inv_view = '''using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class InvoicesView : UserControl
    {
        public InvoicesView() { InitializeComponent(); SetArabicTexts(); DataContext = new InvoicesViewModel(); }
        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\\u0627\\u0644\\u0641\\u0648\\u0627\\u062A\\u064A\\u0631"; } catch { }
            try { BtnRefresh.Content = "\\uD83D\\uDD04 \\u062A\\u062D\\u062F\\u064A\\u062B"; } catch { }
            try { BtnDelete.Content = "\\uD83D\\uDDD1 \\u062D\\u0630\\u0641"; } catch { }
            try { BtnNew.Content = "+ \\u0641\\u0627\\u062A\\u0648\\u0631\\u0629 \\u062C\\u062F\\u064A\\u062F\\u0629"; } catch { }
            try { ColNum.Header = "\\u0627\\u0644\\u0631\\u0642\\u0645"; } catch { }
            try { ColDate.Header = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E"; } catch { }
            try { ColType.Header = "\\u0627\\u0644\\u0646\\u0648\\u0639"; } catch { }
            try { ColParty.Header = "\\u0627\\u0644\\u0637\\u0631\\u0641"; } catch { }
            try { ColTotal.Header = "\\u0625\\u062C\\u0645\\u0627\\u0644\\u064A"; } catch { }
            try { ColPaid.Header = "\\u0645\\u062F\\u0641\\u0648\\u0639"; } catch { }
            try { ColRem.Header = "\\u0645\\u062A\\u0628\\u0642\\u064A"; } catch { }
            try { ColStatus.Header = "\\u062D\\u0627\\u0644\\u0629"; } catch { }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e) { if (DataContext is InvoicesViewModel vm) vm.LoadInvoices(); }
        private void DgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null) { var win = new InvoiceEditWindow(vm.SelectedInvoice.Id); win.ShowDialog(); vm.LoadInvoices(); } }
        private void DgInvoices_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Delete && DataContext is InvoicesViewModel vm && vm.SelectedInvoice != null) { vm.DeleteInvoiceCommand.Execute(null); e.Handled = true; } }
    }
}
'''
with open(os.path.join(VIEWS, "InvoicesView.xaml.cs"), "w", encoding="utf-8-sig") as f:
    f.write(cs_inv_view)
print("  OK: InvoicesView.xaml + .xaml.cs")
# ============================================================
# 7) إنشاء PaymentsView.xaml + .xaml.cs
# ============================================================
print("\n[7/8] Creating PaymentsView.xaml + .xaml.cs...")

xaml_pay_view = '''<UserControl x:Class="Techtoon.UI.Views.PaymentsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             FlowDirection="RightToLeft" Background="#F5F7FA" FontFamily="Tahoma" Loaded="UserControl_Loaded">
    <Grid Margin="10">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <Grid Grid.Row="0" Margin="0,0,0,10">
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <TextBlock x:Name="LblTitle" Grid.Column="0" FontSize="20" FontWeight="Bold" Foreground="#00695C" VerticalAlignment="Center"/>
            <Button x:Name="BtnRefresh" Grid.Column="1" Background="#26A69A" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding RefreshCommand}"/>
            <Button x:Name="BtnEdit" Grid.Column="2" Background="#00897B" Foreground="White" FontWeight="SemiBold" Padding="14,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding EditCommand}"/>
            <Button x:Name="BtnReceipt" Grid.Column="3" Background="#00897B" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Margin="6,0" Command="{Binding NewReceiptCommand}"/>
            <Button x:Name="BtnPayment" Grid.Column="4" Background="#FB8C00" Foreground="White" FontWeight="Bold" Padding="18,8" BorderThickness="0" Cursor="Hand" Command="{Binding NewPaymentCommand}"/>
        </Grid>
        <TextBlock x:Name="LblStatus" Grid.Row="1" Text="{Binding StatusMessage}" FontSize="12" Foreground="#00897B" Margin="0,0,0,8"/>
        <Border Grid.Row="2" Background="White" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <DataGrid x:Name="DgPayments" ItemsSource="{Binding Payments}" SelectedItem="{Binding SelectedPayment}" AutoGenerateColumns="False" IsReadOnly="True" CanUserAddRows="False" HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#ECEFF1" Background="White" FontSize="13" MouseDoubleClick="DgPayments_MouseDoubleClick">
                <DataGrid.Columns>
                    <DataGridTextColumn x:Name="ColVoucher" Binding="{Binding VoucherNo}" Width="140"/>
                    <DataGridTextColumn x:Name="ColDate" Binding="{Binding DateDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColParty" Binding="{Binding PartyName}" Width="*"/>
                    <DataGridTextColumn x:Name="ColType" Binding="{Binding TypeDisplay}" Width="90"/>
                    <DataGridTextColumn x:Name="ColAmount" Binding="{Binding AmountDisplay}" Width="120"/>
                    <DataGridTextColumn x:Name="ColMethod" Binding="{Binding Method}" Width="100"/>
                    <DataGridTemplateColumn x:Name="ColDel" Width="60">
                        <DataGridTemplateColumn.CellTemplate><DataTemplate><Button Content="X" Background="#E53935" Foreground="White" BorderThickness="0" FontSize="12" Cursor="Hand" Padding="8,2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/></DataTemplate></DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
'''
with open(os.path.join(VIEWS, "PaymentsView.xaml"), "w", encoding="ascii") as f:
    f.write(xaml_pay_view)

cs_pay_view = '''using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views
{
    public partial class PaymentsView : UserControl
    {
        public PaymentsView() { InitializeComponent(); SetArabicTexts(); DataContext = new PaymentsViewModel(); }
        private void SetArabicTexts()
        {
            try { LblTitle.Text = "\\u0627\\u0644\\u0645\\u062F\\u0641\\u0648\\u0639\\u0627\\u062A \\u0648\\u0627\\u0644\\u0633\\u0646\\u062F\\u0627\\u062A"; } catch { }
            try { BtnRefresh.Content = "\\uD83D\\uDD04 \\u062A\\u062D\\u062F\\u064A\\u062B"; } catch { }
            try { BtnEdit.Content = "\\u270E \\u062A\\u0639\\u062F\\u064A\\u0644"; } catch { }
            try { BtnReceipt.Content = "+ \\u0633\\u0646\\u062F \\u0642\\u0628\\u0636"; } catch { }
            try { BtnPayment.Content = "+ \\u0633\\u0646\\u062F \\u0635\\u0631\\u0641"; } catch { }
            try { ColVoucher.Header = "\\u0631\\u0642\\u0645 \\u0627\\u0644\\u0633\\u0646\\u062F"; } catch { }
            try { ColDate.Header = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E"; } catch { }
            try { ColParty.Header = "\\u0627\\u0644\\u0637\\u0631\\u0641"; } catch { }
            try { ColType.Header = "\\u0627\\u0644\\u0646\\u0648\\u0639"; } catch { }
            try { ColAmount.Header = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A"; } catch { }
            try { ColMethod.Header = "\\u0627\\u0644\\u0637\\u0631\\u064A\\u0642\\u0629"; } catch { }
            try { ColDel.Header = "\\u062D\\u0630\\u0641"; } catch { }
        }
        private void UserControl_Loaded(object sender, RoutedEventArgs e) { if (DataContext is PaymentsViewModel vm) vm.LoadPayments(); }
        private void DgPayments_MouseDoubleClick(object sender, MouseButtonEventArgs e) { if (DataContext is PaymentsViewModel vm && vm.SelectedPayment != null) { vm.EditCommand.Execute(null); } }
    }
}
'''
with open(os.path.join(VIEWS, "PaymentsView.xaml.cs"), "w", encoding="utf-8-sig") as f:
    f.write(cs_pay_view)
print("  OK: PaymentsView.xaml + .xaml.cs")

# ============================================================
# 8) إنشاء PaymentEditWindow.xaml + .xaml.cs (نافذة السند)
# ============================================================
print("\n[8/8] Creating PaymentEditWindow.xaml + .xaml.cs...")

xaml_pay_edit = '''<Window x:Class="Techtoon.UI.Views.PaymentEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Voucher" Height="700" Width="500" WindowStartupLocation="CenterScreen" FlowDirection="RightToLeft" Background="#F5F7FA" Loaded="Window_Loaded" FontFamily="Tahoma">
    <Grid Margin="14">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
        <TextBlock x:Name="LblTitle" Grid.Row="0" Grid.Column="0" Grid.ColumnSpan="2" FontSize="18" FontWeight="Bold" Foreground="#00695C" Margin="0,0,0,12"/>
        <TextBlock x:Name="LblVoucher" Grid.Row="1" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtVoucher" Grid.Row="1" Grid.Column="1" Padding="6,4" Margin="0,3" IsReadOnly="True"/>
        <TextBlock x:Name="LblDate" Grid.Row="2" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <DatePicker x:Name="DpDate" Grid.Row="2" Grid.Column="1" Padding="6,4" Margin="0,3"/>
        <TextBlock x:Name="LblParty" Grid.Row="3" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbParty" Grid.Row="3" Grid.Column="1" Padding="6,4" Margin="0,3" SelectionChanged="CmbParty_SelectionChanged"/>
        <TextBlock x:Name="LblInvoice" Grid.Row="4" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbInvoice" Grid.Row="4" Grid.Column="1" Padding="6,4" Margin="0,3" SelectionChanged="CmbInvoice_SelectionChanged"/>
        <TextBlock x:Name="LblAmount" Grid.Row="5" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtAmount" Grid.Row="5" Grid.Column="1" Padding="6,4" Margin="0,3" TextAlignment="Right"/>
        <TextBlock x:Name="LblMethod" Grid.Row="6" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbMethod" Grid.Row="6" Grid.Column="1" Padding="6,4" Margin="0,3"/>
        <TextBlock x:Name="LblCashBox" Grid.Row="7" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbCashBox" Grid.Row="7" Grid.Column="1" Padding="6,4" Margin="0,3"/>
        <TextBlock x:Name="LblAccount" Grid.Row="8" Grid.Column="0" VerticalAlignment="Center" Margin="0,5,8,5"/>
        <ComboBox x:Name="CmbAccount" Grid.Row="8" Grid.Column="1" Padding="6,4" Margin="0,3"/>
        <TextBlock x:Name="LblNotes" Grid.Row="9" Grid.Column="0" VerticalAlignment="Top" Margin="0,5,8,5"/>
        <TextBox x:Name="TxtNotes" Grid.Row="9" Grid.Column="1" Padding="6,4" Margin="0,3" Height="60" TextWrapping="Wrap" AcceptsReturn="True"/>
        <TextBlock x:Name="LblStatus" Grid.Row="10" Grid.Column="0" Grid.ColumnSpan="2" Foreground="#00897B" FontSize="12" Margin="0,5,0,5" TextWrapping="Wrap"/>
        <StackPanel Grid.Row="12" Grid.Column="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,10,0,0">
            <Button x:Name="BtnSave" Background="#00897B" Foreground="White" FontWeight="Bold" FontSize="14" Padding="30,10" BorderThickness="0" Cursor="Hand" Margin="0,0,10,0" Click="BtnSave_Click"/>
            <Button x:Name="BtnCancel" Background="#ECEFF1" Foreground="#37474F" FontWeight="SemiBold" FontSize="13" Padding="20,10" BorderThickness="0" Cursor="Hand" Click="BtnCancel_Click"/>
        </StackPanel>
    </Grid>
</Window>
'''
with open(os.path.join(VIEWS, "PaymentEditWindow.xaml"), "w", encoding="ascii") as f:
    f.write(xaml_pay_edit)

cs_pay_edit = '''using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Npgsql;

namespace Techtoon.UI.Views
{
    public class InvoiceOption
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get { return Total - Paid; } }
        public string Display { get { return InvoiceNumber + " - Rem: " + Remaining.ToString("N2"); } }
        public override string ToString() { return Display; }
    }

    public class CashBoxOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() { return Name ?? ""; } }
    public class AccountOption { public int Id { get; set; } public string Name { get; set; } public override string ToString() { return Name ?? ""; } }

    public partial class PaymentEditWindow : Window
    {
        private readonly string _paymentType;
        private readonly int? _editId;
        private List<PartyOption> _parties = new List<PartyOption>();
        private List<InvoiceOption> _invoices = new List<InvoiceOption>();
        private List<CashBoxOption> _cashBoxes = new List<CashBoxOption>();
        private List<AccountOption> _accounts = new List<AccountOption>();

        public static string GetCS() { return "''' + CS + '''"; }

        public PaymentEditWindow(string paymentType, int? editId = null)
        {
            InitializeComponent();
            _paymentType = paymentType;
            _editId = editId;
            SetArabicTexts();
        }

        private void SetArabicTexts()
        {
            try
            {
                if (_paymentType == "receipt") { Title = "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636"; LblTitle.Text = "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636 \\u062C\\u062F\\u064A\\u062F"; }
                else { Title = "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641"; LblTitle.Text = "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641 \\u062C\\u062F\\u064A\\u062F"; }
                LblVoucher.Text = "\\u0631\\u0642\\u0645 \\u0627\\u0644\\u0633\\u0646\\u062F:";
                LblDate.Text = "\\u0627\\u0644\\u062A\\u0627\\u0631\\u064A\\u062E:";
                LblParty.Text = "\\u0627\\u0644\\u0637\\u0631\\u0641:";
                LblInvoice.Text = "\\u0641\\u0627\\u062A\\u0648\\u0631\\u0629 \\u0645\\u0631\\u062A\\u0628\\u0637\\u0629:";
                LblAmount.Text = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A:";
                LblMethod.Text = "\\u0637\\u0631\\u064A\\u0642\\u0629 \\u0627\\u0644\\u062F\\u0641\\u0639:";
                LblCashBox.Text = "\\u0627\\u0644\\u062E\\u0632\\u064A\\u0646\\u0629:";
                LblAccount.Text = "\\u0627\\u0644\\u062D\\u0633\\u0627\\u0628:";
                LblNotes.Text = "\\u0645\\u0644\\u0627\\u062D\\u0638\\u0627\\u062A:";
                BtnSave.Content = "\\uD83D\\uDCBE \\u062D\\u0641\\u0638";
                BtnCancel.Content = "\\u2716 \\u0625\\u0644\\u063A\\u0627\\u0621";
                CmbMethod.Items.Clear();
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0646\\u0642\\u062F\\u064A", Tag = "cash" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0628\\u0637\\u0627\\u0642\\u0629", Tag = "card" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u062A\\u062D\\u0648\\u064A\\u0644 \\u0628\\u0646\\u0643\\u064A", Tag = "bank" });
                CmbMethod.Items.Add(new ComboBoxItem { Content = "\\u0634\\u064A\\u0643", Tag = "check" });
                CmbMethod.SelectedIndex = 0;
            }
            catch { }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DpDate.SelectedDate = DateTime.Today;
            TxtVoucher.Text = GenerateVoucherNumber();
            LoadParties(); LoadCashBoxes(); LoadAccounts();
            if (_editId.HasValue) LoadForEdit();
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private string GenerateVoucherNumber()
        {
            try
            {
                string prefix = _paymentType == "receipt" ? "RCV" : "PAY";
                int year = DateTime.Now.Year;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT voucher_no FROM payments WHERE voucher_no LIKE @p ORDER BY voucher_no DESC LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("p", prefix + "-" + year + "-%");
                        var o = cmd.ExecuteScalar();
                        if (o != null && o != DBNull.Value) { string last = o.ToString(); int d = last.LastIndexOf('-'); if (d >= 0 && int.TryParse(last.Substring(d + 1), out int seq)) return prefix + "-" + year + "-" + (seq + 1).ToString("D4"); }
                    }
                }
            }
            catch { }
            return (_paymentType == "receipt" ? "RCV" : "PAY") + "-" + DateTime.Now.Year + "-0001";
        }

        private void LoadParties()
        {
            try
            {
                _parties.Clear(); CmbParty.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM parties ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) { var p = new PartyOption { Id = r.GetInt32(0), Name = r.GetString(1) }; _parties.Add(p); CmbParty.Items.Add(p); }
                }
            }
            catch { }
        }

        private void LoadCashBoxes()
        {
            try
            {
                _cashBoxes.Clear(); CmbCashBox.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM cash_boxes ORDER BY name LIMIT 100", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) { var c = new CashBoxOption { Id = r.GetInt32(0), Name = r.GetString(1) }; _cashBoxes.Add(c); CmbCashBox.Items.Add(c); }
                }
                if (CmbCashBox.Items.Count > 0) CmbCashBox.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadAccounts()
        {
            try
            {
                _accounts.Clear(); CmbAccount.Items.Clear();
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT id, name FROM accounts ORDER BY name LIMIT 500", conn))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read()) { var a = new AccountOption { Id = r.GetInt32(0), Name = r.GetString(1) }; _accounts.Add(a); CmbAccount.Items.Add(a); }
                }
                if (CmbAccount.Items.Count > 0) CmbAccount.SelectedIndex = 0;
            }
            catch { }
        }

        private void CmbParty_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CmbInvoice.Items.Clear(); _invoices.Clear();
                if (CmbParty.SelectedItem is PartyOption party)
                {
                    string invoiceType = _paymentType == "receipt" ? "sales" : "purchase";
                    using (var conn = new NpgsqlConnection(GetCS()))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("SELECT id, invoice_number, total, paid_amount FROM invoices WHERE party_id = @pid AND type = @type AND paid_amount < total ORDER BY id DESC LIMIT 50", conn))
                        {
                            cmd.Parameters.AddWithValue("pid", party.Id);
                            cmd.Parameters.AddWithValue("type", invoiceType);
                            using (var r = cmd.ExecuteReader())
                                while (r.Read())
                                {
                                    var inv = new InvoiceOption { Id = r.GetInt32(0), InvoiceNumber = r.IsDBNull(1) ? "" : r.GetString(1), Total = r.IsDBNull(2) ? 0 : (decimal)r.GetDouble(2), Paid = r.IsDBNull(3) ? 0 : (decimal)r.GetDouble(3) };
                                    _invoices.Add(inv); CmbInvoice.Items.Add(inv);
                                }
                        }
                    }
                }
            }
            catch { }
        }

        private void CmbInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try { if (CmbInvoice.SelectedItem is InvoiceOption inv) { if (string.IsNullOrWhiteSpace(TxtAmount.Text) || TxtAmount.Text == "0") TxtAmount.Text = inv.Remaining.ToString("N2"); } }
            catch { }
        }

        private void LoadForEdit()
        {
            try
            {
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT voucher_no, date, party_id, type, amount, method, cash_box_id, account_id, notes FROM payments WHERE id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("id", _editId.Value);
                        using (var r = cmd.ExecuteReader())
                            if (r.Read())
                            {
                                TxtVoucher.Text = r.IsDBNull(0) ? "" : r.GetString(0);
                                DpDate.SelectedDate = r.IsDBNull(1) ? DateTime.Today : r.GetDateTime(1);
                                int partyId = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                                foreach (var p in _parties) if (p.Id == partyId) { CmbParty.SelectedItem = p; break; }
                                TxtAmount.Text = (r.IsDBNull(4) ? 0 : (decimal)r.GetDouble(4)).ToString("N2");
                                string method = r.IsDBNull(5) ? "cash" : r.GetString(5);
                                foreach (ComboBoxItem mi in CmbMethod.Items) if (mi.Tag?.ToString() == method) { CmbMethod.SelectedItem = mi; break; }
                                TxtNotes.Text = r.IsDBNull(8) ? "" : r.GetString(8);
                            }
                    }
                }
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!(CmbParty.SelectedItem is PartyOption party)) { LblStatus.Text = "\\u0627\\u062E\\u062A\\u0631 \\u0627\\u0644\\u0637\\u0631\\u0641"; return; }
                if (!decimal.TryParse(TxtAmount.Text, out decimal amount) || amount <= 0) { LblStatus.Text = "\\u0627\\u0644\\u0645\\u0628\\u0644\\u063A \\u063A\\u064A\\u0631 \\u0635\\u062D\\u064A\\u062D"; return; }
                string method = "cash"; if (CmbMethod.SelectedItem is ComboBoxItem mi && mi.Tag != null) method = mi.Tag.ToString();
                int? cashBoxId = null; if (CmbCashBox.SelectedItem is CashBoxOption cb) cashBoxId = cb.Id;
                int? accountId = null; if (CmbAccount.SelectedItem is AccountOption ac) accountId = ac.Id;
                int? linkedInvoiceId = null; if (CmbInvoice.SelectedItem is InvoiceOption inv) linkedInvoiceId = inv.Id;
                using (var conn = new NpgsqlConnection(GetCS()))
                {
                    conn.Open();
                    if (_editId.HasValue)
                    {
                        using (var cmd = new NpgsqlCommand("UPDATE payments SET voucher_no=@vno, date=@dt, party_id=@pid, party_name=(SELECT name FROM parties WHERE id=@pid), type=@tp, amount=@amt, method=@mth, cash_box_id=@cbid, account_id=@acid, notes=@nt WHERE id=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("vno", TxtVoucher.Text); cmd.Parameters.AddWithValue("dt", DpDate.SelectedDate ?? DateTime.Today); cmd.Parameters.AddWithValue("pid", party.Id); cmd.Parameters.AddWithValue("tp", _paymentType); cmd.Parameters.AddWithValue("amt", amount); cmd.Parameters.AddWithValue("mth", method); cmd.Parameters.AddWithValue("cbid", (object)cashBoxId ?? DBNull.Value); cmd.Parameters.AddWithValue("acid", (object)accountId ?? DBNull.Value); cmd.Parameters.AddWithValue("nt", (object)TxtNotes.Text ?? DBNull.Value); cmd.Parameters.AddWithValue("id", _editId.Value); cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new NpgsqlCommand("INSERT INTO payments (voucher_no, date, party_id, party_name, type, amount, method, cash_box_id, account_id, notes) VALUES (@vno, @dt, @pid, (SELECT name FROM parties WHERE id=@pid), @tp, @amt, @mth, @cbid, @acid, @nt)", conn))
                        {
                            cmd.Parameters.AddWithValue("vno", TxtVoucher.Text); cmd.Parameters.AddWithValue("dt", DpDate.SelectedDate ?? DateTime.Today); cmd.Parameters.AddWithValue("pid", party.Id); cmd.Parameters.AddWithValue("tp", _paymentType); cmd.Parameters.AddWithValue("amt", amount); cmd.Parameters.AddWithValue("mth", method); cmd.Parameters.AddWithValue("cbid", (object)cashBoxId ?? DBNull.Value); cmd.Parameters.AddWithValue("acid", (object)accountId ?? DBNull.Value); cmd.Parameters.AddWithValue("nt", (object)TxtNotes.Text ?? DBNull.Value); cmd.ExecuteNonQuery();
                        }
                    }
                    if (linkedInvoiceId.HasValue) { using (var upCmd = new NpgsqlCommand("UPDATE invoices SET paid_amount = paid_amount + @amt WHERE id = @pid", conn)) { upCmd.Parameters.AddWithValue("amt", amount); upCmd.Parameters.AddWithValue("pid", linkedInvoiceId.Value); upCmd.ExecuteNonQuery(); } }
                }
                LblStatus.Text = "\\u062A\\u0645 \\u0627\\u0644\\u062D\\u0641\\u0638 \\u0628\\u0646\\u062C\\u0627\\u062D";
                DialogResult = true; Close();
            }
            catch (Exception ex) { LblStatus.Text = "\\u062E\\u0637\\u0623: " + ex.Message; }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
'''
with open(os.path.join(VIEWS, "PaymentEditWindow.xaml.cs"), "w", encoding="utf-8-sig") as f:
    f.write(cs_pay_edit)
print("  OK: PaymentEditWindow.xaml + .xaml.cs")

# ============================================================
print("\n" + "=" * 60)
print("  DONE! Now run:")
print("  dotnet build Techtoon.UI")
print("  dotnet run --project Techtoon.UI")
print("=" * 60)
 $file = "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditViewModel.cs"
 $content = Get-Content $file -Raw -Encoding UTF8

# أضف دالة LoadFromInvoiceAsync قبل SaveAsync
if ($content -notmatch 'LoadFromInvoiceAsync') {
    $marker = "public async System.Threading.Tasks.Task SaveAsync()"
    $idx = $content.IndexOf($marker)
    if ($idx -gt 0) {
        $method = @'
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
                    // Load lines
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
                    // Load payments
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

        '@
        $content = $content.Substring(0, $idx) + $method + $content.Substring($idx)
    }
}

# أصلح الاستدعاء في xaml.cs (السطر 16)
 $file2 = "D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\Views\InvoiceEditWindow.xaml.cs"
 $content2 = Get-Content $file2 -Raw -Encoding UTF8
 $content2 = $content2 -replace 'await _vm\.LoadFromInvoiceAsync\?\.\w+\(_editId\.Value\);', 'await _vm.LoadFromInvoiceAsync(_editId.Value);'
[System.IO.File]::WriteAllText($file2, $content2, [System.Text.UTF8Encoding]::new($true))

[System.IO.File]::WriteAllText($file, $content, [System.Text.UTF8Encoding]::new($true))
Write-Host "OK: Added LoadFromInvoiceAsync method + fixed call" -ForegroundColor Green