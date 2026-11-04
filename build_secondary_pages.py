# build_secondary_pages.py
from pathlib import Path

ROOT = Path(r"D:\PROG\Accounting Prog\Techtoon Accounting C+")
UI = ROOT / "Techtoon.UI"
VM = UI / "ViewModels"
VIEWS = UI / "Views"
SERVICES = UI / "Services"
DOCS = UI / "Documents"

for d in [VM, VIEWS, SERVICES, DOCS]:
    d.mkdir(parents=True, exist_ok=True)

# ========================================================
# 1. Accounts (شجرة الحسابات)
# ========================================================

(VM / "AccountsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class AccountsViewModel : ViewModelBase
{
    private ObservableCollection<Account> _accounts = new();
    private Account? _selectedAccount;

    public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
    public Account? SelectedAccount { get => _selectedAccount; set => SetProperty(ref _selectedAccount, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand DeleteCommand { get; }

    public AccountsViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewCommand = new RelayCommand(() => OpenEdit(null));
        EditCommand = new RelayCommand(() => OpenEdit(SelectedAccount), () => SelectedAccount != null);
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedAccount != null);
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Accounts.Include(a => a.Children).OrderBy(a => a.Code).ToListAsync();
            var roots = list.Where(a => a.ParentId == null).ToList();
            Accounts = new ObservableCollection<Account>(roots);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void OpenEdit(Account? account)
    {
        var window = new AccountEditWindow(account) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedAccount == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedAccount.Name + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var acc = await db.Accounts.FirstOrDefaultAsync(x => x.Id == SelectedAccount.Id);
            if (acc != null) { db.Accounts.Remove(acc); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "AccountEditViewModel.cs").write_text("""using System.Collections.ObjectModel;
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
        new("ASSET", "أصول"), new("LIABILITY", "خصوم"), new("EQUITY", "حقوق ملكية"),
        new("REVENUE", "إيرادات"), new("EXPENSE", "مصروفات")
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
    public string WindowTitle => IsEdit ? "تعديل حساب" : "حساب جديد";

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

    public async override Task LoadAsync()
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
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "AccountEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.AccountEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="450" Width="500"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15">
                        <TextBlock Text="الكود:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding Code, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15">
                        <TextBlock Text="النوع:" Margin="0,0,0,5"/>
                        <ComboBox ItemsSource="{Binding TypeOptions}" SelectedItem="{Binding TypeOption}" Padding="8,6"/>
                    </StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15">
                    <TextBlock Text="الاسم:" Margin="0,0,0,5"/>
                    <TextBox Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                </StackPanel>
                <StackPanel>
                    <TextBlock Text="الحساب الأب:" Margin="0,0,0,5"/>
                    <ComboBox ItemsSource="{Binding AllAccounts}" DisplayMemberPath="Name" SelectedItem="{Binding Parent}" Padding="8,6"/>
                </StackPanel>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "AccountEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class AccountEditWindow : Window
{
    private readonly AccountEditViewModel _vm;
    public AccountEditWindow(Account? account = null)
    {
        InitializeComponent();
        _vm = new AccountEditViewModel();
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        if (account != null) _vm.Id = account.Id;
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "AccountsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.AccountsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:models="clr-namespace:Techtoon.Core.Models;assembly=Techtoon.Core"
             Loaded="AccountsView_Loaded">
    <UserControl.Resources>
        <HierarchicalDataTemplate x:Key="AccountTemplate" DataType="{x:Type models:Account}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="{Binding Code}" FontWeight="Bold" Margin="0,0,10,0" Foreground="{StaticResource PrimaryBrush}"/>
                <TextBlock Text="{Binding Name}"/>
            </StackPanel>
        </HierarchicalDataTemplate>
    </UserControl.Resources>
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock Text="شجرة الحسابات" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="+ حساب جديد" Style="{StaticResource PrimaryButton}" Command="{Binding NewCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Padding="10">
            <Grid>
                <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="180"/></Grid.ColumnDefinitions>
                <TreeView Grid.Column="0" ItemsSource="{Binding Accounts}" ItemTemplate="{StaticResource AccountTemplate}" SelectedItemChanged="TreeView_SelectedItemChanged" Margin="5" Padding="5"/>
                <StackPanel Grid.Column="1" VerticalAlignment="Top" Margin="10,5">
                    <TextBlock Text="إجراءات" Style="{StaticResource CardTitle}" Margin="0,0,0,10"/>
                    <Button Content="✏️ تعديل" Style="{StaticResource PrimaryButton}" Command="{Binding EditCommand}" Margin="0,0,0,8" HorizontalAlignment="Stretch"/>
                    <Button Content="🗑️ حذف" Style="{StaticResource PrimaryButton}" Command="{Binding DeleteCommand}" Background="{StaticResource ErrorBrush}" HorizontalAlignment="Stretch"/>
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

(VIEWS / "AccountsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.Core.Models;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class AccountsView : UserControl
{
    public AccountsView() { InitializeComponent(); }
    private async void AccountsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountsViewModel vm) await vm.LoadAsync();
    }
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is AccountsViewModel vm && e.NewValue is Account account) vm.SelectedAccount = account;
    }
}
""", encoding="utf-8")

# ========================================================
# 2. Payments (المدفوعات)
# ========================================================

(VM / "PaymentsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;
using Techtoon.UI.Views;

namespace Techtoon.UI.ViewModels;

public class PaymentsViewModel : ViewModelBase
{
    private ObservableCollection<Payment> _payments = new();
    private string _searchText = "";
    private Payment? _selectedPayment;

    public ObservableCollection<Payment> Payments { get => _payments; set => SetProperty(ref _payments, value); }
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) ApplyFilter(); } }
    public Payment? SelectedPayment { get => _selectedPayment; set => SetProperty(ref _selectedPayment, value); }

    public ICommand RefreshCommand { get; }
    public ICommand NewReceiptCommand { get; }
    public ICommand NewPaymentCommand { get; }
    public ICommand DeleteCommand { get; }

    public PaymentsViewModel()
    {
        RefreshCommand = new RelayCommand(async () => await LoadAsync());
        NewReceiptCommand = new RelayCommand(() => OpenEdit("receipt"));
        NewPaymentCommand = new RelayCommand(() => OpenEdit("payment"));
        DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => SelectedPayment != null);
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var list = await db.Payments.Include(p => p.Party).Include(p => p.CashBox).OrderByDescending(p => p.Date).Take(200).ToListAsync();
            Payments = new ObservableCollection<Payment>(list);
            ApplyFilter();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine("LoadAsync error: " + ex.Message); }
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        var s = SearchText.Trim().ToLower();
        var filtered = Payments.Where(p => (p.ReferenceNumber?.ToLower().Contains(s) == true) || (p.Party?.Name?.ToLower().Contains(s) == true)).ToList();
        Payments = new ObservableCollection<Payment>(filtered);
    }

    private void OpenEdit(string type)
    {
        var window = new PaymentEditWindow(type) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
        if (window.DialogResult == true) _ = LoadAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedPayment == null) return;
        var confirm = System.Windows.MessageBox.Show("Delete: " + SelectedPayment.ReferenceNumber + "?", "Confirm", System.Windows.MessageBoxButton.YesNo);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var p = await db.Payments.FirstOrDefaultAsync(x => x.Id == SelectedPayment.Id);
            if (p != null) { db.Payments.Remove(p); await db.SaveChangesAsync(); await LoadAsync(); }
        }
        catch (Exception ex) { System.Windows.MessageBox.Show("Error: " + ex.Message, "Error"); }
    }
}
""", encoding="utf-8")

(VM / "PaymentEditViewModel.cs").write_text("""using System.Collections.ObjectModel;
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
    private string _statusMessage = "";

    public DateTime Date { get => _date; set => SetProperty(ref _date, value); }
    public Party? Party { get => _party; set => SetProperty(ref _party, value); }
    public double Amount { get => _amount; set => SetProperty(ref _amount, value); }
    public string PaymentType { get => _paymentType; set => SetProperty(ref _paymentType, value); }
    public string PaymentMethod { get => _paymentMethod; set => SetProperty(ref _paymentMethod, value); }
    public CashBox? CashBox { get => _cashBox; set => SetProperty(ref _cashBox, value); }
    public Account? CounterAccount { get => _counterAccount; set => SetProperty(ref _counterAccount, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public ObservableCollection<Party> Parties { get => _parties; set => SetProperty(ref _parties, value); }
    public ObservableCollection<CashBox> CashBoxes { get => _cashBoxes; set => SetProperty(ref _cashBoxes, value); }
    public ObservableCollection<Account> Accounts { get => _accounts; set => SetProperty(ref _accounts, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsReceipt => PaymentType == "receipt";
    public string WindowTitle => IsReceipt ? "سند قبض" : "سند صرف";

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
                var je = new JournalEntry { Date = Date, Description = (IsReceipt ? "سند قبض - " : "سند صرف - ") + Party.Name + " (" + refNum + ")", ReferenceType = "payment" };
                if (IsReceipt) { je.Lines.Add(new JournalLine { AccountId = cashBox.Account.Id, Debit = Amount }); je.Lines.Add(new JournalLine { AccountId = counterAcc.Id, Credit = Amount }); }
                else { je.Lines.Add(new JournalLine { AccountId = counterAcc.Id, Debit = Amount }); je.Lines.Add(new JournalLine { AccountId = cashBox.Account.Id, Credit = Amount }); }
                db.JournalEntries.Add(je);
            }
            await db.SaveChangesAsync();
            StatusMessage = "Saved OK - " + refNum; DialogResult = true; RequestClose?.Invoke();
        }
        catch (DbUpdateException ex) { var i = ex.InnerException; var m = ex.Message; while (i != null) { m += "\\n" + i.Message; i = i.InnerException; } StatusMessage = m; }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "PaymentEditWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.Views.PaymentEditWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="{Binding WindowTitle}" Height="600" Width="650"
        WindowStartupLocation="CenterOwner" Background="{StaticResource BackgroundBrush}" FlowDirection="RightToLeft">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
        <TextBlock Grid.Row="0" Text="{Binding WindowTitle}" Style="{StaticResource PageTitle}" Foreground="{StaticResource PrimaryBrush}"/>
        <Border Grid.Row="1" Style="{StaticResource Card}">
            <StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15"><TextBlock Text="التاريخ:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding Date}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15"><TextBlock Text="طريقة الدفع:" Margin="0,0,0,5"/><ComboBox SelectedValue="{Binding PaymentMethod}" SelectedValuePath="Tag" Padding="8,6"><ComboBoxItem Content="نقدي" Tag="cash"/><ComboBoxItem Content="شيك" Tag="cheque"/><ComboBoxItem Content="تحويل" Tag="bank_transfer"/></ComboBox></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="العميل/المورد:" Margin="0,0,0,5"/><ComboBox ItemsSource="{Binding Parties}" DisplayMemberPath="Name" SelectedItem="{Binding Party}" Padding="8,6"/></StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,15"><TextBlock Text="الخزينة:" Margin="0,0,0,5"/><ComboBox ItemsSource="{Binding CashBoxes}" DisplayMemberPath="Name" SelectedItem="{Binding CashBox}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,15"><TextBlock Text="الحساب المقابل:" Margin="0,0,0,5"/><ComboBox ItemsSource="{Binding Accounts}" DisplayMemberPath="Name" SelectedItem="{Binding CounterAccount}" Padding="8,6"/></StackPanel>
                </Grid>
                <StackPanel Margin="0,0,0,15"><TextBlock Text="المبلغ:" Margin="0,0,0,5"/><TextBox Text="{Binding Amount, UpdateSourceTrigger=LostFocus}" Padding="8,6" FontSize="18" FontWeight="Bold"/></StackPanel>
                <StackPanel><TextBlock Text="ملاحظات:" Margin="0,0,0,5"/><TextBox Text="{Binding Notes, UpdateSourceTrigger=PropertyChanged}" Padding="8,6" AcceptsReturn="True" TextWrapping="Wrap" Height="60"/></StackPanel>
            </StackPanel>
        </Border>
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center" Margin="0,0,20,0" TextWrapping="Wrap" MaxWidth="350"/>
            <Button Content="إلغاء" Style="{StaticResource PrimaryButton}" Command="{Binding CancelCommand}" Background="#757575" Margin="0,0,10,0"/>
            <Button Content="💾 حفظ" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}"/>
        </StackPanel>
    </Grid>
</Window>
""", encoding="utf-8")

(VIEWS / "PaymentEditWindow.xaml.cs").write_text("""using System.Windows;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PaymentEditWindow : Window
{
    private readonly PaymentEditViewModel _vm;
    public PaymentEditWindow(string paymentType)
    {
        InitializeComponent();
        _vm = new PaymentEditViewModel(paymentType);
        DataContext = _vm;
        _vm.RequestClose += () => { this.DialogResult = _vm.DialogResult; this.Close(); };
        Loaded += async (_, _) => await _vm.LoadAsync();
    }
}
""", encoding="utf-8")

(VIEWS / "PaymentsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.PaymentsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="PaymentsView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0} LastChildFill="False">
            <TextBlock Text="المدفوعات والسندات" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <StackPanel Orientation="Horizontal" DockPanel.Dock="Right" Margin="0,0,0,20">
                <Button Content="🔄 تحديث" Style="{StaticResource PrimaryButton}" Command="{Binding RefreshCommand}" Margin="0,0,10,0"/>
                <Button Content="📥 سند قبض" Style="{StaticResource PrimaryButton}" Command="{Binding NewReceiptCommand}" Margin="0,0,10,0"/>
                <Button Content="📤 سند صرف" Style="{StaticResource PrimaryButton}" Command="{Binding NewPaymentCommand}"/>
            </StackPanel>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="15">
            <DockPanel>
                <TextBlock Text="🔍" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" Width="300" Padding="8,6" VerticalAlignment="Center"/>
            </DockPanel>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding Payments}" SelectedItem="{Binding SelectedPayment}"
                      AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      SelectionMode="Single" HeadersVisibility="Column" GridLinesVisibility="Horizontal"
                      HorizontalGridLinesBrush="#E0E0E0" ColumnHeaderHeight="40" RowHeight="36" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="رقم السند" Binding="{Binding ReferenceNumber}" Width="140"/>
                    <DataGridTextColumn Header="التاريخ" Binding="{Binding Date, StringFormat=yyyy-MM-dd}" Width="120"/>
                    <DataGridTextColumn Header="العميل/المورد" Binding="{Binding Party.Name}" Width="*"/>
                    <DataGridTextColumn Header="النوع" Binding="{Binding PaymentType}" Width="80"/>
                    <DataGridTextColumn Header="المبلغ" Binding="{Binding Amount, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الخزينة" Binding="{Binding CashBox.Name}" Width="120"/>
                    <DataGridTemplateColumn Header="حذف" Width="80">
                        <DataGridTemplateColumn.CellTemplate>
                            <DataTemplate>
                                <Button Content="🗑️" Padding="4" Margin="2" Command="{Binding DataContext.DeleteCommand, RelativeSource={RelativeSource AncestorType=DataGrid}}" CommandParameter="{Binding}"/>
                            </DataTemplate>
                        </DataGridTemplateColumn.CellTemplate>
                    </DataGridTemplateColumn>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

# Fix XAML typo
xaml_path = VIEWS / "PaymentsView.xaml"
x = xaml_path.read_text(encoding="utf-8")
x = x.replace('Grid.Row="0}', 'Grid.Row="0"')
xaml_path.write_text(x, encoding="utf-8")

(VIEWS / "PaymentsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class PaymentsView : UserControl
{
    public PaymentsView() { InitializeComponent(); }
    private async void PaymentsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PaymentsViewModel vm) await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 3. Reports (التقارير)
# ========================================================

(VM / "ReportsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class AccountBalanceRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public double Debit { get; set; }
    public double Credit { get; set; }
    public double Balance => Debit - Credit;
}

public class ReportsViewModel : ViewModelBase
{
    private string _selectedReport = "trial_balance";
    private DateTime _fromDate = DateTime.Now.AddDays(-30);
    private DateTime _toDate = DateTime.Now;
    private ObservableCollection<AccountBalanceRow> _accountBalances = new();
    private string _statusMessage = "";

    public string SelectedReport { get => _selectedReport; set { if (SetProperty(ref _selectedReport, value)) { OnPropertyChanged(nameof(IsTrialBalance)); OnPropertyChanged(nameof(IsIncomeStatement)); OnPropertyChanged(nameof(IsBalanceSheet)); } } }
    public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
    public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }
    public ObservableCollection<AccountBalanceRow> AccountBalances { get => _accountBalances; set => SetProperty(ref _accountBalances, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    public bool IsTrialBalance => SelectedReport == "trial_balance";
    public bool IsIncomeStatement => SelectedReport == "income_statement";
    public bool IsBalanceSheet => SelectedReport == "balance_sheet";

    public double TotalDebit => AccountBalances.Sum(r => r.Debit);
    public double TotalCredit => AccountBalances.Sum(r => r.Credit);
    public double TotalBalance => AccountBalances.Sum(r => r.Balance);

    public ICommand LoadReportCommand { get; }

    public ReportsViewModel()
    {
        LoadReportCommand = new RelayCommand(async () => await LoadReportAsync());
    }

    private async Task LoadReportAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            AccountBalances.Clear();
            var query = db.JournalLines.Include(jl => jl.Account).Include(jl => jl.Entry).AsQueryable();
            if (IsTrialBalance) query = query.Where(jl => jl.Entry.Date >= FromDate && jl.Entry.Date <= ToDate);
            else if (IsIncomeStatement) query = query.Where(jl => jl.Account.Type == "REVENUE" || jl.Account.Type == "EXPENSE");
            else if (IsBalanceSheet) query = query.Where(jl => jl.Account.Type == "ASSET" || jl.Account.Type == "LIABILITY" || jl.Account.Type == "EQUITY");

            var lines = await query.ToListAsync();
            var grouped = lines.GroupBy(jl => new { jl.Account.Id, jl.Account.Code, jl.Account.Name, Type = jl.Account.Type.ToString() })
                .Select(g => new AccountBalanceRow { Code = g.Key.Code, Name = g.Key.Name, Type = g.Key.Type, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
                .OrderBy(r => r.Code).ToList();
            AccountBalances = new ObservableCollection<AccountBalanceRow>(grouped);
            OnPropertyChanged(nameof(TotalDebit)); OnPropertyChanged(nameof(TotalCredit)); OnPropertyChanged(nameof(TotalBalance));
            StatusMessage = "OK";
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }
}
""", encoding="utf-8")

(VIEWS / "ReportsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.ReportsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="ReportsView_Loaded">
    <Grid Margin="20">
        <Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/></Grid.RowDefinitions>
        <DockPanel Grid.Row="0" LastChildFill="False">
            <TextBlock Text="التقارير" Style="{StaticResource PageTitle}" DockPanel.Dock="Left"/>
            <Button Content="📊 عرض التقرير" Style="{StaticResource PrimaryButton}" Command="{Binding LoadReportCommand}" DockPanel.Dock="Right"/>
        </DockPanel>
        <Border Grid.Row="1" Style="{StaticResource Card}" Margin="0,0,0,15" Padding="15">
            <StackPanel>
                <TextBlock Text="نوع التقرير:" Margin="0,0,0,8" FontWeight="SemiBold"/>
                <StackPanel Orientation="Horizontal" Margin="0,0,0,15">
                    <RadioButton Content="ميزان المراجعة" GroupName="ReportType" Margin="0,0,15,0" IsChecked="{Binding IsTrialBalance, Mode=OneWay}" Click="ReportType_Click" Tag="trial_balance"/>
                    <RadioButton Content="قائمة الدخل" GroupName="ReportType" Margin="0,0,15,0" IsChecked="{Binding IsIncomeStatement, Mode=OneWay}" Click="ReportType_Click" Tag="income_statement"/>
                    <RadioButton Content="الميزانية العمومية" GroupName="ReportType" IsChecked="{Binding IsBalanceSheet, Mode=OneWay}" Click="ReportType_Click" Tag="balance_sheet"/>
                </StackPanel>
                <Grid>
                    <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>
                    <StackPanel Grid.Column="0" Margin="0,0,10,0"><TextBlock Text="من تاريخ:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding FromDate}" Padding="8,6"/></StackPanel>
                    <StackPanel Grid.Column="1" Margin="10,0,0,0"><TextBlock Text="إلى تاريخ:" Margin="0,0,0,5"/><DatePicker SelectedDate="{Binding ToDate}" Padding="8,6"/></StackPanel>
                </Grid>
            </StackPanel>
        </Border>
        <Border Grid.Row="2" Style="{StaticResource Card}" Padding="0">
            <DataGrid ItemsSource="{Binding AccountBalances}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True"
                      HeadersVisibility="Column" GridLinesVisibility="Horizontal" HorizontalGridLinesBrush="#E0E0E0"
                      ColumnHeaderHeight="40" RowHeight="32" BorderThickness="0" FlowDirection="RightToLeft">
                <DataGrid.Columns>
                    <DataGridTextColumn Header="الكود" Binding="{Binding Code}" Width="120"/>
                    <DataGridTextColumn Header="الاسم" Binding="{Binding Name}" Width="*"/>
                    <DataGridTextColumn Header="النوع" Binding="{Binding Type}" Width="100"/>
                    <DataGridTextColumn Header="مدين" Binding="{Binding Debit, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="دائن" Binding="{Binding Credit, StringFormat=N2}" Width="120"/>
                    <DataGridTextColumn Header="الرصيد" Binding="{Binding Balance, StringFormat=N2}" Width="120"/>
                </DataGrid.Columns>
            </DataGrid>
        </Border>
    </Grid>
</UserControl>
""", encoding="utf-8")

(VIEWS / "ReportsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class ReportsView : UserControl
{
    public ReportsView() { InitializeComponent(); }
    private void ReportsView_Loaded(object sender, RoutedEventArgs e) { }
    private void ReportType_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton rb && DataContext is ReportsViewModel vm && rb.Tag is string tag) vm.SelectedReport = tag;
    }
}
""", encoding="utf-8")

# ========================================================
# 4. Settings (الإعدادات)
# ========================================================

(VM / "SettingsViewModel.cs").write_text("""using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Techtoon.Core.Models;
using Techtoon.Data.Context;

namespace Techtoon.UI.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private string _companyName = "Techtoon Accounting";
    private string _companyPhone = "";
    private string _companyAddress = "";
    private string _statusMessage = "";
    private ObservableCollection<User> _users = new();

    public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
    public string CompanyPhone { get => _companyPhone; set => SetProperty(ref _companyPhone, value); }
    public string CompanyAddress { get => _companyAddress; set => SetProperty(ref _companyAddress, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public ObservableCollection<User> Users { get => _users; set => SetProperty(ref _users, value); }

    public ICommand SaveCommand { get; }
    public ICommand RefreshUsersCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(SaveSettings);
        RefreshUsersCommand = new RelayCommand(async () => await LoadUsersAsync());
    }

    public override async Task LoadAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            var settings = await db.AppSettings.ToListAsync();
            CompanyName = settings.FirstOrDefault(s => s.Key == "company_name")?.Value ?? "Techtoon Accounting";
            CompanyPhone = settings.FirstOrDefault(s => s.Key == "company_phone")?.Value ?? "";
            CompanyAddress = settings.FirstOrDefault(s => s.Key == "company_address")?.Value ?? "";
            await LoadUsersAsync();
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            Users = new ObservableCollection<User>(await db.Users.OrderBy(u => u.Username).ToListAsync());
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TechtoonDbContext>();
            SaveSetting(db, "company_name", CompanyName);
            SaveSetting(db, "company_phone", CompanyPhone);
            SaveSetting(db, "company_address", CompanyAddress);
            db.SaveChanges();
            StatusMessage = "Saved OK";
        }
        catch (Exception ex) { StatusMessage = "Error: " + ex.Message; }
    }

    private void SaveSetting(TechtoonDbContext db, string key, string value)
    {
        var s = db.AppSettings.FirstOrDefault(x => x.Key == key);
        if (s == null) db.AppSettings.Add(new AppSetting { Key = key, Value = value, Category = "general" });
        else s.Value = value;
    }
}
""", encoding="utf-8")

(VIEWS / "SettingsView.xaml").write_text("""<UserControl x:Class="Techtoon.UI.Views.SettingsView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Loaded="SettingsView_Loaded">
    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="20">
            <TextBlock Text="الإعدادات" Style="{StaticResource PageTitle}"/>
            <Border Style="{StaticResource Card}" Margin="0,0,0,15">
                <StackPanel>
                    <TextBlock Text="معلومات الشركة" Style="{StaticResource CardTitle}"/>
                    <StackPanel Margin="0,0,0,15">
                        <TextBlock Text="اسم الشركة:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding CompanyName, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Margin="0,0,0,15">
                        <TextBlock Text="الهاتف:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding CompanyPhone, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                    <StackPanel Margin="0,0,0,15">
                        <TextBlock Text="العنوان:" Margin="0,0,0,5"/>
                        <TextBox Text="{Binding CompanyAddress, UpdateSourceTrigger=PropertyChanged}" Padding="8,6"/>
                    </StackPanel>
                </StackPanel>
            </Border>
            <StackPanel Orientation="Horizontal" Margin="0,0,0,20">
                <Button Content="💾 حفظ الإعدادات" Style="{StaticResource PrimaryButton}" Command="{Binding SaveCommand}" Margin="0,0,15,0"/>
                <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource SuccessBrush}" VerticalAlignment="Center"/>
            </StackPanel>
            <Border Style="{StaticResource Card}">
                <StackPanel>
                    <TextBlock Text="إدارة المستخدمين" Style="{StaticResource CardTitle}"/>
                    <DataGrid ItemsSource="{Binding Users}" AutoGenerateColumns="False" CanUserAddRows="False" IsReadOnly="True" Height="200" BorderThickness="0">
                        <DataGrid.Columns>
                            <DataGridTextColumn Header="اسم المستخدم" Binding="{Binding Username}" Width="*"/>
                            <DataGridTextColumn Header="الاسم" Binding="{Binding FullName}" Width="*"/>
                            <DataGridTextColumn Header="الدور" Binding="{Binding Role}" Width="100"/>
                            <DataGridCheckBoxColumn Header="نشط" Binding="{Binding IsActive}" Width="50"/>
                        </DataGrid.Columns>
                    </DataGrid>
                </StackPanel>
            </Border>
        </StackPanel>
    </ScrollViewer>
</UserControl>
""", encoding="utf-8")

(VIEWS / "SettingsView.xaml.cs").write_text("""using System.Windows;
using System.Windows.Controls;
using Techtoon.UI.ViewModels;

namespace Techtoon.UI.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() { InitializeComponent(); }
    private async void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm) await vm.LoadAsync();
    }
}
""", encoding="utf-8")

# ========================================================
# 5. Update MainViewModel + MainWindow to add ALL navigation
# ========================================================

(VM / "MainViewModel.cs").write_text("""using System.Windows.Input;
using Techtoon.UI.Services;

namespace Techtoon.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase _currentView;
    private string _activeButton = "Dashboard";
    private string _currentUserDisplay = "";
    private bool _isLoggedIn = false;
    private string _username = "";
    private string _password = "";
    private string _errorMessage = "";

    public ViewModelBase CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
    public string ActiveButton { get => _activeButton; set => SetProperty(ref _activeButton, value); }
    public string CurrentUserDisplay { get => _currentUserDisplay; set => SetProperty(ref _currentUserDisplay, value); }
    public bool IsLoggedIn { get => _isLoggedIn; set { if (SetProperty(ref _isLoggedIn, value)) { OnPropertyChanged(nameof(IsLoginScreen)); OnPropertyChanged(nameof(IsAppVisible)); } } }
    public bool IsLoginScreen => !IsLoggedIn;
    public bool IsAppVisible => IsLoggedIn;
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

    public ICommand LoginCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        _currentView = new ViewModelBase();
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        NavigateCommand = new RelayCommand<string>(Navigate);
        LogoutCommand = new RelayCommand(Logout);
    }

    private async Task LoginAsync()
    {
        ErrorMessage = "";
        var result = await AuthService.LoginAsync(Username ?? "", Password ?? "");
        if (result.Success)
        {
            if (AuthService.CurrentUser != null) CurrentUserDisplay = AuthService.CurrentUser.FullName + " (" + AuthService.CurrentUser.Role + ")";
            IsLoggedIn = true; Navigate("Dashboard");
        }
        else ErrorMessage = result.Message;
    }

    private void Navigate(string? view)
    {
        if (string.IsNullOrEmpty(view) || !IsLoggedIn) return;
        ActiveButton = view;
        CurrentView = view switch
        {
            "Dashboard" => new DashboardViewModel(),
            "Invoices" => new InvoicesViewModel(),
            "Parties" => new PartiesViewModel(),
            "Items" => new ItemsViewModel(),
            "Accounts" => new AccountsViewModel(),
            "Payments" => new PaymentsViewModel(),
            "Reports" => new ReportsViewModel(),
            "Settings" => new SettingsViewModel(),
            _ => new ViewModelBase()
        };
    }

    private void Logout()
    {
        AuthService.Logout();
        IsLoggedIn = false; Username = ""; Password = ""; ErrorMessage = ""; CurrentUserDisplay = "";
    }
}
""", encoding="utf-8")

(UI / "MainWindow.xaml").write_text("""<Window x:Class="Techtoon.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:Techtoon.UI.ViewModels"
        xmlns:views="clr-namespace:Techtoon.UI.Views"
        Title="Techtoon Accounting" Height="750" Width="1280"
        WindowStartupLocation="CenterScreen" Background="{StaticResource BackgroundBrush}"
        FlowDirection="RightToLeft" UseLayoutRounding="True" TextOptions.TextFormattingMode="Display">

    <Window.DataContext>
        <vm:MainViewModel/>
    </Window.DataContext>

    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BoolToVis"/>
        <DataTemplate DataType="{x:Type vm:DashboardViewModel}"><views:DashboardView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:InvoicesViewModel}"><views:InvoicesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:PartiesViewModel}"><views:PartiesView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:ItemsViewModel}"><views:ItemsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:AccountsViewModel}"><views:AccountsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:PaymentsViewModel}"><views:PaymentsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:ReportsViewModel}"><views:ReportsView/></DataTemplate>
        <DataTemplate DataType="{x:Type vm:SettingsViewModel}"><views:SettingsView/></DataTemplate>
    </Window.Resources>

    <Grid>
        <Grid Visibility="{Binding IsLoginScreen, Converter={StaticResource BoolToVis}}">
            <Grid.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
                    <GradientStop Color="#263238" Offset="0"/>
                    <GradientStop Color="#00695C" Offset="1"/>
                </LinearGradientBrush>
            </Grid.Background>
            <Border HorizontalAlignment="Center" VerticalAlignment="Center" Background="White" CornerRadius="8" Padding="40" Width="400">
                <StackPanel>
                    <TextBlock Text="Techtoon" FontSize="36" FontWeight="Bold" Foreground="{StaticResource PrimaryBrush}" HorizontalAlignment="Center"/>
                    <TextBlock Text="Accounting System" FontSize="14" Foreground="{StaticResource TextSecondaryBrush}" HorizontalAlignment="Center" Margin="0,5,0,25"/>
                    <TextBlock Text="تسجيل الدخول" FontSize="20" FontWeight="Bold" Margin="0,0,0,20" HorizontalAlignment="Center"/>
                    <TextBlock Text="اسم المستخدم:" Margin="0,0,0,5"/>
                    <TextBox x:Name="UsernameBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" KeyDown="UsernameBox_KeyDown"/>
                    <TextBlock Text="كلمة المرور:" Margin="0,0,0,5"/>
                    <PasswordBox x:Name="PasswordBox" Padding="10,8" FontSize="14" Margin="0,0,0,15" KeyDown="PasswordBox_KeyDown"/>
                    <TextBlock Text="{Binding ErrorMessage}" Foreground="{StaticResource ErrorBrush}" Margin="0,0,0,15" TextWrapping="Wrap"/>
                    <Button Content="🔐 تسجيل الدخول" Style="{StaticResource PrimaryButton}" Click="LoginButton_Click" Padding="0,12" FontSize="15" Margin="0,5,0,15"/>
                    <Border Background="#E3F2FD" CornerRadius="4" Padding="15,10">
                        <StackPanel>
                            <TextBlock Text="بيانات الدخول الافتراضية:" FontWeight="SemiBold" Foreground="#1565C0" Margin="0,0,0,5"/>
                            <TextBlock Text="admin / admin123" Foreground="#1565C0" FontSize="13"/>
                        </StackPanel>
                    </Border>
                </StackPanel>
            </Border>
        </Grid>

        <Grid Visibility="{Binding IsAppVisible, Converter={StaticResource BoolToVis}}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="240"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Border Grid.Column="0" Background="{StaticResource SidebarBrush}">
                <DockPanel>
                    <Border DockPanel.Dock="Top" Padding="20,25,20,25" Background="{StaticResource PrimaryDarkBrush}">
                        <StackPanel>
                            <TextBlock Text="Techtoon" Foreground="White" FontSize="22" FontWeight="Bold"/>
                            <TextBlock Text="Accounting System" Foreground="#B2DFDB" FontSize="11"/>
                        </StackPanel>
                    </Border>
                    <Border DockPanel.Dock="Bottom" Background="#1A232A" Padding="15">
                        <StackPanel>
                            <TextBlock Text="المستخدم:" Foreground="#78909C" FontSize="10" Margin="0,0,0,3"/>
                            <TextBlock Text="{Binding CurrentUserDisplay}" Foreground="White" FontSize="12" FontWeight="SemiBold" TextWrapping="Wrap" Margin="0,0,0,10"/>
                            <Button Content="🚪 تسجيل الخروج" Style="{StaticResource SidebarButton}" Command="{Binding LogoutCommand}" HorizontalContentAlignment="Center" Background="#D32F2F" Foreground="White"/>
                            <TextBlock Text="v2.0.0" Foreground="#546E7A" FontSize="10" Margin="0,10,0,0" HorizontalAlignment="Center"/>
                        </StackPanel>
                    </Border>
                    <ScrollViewer VerticalScrollBarVisibility="Auto">
                        <StackPanel Margin="0,10">
                            <Button Content="  🏠 لوحة التحكم" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Dashboard"/>
                            <Button Content="  🛒 الفواتير" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Invoices"/>
                            <Button Content="  👥 العملاء والموردون" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Parties"/>
                            <Button Content="  📦 الأصناف" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Items"/>
                            <Button Content="  🌳 شجرة الحسابات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Accounts"/>
                            <Button Content="  💰 المدفوعات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Payments"/>
                            <Button Content="  📊 التقارير" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Reports"/>
                            <Button Content="  ⚙️ الإعدادات" Style="{StaticResource SidebarButton}" Command="{Binding NavigateCommand}" CommandParameter="Settings"/>
                        </StackPanel>
                    </ScrollViewer>
                </DockPanel>
            </Border>
            <ContentControl Grid.Column="1" Content="{Binding CurrentView}"/>
        </Grid>
    </Grid>
</Window>
""", encoding="utf-8")

print("=== Secondary Pages Built ===")
print("  - Accounts (TreeView + EditWindow)")
print("  - Payments (Receipt/Payment + EditWindow)")
print("  - Reports (3 types)")
print("  - Settings (Company + Users)")
print("  - MainViewModel + MainWindow updated with 8 buttons")