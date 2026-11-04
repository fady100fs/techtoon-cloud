using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels;

public class InvoiceLineItem : INotifyPropertyChanged
{
    private Item? _item;
    private double _quantity = 1;
    private double _price;
    private double _total;
    private string _searchText = "";
    private ObservableCollection<Item> _filteredItems = new();

    public ObservableCollection<Item> AvailableItems { get; set; } = new();
    public ObservableCollection<Item> FilteredItems { get => _filteredItems; set => SetField(ref _filteredItems, value); }
    public string SearchText
    {
        get => _searchText;
        set { if (SetField(ref _searchText, value)) UpdateFilteredItems(); }
    }
    public Item? Item
    {
        get => _item;
        set
        {
            if (SetField(ref _item, value))
            {
                if (_item != null) { Price = _item.SellPrice; SearchText = _item.SearchDisplay; }
                Recompute();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemName)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ItemId)));
            }
        }
    }
    public int ItemId => Item?.Id ?? 0;
    public string ItemName => Item?.Name ?? "";
    public double Quantity { get => _quantity; set { if (SetField(ref _quantity, value)) Recompute(); } }
    public double Price { get => _price; set { if (SetField(ref _price, value)) Recompute(); } }
    public double Total { get => _total; set => SetField(ref _total, value); }

    public void InitializeFilteredItems() => FilteredItems = new ObservableCollection<Item>(AvailableItems);

    private void UpdateFilteredItems()
    {
        if (AvailableItems == null || AvailableItems.Count == 0) { FilteredItems = new ObservableCollection<Item>(); return; }
        if (AvailableItems.Any(i => i.SearchDisplay == SearchText)) return;
        if (string.IsNullOrWhiteSpace(SearchText)) { FilteredItems = new ObservableCollection<Item>(AvailableItems); return; }
        var s = SearchText.Trim().ToLower();
        FilteredItems = new ObservableCollection<Item>(AvailableItems.Where(i => (i.Name?.ToLower().Contains(s) == true) || (i.DisplayCode?.ToLower().Contains(s) == true) || (i.Barcode?.ToLower().Contains(s) == true) || (i.SearchDisplay?.ToLower().Contains(s) == true)).Take(50));
    }

    private void Recompute() => Total = Quantity * Price;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}

public class InvoicePaymentRow : ViewModelBase
{
    private double _amount;
    private string _paymentMethod = "cash";
    private string _referenceNumber = "";
    public int Id { get; set; }
    public double Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public string ReferenceNumber { get => _referenceNumber; set => SetProperty(ref _referenceNumber, value); }
}

public class InvoiceEditViewModel : ViewModelBase
{
    private int _id;
    private string _invoiceNumber = "";
    private DateTime _date = DateTime.Today;
    private Party? _party;
    private string _type = "sale";
    private double _discountPercentage;
    private double _discountAmount;
    private double _taxRate = 14;
    private ObservableCollection<Party> _parties = new();
    private ObservableCollection<Item> _items = new();
    private ObservableCollection<InvoiceLineItem> _lines = new();
    private string _statusMessage = "";
    private double _subtotal;
    private double _taxAmount;
    private double _netAmount;

    private ObservableCollection<InvoicePaymentRow> _invoicePayments = new();
    private double _newPayAmount;
    private string _newPayMethod = "cash";
    private string _newPayRef = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string InvoiceNumber { get => _invoiceNumber; set => SetProperty(ref _invoiceNumber, value); }
    public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
    public Party? Party { get => _party; set => SetProperty(ref _party, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public double DiscountPercentage { get => _discountPercentage; set { if (SetProperty(ref _discountPercentage, value)) RecomputeTotals(); } }
    public double DiscountAmount { get => _discountAmount; set { if (SetProperty(ref _discountAmount, value)) RecomputeTotals(); } }
    public double TaxRate { get => _taxRate; set { if (SetProperty(ref _taxRate, value)) RecomputeTotals(); } }
    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public ObservableCollection<Item> Items { get => _items; set => SetProperty(ref _items, value); }
    public ObservableCollection<InvoiceLineItem> Lines { get => _lines; set { if (SetProperty(ref _lines, value)) { foreach (var l in _lines) { l.PropertyChanged += Line_PropertyChanged; l.AvailableItems = Items; l.InitializeFilteredItems(); } } } }
    public double Subtotal { get => _subtotal; set => SetProperty(ref _subtotal, value); }
    public double TaxAmount { get => _taxAmount; set => SetProperty(ref _taxAmount, value); }
    public double NetAmount { get => _netAmount; set => SetProperty(ref _netAmount, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل الفاتورة" : "فاتورة جديدة";
    public ObservableCollection<InvoicePaymentRow> InvoicePayments { get => _invoicePayments; set => SetProperty(ref _invoicePayments, value); }
    public double NewPayAmount { get => _newPayAmount; set => SetProperty(ref _newPayAmount, value); }
    public string NewPayMethod { get => _newPayMethod; set => SetProperty(ref _newPayMethod, value); }
    public string NewPayRef { get => _newPayRef; set => SetProperty(ref _newPayRef, value); }
    public bool ShowPaymentsSection => IsEdit;

    public ICommand AddLineCommand { get; }
    public ICommand RemoveLineCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand DeletePaymentCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public InvoiceEditViewModel()
    {
        AddLineCommand = new RelayCommand(AddLine);
        RemoveLineCommand = new RelayCommand<InvoiceLineItem>(RemoveLine);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
        AddPaymentCommand = new RelayCommand(async () => await AddPaymentAsync());
        DeletePaymentCommand = new RelayCommand<InvoicePaymentRow>(async (p) => await DeletePaymentAsync(p));
        _lines.CollectionChanged += (s, e) => { if (e.NewItems != null) foreach (InvoiceLineItem l in e.NewItems) { l.PropertyChanged += Line_PropertyChanged; l.AvailableItems = Items; l.InitializeFilteredItems(); } RecomputeTotals(); };
    }

    public async override Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Parties = new ObservableCollection<Party>(await db.Parties.OrderBy(p => p.Name).ToListAsync());
            Items = new ObservableCollection<Item>(await db.Items.OrderBy(i => i.Name).ToListAsync());
            foreach (var line in Lines) { line.AvailableItems = Items; line.InitializeFilteredItems(); }
            if (Id == 0) await GenerateInvoiceNumberAsync();
            else await LoadFromInvoiceAsync(Id);
        }
        catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
    }

    private async Task GenerateInvoiceNumberAsync()
    {
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var prefix = Type == "sale" ? "SAL-" : "PUR-"; var year = DateTime.Now.Year; var count = await db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix + year)); InvoiceNumber = prefix + year + "-" + (count + 1).ToString("D4"); }
        catch { }
    }

    public async Task LoadFromInvoiceAsync(int invoiceId)
    {
        try
        {
            Id = invoiceId;
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var invoice = await db.Invoices.Include(i => i.Party).Include(i => i.Lines).ThenInclude(l => l.Item).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return;
            InvoiceNumber = invoice.InvoiceNumber; Date = invoice.Date; Type = invoice.Type; Party = invoice.Party; DiscountPercentage = invoice.DiscountPercentage; TaxRate = invoice.TaxRate;
            Lines.Clear();
            foreach (var line in invoice.Lines) { var l = new InvoiceLineItem { Item = line.Item, Quantity = line.Quantity, Price = line.Price }; l.AvailableItems = Items; l.InitializeFilteredItems(); Lines.Add(l); }
            RecomputeTotals();
            OnPropertyChanged(nameof(WindowTitle)); OnPropertyChanged(nameof(ShowPaymentsSection));
            var payments = await db.InvoicePayments.Where(p => p.InvoiceId == invoiceId).ToListAsync();
            InvoicePayments = new ObservableCollection<InvoicePaymentRow>(payments.Select(p => new InvoicePaymentRow { Id = p.Id, Amount = p.Amount, PaymentMethod = p.PaymentMethod, ReferenceNumber = p.ReferenceNumber ?? "" }));
        }
        catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
    }

    private void AddLine() { if (Items.Count == 0) { StatusMessage = "لا توجد أصناف."; return; } var line = new InvoiceLineItem { AvailableItems = Items }; line.InitializeFilteredItems(); Lines.Add(line); }
    private void RemoveLine(InvoiceLineItem? line) { if (line != null) Lines.Remove(line); }
    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(InvoiceLineItem.Total)) RecomputeTotals(); }
    private void RecomputeTotals() { Subtotal = Lines.Sum(l => l.Total); if (DiscountPercentage > 0) DiscountAmount = Subtotal * DiscountPercentage / 100; var afterDiscount = Subtotal - DiscountAmount; TaxAmount = afterDiscount * TaxRate / 100; NetAmount = afterDiscount + TaxAmount; }

    private async Task SaveAsync()
    {
        try
        {
            if (Party == null) { StatusMessage = "اختر العميل/المورد"; return; }
            if (Lines.Count == 0) { StatusMessage = "أضف بنداً واحداً على الأقل"; return; }
            if (Lines.Any(l => l.Item == null)) { StatusMessage = "اختر صنفاً لكل بند"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Invoice invoice;
            if (Id > 0) { invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == Id) ?? new Invoice(); db.InvoiceLines.RemoveRange(invoice.Lines); }
            else { invoice = new Invoice(); db.Invoices.Add(invoice); }
            invoice.InvoiceNumber = InvoiceNumber; invoice.Date = Date; invoice.PartyId = Party.Id; invoice.Type = Type; invoice.TotalAmount = Subtotal; invoice.DiscountAmount = Subtotal * DiscountPercentage / 100; invoice.DiscountPercentage = DiscountPercentage; invoice.TaxRate = TaxRate; invoice.TaxAmount = TaxAmount; invoice.NetAmount = NetAmount; invoice.Status = "pending"; invoice.CurrencyId = 1;
            foreach (var line in Lines) invoice.Lines.Add(new InvoiceLine { ItemId = line.ItemId, Quantity = line.Quantity, Price = line.Price, Total = line.Total, CostPrice = line.Item?.CostPrice ?? 0 });
            await db.SaveChangesAsync();
            try { await InventoryService.UpdateStockForInvoiceAsync(db, invoice, Id > 0); } catch (Exception stockEx) { System.Diagnostics.Debug.WriteLine("Inventory update error: " + stockEx.Message); }
            StatusMessage = "✅ تم حفظ الفاتورة بنجاح - رقم: " + invoice.InvoiceNumber; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var inner = ex.InnerException; var msg = "Error: " + ex.Message; while (inner != null) { msg += "\n→ " + inner.Message; inner = inner.InnerException; } StatusMessage = msg; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task AddPaymentAsync()
    {
        if (NewPayAmount <= 0) { StatusMessage = "المبلغ يجب أن يكون أكبر من صفر"; return; }
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var payment = new InvoicePayment { InvoiceId = Id, Date = DateTime.UtcNow, Amount = NewPayAmount, PaymentMethod = NewPayMethod, ReferenceNumber = string.IsNullOrEmpty(NewPayRef) ? null : NewPayRef, CreatedAt = DateTime.UtcNow }; db.InvoicePayments.Add(payment); await db.SaveChangesAsync(); InvoicePayments.Add(new InvoicePaymentRow { Id = payment.Id, Amount = payment.Amount, PaymentMethod = payment.PaymentMethod, ReferenceNumber = payment.ReferenceNumber ?? "" }); NewPayAmount = 0; NewPayRef = ""; StatusMessage = "✅ تمت إضافة الدفعة بنجاح"; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task DeletePaymentAsync(InvoicePaymentRow? pay)
    {
        if (pay == null) return;
        try { using var scope = App.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>(); var p = await db.InvoicePayments.FirstOrDefaultAsync(x => x.Id == pay.Id); if (p != null) { db.InvoicePayments.Remove(p); await db.SaveChangesAsync(); InvoicePayments.Remove(pay); StatusMessage = "✅ تم حذف الدفعة"; } }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
