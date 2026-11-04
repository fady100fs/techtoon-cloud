import 'dart:convert';
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

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    final api = Provider.of<ApiService>(context, listen: false);
    final data = await api.getRaw('api/items/low-stock');
    try {
      setState(() {
        _items = jsonDecode(data);
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
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
              child: _items.isEmpty
                  ? Center(child: Text('لا توجد أصناف منخفضة المخزون'))
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
    );
  }
}