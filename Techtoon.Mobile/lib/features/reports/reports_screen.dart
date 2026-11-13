import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class ReportsScreen extends StatefulWidget {
  @override
  _ReportsScreenState createState() => _ReportsScreenState();
}

class _ReportsScreenState extends State<ReportsScreen> {
  bool _isLoading = true;
  String? _error;

  int _invoiceCount = 0;
  double _invoiceTotal = 0;
  int _paymentCount = 0;
  double _receiptsTotal = 0;
  double _paymentsTotal = 0;
  int _expenseCount = 0;
  double _expensesTotal = 0;
  int _itemCount = 0;
  int _partyCount = 0;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    final api = Provider.of<ApiService>(context, listen: false);
    try {
      // Real data straight from the API — no hardcoded numbers.
      final invoices = await api.getAllItems('api/invoices');
      final payments = await api.getAllItems('api/payments');
      final expenses = await api.getAllItems('api/expenses');
      final items = await api.getAllItems('api/items');
      final parties = await api.getAllItems('api/parties');
      if (!mounted) return;

      var invoiceTotal = 0.0;
      for (final inv in invoices) {
        invoiceTotal += _toDouble(inv['total']);
      }

      var receipts = 0.0;
      var paid = 0.0;
      for (final p in payments) {
        if (p['paymentType'] == 'payment') {
          paid += _toDouble(p['amount']);
        } else {
          receipts += _toDouble(p['amount']);
        }
      }

      var expensesTotal = 0.0;
      for (final e in expenses) {
        expensesTotal += _toDouble(e['amount']);
      }

      setState(() {
        _invoiceCount = invoices.length;
        _invoiceTotal = invoiceTotal;
        _paymentCount = payments.length;
        _receiptsTotal = receipts;
        _paymentsTotal = paid;
        _expenseCount = expenses.length;
        _expensesTotal = expensesTotal;
        _itemCount = items.length;
        _partyCount = parties.length;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e is ApiException ? e.message : 'تعذر تحميل التقارير';
        _isLoading = false;
      });
    }
  }

  double _toDouble(dynamic value) {
    if (value is num) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0;
    return 0;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('التقارير'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: EdgeInsets.all(12),
                children: [
                  if (_error != null) _errorBanner(),
                  _sectionTitle('الفواتير'),
                  Row(
                    children: [
                      Expanded(
                        child: _reportCard('عدد الفواتير', '$_invoiceCount',
                            Icons.receipt_long, Colors.blue),
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: _reportCard(
                            'إجمالي الفواتير', _invoiceTotal.toStringAsFixed(2),
                            Icons.calculate, Colors.indigo),
                      ),
                    ],
                  ),
                  _sectionTitle('المقبوضات والمدفوعات'),
                  Row(
                    children: [
                      Expanded(
                        child: _reportCard('عدد السندات', '$_paymentCount',
                            Icons.payments, Colors.purple),
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: _reportCard('إجمالي المقبوضات',
                            _receiptsTotal.toStringAsFixed(2), Icons.south_west,
                            Colors.green),
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: _reportCard('إجمالي المدفوعات',
                            _paymentsTotal.toStringAsFixed(2), Icons.north_east,
                            Colors.red),
                      ),
                    ],
                  ),
                  _sectionTitle('المصروفات'),
                  Row(
                    children: [
                      Expanded(
                        child: _reportCard('عدد المصروفات', '$_expenseCount',
                            Icons.money_off, Colors.orange),
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: _reportCard('إجمالي المصروفات',
                            _expensesTotal.toStringAsFixed(2), Icons.trending_down,
                            Colors.deepOrange),
                      ),
                    ],
                  ),
                  _sectionTitle('الأرصدة والأطراف'),
                  Row(
                    children: [
                      Expanded(
                        child: _reportCard('عدد الأصناف', '$_itemCount',
                            Icons.inventory_2, Colors.teal),
                      ),
                      SizedBox(width: 8),
                      Expanded(
                        child: _reportCard('عدد الأطراف', '$_partyCount',
                            Icons.people, Colors.brown),
                      ),
                    ],
                  ),
                  SizedBox(height: 12),
                  Center(
                    child: Text(
                      'البيانات محسوبة مباشرة من واجهة البرمجة',
                      style: TextStyle(color: Colors.grey[600], fontSize: 12),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _sectionTitle(String title) {
    return Padding(
      padding: EdgeInsets.only(top: 8, bottom: 6),
      child: Text(
        title,
        style: TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.bold,
          color: Colors.blueGrey,
        ),
      ),
    );
  }

  Widget _reportCard(String title, String value, IconData icon, Color color) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Container(
        padding: EdgeInsets.symmetric(vertical: 14, horizontal: 8),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 26, color: color),
            SizedBox(height: 6),
            FittedBox(
              child: Text(
                value,
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
            ),
            SizedBox(height: 2),
            Text(
              title,
              style: TextStyle(fontSize: 11, color: Colors.grey[600]),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _errorBanner() {
    return Container(
      width: double.infinity,
      margin: EdgeInsets.only(bottom: 10),
      padding: EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: Colors.red.shade50,
        border: Border.all(color: Colors.red.shade300),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline, color: Colors.red.shade700, size: 20),
          SizedBox(width: 8),
          Expanded(
            child: Text(
              _error!,
              style: TextStyle(color: Colors.red.shade800, fontSize: 13),
            ),
          ),
          IconButton(
            icon: Icon(Icons.refresh, color: Colors.red.shade700, size: 20),
            onPressed: _load,
            tooltip: 'إعادة المحاولة',
          ),
        ],
      ),
    );
  }
}
