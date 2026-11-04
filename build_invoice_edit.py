# build_invoice_edit.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C#")
PAGES = ROOT / "Techtoon.Blazor" / "Pages"

# ========================================================
# InvoiceEdit.razor
# ========================================================

(PAGES / "InvoiceEdit.razor").write_text("""@page "/invoices/edit/{Id:int}"
@inject TechtoonDbContext db
@inject NavigationManager Nav

<h1 class="mb-4">@(Id == 0 ? "فاتورة جديدة" : "تعديل فاتورة")</h1>

<div class="card shadow-sm mb-4">
    <div class="card-body">
        <div class="row g-3">
            <div class="col-md-3">
                <label class="form-label">رقم الفاتورة</label>
                <input class="form-control" value="@_invoiceNumber" readonly />
            </div>
            <div class="col-md-3">
                <label class="form-label">التاريخ</label>
                <input type="date" class="form-control" @bind="_date" />
            </div>
            <div class="col-md-3">
                <label class="form-label">النوع</label>
                <select class="form-select" @bind="_type">
                    <option value="sale">مبيعات</option>
                    <option value="purchase">مشتريات</option>
                </select>
            </div>
            <div class="col-md-3">
                <label class="form-label">العميل/المورد</label>
                <select class="form-select" @bind="_partyId">
                    @foreach (var p in _parties)
                    {
                        <option value="@p.Id">@p.Name</option>
                    }
                </select>
            </div>
        </div>
    </div>
</div>

<div class="card shadow-sm mb-4">
    <div class="card-header d-flex justify-content-between align-items-center">
        <h5 class="mb-0">بنود الفاتورة</h5>
        <button class="btn btn-sm btn-primary" @onclick="AddLine">+ إضافة بند</button>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover mb-0">
                <thead class="table-light">
                    <tr>
                        <th style="width: 40%;">الصنف</th>
                        <th style="width: 15%;">الكمية</th>
                        <th style="width: 15%;">السعر</th>
                        <th style="width: 15%;">الإجمالي</th>
                        <th style="width: 15%;">حذف</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var line in _lines)
                    {
                        <tr>
                            <td>
                                <div class="position-relative">
                                    <input class="form-control" 
                                           value="@line.SearchText" 
                                           @oninput="(e) => OnItemSearchInput(e, line)"
                                           @onblur="() => HideDropdown(line)"
                                           placeholder="ابحث بالاسم أو الكود..." />
                                    @if (line.ShowDropdown && line.FilteredItems.Any())
                                    {
                                        <div class="dropdown-menu show w-100" style="position:absolute; top:100%; z-index:1000; max-height:200px; overflow-y:auto;">
                                            @foreach (var item in line.FilteredItems)
                                            {
                                                <button class="dropdown-item text-end" @onclick="() => SelectItem(line, item)">
                                                    @($"ITM-{item.Id:D5} - {item.Name}")
                                                </button>
                                            }
                                        </div>
                                    }
                                </div>
                            </td>
                            <td><input type="number" class="form-control" @bind="line.Quantity" @bind:event="onchange" /></td>
                            <td><input type="number" class="form-control" @bind="line.Price" @bind:event="onchange" /></td>
                            <td>@((line.Quantity * line.Price).ToString("N2"))</td>
                            <td><button class="btn btn-sm btn-outline-danger" @onclick="() => RemoveLine(line)">🗑️</button></td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
</div>

<div class="row mb-4">
    <div class="col-md-6"></div>
    <div class="col-md-6">
        <div class="card shadow-sm">
            <div class="card-body">
                <div class="d-flex justify-content-between mb-2">
                    <span>المجموع:</span>
                    <strong>@_subtotal.ToString("N2")</strong>
                </div>
                <div class="d-flex justify-content-between mb-2">
                    <span>الضريبة (14%):</span>
                    <strong>@_taxAmount.ToString("N2")</strong>
                </div>
                <div class="d-flex justify-content-between fs-4 text-primary">
                    <span>الإجمالي:</span>
                    <strong>@_netAmount.ToString("N2")</strong>
                </div>
            </div>
        </div>
    </div>
</div>

<div class="d-flex justify-content-end gap-2 mb-4">
    <button class="btn btn-secondary" @onclick="Cancel">إلغاء</button>
    <button class="btn btn-primary" @onclick="Save">💾 حفظ الفاتورة</button>
</div>

@if (Id > 0)
{
    <div class="card shadow-sm">
        <div class="card-header">
            <h5 class="mb-0">المدفوعات على هذه الفاتورة</h5>
        </div>
        <div class="card-body">
            <div class="row g-2 mb-3">
                <div class="col-md-3">
                    <label class="form-label">المبلغ</label>
                    <input type="number" class="form-control" @bind="_newPayAmount" />
                </div>
                <div class="col-md-4">
                    <label class="form-label">طريقة الدفع</label>
                    <select class="form-select" @bind="_newPayMethod">
                        <option value="cash">نقدي</option>
                        <option value="bank_transfer">تحويل بنكي</option>
                        <option value="cheque">شيك</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label">المرجع</label>
                    <input class="form-control" @bind="_newPayRef" />
                </div>
                <div class="col-md-2 d-flex align-items-end">
                    <button class="btn btn-success w-100" @onclick="AddPayment">+ دفعة</button>
                </div>
            </div>

            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>التاريخ</th>
                        <th>المبلغ</th>
                        <th>الطريقة</th>
                        <th>المرجع</th>
                        <th>حذف</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var pay in _invoicePayments)
                    {
                        <tr>
                            <td>@pay.Date.ToString("yyyy-MM-dd")</td>
                            <td>@pay.Amount.ToString("N2")</td>
                            <td>@pay.PaymentMethod</td>
                            <td>@pay.ReferenceNumber</td>
                            <td><button class="btn btn-sm btn-outline-danger" @onclick="() => DeletePayment(pay)">🗑️</button></td>
                        </tr>
                    }
                    @if (!_invoicePayments.Any())
                    {
                        <tr><td colspan="5" class="text-center text-muted">لا توجد مدفوعات مسجلة بعد.</td></tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
}

@code {
    [Parameter] public int Id { get; set; }
    private string _invoiceNumber = "";
    private DateTime _date = DateTime.Today;
    private string _type = "sale";
    private int _partyId;
    private List<Party> _parties = new();
    private List<Item> _allItems = new();
    private List<LineItem> _lines = new();

    // Payments
    private List<InvoicePayment> _invoicePayments = new();
    private double _newPayAmount;
    private string _newPayMethod = "cash";
    private string _newPayRef = "";

    private double _subtotal => _lines.Sum(l => l.Quantity * l.Price);
    private double _taxAmount => _subtotal * 0.14;
    private double _netAmount => _subtotal + _taxAmount;

    protected override async Task OnInitializedAsync()
    {
        _parties = await db.Parties.OrderBy(p => p.Name).ToListAsync();
        _allItems = await db.Items.OrderBy(i => i.Name).Take(500).ToListAsync();

        if (Id > 0)
        {
            var inv = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == Id);
            if (inv != null)
            {
                _invoiceNumber = inv.InvoiceNumber;
                _date = inv.Date;
                _type = inv.Type;
                _partyId = inv.PartyId;

                foreach (var line in inv.Lines)
                {
                    var item = _allItems.FirstOrDefault(x => x.Id == line.ItemId);
                    _lines.Add(new LineItem
                    {
                        ItemId = line.ItemId,
                        SearchText = item != null ? $@"ITM-{item.Id:D5} - {item.Name}" : "",
                        Quantity = line.Quantity,
                        Price = line.Price
                    });
                }

                _invoicePayments = await db.InvoicePayments.Where(p => p.InvoiceId == Id).ToListAsync();
            }
        }
        else
        {
            var year = DateTime.Now.Year;
            var prefix = _type == "sale" ? "SAL-" : "PUR-";
            var count = await db.Invoices.CountAsync(i => i.InvoiceNumber.StartsWith(prefix + year));
            _invoiceNumber = $@"{prefix}{year}-{count + 1:D4}";
            _lines.Add(new LineItem());
        }
    }

    private void AddLine() => _lines.Add(new LineItem());
    private void RemoveLine(LineItem line) => _lines.Remove(line);

    private void OnItemSearchInput(ChangeEventArgs e, LineItem line)
    {
        line.SearchText = e.Value?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(line.SearchText))
        {
            line.ShowDropdown = false;
            return;
        }
        line.FilteredItems = _allItems.Where(i => 
            i.Name.Contains(line.SearchText, StringComparison.OrdinalIgnoreCase) || 
            $@"ITM-{i.Id:D5}".Contains(line.SearchText, StringComparison.OrdinalIgnoreCase))
            .Take(10).ToList();
        line.ShowDropdown = line.FilteredItems.Any();
    }

    private void HideDropdown(LineItem line) => line.ShowDropdown = false;

    private void SelectItem(LineItem line, Item item)
    {
        line.ItemId = item.Id;
        line.SearchText = $@"ITM-{item.Id:D5} - {item.Name}";
        line.Price = item.SellPrice;
        line.ShowDropdown = false;
    }

    private async Task Save()
    {
        if (_partyId == 0 || !_lines.Any(l => l.ItemId > 0)) return;

        var inv = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == Id);
        if (inv == null)
        {
            inv = new Invoice();
            db.Invoices.Add(inv);
        }
        else
        {
            db.InvoiceLines.RemoveRange(inv.Lines);
        }

        inv.InvoiceNumber = _invoiceNumber;
        inv.Date = _date;
        inv.Type = _type;
        inv.PartyId = _partyId;
        inv.TotalAmount = _subtotal;
        inv.TaxAmount = _taxAmount;
        inv.NetAmount = _netAmount;
        inv.Status = "pending";
        inv.CurrencyId = 1;

        foreach (var line in _lines.Where(l => l.ItemId > 0))
        {
            inv.Lines.Add(new InvoiceLine { ItemId = line.ItemId, Quantity = line.Quantity, Price = line.Price, Total = line.Quantity * line.Price });
        }

        await db.SaveChangesAsync();
        Nav.NavigateTo("/invoices");
    }

    private async Task AddPayment()
    {
        if (_newPayAmount <= 0) return;
        db.InvoicePayments.Add(new InvoicePayment
        {
            InvoiceId = Id,
            Date = DateTime.UtcNow,
            Amount = _newPayAmount,
            PaymentMethod = _newPayMethod,
            ReferenceNumber = _newPayRef
        });
        await db.SaveChangesAsync();
        _invoicePayments = await db.InvoicePayments.Where(p => p.InvoiceId == Id).ToListAsync();
        _newPayAmount = 0; _newPayRef = "";
    }

    private async Task DeletePayment(InvoicePayment pay)
    {
        db.InvoicePayments.Remove(pay);
        await db.SaveChangesAsync();
        _invoicePayments.Remove(pay);
    }

    private void Cancel() => Nav.NavigateTo("/invoices");

    public class LineItem
    {
        public int ItemId { get; set; }
        public string SearchText { get; set; } = "";
        public double Quantity { get; set; } = 1;
        public double Price { get; set; }
        public bool ShowDropdown { get; set; }
        public List<Item> FilteredItems { get; set; } = new();
    }
}
""", encoding="utf-8")
print("Created: InvoiceEdit.razor")

print("\\n=== Invoice Edit Page Built ===")