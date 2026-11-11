using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
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

    public bool CanViewUsers => AuthService.HasPermission("manage_users");
    public bool CanViewInvoices => AuthService.HasPermission("sales");
    public bool CanViewReturns => AuthService.HasPermission("returns");
    public bool CanViewInventory => AuthService.HasPermission("stock_adjustment") || AuthService.HasPermission("inventory.count.create") || AuthService.HasPermission("inventory.adjustment.create");
    public bool CanViewDamaged => AuthService.HasPermission("inventory.damaged.create");
    public bool CanViewWriteoff => AuthService.HasPermission("inventory.writeoff.approve") || AuthService.HasPermission("inventory.writeoff.execute");
    public bool CanViewAccounts => AuthService.HasPermission("profit_reports");
    public bool CanViewExpenses => AuthService.HasPermission("expenses");
    public bool CanViewReports => AuthService.HasPermission("profit_reports") || AuthService.HasPermission("inventory.reports.view");
    public bool CanViewSettings => AuthService.HasPermission("settings");
    public bool CanViewStockLevels => AuthService.HasPermission("stock_adjustment") || AuthService.HasPermission("inventory.count.create");

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
        var loginResult = AuthService.Login((Username ?? "").Trim(), Password ?? "");
        if (loginResult.Success)
        {
            OnPropertyChanged(nameof(CanViewUsers));
            OnPropertyChanged(nameof(CanViewInvoices));
            OnPropertyChanged(nameof(CanViewReturns));
            OnPropertyChanged(nameof(CanViewInventory));
            OnPropertyChanged(nameof(CanViewDamaged));
            OnPropertyChanged(nameof(CanViewWriteoff));
            OnPropertyChanged(nameof(CanViewAccounts));
            OnPropertyChanged(nameof(CanViewExpenses));
            OnPropertyChanged(nameof(CanViewReports));
            OnPropertyChanged(nameof(CanViewSettings));
            OnPropertyChanged(nameof(CanViewStockLevels));
            if (AuthService.CurrentUser != null) CurrentUserDisplay = loginResult.User.FullName + " (" + loginResult.User.Role + ")";
            IsLoggedIn = true;
            // P5: إذا كان البرنامج غير مفعّل نوجّه المستخدم مباشرةً إلى تبويب التفعيل داخل الإعدادات.
            if (App.LicenseActivationRequired)
            {
                Navigate("LicenseActivation");
                return;
            }
            Navigate("Dashboard");
        }
        else ErrorMessage = "\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062F\u062E\u0648\u0644 \u063A\u064A\u0631 \u0635\u062D\u064A\u062D\u0629";
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
            "CashBoxes" => new CashBoxesViewModel(),
            "Settings" => new SettingsViewModel(),
            "Loans" => new LoansViewModel(),
            "DataManagement" => new DataManagementViewModel(),
            "DataExchange" => new DataExchangeViewModel(),
            "Users" => new UsersViewModel(),
            "StockLevels" => new StockLevelsViewModel(),
            "StockCounts" => new StockCountSessionsViewModel(),
            "StockAdjustments" => new StockAdjustmentsViewModel(),
            "DamagedItems" => new DamagedItemsViewModel(),
            "WriteoffRequests" => new WriteoffRequestsViewModel(),
            "Employees" => new EmployeesViewModel { OpenDependencyWindow = OpenDependencyWindowInternal },
            "Warehouses" => new WarehousesViewModel { OpenDependencyWindow = OpenDependencyWindowInternal },
            "ExpenseCategories" => new ExpenseCategoriesViewModel { OpenDependencyWindow = OpenDependencyWindowInternal },
            "CostCenters" => new CostCentersViewModel { OpenDependencyWindow = OpenDependencyWindowInternal },
            "CostAllocations" => new CostAllocationsViewModel(),
            "AccountingPeriods" => new AccountingPeriodsViewModel(),
            "JournalEntries" => new JournalEntriesViewModel(),
            "Budgets" => new BudgetsViewModel(),
            "FixedAssets" => new FixedAssetsViewModel(),
            "WarehouseTransfers" => new WarehouseTransfersViewModel(),
            "PettyCash" => new PettyCashViewModel(),
            "BankReconciliation" => new BankReconciliationViewModel(),
            "RecurringJournals" => new RecurringJournalsViewModel(),
            "RecurringInvoices" => new RecurringInvoicesViewModel(),
            "ReportingSettings" => new ReportingSettingsViewModel(),
            "RevenueRecognition" => new RevenueRecognitionViewModel(),
            "BankStatementImport" => new BankStatementImportViewModel(),
            "BadDebts" => new BadDebtsViewModel(),
            "EarlyPaymentDiscount" => new EarlyPaymentDiscountViewModel(),
            "LatePaymentFees" => new LatePaymentFeesViewModel(),
            "Salaries" => new SalariesViewModel(),
            // التفعيل داخل الإعدادات — تبويب «تفعيل البرنامج»
            "LicenseActivation" => new SettingsViewModel(1),
            _ => new ViewModelBase()
        };
    }

    private void OpenDependencyWindowInternal(string windowName, int id)
    {
        try
        {
            if (string.IsNullOrEmpty(windowName)) return;
            // تصحيح اسم نافذة الأصناف
            if (windowName == "ItemsEditWindow") windowName = "ItemEditWindow";
            var type = Type.GetType($"Techtoon.UI.Views.{windowName}, Techtoon.UI");
            if (type != null)
            {
                var win = (System.Windows.Window)Activator.CreateInstance(type, id);
                win.ShowDialog();
            }
            else
            {
                System.Windows.MessageBox.Show($"Window '{windowName}' not found.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Error opening record: " + ex.Message, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void Logout()
    {
        AuthService.Logout();
        IsLoggedIn = false; Username = ""; Password = ""; ErrorMessage = ""; CurrentUserDisplay = "";
    }
}