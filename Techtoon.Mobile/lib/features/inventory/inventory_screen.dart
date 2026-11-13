import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class InventoryScreen extends StatefulWidget {
  @override
  _InventoryScreenState createState() => _InventoryScreenState();
}

class _InventoryScreenState extends State<InventoryScreen> {
  List<dynamic> _items = [];
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
      final items = await api.getList('api/items/low-stock');
      if (!mounted) return;
      setState(() {
        _items = items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _items = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل المخزون';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('المخزون (الأصناف منخفضة المخزون)'), backgroundColor: Colors.blueGrey),
      body: _isLoading
          ? Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: Column(
                children: [
                  if (_error != null) _errorBanner(),
                  Expanded(
                    child: _items.isEmpty
                        ? Center(
                            child: Text(
                              _error != null ? 'تعذر تحميل المخزون' : 'لا توجد أصناف منخفضة المخزون',
                            ),
                          )
                        : ListView.builder(
                            itemCount: _items.length,
                            itemBuilder: (context, i) {
                              final item = _items[i];
                              final stock = (item['currentStock'] ?? 0).toDouble();
                              final min = (item['minStock'] ?? 0).toDouble();
                              return Card(
                                margin: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                                color: stock <= min ? Colors.red[50] : null,
                                child: ListTile(
                                  leading: CircleAvatar(backgroundColor: Colors.orange, child: Icon(Icons.warning, color: Colors.white)),
                                  title: Text(item['name'] ?? ''),
                                  subtitle: Text('المتوفر: ${stock.toStringAsFixed(0)} | الحد الأدنى: ${min.toStringAsFixed(0)}'),
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