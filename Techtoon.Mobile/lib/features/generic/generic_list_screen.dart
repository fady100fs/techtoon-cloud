import 'package:flutter/material.dart';
import '../../core/services/api_service.dart';
import 'package:provider/provider.dart';

class GenericListScreen extends StatefulWidget {
  final String title;
  final String endpoint;
  final List<Map<String, dynamic>> columns;

  const GenericListScreen({
    Key? key,
    required this.title,
    required this.endpoint,
    required this.columns,
  }) : super(key: key);

  @override
  _GenericListScreenState createState() => _GenericListScreenState();
}

class _GenericListScreenState extends State<GenericListScreen> {
  List<dynamic> _items = [];
  bool _isLoading = true;
  String _searchText = '';

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final api = Provider.of<ApiService>(context, listen: false);
      _items = await api.getList(widget.endpoint);
    } catch (e) {
      _items = [];
    }
    setState(() => _isLoading = false);
  }

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: _loadData,
      child: Column(
        children: [
          // Search Bar
          Padding(
            padding: EdgeInsets.all(8),
            child: TextField(
              decoration: InputDecoration(
                hintText: 'بحث...',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
              onChanged: (val) => setState(() => _searchText = val),
            ),
          ),
          // Data List
          Expanded(
            child: _isLoading
                ? Center(child: CircularProgressIndicator())
                : _items.isEmpty
                    ? Center(child: Text('لا توجد بيانات'))
                    : ListView.builder(
                        itemCount: _items.length,
                        itemBuilder: (ctx, i) {
                          final item = _items[i];
                          // Filter by search
                          if (_searchText.isNotEmpty) {
                            bool match = false;
                            for (var col in widget.columns) {
                              final val = item[col['field']]?.toString() ?? '';
                              if (val.toLowerCase().contains(_searchText.toLowerCase())) {
                                match = true;
                                break;
                              }
                            }
                            if (!match) return SizedBox.shrink();
                          }
                          return Card(
                            margin: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            child: ListTile(
                              title: Text(
                                item[widget.columns[0]['field']]?.toString() ?? '-',
                                style: TextStyle(fontWeight: FontWeight.bold),
                              ),
                              subtitle: Text(
                                widget.columns.skip(1).map((c) =>
                                  '\${c['label']}: \${item[c['field']] ?? '-'}'
                                ).join(' | '),
                              ),
                              trailing: Icon(Icons.chevron_left),
                            ),
                          );
                        },
                      ),
          ),
        ],
      ),
    );
  }
}
