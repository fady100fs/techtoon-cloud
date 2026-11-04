# fix_voucher2.py - إعادة كتابة PaymentEditViewModel.cs + ربط الفواتير
import os

FILE = r"D:\PROG\Accounting Prog\Techtoon Accounting C+\Techtoon.UI\ViewModels\PaymentEditViewModel.cs"
BACKUP = FILE + ".bak_v2"

# نسخة احتياطية
import shutil
if os.path.exists(FILE):
    shutil.copy2(FILE, BACKUP)
    print(f"Backup: {BACKUP}")

# كتابة الملف الجديد بـ UTF-8 مع BOM
content = '''using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class PaymentEditViewModel : ViewModelBase
{
    private DateTime _date = DateTime.Today;
    private Party? _party;
    private double _amount;
    private string _paymentType;
    private string _paymentMethod = "cash";
    private CashBox? _cashBox;
    private Account? _counterAccount;
    private string _notes = "";
    private ObservableCollection<Party> _parties = new();
    private ObservableCollection<CashBox> _cashBoxes = new();
    private ObservableCollection<Account> _accounts = new();
    private ObservableCollection<Invoice> _unpaidInvoices = new();
    private Invoice? _selectedInvoice;
    private string _statusMessage = "";

    public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
    public Party? Party
    {
        get => _party;
        set
        {
            if (SetProperty(ref _party, value))
            {
                _ = LoadUnpaidInvoicesAsync();
            }
        }
    }
    public double Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public string PaymentType { get => _paymentType; set => SetProperty(ref _paymentType, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public CashBox? CashBox { get => _cashBox; set => SetProperty(ref _cashBox, value); }
    public Account? CounterAccount { get => _counterAccount; set => SetProperty(ref _counterAccount, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public ObservableCollection<CashBox> CashBoxes { get => _cashBoxes; set => SetProperty(ref _cashBoxes, value); }
    public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
    public ObservableCollection<Invoice> UnpaidInvoices { get => _unpaidInvoices; set => SetProperty(ref _unpaidInvoices, value); }
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                if (_selectedInvoice != null)
                {
                    // Auto-fill amount with remaining
                    var remaining = (_selectedInvoice.TotalAmount ?? 0) - (_selectedInvoice.PaidAmount ?? 0);
                    if (remaining > 0) Amount = remaining;
                }
            }
        }
    }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsReceipt => PaymentType == "receipt";
    public string WindowTitle => IsReceipt ? "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636" : "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public PaymentEditViewModel(string paymentType)
    {
        PaymentType = paymentType;
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async override Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Parties = new ObservableCollection<Party>(await db.Parties.OrderBy(p => p.Name).ToListAsync());
            CashBoxes = new ObservableCollection<CashBox>(await db.CashBoxes.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync());
            Accounts = new ObservableCollection<Account>(await db.Accounts.OrderBy(a => a.Code).ToListAsync());
            CashBox = CashBoxes.FirstOrDefault();
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task LoadUnpaidInvoicesAsync()
    {
        try
        {
            UnpaidInvoices.Clear();
            if (_party == null) return;
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            // ابحث عن فواتير هذا الطرف غير مدفوعة بالكامل
            var invoiceType = IsReceipt ? "sales" : "purchase";
            var invoices = await db.Invoices
                .Where(i => i.PartyId == _party.Id && i.Type == invoiceType && i.PaidAmount < i.TotalAmount)
                .OrderByDescending(i => i.Id)
                .Take(50)
                .ToListAsync();
            UnpaidInvoices = new ObservableCollection<Invoice>(invoices);
        }
        catch { }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (Party == null) { StatusMessage = "Select party"; return; }
            if (CashBox == null || CashBox.Account == null) { StatusMessage = "Select cash box"; return; }
            if (CounterAccount == null) { StatusMessage = "Select account"; return; }
            if (Amount <= 0) { StatusMessage = "Amount > 0"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();

            var prefix = IsReceipt ? "RCV-" : "PAY-";
            var year = DateTime.Now.Year;
            var count = await db.Payments.CountAsync(p => p.ReferenceNumber != null && p.ReferenceNumber.StartsWith(prefix + year));
            var refNum = prefix + year + "-" + (count + 1).ToString("D4");

            var payment = new Payment
            {
                Date = Date, PartyId = Party.Id, Amount = Amount, PaymentType = PaymentType,
                PaymentMethod = PaymentMethod, ReferenceNumber = refNum,
                Notes = string.IsNullOrEmpty(Notes) ? null : Notes, CurrencyId = 1, CashBoxId = CashBox.Id
            };
            db.Payments.Add(payment);

            var cashBox = await db.CashBoxes.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == CashBox.Id);
            var counterAcc = await db.Accounts.FirstOrDefaultAsync(a => a.Id == CounterAccount.Id);
            if (cashBox?.Account != null && counterAcc != null)
            {
                var je = new JournalEntry { Date = Date, Description = (IsReceipt ? "\\u0633\\u0646\\u062F \\u0642\\u0628\\u0636 - " : "\\u0633\\u0646\\u062F \\u0635\\u0631\\u0641 - ") + Party.Name + " (" + refNum + ")", ReferenceType = "payment" };
                if (IsReceipt) { je.Lines.Add(new JournalLine { AccountId = cashBox.Account.Id, Debit = Amount }); je.Lines.Add(new JournalLine { AccountId = counterAcc.Id, Credit = Amount }); }
                else { je.Lines.Add(new JournalLine { AccountId = counterAcc.Id, Debit = Amount }); je.Lines.Add(new JournalLine { AccountId = cashBox.Account.Id, Credit = Amount }); }
                db.JournalEntries.Add(je);
            }

            // Update linked invoice paid_amount
            if (SelectedInvoice != null)
            {
                var inv = await db.Invoices.FirstOrDefaultAsync(i => i.Id == SelectedInvoice.Id);
                if (inv != null)
                {
                    inv.PaidAmount = (inv.PaidAmount ?? 0) + Amount;
                }
            }

            await db.SaveChangesAsync();
            StatusMessage = "Saved OK - " + refNum; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m+= "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
'''

with open(FILE, "w", encoding="utf-8-sig") as f:
    f.write(content)

print("OK: PaymentEditViewModel.cs rewritten")
print("  - Fixed Arabic encoding (Unicode escapes)")
print("  - Added UnpaidInvoices collection")
print("  - Added SelectedInvoice property")
print("  - Auto-load invoices when Party selected")
print("  - Auto-fill amount on invoice selection")
print("  - Update paid_amount on save")
print()
print("Now run: dotnet build Techtoon.UI")