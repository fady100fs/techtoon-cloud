using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class PartyEditViewModel : ViewModelBase
{
    private int _id;
    private string _name = "";
    private string _type = "customer";
    private string _phone = "";
    private string _address = "";
    private double _creditLimit;
    private double _openingBalance;
    private Account? _account;
    private ObservableCollection<Account> _accounts = new();
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public string Phone { get => _phone; set => SetProperty(ref _phone, value); }
    public string Address { get => _address; set => SetProperty(ref _address, value); }
    public double CreditLimit { get => _creditLimit; set => SetProperty(ref _creditLimit, value); }
    public double OpeningBalance { get => _openingBalance; set => SetProperty(ref _openingBalance, value); }
    public Account? Account { get => _account; set => SetProperty(ref _account, value); }
    public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "تعديل عميل/مورد" : "عميل/مورد جديد";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public PartyEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
    }

    public async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Accounts = new ObservableCollection<Account>(await db.Accounts.OrderBy(a => a.Code).ToListAsync());
            if (Id > 0)
            {
                var p = await db.Parties.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == Id);
                if (p != null)
                {
                    Name = p.Name; Type = p.Type; Phone = p.Phone ?? ""; Address = p.Address ?? "";
                    CreditLimit = p.CreditLimit; OpeningBalance = p.OpeningBalance;
                    Account = Accounts.FirstOrDefault(a => a.Id == p.AccountId);
                }
            }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "Name required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Party p;
            if (Id > 0) { p = await db.Parties.FirstOrDefaultAsync(x => x.Id == Id); if (p == null) { StatusMessage = "Not found"; return; } }
            else { p = new Party(); db.Parties.Add(p); }
            p.Name = Name; p.Type = Type; p.Phone = string.IsNullOrEmpty(Phone) ? null : Phone;
            p.Address = string.IsNullOrEmpty(Address) ? null : Address; p.CreditLimit = CreditLimit; p.OpeningBalance = OpeningBalance;
            if (Account != null) { var acc = await db.Accounts.FirstOrDefaultAsync(a => a.Id == Account.Id); if (acc != null) p.AccountId = acc.Id; }
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
