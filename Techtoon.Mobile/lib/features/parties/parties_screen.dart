import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class PartiesScreen extends StatefulWidget {
  final String? type;
  PartiesScreen({this.type});

  @override
  _PartiesScreenState createState() => _PartiesScreenState();
}

class _PartiesScreenState extends State<PartiesScreen> {
  List<dynamic> _parties = [];
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
          ? await api.getItemsByField('api/parties', 'type', widget.type!)
          : await api.getAllItems('api/parties');
      if (!mounted) return;
      setState(() {
        _parties = items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _parties = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل الأطراف';
        _isLoading = false;
      });
    }
  }

  Future<void> _deleteSelected() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('تأكيد الحذف'),
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} طرف؟'),
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
        await api.delete('api/parties/$id');
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

  Future<void> _showPartyForm([Map<String, dynamic>? party]) async {
    final isEdit = party != null;
    final nameCtrl = TextEditingController(text: party?['name'] ?? '');
    final phoneCtrl = TextEditingController(text: party?['phone'] ?? '');
    final addrCtrl = TextEditingController(text: party?['address'] ?? '');

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isEdit ? 'تعديل بيانات' : 'إضافة جديد'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(controller: nameCtrl, decoration: InputDecoration(labelText: 'الاسم', border: OutlineInputBorder())),
              SizedBox(height: 8),
              TextField(controller: phoneCtrl, decoration: InputDecoration(labelText: 'الهاتف', border: OutlineInputBorder()), keyboardType: TextInputType.phone),
              SizedBox(height: 8),
              TextField(controller: addrCtrl, decoration: InputDecoration(labelText: 'العنوان', border: OutlineInputBorder()), maxLines: 2),
            ],
          ),
        ),
        actions: [
          if (isEdit)
            TextButton(
              onPressed: () async {
                final api = Provider.of<ApiService>(context, listen: false);
                try {
                  await api.delete('api/parties/${party!['id']}');
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
              final body = {
                'name': nameCtrl.text.trim(),
                'type': widget.type ?? 'customer',
                'phone': phoneCtrl.text.trim(),
                'address': addrCtrl.text.trim(),
                'openingBalance': 0.0,
              };
              final api = Provider.of<ApiService>(context, listen: false);
              try {
                if (isEdit) {
                  await api.put('api/parties/${party!['id']}', body);
                } else {
                  await api.post('api/parties', body);
                }
                Navigator.pop(ctx, true);
              } catch (e) {
                if (!ctx.mounted) return;
                ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
                  content: Text(e is ApiException ? e.message : 'تعذر الحفظ'),
                  backgroundColor: Colors.red,
                ));
              }
            },
            child: Text('حفظ التعديلات'),
          ),
        ],
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
        title: Text(_isSelectMode ? 'تحديد ${_selectedIds.length}' : (widget.type == 'customer' ? 'العملاء' : widget.type == 'supplier' ? 'الموردون' : 'الأطراف')),
        backgroundColor: Colors.blueGrey,
        actions: _isSelectMode ? [
          IconButton(icon: Icon(Icons.delete), onPressed: _deleteSelected),
          IconButton(icon: Icon(Icons.close), onPressed: () => setState(() { _isSelectMode = false; _selectedIds.clear(); })),
        ] : null,
      ),
      floatingActionButton: !_isSelectMode ? FloatingActionButton(
        onPressed: () => _showPartyForm(),
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
                    child: _parties.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل الأطراف' : 'لا توجد بيانات',
                            ),
                          )
                        : ListView.builder(
                            itemCount: _parties.length,
                            itemBuilder: (context, i) {
                              final p = _parties[i];
                              final bal = (p['currentBalance'] ?? 0).toDouble();
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
                                      _showPartyForm(p);
                                    }
                                  },
                                  leading: CircleAvatar(
                                    backgroundColor: bal > 0 ? Colors.red : bal < 0 ? Colors.green : Colors.grey,
                                    child: _isSelectMode
                                        ? Icon(isSelected ? Icons.check_box : Icons.check_box_outline_blank, color: Colors.white)
                                        : Icon(Icons.person, color: Colors.white),
                                  ),
                                  title: Text(p['name'] ?? '', style: TextStyle(fontWeight: FontWeight.bold)),
                                  subtitle: Text(p['phone'] ?? 'لا يوجد هاتف'),
                                  trailing: Text('${bal.toStringAsFixed(2)}', style: TextStyle(fontWeight: FontWeight.bold, color: bal > 0 ? Colors.red : Colors.green)),
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