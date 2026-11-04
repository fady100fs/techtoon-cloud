using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class AccountTypeOption
{
    public string Value { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public AccountTypeOption(string v, string d) { Value = v; DisplayName = d; }
    public static List<AccountTypeOption> GetAll() => new() {
        new("ASSET", "Ø£ØµÙˆÙ„"), new("LIABILITY", "Ø®ØµÙˆÙ…"), new("EQUITY", "Ø­Ù‚ÙˆÙ‚ Ù…Ù„ÙƒÙŠØ©"),
        new("REVENUE", "Ø¥ÙŠØ±Ø§Ø¯Ø§Øª"), new("EXPENSE", "Ù…ØµØ±ÙˆÙØ§Øª")
    };
    public override string ToString() => DisplayName;
}

public class AccountEditViewModel : ViewModelBase
{
    private int _id;
    private string _code = "";
    private string _name = "";
    private string _type = "ASSET";
    private AccountTypeOption? _typeOption;
    private Account? _parent;
    private ObservableCollection<Account> _allAccounts = new();
    private List<AccountTypeOption> _typeOptions = AccountTypeOption.GetAll();
    private string _statusMessage = "";

    public int Id { get => _id; set => SetProperty(ref _id, value); }
    public string Code { get => _code; set => SetProperty(ref _code, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Type { get => _type; set => SetProperty(ref _type, value); }
    public AccountTypeOption? TypeOption { get => _typeOption; set { if (SetProperty(ref _typeOption, value) && value != null) Type = value.Value; } }
    public Account? Parent { get => _parent; set => SetProperty(ref _parent, value); }
    public ObservableCollection<Account> AllAccounts { get => _allAccounts; set => SetProperty(ref _allAccounts, value); }
    public List<AccountTypeOption> TypeOptions { get => _typeOptions; set => SetProperty(ref _typeOptions, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsEdit => Id > 0;
    public string WindowTitle => IsEdit ? "ØªØ¹Ø¯ÙŠÙ„ Ø­Ø³Ø§Ø¨" : "Ø­Ø³Ø§Ø¨ Ø¬Ø¯ÙŠØ¯";

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public bool? DialogResult { get; private set; }
    public event Action? RequestClose;

    public AccountEditViewModel()
    {
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        CancelCommand = new RelayCommand(() => { DialogResult = false; RequestClose?.Invoke(); });
        _typeOption = _typeOptions.FirstOrDefault();
    }

    public async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            AllAccounts = new ObservableCollection<Account>(await db.Accounts.OrderBy(a => a.Code).ToListAsync());
            if (Id > 0)
            {
                var acc = await db.Accounts.FirstOrDefaultAsync(a => a.Id == Id);
                if (acc != null)
                {
                    Code = acc.Code; Name = acc.Name; Type = acc.Type;
                    TypeOption = _typeOptions.FirstOrDefault(t => t.Value == acc.Type);
                    Parent = AllAccounts.FirstOrDefault(a => a.Id == acc.ParentId);
                }
            }
            else if (string.IsNullOrEmpty(Code))
            {
                var count = await db.Accounts.CountAsync();
                Code = (1000 + count * 10).ToString();
            }
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Code)) { StatusMessage = "Code required"; return; }
            if (string.IsNullOrWhiteSpace(Name)) { StatusMessage = "Name required"; return; }
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Account acc;
            if (Id > 0) { acc = await db.Accounts.FirstOrDefaultAsync(a => a.Id == Id); if (acc == null) { StatusMessage = "Not found"; return; } }
            else { acc = new Account(); db.Accounts.Add(acc); }
            acc.Code = Code; acc.Name = Name; acc.Type = Type; acc.ParentId = Parent?.Id;
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK"; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
