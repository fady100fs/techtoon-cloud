import 'dart:convert';
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

  @override
  void initState() {
    super.initState();
    _loadStats();
  }

  Future<void> _loadStats() async {
    final api = Provider.of<ApiService>(context, listen: false);
    try {
      final inv = await api.getRaw('api/invoices?pageSize=1');
      final items = await api.getRaw('api/items?pageSize=1');
      final parties = await api.getRaw('api/parties?pageSize=1');
      final pay = await api.getRaw('api/payments?pageSize=1');

      setState(() {
        _invoices = _extractCount(inv);
        _items = _extractCount(items);
        _parties = _extractCount(parties);
        _payments = _extractCount(pay);
        _isLoading = false;
      });
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  int _extractCount(String json) {
    try {
      final data = jsonDecode(json);
      return data['totalCount'] ?? 0;
    } catch (_) { return 0; }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('لوحة المعلومات'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _loadStats,
              child: GridView.count(
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
}