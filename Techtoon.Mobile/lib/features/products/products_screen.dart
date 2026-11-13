import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class ProductsScreen extends StatefulWidget {
  @override
  _ProductsScreenState createState() => _ProductsScreenState();
}

class _ProductsScreenState extends State<ProductsScreen> {
  List<dynamic> _items = [];
  List<dynamic> _filtered = [];
  bool _isLoading = true;
  bool _isSelectMode = false;
  String? _error;
  final Set<int> _selectedIds = {};
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    _loadItems();
  }

  Future<void> _loadItems() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    final api = Provider.of<ApiService>(context, listen: false);
    try {
      final data = await api.getItem('api/items?pageSize=200');
      if (!mounted) return;
      setState(() {
        _items = data['items'] as List<dynamic>? ?? [];
        _filtered = _items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _items = [];
        _filtered = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل الأصناف';
        _isLoading = false;
      });
    }
  }

  void _filter(String q) {
    setState(() {
      _filtered = _items.where((i) => (i['name'] ?? '').toLowerCase().contains(q.toLowerCase())).toList();
    });
  }

  Future<void> _deleteSelected() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('تأكيد الحذف'),
        content: Text('هل أنت متأكد من حذف ${_selectedIds.length} عنصر؟'),
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
        await api.delete('api/items/$id');
      }
      setState(() { _isSelectMode = false; _selectedIds.clear(); });
      await _loadItems();
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

  Future<void> _showItemForm([Map<String, dynamic>? item]) async {
    final isEdit = item != null;
    final nameCtrl = TextEditingController(text: item?['name'] ?? '');
    final barcodeCtrl = TextEditingController(text: item?['barcode'] ?? '');
    final priceCtrl = TextEditingController(text: (item?['sellPrice'] ?? 0).toString());
    final costCtrl = TextEditingController(text: (item?['costPrice'] ?? 0).toString());
    final stockCtrl = TextEditingController(text: (item?['openingQuantity'] ?? 0).toString());
    final minStockCtrl = TextEditingController(text: (item?['minStock'] ?? 0).toString());

    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(isEdit ? 'تعديل صنف' : 'إضافة صنف جديد'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _input(nameCtrl, 'اسم الصنف'),
              SizedBox(height: 8),
              _input(barcodeCtrl, 'الباركود'),
              SizedBox(height: 8),
              _input(priceCtrl, 'سعر البيع', num: true),
              SizedBox(height: 8),
              _input(costCtrl, 'سعر التكلفة', num: true),
              SizedBox(height: 8),
              _input(stockCtrl, 'الكمية الافتتاحية', num: true),
              SizedBox(height: 8),
              _input(minStockCtrl, 'الحد الأدنى للمخزون', num: true),
            ],
          ),
        ),
        actions: [
          if (isEdit)
            TextButton(
              onPressed: () async {
                final api = Provider.of<ApiService>(context, listen: false);
                try {
                  await api.delete('api/items/${item!['id']}');
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
                'barcode': barcodeCtrl.text.trim(),
                'sellPrice': double.tryParse(priceCtrl.text) ?? 0,
                'costPrice': double.tryParse(costCtrl.text) ?? 0,
                'openingQuantity': double.tryParse(stockCtrl.text) ?? 0,
                'minStock': double.tryParse(minStockCtrl.text) ?? 0,
                'isKit': false,
              };
              final api = Provider.of<ApiService>(context, listen: false);
              try {
                if (isEdit) {
                  await api.put('api/items/${item!['id']}', body);
                } else {
                  await api.post('api/items', body);
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
      await _loadItems();
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('تم الحفظ بنجاح'), backgroundColor: Colors.green));
    }
  }

  Widget _input(TextEditingController ctrl, String label, {bool num = false}) {
    return TextField(
      controller: ctrl,
      keyboardType: num ? TextInputType.numberWithOptions(decimal: true) : TextInputType.text,
      decoration: InputDecoration(labelText: label, border: OutlineInputBorder()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(_isSelectMode ? 'تحديد ${_selectedIds.length}' : 'الأصناف'),
        backgroundColor: Colors.blueGrey,
        actions: _isSelectMode ? [
          IconButton(icon: Icon(Icons.delete), onPressed: _deleteSelected),
          IconButton(icon: Icon(Icons.close), onPressed: () => setState(() { _isSelectMode = false; _selectedIds.clear(); })),
        ] : null,
      ),
      floatingActionButton: !_isSelectMode ? FloatingActionButton(
        onPressed: () => _showItemForm(),
        child: Icon(Icons.add),
        backgroundColor: Colors.blueGrey,
      ) : null,
      body: Column(
        children: [
          Padding(
            padding: EdgeInsets.all(12),
            child: TextField(
              controller: _search,
              onChanged: _filter,
              decoration: InputDecoration(
                hintText: 'بحث بالاسم...',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
            ),
          ),
          if (_error != null) _errorBanner(),
          Expanded(
            child: _isLoading
                ? Center(child: CircularProgressIndicator())
                : RefreshIndicator(
                    onRefresh: _loadItems,
                    child: _filtered.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل الأصناف' : 'لا توجد أصناف',
                            ),
                          )
                        : ListView.builder(
                            itemCount: _filtered.length,
                            itemBuilder: (context, index) {
                              final item = _filtered[index];
                              final stock = (item['currentStock'] ?? 0).toDouble();
                              final price = (item['sellPrice'] ?? 0).toDouble();
                              final id = item['id'] as int;
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
                                      _showItemForm(item);
                                    }
                                  },
                                  leading: CircleAvatar(
                                    backgroundColor: stock > 0 ? Colors.green : Colors.red,
                                    child: _isSelectMode
                                        ? Icon(isSelected ? Icons.check_box : Icons.check_box_outline_blank, color: Colors.white)
                                        : Icon(Icons.inventory, color: Colors.white, size: 20),
                                  ),
                                  title: Text(item['name'] ?? '', style: TextStyle(fontWeight: FontWeight.bold)),
                                  subtitle: Text('السعر: ${price.toStringAsFixed(2)} | المخزون: ${stock.toStringAsFixed(0)}'),
                                  trailing: item['isKit'] == true
                                      ? Container(padding: EdgeInsets.all(4), decoration: BoxDecoration(color: Colors.blue[100], borderRadius: BorderRadius.circular(4)), child: Text('كيت', style: TextStyle(fontSize: 10)))
                                      : null,
                                ),
                              );
                            },
                          ),
                  ),
          ),
        ],
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
            onPressed: _loadItems,
            tooltip: 'إعادة المحاولة',
          ),
        ],
      ),
    );
  }
}