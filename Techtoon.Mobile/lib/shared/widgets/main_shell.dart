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
              _drawerItem(Icons.dashboard, 'لوحة المعلومات', () => _navigate(DashboardScreen(), 'لوحة المعلومات')),
              _drawerItem(Icons.inventory_2, 'الأصناف', () => _navigate(ProductsScreen(), 'الأصناف')),
              Divider(),
              _drawerItem(Icons.receipt_long, 'فواتير المبيعات', () => _navigate(InvoicesScreen(type: 'sales'), 'فواتير المبيعات')),
              _drawerItem(Icons.shopping_cart, 'فواتير المشتريات', () => _navigate(InvoicesScreen(type: 'purchase'), 'فواتير المشتريات')),
              Divider(),
              _drawerItem(Icons.people, 'العملاء', () => _navigate(PartiesScreen(type: 'customer'), 'العملاء')),
              _drawerItem(Icons.local_shipping, 'الموردون', () => _navigate(PartiesScreen(type: 'supplier'), 'الموردون')),
              Divider(),
              _drawerItem(Icons.south_west, 'سندات القبض', () => _navigate(PaymentsScreen(type: 'receipt'), 'سندات القبض')),
              _drawerItem(Icons.north_east, 'سندات الصرف', () => _navigate(PaymentsScreen(type: 'payment'), 'سندات الصرف')),
              _drawerItem(Icons.money_off, 'المصروفات', () => _navigate(ExpensesScreen(), 'المصروفات')),
              _drawerItem(Icons.account_balance_wallet, 'الخزائن', () => _navigate(CashBoxesScreen(), 'الخزائن')),
              Divider(),
              _drawerItem(Icons.warehouse, 'المخزون', () => _navigate(InventoryScreen(), 'المخزون')),
              _drawerItem(Icons.credit_card, 'القروض', () => _navigate(LoansScreen(), 'القروض')),
              Divider(),
              _drawerItem(Icons.assessment, 'التقارير', () => _navigate(ReportsScreen(), 'التقارير')),
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
}