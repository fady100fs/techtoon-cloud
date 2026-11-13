import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class ExpensesScreen extends StatefulWidget {
  @override
  _ExpensesScreenState createState() => _ExpensesScreenState();
}

class _ExpensesScreenState extends State<ExpensesScreen> {
  List<dynamic> _expenses = [];
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
      final data = await api.getItem('api/expenses?pageSize=50');
      if (!mounted) return;
      setState(() {
        _expenses = data['items'] as List<dynamic>? ?? [];
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _expenses = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل المصروفات';
        _isLoading = false;
      });
    }

    // Cash boxes are needed by the form (POST/PUT /api/expenses expects cashBoxId).
    try {
      final boxes = await api.getList('api/cashboxes');
      if (!mounted) return;
      setState(() => _cashBoxes = boxes);
    } catch (_) {
      // The list stays empty — the form will simply have no cash box options.
    }
  }

  Future<void> _deleteSelected() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('تأكيد الحذف'),
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} مصروف؟'),
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
        await api.delete('api/expenses/$id');
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

  Future<void> _showExpenseForm([Map<String, dynamic>? exp]) async {
    final isEdit = exp != null;
    final descCtrl = TextEditingController(text: exp?['description'] ?? '');
    final amtCtrl = TextEditingController(text: (exp?['amount'] ?? 0).toString());
    final methodCtrl = TextEditingController(text: exp?['paymentMethod'] ?? 'cash');

    // ExpenseCreateDto accepts cashBoxId — default to the first available cash box.
    int? selectedCashBoxId = exp?['cashBoxId'];
    if (selectedCashBoxId == null && _cashBoxes.isNotEmpty) {
      selectedCashBoxId = _cashBoxes.first['id'] as int?;
    }

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setState) => AlertDialog(
          title: Text(isEdit ? 'تعديل مصروف' : 'إضافة مصروف'),
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
                TextField(controller: descCtrl, decoration: InputDecoration(labelText: 'الوصف', border: OutlineInputBorder())),
                SizedBox(height: 8),
                TextField(controller: amtCtrl, decoration: InputDecoration(labelText: 'المبلغ', border: OutlineInputBorder()), keyboardType: TextInputType.numberWithOptions(decimal: true)),
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
                TextField(controller: methodCtrl, decoration: InputDecoration(labelText: 'طريقة الدفع', border: OutlineInputBorder())),
              ],
            ),
          ),
          actions: [
            if (isEdit)
              TextButton(
                onPressed: () async {
                  final api = Provider.of<ApiService>(context, listen: false);
                  try {
                    await api.delete('api/expenses/${exp!['id']}');
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
                final method = methodCtrl.text.trim();
                // ExpenseCreateDto: date, categoryId, amount, description,
                // paymentMethod, cashBoxId, notes.
                // There is no endpoint to list expense categories, so categoryId is
                // omitted and the API resolves the first available category itself.
                final body = <String, dynamic>{
                  'date': exp?['date'] ?? DateTime.now().toIso8601String(),
                  'amount': amount,
                  'description': descCtrl.text.trim(),
                  'paymentMethod': method.isEmpty ? 'cash' : method,
                  'cashBoxId': selectedCashBoxId,
                  'notes': exp?['notes'] ?? '',
                };
                final api = Provider.of<ApiService>(context, listen: false);
                try {
                  if (isEdit) {
                    await api.put('api/expenses/${exp!['id']}', body);
                  } else {
                    await api.post('api/expenses', body);
                  }
                  Navigator.pop(ctx, true);
                } catch (e) {
                  if (!ctx.mounted) return;
                  ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                    content: Text(e is ApiException ? e.message : 'تعذر حفظ المصروف'),
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
        title: Text(_isSelectMode ? 'تحديد ${_selectedIds.length}' : 'المصروفات'),
        backgroundColor: Colors.blueGrey,
        actions: _isSelectMode ? [
          IconButton(icon: Icon(Icons.delete), onPressed: _deleteSelected),
          IconButton(icon: Icon(Icons.close), onPressed: () => setState(() { _isSelectMode = false; _selectedIds.clear(); })),
        ] : null,
      ),
      floatingActionButton: !_isSelectMode ? FloatingActionButton(
        onPressed: () => _showExpenseForm(),
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
                    child: _expenses.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل المصروفات' : 'لا توجد مصروفات',
                            ),
                          )
                        : ListView.builder(
                            itemCount: _expenses.length,
                            itemBuilder: (context, i) {
                              final e = _expenses[i];
                              final amt = (e['amount'] ?? 0).toDouble();
                              final id = e['id'] as int;
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
                                      _showExpenseForm(e);
                                    }
                                  },
                                  leading: CircleAvatar(
                                    backgroundColor: Colors.red,
                                    child: _isSelectMode
                                        ? Icon(isSelected ? Icons.check_box : Icons.check_box_outline_blank, color: Colors.white)
                                        : Icon(Icons.money_off, color: Colors.white),
                                  ),
                                  title: Text(e['description'] ?? 'مصروف'),
                                  subtitle: Text('${e['date'] ?? ''} | ${e['paymentMethod'] ?? ''}'),
                                  trailing: Text('${amt.toStringAsFixed(2)}', style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
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
