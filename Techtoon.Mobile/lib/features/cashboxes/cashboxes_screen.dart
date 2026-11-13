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
  String? _error;

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
      final boxes = await api.getList('api/cashboxes');
      if (!mounted) return;
      setState(() {
        _boxes = boxes;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _boxes = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل الخزائن';
        _isLoading = false;
      });
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
              child: Column(
                children: [
                  if (_error != null) _errorBanner(),
                  Expanded(
                    child: _boxes.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل الخزائن' : 'لا توجد خزائن',
                            ),
                          )
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
            onPressed: _load,
            tooltip: 'إعادة المحاولة',
          ),
        ],
      ),
    );
  }
}