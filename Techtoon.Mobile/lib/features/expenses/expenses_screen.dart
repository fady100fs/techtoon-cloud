import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class ExpensesScreen extends StatefulWidget {
  @override
  _ExpensesScreenState createState() => _ExpensesScreenState();
}

class _ExpensesScreenState extends State<ExpensesScreen> {
  List<dynamic> _expenses = [];
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
    final data = await api.getRaw('api/expenses?pageSize=50');
    try {
      final json = jsonDecode(data);
      setState(() {
        _expenses = json['items'] ?? [];
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
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} مصروف؟'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
          TextButton(onPressed: () => Navigator.pop(ctx, true), child: Text('حذف', style: TextStyle(color: Colors.red))),
        ],
      ),
    );
    if (confirmed != true) return;

    final api = Provider.of<ApiService>(context, listen: false);
    for (final id in _selectedIds.toList()) {
      await api.delete('api/expenses/$id');
    }
    setState(() { _isSelectMode = false; _selectedIds.clear(); });
    _load();
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحذف'), backgroundColor: Colors.green));
  }

  Future<void> _showExpenseForm([Map<String, dynamic>? exp]) async {
    final isEdit = exp != null;
    final descCtrl = TextEditingController(text: exp?['description'] ?? '');
    final amtCtrl = TextEditingController(text: (exp?['amount'] ?? 0).toString());
    final methodCtrl = TextEditingController(text: exp?['paymentMethod'] ?? 'cash');

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isEdit ? 'تعديل مصروف' : 'إضافة مصروف'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(controller: descCtrl, decoration: InputDecoration(labelText: 'الوصف', border: OutlineInputBorder())),
            SizedBox(height: 8),
            TextField(controller: amtCtrl, decoration: InputDecoration(labelText: 'المبلغ', border: OutlineInputBorder()), keyboardType: TextInputType.numberWithOptions(decimal: true)),
            SizedBox(height: 8),
            TextField(controller: methodCtrl, decoration: InputDecoration(labelText: 'طريقة الدفع', border: OutlineInputBorder())),
          ],
        ),
        actions: [
          if (isEdit)
            TextButton(
              onPressed: () async {
                final api = Provider.of<ApiService>(context, listen: false);
                await api.delete('api/expenses/${exp!['id']}');
                Navigator.pop(ctx, true);
              },
              child: Text('حذف', style: TextStyle(color: Colors.red)),
            ),
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: Text('إلغاء')),
          ElevatedButton(
            onPressed: () async {
              final body = {
                'date': exp?['date'] ?? DateTime.now().toIso8601String(),
                'amount': double.tryParse(amtCtrl.text) ?? 0,
                'description': descCtrl.text.trim(),
                'paymentMethod': methodCtrl.text.trim(),
                'notes': '',
              };
              final api = Provider.of<ApiService>(context, listen: false);
              if (isEdit) {
                await api.put('api/expenses/${exp!['id']}', body);
              } else {
                await api.post('api/expenses', body);
              }
              Navigator.pop(ctx, true);
            },
            child: Text('حفظ التعديلات'),
          ),
        ],
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
              child: _expenses.isEmpty
                  ? Center(child: Text('لا توجد مصروفات'))
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
    );
  }
}