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
  List<dynamic> _cashBoxes = [];
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
          ? await api.getItemsByField('api/payments', 'paymentType', widget.type!)
          : await api.getAllItems('api/payments');
      if (!mounted) return;
      setState(() {
        _payments = items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _payments = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل المدفوعات';
        _isLoading = false;
      });
    }

    // Reference data (parties + cash boxes). A failure here must not hide the list.
    try {
      final parties = await api.getItem('api/parties?pageSize=100');
      final boxes = await api.getList('api/cashboxes');
      if (!mounted) return;
      setState(() {
        _parties = parties['items'] as List<dynamic>? ?? [];
        _cashBoxes = boxes;
      });
    } catch (_) {
      // The reference lists stay empty — the form will simply have no options.
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
    try {
      for (final id in _selectedIds.toList()) {
        await api.delete('api/payments/$id');
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

  Future<void> _showPaymentForm([Map<String, dynamic>? pay]) async {
    final isEdit = pay != null;
    final amtCtrl = TextEditingController(text: (pay?['amount'] ?? 0).toString());
    final noteCtrl = TextEditingController(text: pay?['notes'] ?? '');
    int? selectedPartyId = pay?['partyId'];
    String method = pay?['paymentMethod'] ?? 'cash';

    // POST/PUT /api/payments require cashBoxId — the payment must affect a cash box.
    int? selectedCashBoxId = pay?['cashBoxId'];
    if (selectedCashBoxId == null && _cashBoxes.isNotEmpty) {
      selectedCashBoxId = _cashBoxes.first['id'] as int?;
    }

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isEdit ? 'تعديل ${widget.type == 'receipt' ? 'سند قبض' : 'سند صرف'}' : 'إضافة ${widget.type == 'receipt' ? 'سند قبض' : 'سند صرف'}'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (_cashBoxes.isEmpty)
                  Padding(
                    padding: EdgeInsets.only(bottom: 8),
                    child: Text(
                      'تعذر تحميل الخزائن — تحقق من الاتصال بالخادم',
                      style: TextStyle(color: Colors.red, fontSize: 12),
                    ),
                  ),
                DropdownButtonFormField<int>(
                  value: _parties.any((p) => p['id'] == selectedPartyId) ? selectedPartyId : null,
                  decoration: InputDecoration(labelText: 'الطرف', border: OutlineInputBorder()),
                  items: _parties.map<DropdownMenuItem<int>>((p) {
                    return DropdownMenuItem<int>(value: p['id'], child: Text(p['name'] ?? ''));
                  }).toList(),
                  onChanged: (v) => setState(() => selectedPartyId = v),
                ),
                SizedBox(height: 8),
                DropdownButtonFormField<int>(
                  value: _cashBoxes.any((b) => b['id'] == selectedCashBoxId) ? selectedCashBoxId : null,
                  decoration: InputDecoration(labelText: 'الخزينة / الحساب', border: OutlineInputBorder()),
                  items: _cashBoxes.map<DropdownMenuItem<int>>((b) {
                    return DropdownMenuItem<int>(value: b['id'], child: Text(b['name'] ?? ''));
                  }).toList(),
                  onChanged: (v) => setState(() => selectedCashBoxId = v),
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
          ),
          actions: [
            if (isEdit)
              TextButton(
                onPressed: () async {
                  final api = Provider.of<ApiService>(context, listen: false);
                  try {
                    await api.delete('api/payments/${pay!['id']}');
                    Navigator.pop(ctx, true);
                  } catch (e) {
                    if (!ctx.mounted) return;
                    ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                      content: Text(e is ApiException ? e.message : 'تعذر الحذف'),
                      backgroundColor: Colors.red,
                    ));
                  }
                },
                child: Text('حذف', style: TextStyle(color: Colors.red)),
              ),
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
            ElevatedButton(
              onPressed: () async {
                final amount = double.tryParse(amtCtrl.text.trim()) ?? 0;
                if (amount <= 0) {
                  ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                      content: Text('أدخل مبلغاً أكبر من صفر'), backgroundColor: Colors.red));
                  return;
                }
                if (selectedCashBoxId == null) {
                  ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                      content: Text('اختر الخزينة أو الحساب'), backgroundColor: Colors.red));
                  return;
                }
                final body = {
                  'date': pay?['date'] ?? DateTime.now().toIso8601String(),
                  'partyId': selectedPartyId,
                  'amount': amount,
                  'paymentType': widget.type ?? 'receipt',
                  'paymentMethod': method,
                  'notes': noteCtrl.text.trim(),
                  'cashBoxId': selectedCashBoxId,
                };
                final api = Provider.of<ApiService>(context, listen: false);
                try {
                  if (isEdit) {
                    await api.put('api/payments/${pay!['id']}', body);
                  } else {
                    await api.post('api/payments', body);
                  }
                  Navigator.pop(ctx, true);
                } catch (e) {
                  if (!ctx.mounted) return;
                  ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                    content: Text(e is ApiException ? e.message : 'تعذر حفظ السند'),
                    backgroundColor: Colors.red,
                  ));
                }
              },
              child: Text('حفظ التعديلات'),
            ),
          ],
        ),
      ),
    );
    if (result == true) {
      await _load();
      if (!mounted) return;
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
              child: Column(
                children: [
                  if (_error != null) _errorBanner(),
                  Expanded(
                    child: _payments.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل المدفوعات' : 'لا توجد مدفوعات',
                            ),
                          )
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
