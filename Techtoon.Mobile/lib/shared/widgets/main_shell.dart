import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';
import '../../features/auth/login_screen.dart';
import '../../features/dashboard/dashboard_screen.dart';
import '../../features/products/products_screen.dart';
import '../../features/invoices/invoices_screen.dart';
import '../../features/parties/parties_screen.dart';
import '../../features/payments/payments_screen.dart';
import '../../features/expenses/expenses_screen.dart';
import '../../features/cashboxes/cashboxes_screen.dart';
import '../../features/reports/reports_screen.dart';
import '../../features/inventory/inventory_screen.dart';
import '../../features/loans/loans_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../features/generic/generic_list_screen.dart';

class MainShell extends StatefulWidget {
  @override
  _MainShellState createState() => _MainShellState();
}

class _MainShellState extends State<MainShell> {
  Widget _currentPage = DashboardScreen();
  String _currentTitle = 'لوحة المعلومات';

  void _navigate(Widget page, String title) {
    Navigator.pop(context);
    setState(() {
      _currentPage = page;
      _currentTitle = title;
    });
  }

  Widget _genericScreen(String title, String endpoint, List<Map<String, dynamic>> columns) {
    return GenericListScreen(title: title, endpoint: endpoint, columns: columns);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_currentTitle, style: TextStyle(color: Colors.white)),
        backgroundColor: Colors.blueGrey,
        iconTheme: IconThemeData(color: Colors.white),
      ),
      drawer: Drawer(
        child: Directionality(
          textDirection: TextDirection.rtl,
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              UserAccountsDrawerHeader(
                accountName: Text('Techtoon'),
                accountEmail: Text('النظام المحاسبي'),
                currentAccountPicture: CircleAvatar(child: Icon(Icons.account_circle, size: 40), backgroundColor: Colors.white),
                decoration: BoxDecoration(color: Colors.blueGrey),
              ),
              // === لوحة المعلومات ===
              _drawerItem(Icons.dashboard, 'لوحة المعلومات', () => _navigate(DashboardScreen(), 'لوحة المعلومات')),
              Divider(),

              // === المبيعات والمشتريات ===
              _sectionHeader('المبيعات والمشتريات'),
              _drawerItem(Icons.receipt_long, 'فواتير المبيعات', () => _navigate(InvoicesScreen(type: 'sales'), 'فواتير المبيعات')),
              _drawerItem(Icons.shopping_cart, 'فواتير المشتريات', () => _navigate(InvoicesScreen(type: 'purchase'), 'فواتير المشتريات')),
              _drawerItem(Icons.people, 'العملاء', () => _navigate(PartiesScreen(type: 'customer'), 'العملاء')),
              _drawerItem(Icons.local_shipping, 'الموردون', () => _navigate(PartiesScreen(type: 'supplier'), 'الموردون')),
              _drawerItem(Icons.south_west, 'سندات القبض', () => _navigate(PaymentsScreen(type: 'receipt'), 'سندات القبض')),
              _drawerItem(Icons.north_east, 'سندات الصرف', () => _navigate(PaymentsScreen(type: 'payment'), 'سندات الصرف')),
              Divider(),

              // === المخزون والأصول ===
              _sectionHeader('المخزون والأصول'),
              _drawerItem(Icons.inventory_2, 'الأصناف', () => _navigate(ProductsScreen(), 'الأصناف')),
              _drawerItem(Icons.warehouse, 'إدارة المخازن', () => _navigate(
                _genericScreen('إدارة المخازن', 'api/warehouses', [
                  {'field': 'name', 'label': 'الاسم'},
                  {'field': 'code', 'label': 'الكود'},
                  {'field': 'location', 'label': 'الموقع'},
                ]), 'إدارة المخازن')),
              _drawerItem(Icons.swap_horiz, 'تحويل بين المخازن', () => _navigate(
                _genericScreen('تحويل بين المخازن', 'api/warehouse-transfers', [
                  {'field': 'transferNumber', 'label': 'رقم التحويل'},
                  {'field': 'date', 'label': 'التاريخ'},
                  {'field': 'status', 'label': 'الحالة'},
                ]), 'تحويل بين المخازن')),
              _drawerItem(Icons.inventory, 'المخزون', () => _navigate(InventoryScreen(), 'المخزون')),
              Divider(),

              // === الموارد البشرية ===
              _sectionHeader('الموارد البشرية'),
              _drawerItem(Icons.person, 'إدارة الموظفين', () => _navigate(
                _genericScreen('إدارة الموظفين', 'api/employees', [
                  {'field': 'fullName', 'label': 'الاسم'},
                  {'field': 'jobTitle', 'label': 'الوظيفة'},
                  {'field': 'phone', 'label': 'الهاتف'},
                ]), 'إدارة الموظفين')),
              _drawerItem(Icons.payments, 'سجلات المرتبات', () => _navigate(
                _genericScreen('سجلات المرتبات', 'api/salaries', [
                  {'field': 'employeeName', 'label': 'الموظف'},
                  {'field': 'netSalary', 'label': 'الصافي'},
                  {'field': 'paymentDate', 'label': 'التاريخ'},
                ]), 'سجلات المرتبات')),
              _drawerItem(Icons.credit_card, 'السلف والقروض', () => _navigate(LoansScreen(), 'السلف والقروض')),
              _drawerItem(Icons.account_balance_wallet, 'العهد والمصروفات', () => _navigate(
                _genericScreen('العهد والمصروفات', 'api/petty-cash/custodies', [
                  {'field': 'custodyNumber', 'label': 'رقم العهدة'},
                  {'field': 'employeeName', 'label': 'الموظف'},
                  {'field': 'amount', 'label': 'المبلغ'},
                ]), 'العهد والمصروفات')),
              Divider(),

              // === المالية والمصرفية ===
              _sectionHeader('المالية والمصرفية'),
              _drawerItem(Icons.account_balance, 'الخزائن والبنوك', () => _navigate(CashBoxesScreen(), 'الخزائن والبنوك')),
              _drawerItem(Icons.receipt, 'التسوية البنكية', () => _navigate(
                _genericScreen('التسوية البنكية', 'api/bank-reconciliation/statements', [
                  {'field': 'cashBoxName', 'label': 'الحساب'},
                  {'field': 'periodStart', 'label': 'من'},
                  {'field': 'periodEnd', 'label': 'إلى'},
                  {'field': 'openingBalance', 'label': 'رصيد افتتاحي'},
                  {'field': 'closingBalance', 'label': 'رصيد ختامي'},
                  {'field': 'status', 'label': 'الحالة'},
                ]), 'التسوية البنكية')),
              _drawerItem(Icons.money, 'المصروفات', () => _navigate(ExpensesScreen(), 'المصروفات')),
              _drawerItem(Icons.delete, 'الديون المعدومة', () => _navigate(
                _genericScreen('الديون المعدومة', 'api/bad-debts', [
                  {'field': 'writeoffNumber', 'label': 'رقم الشطب'},
                  {'field': 'partyName', 'label': 'الطرف'},
                  {'field': 'writeoffAmount', 'label': 'المبلغ'},
                ]), 'الديون المعدومة')),
              Divider(),

              // === المحاسبة والتقارير ===
              _sectionHeader('المحاسبة والتقارير'),
              _drawerItem(Icons.replay, 'القيود الدورية', () => _navigate(
                _genericScreen('القيود الدورية', 'api/recurring-journals', [
                  {'field': 'name', 'label': 'الاسم'},
                  {'field': 'frequency', 'label': 'التكرار'},
                  {'field': 'nextRunDate', 'label': 'التنفيذ القادم'},
                ]), 'القيود الدورية')),
              _drawerItem(Icons.assessment, 'التقارير', () => _navigate(ReportsScreen(), 'التقارير')),
              Divider(),

              // === الإعدادات ===
              _sectionHeader('الإعدادات'),
              _drawerItem(Icons.settings, 'الإعدادات', () => _navigate(SettingsScreen(), 'الإعدادات')),
              Divider(),
              _drawerItem(Icons.logout, 'تسجيل الخروج', () {
                Navigator.pop(context);
                final api = Provider.of<ApiService>(context, listen: false);
                api.setToken('');
                Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => LoginScreen()));
              }, color: Colors.red),
            ],
          ),
        ),
      ),
      body: _currentPage,
    );
  }

  Widget _drawerItem(IconData icon, String title, VoidCallback onTap, {Color? color}) {
    return ListTile(
      leading: Icon(icon, color: color ?? Colors.blueGrey),
      title: Text(title, style: TextStyle(color: color)),
      onTap: onTap,
    );
  }

  Widget _sectionHeader(String title) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Text(
        title,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.bold,
          color: Colors.grey[600],
        ),
      ),
    );
  }
}
