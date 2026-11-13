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
  String? _error;
  final Set<int> _selectedIds = {};

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
      // The API's server-side `type` filter is broken (it answers 400), so the
      // list is fetched unfiltered and narrowed down on the device.
      final items = widget.type != null
          ? await api.getItemsByField('api/invoices', 'type', widget.type!)
          : await api.getAllItems('api/invoices');
      if (!mounted) return;
      setState(() {
        _invoices = items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _invoices = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل الفواتير';
        _isLoading = false;
      });
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
    try {
      for (final id in _selectedIds.toList()) {
        await api.delete('api/invoices/$id');
      }
      setState(() { _isSelectMode = false; _selectedIds.clear(); });
      await _load();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحذف'), backgroundColor: Colors.green));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(e is ApiException ? e.message : 'تعذر الحذف'),
        backgroundColor: Colors.red,
      ));
    }
  }

  Future<void> _viewInvoice(Map<String, dynamic> inv) async {
    final api = Provider.of<ApiService>(context, listen: false);
    Map<String, dynamic> details;
    try {
      details = await api.getItem('api/invoices/${inv['id']}');
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(e is ApiException ? e.message : 'تعذر تحميل تفاصيل الفاتورة'),
        backgroundColor: Colors.red,
      ));
      return;
    }

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
              try {
                await api.delete('api/invoices/${inv['id']}');
                Navigator.pop(ctx);
                await _load();
                if (!mounted) return;
                ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم حذف الفاتورة'), backgroundColor: Colors.green));
              } catch (e) {
                if (!ctx.mounted) return;
                ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                  content: Text(e is ApiException ? e.message : 'تعذر حذف الفاتورة'),
                  backgroundColor: Colors.red,
                ));
              }
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
              child: Column(
                children: [
                  if (_error != null) _errorBanner(),
                  Expanded(
                    child: _invoices.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل الفواتير' : 'لا توجد فواتير',
                            ),
                          )
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