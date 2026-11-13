import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class DashboardScreen extends StatefulWidget {
  @override
  _DashboardScreenState createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  int _invoices = 0;
  int _items = 0;
  int _parties = 0;
  int _payments = 0;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadStats();
  }

  Future<void> _loadStats() async {
    final api = Provider.of<ApiService>(context, listen: false);
    try {
      final inv = await api.getItem('api/invoices?pageSize=1');
      final items = await api.getItem('api/items?pageSize=1');
      final parties = await api.getItem('api/parties?pageSize=1');
      final pay = await api.getItem('api/payments?pageSize=1');
      if (!mounted) return;

      setState(() {
        _invoices = _countOf(inv);
        _items = _countOf(items);
        _parties = _countOf(parties);
        _payments = _countOf(pay);
        _error = null;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e is ApiException ? e.message : 'تعذر تحميل لوحة المعلومات';
        _isLoading = false;
      });
    }
  }

  int _countOf(Map<String, dynamic> data) {
    final total = data['totalCount'];
    return total is int ? total : 0;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('لوحة المعلومات'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadStats,
              child: ListView(
                children: [
                  if (_error != null) _errorBanner(),
                  GridView.count(
                    shrinkWrap: true,
                    physics: NeverScrollableScrollPhysics(),
                    crossAxisCount: 2,
                    padding: EdgeInsets.all(16),
                    mainAxisSpacing: 12,
                    crossAxisSpacing: 12,
                    children: [
                      _statCard('الفواتير', _invoices, Icons.receipt_long, Colors.blue),
                      _statCard('الأصناف', _items, Icons.inventory_2, Colors.green),
                      _statCard('العملاء/الموردين', _parties, Icons.people, Colors.orange),
                      _statCard('المدفوعات', _payments, Icons.payments, Colors.purple),
                    ],
                  ),
                ],
              ),
            ),
    );
  }

  Widget _statCard(String title, int count, IconData icon, Color color) {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Container(
        padding: EdgeInsets.all(16),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 40, color: color),
            SizedBox(height: 8),
            Text('$count', style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold)),
            SizedBox(height: 4),
            Text(title, style: TextStyle(fontSize: 14, color: Colors.grey[600])),
          ],
        ),
      ),
    );
  }

  Widget _errorBanner() {
    return Container(
      width: double.infinity,
      margin: EdgeInsets.symmetric(horizontal: 12, vertical: 8),
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
            onPressed: _loadStats,
            tooltip: 'إعادة المحاولة',
          ),
        ],
      ),
    );
  }
}
