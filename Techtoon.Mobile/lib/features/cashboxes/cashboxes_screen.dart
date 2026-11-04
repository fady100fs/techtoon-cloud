import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class CashBoxesScreen extends StatefulWidget {
  @override
  _CashBoxesScreenState createState() => _CashBoxesScreenState();
}

class _CashBoxesScreenState extends State<CashBoxesScreen> {
  List<dynamic> _boxes = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    final api = Provider.of<ApiService>(context, listen: false);
    final data = await api.getRaw('api/cashboxes');
    try {
      setState(() {
        _boxes = jsonDecode(data);
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('الخزائن'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _boxes.isEmpty
                  ? Center(child: Text('لا توجد خزائن'))
                  : ListView.builder(
                      itemCount: _boxes.length,
                      itemBuilder: (context, i) {
                        final b = _boxes[i];
                        final bal = (b['currentBalance'] ?? 0).toDouble();
                        return Card(
                          margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                          child: ListTile(
                            leading: CircleAvatar(backgroundColor: Colors.blueGrey, child: Icon(Icons.account_balance_wallet, color: Colors.white)),
                            title: Text(b['name'] ?? '', style: TextStyle(fontWeight: FontWeight.bold)),
                            subtitle: Text('${b['currency'] ?? 'EGP'} | رصيد افتتاحي: ${(b['openingBalance'] ?? 0).toDouble().toStringAsFixed(2)}'),
                            trailing: Text('${bal.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: bal >= 0 ? Colors.green : Colors.red)),
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}