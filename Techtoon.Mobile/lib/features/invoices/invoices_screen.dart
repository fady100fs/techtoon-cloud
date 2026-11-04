import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class InvoicesScreen extends StatefulWidget {
  final String? type;
  InvoicesScreen({this.type});

  @override
  _InvoicesScreenState createState() => _InvoicesScreenState();
}

class _InvoicesScreenState extends State<InvoicesScreen> {
  List<dynamic> _invoices = [];
  bool _isLoading = true;
  bool _isSelectMode = false;
  final Set<int> _selectedIds = {};

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    final api = Provider.of<ApiService>(context, listen: false);
    final ep = widget.type != null ? 'api/invoices?type=${widget.type}&pageSize=50' : 'api/invoices?pageSize=50';
    final data = await api.getRaw(ep);
    try {
      final json = jsonDecode(data);
      setState(() {
        _invoices = json['items'] ?? [];
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _deleteSelected() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('تأكيد الحذف'),
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} فاتورة؟'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text('حذف', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;

    final api = Provider.of<ApiService>(context, listen: false);
    for (final id in _selectedIds.toList()) {
      await api.delete('api/invoices/$id');
    }
    setState(() { _isSelectMode = false; _selectedIds.clear(); });
    _load();
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحذف'), backgroundColor: Colors.green));
  }

  Future<void> _viewInvoice(Map<String, dynamic> inv) async {
    final api = Provider.of<ApiService>(context, listen: false);
    final details = await api.getItem('api/invoices/${inv['id']}');
    
    if (!mounted) return;
    
    await showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('تفاصيل الفاتورة ${inv['invoiceNumber']}'),
        content: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('الطرف: ${inv['partyName'] ?? ''}', style: TextStyle(fontWeight: FontWeight.bold)),
              Text('التاريخ: ${inv['date'] ?? ''}'),
              Text('الإجمالي: ${(inv['total'] ?? 0).toDouble().toStringAsFixed(2)}'),
              Text('المدفوع: ${(inv['paidAmount'] ?? 0).toDouble().toStringAsFixed(2)}'),
              Text('الحالة: ${inv['status'] ?? ''}'),
              Divider(),
              Text('البنود:', style: TextStyle(fontWeight: FontWeight.bold)),
              ...(details['lines'] as List<dynamic>? ?? []).map((l) => Padding(
                padding: EdgeInsets.symmetric(vertical: 4),
                child: Text('${l['itemName']} | كمية: ${l['quantity']} | سعر: ${l['price']} | إجمالي: ${l['total']}'),
              )).toList(),
              if ((details['payments'] as List<dynamic>? ?? []).isNotEmpty) ...[
                Divider(),
                Text('المدفوعات:', style: TextStyle(fontWeight: FontWeight.bold)),
                ...(details['payments'] as List<dynamic>? ?? []).map((p) => Padding(
                  padding: EdgeInsets.symmetric(vertical: 4),
                  child: Text('${p['method']} | مبلغ: ${p['amount']}'),
                )).toList(),
              ]
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () async {
              await api.delete('api/invoices/${inv['id']}');
              Navigator.pop(ctx);
              _load();
              ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم حذف الفاتورة'), backgroundColor: Colors.green));
            },
            child: Text('حذف الفاتورة', style: TextStyle(color: Colors.red)),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text('إغلاق'),
          ),
        ],
      ),
    );
  }

  Color _statusColor(String? status) {
    switch (status) {
      case 'paid': return Colors.green;
      case 'partial': return Colors.orange;
      case 'unpaid': return Colors.red;
      case 'cancelled': return Colors.grey;
      default: return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isSelectMode ? 'تحديد ${_selectedIds.length}' : (widget.type == 'sales' ? 'فواتير المبيعات' : widget.type == 'purchase' ? 'فواتير المشتريات' : 'الفواتير')),
        backgroundColor: Colors.blueGrey,
        actions: _isSelectMode ? [
          IconButton(icon: Icon(Icons.delete), onPressed: _deleteSelected),
          IconButton(icon: Icon(Icons.close), onPressed: () => setState(() { _isSelectMode = false; _selectedIds.clear(); })),
        ] : null,
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _invoices.isEmpty
                  ? Center(child: Text('لا توجد فواتير'))
                  : ListView.builder(
                      itemCount: _invoices.length,
                      itemBuilder: (context, i) {
                        final inv = _invoices[i];
                        final total = (inv['total'] ?? 0).toDouble();
                        final id = inv['id'] as int;
                        final isSelected = _selectedIds.contains(id);

                        return Card(
                          color: isSelected ? Colors.blueGrey[100] : null,
                          margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                          child: ListTile(
                            onLongPress: () {
                              setState(() {
                                _isSelectMode = true;
                                _selectedIds.add(id);
                              });
                            },
                            onTap: () {
                              if (_isSelectMode) {
                                setState(() {
                                  if (isSelected) _selectedIds.remove(id);
                                  else _selectedIds.add(id);
                                  if (_selectedIds.isEmpty) _isSelectMode = false;
                                });
                              } else {
                                _viewInvoice(inv);
                              }
                            },
                            title: Text(inv['invoiceNumber'] ?? '', style: TextStyle(fontWeight: FontWeight.bold)),
                            subtitle: Text('${inv['partyName'] ?? ''} | ${inv['date'] ?? ''}'),
                            trailing: Column(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Text('${total.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.bold)),
                                Container(
                                  padding: EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                  decoration: BoxDecoration(color: _statusColor(inv['status']), borderRadius: BorderRadius.circular(8)),
                                  child: Text(inv['status'] ?? '', style: TextStyle(color: Colors.white, fontSize: 10)),
                                ),
                              ],
                            ),
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}