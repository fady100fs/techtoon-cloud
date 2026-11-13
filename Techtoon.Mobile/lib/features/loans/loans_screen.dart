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
      // GET /api/loans returns a paged result:
      // { items: [{ id, loanNumber, borrowerName, loanType, principalAmount,
      //             paidAmount, remainingAmount, status }], totalCount, ... }
      final data = await api.getItem('api/loans?pageSize=50');
      if (!mounted) return;
      setState(() {
        _loans = data['items'] as List<dynamic>? ?? [];
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loans = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل القروض';
        _isLoading = false;
      });
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
              child: Column(
                children: [
                  if (_error != null) _errorBanner(),
                  Expanded(
                    child: _loans.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل القروض' : 'لا توجد قروض',
                            ),
                          )
                        : ListView.builder(
                            itemCount: _loans.length,
                            itemBuilder: (context, i) {
                              final l = _loans[i];
                              final principal =
                                  (l['principalAmount'] ?? 0).toDouble();
                              final paid = (l['paidAmount'] ?? 0).toDouble();
                              final remaining =
                                  (l['remainingAmount'] ?? 0).toDouble();
                              final status = l['status'] ?? '';
                              final type = l['loanType'] ?? '';
                              final number = l['loanNumber'] ?? '';

                              return Card(
                                margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                                child: ListTile(
                                  leading: CircleAvatar(
                                      backgroundColor: Colors.purple,
                                      child: Icon(Icons.credit_card, color: Colors.white)),
                                  title: Text(l['borrowerName'] ?? 'قرض'),
                                  subtitle: Text(
                                    'رقم: $number | النوع: $type | الحالة: $status\n'
                                    'المسدد: ${paid.toStringAsFixed(2)} | المتبقي: ${remaining.toStringAsFixed(2)}',
                                  ),
                                  isThreeLine: true,
                                  trailing: Text(
                                    '${principal.toStringAsFixed(2)}',
                                    style: TextStyle(
                                        fontWeight: FontWeight.bold, color: Colors.purple),
                                  ),
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
