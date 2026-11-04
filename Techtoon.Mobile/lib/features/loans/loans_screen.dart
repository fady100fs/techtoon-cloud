import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class LoansScreen extends StatefulWidget {
  @override
  _LoansScreenState createState() => _LoansScreenState();
}

class _LoansScreenState extends State<LoansScreen> {
  List<dynamic> _loans = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    final api = Provider.of<ApiService>(context, listen: false);
    // Assuming API has a loans endpoint. If not, it will return empty.
    final data = await api.getRaw('api/loans?pageSize=50');
    try {
      final json = jsonDecode(data);
      setState(() {
        _loans = json['items'] ?? [];
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('القروض'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _loans.isEmpty
                  ? Center(child: Text('لا توجد قروض'))
                  : ListView.builder(
                      itemCount: _loans.length,
                      itemBuilder: (context, i) {
                        final l = _loans[i];
                        final amt = (l['amount'] ?? 0).toDouble();
                        return Card(
                          margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                          child: ListTile(
                            leading: CircleAvatar(backgroundColor: Colors.purple, child: Icon(Icons.credit_card, color: Colors.white)),
                            title: Text(l['loanName'] ?? l['borrowerName'] ?? 'قرض'),
                            subtitle: Text('المبلغ: ${amt.toStringAsFixed(2)} | التاريخ: ${l['loanDate'] ?? ''}'),
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}