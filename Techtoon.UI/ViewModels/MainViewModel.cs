using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Techtoon.Core.Permissions;
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

    // ===================== الصفحات والصلاحيات =====================
    // كل صفحة مرتبطة بصلاحية واحدة من PermissionCatalog.PagePermission.
    // إخفاء زر القائمة + منع الانتقال المباشر = صلاحية فعلية لا شكلية.

    public bool CanViewDashboard => Can("Dashboard");
    public bool CanViewInvoices => Can("Invoices");
    public bool CanViewParties => Can("Parties");
    public bool CanViewPayments => Can("Payments");
    public bool CanViewItems => Can("Items");
    public bool CanViewWarehouses => Can("Warehouses");
    public bool CanViewStockLevels => Can("StockLevels");
    public bool CanViewWarehouseTransfers => Can("WarehouseTransfers");
    public bool CanViewStockCounts => Can("StockCounts");
    public bool CanViewStockAdjustments => Can("StockAdjustments");
    public bool CanViewDamagedItems => Can("DamagedItems");
    public bool CanViewWriteoffRequests => Can("WriteoffRequests");
    public bool CanViewEmployees => Can("Employees");
    public bool CanViewSalaries => Can("Salaries");
    public bool CanViewLoans => Can("Loans");
    public bool CanViewPettyCash => Can("PettyCash");
    public bool CanViewCashBoxes => Can("CashBoxes");
    public bool CanViewBankReconciliation => Can("BankReconciliation");
    public bool CanViewExpenseCategories => Can("ExpenseCategories");
    public bool CanViewBadDebts => Can("BadDebts");
    public bool CanViewEarlyPaymentDiscount => Can("EarlyPaymentDiscount");
    public bool CanViewLatePaymentFees => Can("LatePaymentFees");
    public bool CanViewAccounts => Can("Accounts");
    public bool CanViewRecurringJournals => Can("RecurringJournals");
    public bool CanViewRecurringInvoices => Can("RecurringInvoices");
    public bool CanViewReportingSettings => Can("ReportingSettings");
    public bool CanViewRevenueRecognition => Can("RevenueRecognition");
    public bool CanViewBankStatementImport => Can("BankStatementImport");
    public bool CanViewCostCenters => Can("CostCenters");
    public bool CanViewCostAllocations => Can("CostAllocations");
    public bool CanViewAccountingPeriods => Can("AccountingPeriods");
    public bool CanViewJournalEntries => Can("JournalEntries");
    public bool CanViewBudgets => Can("Budgets");
    public bool CanViewFixedAssets => Can("FixedAssets");
    public bool CanViewReports => Can("Reports");
    public bool CanViewAuditLog => Can("AuditLog");
    public bool CanViewSettings => Can("Settings");
    public bool CanViewUsers => Can("Users");
    public bool CanViewDataManagement => Can("DataManagement");
    public bool CanViewDataExchange => Can("DataExchange");
    public bool CanViewRolesPermissions => Can("RolesPermissions");

    // مجموعات القائمة الجانبية (تُخفى إن كان كل أعضائها مخفيًا)
    public bool CanViewSalesGroup => CanViewInvoices || CanViewParties || CanViewPayments;
    public bool CanViewInventoryGroup => CanViewItems || CanViewWarehouses || CanViewStockLevels ||
                                         CanViewWarehouseTransfers || CanViewStockCounts ||
                                         CanViewStockAdjustments || CanViewDamagedItems || CanViewWriteoffRequests;
    public bool CanViewHRGroup => CanViewEmployees || CanViewSalaries || CanViewLoans || CanViewPettyCash;
    public bool CanViewFinanceGroup => CanViewCashBoxes || CanViewBankReconciliation || CanViewExpenseCategories ||
                                       CanViewBadDebts || CanViewEarlyPaymentDiscount || CanViewLatePaymentFees;
    public bool CanViewAccountingGroup => CanViewAccounts || CanViewRecurringJournals || CanViewRecurringInvoices ||
                                          CanViewReportingSettings || CanViewRevenueRecognition ||
                                          CanViewBankStatementImport || CanViewCostCenters || CanViewCostAllocations ||
                                          CanViewAccountingPeriods || CanViewJournalEntries || CanViewBudgets ||
                                          CanViewFixedAssets || CanViewReports || CanViewAuditLog;
    public bool CanViewAdminGroup => CanViewSettings || CanViewUsers || CanViewDataManagement ||
                                     CanViewDataExchange || CanViewRolesPermissions;

    /// <summary>هل المستخدم مسموح له بعرض هذه الصفحة؟</summary>
    public bool Can(string pageKey)
    {
        // نعتمد على حالة المصادقة نفسها (لا على IsLoggedIn) حتى تُحسب الرؤية
        // بشكل صحيح فور تسجيل الدخول.
        if (!AuthService.IsLoggedIn) return false;
        var perm = PermissionCatalog.PagePermission(pageKey);
        if (string.IsNullOrEmpty(perm)) return true;   // صفحة بلا صلاحية — متاحة للجميع
        return AuthService.HasPermission(perm);
    }

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
            CurrentUserDisplay = (loginResult.User.FullName ?? loginResult.User.Username) +
                                 " (" + AuthService.CurrentRoleDisplayName + ")";
            IsLoggedIn = true;
            // تُحدَّث أعلام الرؤية بعد ضبط حالة الدخول — وإلا حُسبت كلها false.
            RefreshPermissionFlags();
            // P5: إذا كان البرنامج غير مفعّل نوجّه المستخدم مباشرةً إلى تبويب التفعيل داخل الإعدادات.
            if (App.LicenseActivationRequired)
            {
                Navigate("LicenseActivation");
                return;
            }
            Navigate(FirstAllowedPage());
        }
        else ErrorMessage = "\u0628\u064A\u0627\u0646\u0627\u062A \u0627\u0644\u062F\u062E\u0648\u0644 \u063A\u064A\u0631 \u0635\u062D\u064A\u062D\u0629";
    }

    /// <summary>
    /// أول صفحة يملك المستخدم صلاحيتها — نستخدمها بعد الدخول حتى لا يُرفض
    /// المستخدم الذي لا يملك صلاحية لوحة التحكم.
    /// </summary>
    private string FirstAllowedPage()
    {
        if (Can("Dashboard")) return "Dashboard";
        foreach (var page in new[]
        {
            "Invoices", "Parties", "Items", "Payments", "StockLevels", "CashBoxes",
            "Accounts", "Reports", "JournalEntries", "Users", "Settings"
        })
        {
            if (Can(page)) return page;
        }
        return "Dashboard";
    }

    /// <summary>يُحدِّث كل خصائص الرؤية بعد الدخول أو تغيير الدور.</summary>
    public void RefreshPermissionFlags()
    {
        OnPropertyChanged(nameof(CanViewDashboard));
        OnPropertyChanged(nameof(CanViewInvoices));
        OnPropertyChanged(nameof(CanViewParties));
        OnPropertyChanged(nameof(CanViewPayments));
        OnPropertyChanged(nameof(CanViewItems));
        OnPropertyChanged(nameof(CanViewWarehouses));
        OnPropertyChanged(nameof(CanViewStockLevels));
        OnPropertyChanged(nameof(CanViewWarehouseTransfers));
        OnPropertyChanged(nameof(CanViewStockCounts));
        OnPropertyChanged(nameof(CanViewStockAdjustments));
        OnPropertyChanged(nameof(CanViewDamagedItems));
        OnPropertyChanged(nameof(CanViewWriteoffRequests));
        OnPropertyChanged(nameof(CanViewEmployees));
        OnPropertyChanged(nameof(CanViewSalaries));
        OnPropertyChanged(nameof(CanViewLoans));
        OnPropertyChanged(nameof(CanViewPettyCash));
        OnPropertyChanged(nameof(CanViewCashBoxes));
        OnPropertyChanged(nameof(CanViewBankReconciliation));
        OnPropertyChanged(nameof(CanViewExpenseCategories));
        OnPropertyChanged(nameof(CanViewBadDebts));
        OnPropertyChanged(nameof(CanViewEarlyPaymentDiscount));
        OnPropertyChanged(nameof(CanViewLatePaymentFees));
        OnPropertyChanged(nameof(CanViewAccounts));
        OnPropertyChanged(nameof(CanViewRecurringJournals));
        OnPropertyChanged(nameof(CanViewRecurringInvoices));
        OnPropertyChanged(nameof(CanViewReportingSettings));
        OnPropertyChanged(nameof(CanViewRevenueRecognition));
        OnPropertyChanged(nameof(CanViewBankStatementImport));
        OnPropertyChanged(nameof(CanViewCostCenters));
        OnPropertyChanged(nameof(CanViewCostAllocations));
        OnPropertyChanged(nameof(CanViewAccountingPeriods));
        OnPropertyChanged(nameof(CanViewJournalEntries));
        OnPropertyChanged(nameof(CanViewBudgets));
        OnPropertyChanged(nameof(CanViewFixedAssets));
        OnPropertyChanged(nameof(CanViewReports));
        OnPropertyChanged(nameof(CanViewAuditLog));
        OnPropertyChanged(nameof(CanViewSettings));
        OnPropertyChanged(nameof(CanViewUsers));
        OnPropertyChanged(nameof(CanViewDataManagement));
        OnPropertyChanged(nameof(CanViewDataExchange));
        OnPropertyChanged(nameof(CanViewRolesPermissions));
        OnPropertyChanged(nameof(CanViewSalesGroup));
        OnPropertyChanged(nameof(CanViewInventoryGroup));
        OnPropertyChanged(nameof(CanViewHRGroup));
        OnPropertyChanged(nameof(CanViewFinanceGroup));
        OnPropertyChanged(nameof(CanViewAccountingGroup));
        OnPropertyChanged(nameof(CanViewAdminGroup));
    }

    private void Navigate(string? view)
    {
        if (string.IsNullOrEmpty(view) || !IsLoggedIn) return;

        // صفحة التفعيل متاحة دائمًا (البرنامج غير مفعّل بعد).
        if (view != "LicenseActivation" && !Can(view))
        {
            App.Log("Navigation to '" + view + "' rejected: missing permission '" +
                    PermissionCatalog.PagePermission(view) + "'");
            System.Windows.MessageBox.Show(
                "ليس لديك صلاحية الوصول إلى هذه الشاشة.",
                "صلاحية مرفوضة", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

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
            "RolesPermissions" => new RolesPermissionsViewModel(),
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
            if (type != null && typeof(System.Windows.Window).IsAssignableFrom(type))
            {
                var win = (System.Windows.Window)Activator.CreateInstance(type, id);
                win.ShowDialog();
                return;
            }

            // الاعتماد قد يشير إلى صفحة (UserControl) لا إلى نافذة — في هذه الحالة ننتقل إلى الصفحة
            if (!string.IsNullOrEmpty(PermissionCatalog.PagePermission(windowName)))
            {
                Navigate(windowName);
                return;
            }

            System.Windows.MessageBox.Show($"Window '{windowName}' not found.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
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
