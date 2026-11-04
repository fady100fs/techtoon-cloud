import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class PaymentsScreen extends StatefulWidget {
  final String? type;
  PaymentsScreen({this.type});

  @override
  _PaymentsScreenState createState() => _PaymentsScreenState();
}

class _PaymentsScreenState extends State<PaymentsScreen> {
  List<dynamic> _payments = [];
  List<dynamic> _parties = [];
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
    final ep = widget.type != null ? 'api/payments?type=${widget.type}&pageSize=50' : 'api/payments?pageSize=50';
    final data = await api.getRaw(ep);
    final partyData = await api.getRaw('api/parties?pageSize=100');
    try {
      setState(() {
        _payments = jsonDecode(data)['items'] ?? [];
        _parties = jsonDecode(partyData)['items'] ?? [];
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
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} سند؟'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text('حذف', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;

    final api = Provider.of<ApiService>(context, listen: false);
    for (final id in _selectedIds.toList()) {
      await api.delete('api/payments/$id');
    }
    setState(() { _isSelectMode = false; _selectedIds.clear(); });
    _load();
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحذف'), backgroundColor: Colors.green));
  }

  Future<void> _showPaymentForm([Map<String, dynamic>? pay]) async {
    final isEdit = pay != null;
    final amtCtrl = TextEditingController(text: (pay?['amount'] ?? 0).toString());
    final noteCtrl = TextEditingController(text: pay?['notes'] ?? '');
    int? selectedPartyId = pay?['partyId'];
    String method = pay?['paymentMethod'] ?? 'cash';

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isEdit ? 'تعديل ${widget.type == 'receipt' ? 'سند قبض' : 'سند صرف'}' : 'إضافة ${widget.type == 'receipt' ? 'سند قبض' : 'سند صرف'}'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              DropdownButtonFormField<int>(
                value: selectedPartyId,
                decoration: InputDecoration(labelText: 'الطرف', border: OutlineInputBorder()),
                items: _parties.map<DropdownMenuItem<int>>((p) {
                  return DropdownMenuItem<int>(value: p['id'], child: Text(p['name'] ?? ''));
                }).toList(),
                onChanged: (v) => setState(() => selectedPartyId = v),
              ),
              SizedBox(height: 8),
              TextField(controller: amtCtrl, decoration: InputDecoration(labelText: 'المبلغ', border: OutlineInputBorder()), keyboardType: TextInputType.numberWithOptions(decimal: true)),
              SizedBox(height: 8),
              DropdownButtonFormField<String>(
                value: method,
                decoration: InputDecoration(labelText: 'طريقة الدفع', border: OutlineInputBorder()),
                items: ['cash', 'card', 'transfer', 'cheque'].map<DropdownMenuItem<String>>((m) {
                  return DropdownMenuItem<String>(value: m, child: Text(m));
                }).toList(),
                onChanged: (v) => setState(() => method = v ?? 'cash'),
              ),
              SizedBox(height: 8),
              TextField(controller: noteCtrl, decoration: InputDecoration(labelText: 'ملاحظات', border: OutlineInputBorder())),
            ],
          ),
          actions: [
            if (isEdit)
              TextButton(
                onPressed: () async {
                  final api = Provider.of<ApiService>(context, listen: false);
                  await api.delete('api/payments/${pay!['id']}');
                  Navigator.pop(ctx, true);
                },
                child: Text('حذف', style: TextStyle(color: Colors.red)),
              ),
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
            ElevatedButton(
              onPressed: () async {
                final body = {
                  'date': pay?['date'] ?? DateTime.now().toIso8601String(),
                  'partyId': selectedPartyId,
                  'amount': double.tryParse(amtCtrl.text) ?? 0,
                  'paymentType': widget.type ?? 'receipt',
                  'paymentMethod': method,
                  'notes': noteCtrl.text.trim(),
                };
                final api = Provider.of<ApiService>(context, listen: false);
                if (isEdit) {
                  await api.put('api/payments/${pay!['id']}', body);
                } else {
                  await api.post('api/payments', body);
                }
                Navigator.pop(ctx, true);
              },
              child: Text('حفظ التعديلات'),
            ),
          ],
        ),
      ),
    );
    if (result == true) {
      _load();
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحفظ'), backgroundColor: Colors.green));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isSelectMode ? 'تحديد ${_selectedIds.length}' : (widget.type == 'receipt' ? 'سندات القبض' : widget.type == 'payment' ? 'سندات الصرف' : 'المدفوعات')),
        backgroundColor: Colors.blueGrey,
        actions: _isSelectMode ? [
          IconButton(icon: Icon(Icons.delete), onPressed: _deleteSelected),
          IconButton(icon: Icon(Icons.close), onPressed: () => setState(() { _isSelectMode = false; _selectedIds.clear(); })),
        ] : null,
      ),
      floatingActionButton: !_isSelectMode ? FloatingActionButton(
        onPressed: () => _showPaymentForm(),
        child: Icon(Icons.add),
        backgroundColor: Colors.blueGrey,
      ) : null,
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: _payments.isEmpty
                  ? Center(child: Text('لا توجد مدفوعات'))
                  : ListView.builder(
                      itemCount: _payments.length,
                      itemBuilder: (context, i) {
                        final p = _payments[i];
                        final amt = (p['amount'] ?? 0).toDouble();
                        final isReceipt = p['paymentType'] == 'receipt';
                        final id = p['id'] as int;
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
                                _showPaymentForm(p);
                              }
                            },
                            leading: CircleAvatar(
                              backgroundColor: isReceipt ? Colors.green : Colors.red,
                              child: _isSelectMode
                                  ? Icon(isSelected ? Icons.check_box : Icons.check_box_outline_blank, color: Colors.white)
                                  : Icon(isReceipt ? Icons.south_west : Icons.north_east, color: Colors.white),
                            ),
                            title: Text(p['partyName'] ?? 'طرف غير محدد'),
                            subtitle: Text('${p['voucherNo'] ?? ''} | ${p['date'] ?? ''}'),
                            trailing: Text('${amt.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.bold, color: isReceipt ? Colors.green : Colors.red)),
                          ),
                        );
                      },
                    ),
            ),
    );
  }
}