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
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final api = Provider.of<ApiService>(context, listen: false);
      final items = await api.getList(widget.endpoint);
      if (!mounted) return;
      setState(() {
        _items = items;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _items = [];
        _error = e is ApiException ? e.message : 'تعذر تحميل البيانات';
        _isLoading = false;
      });
    }
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
          // API error — a failed request must never look like "no data".
          if (_error != null) _errorBanner(),
          // Data List
          Expanded(
            child: _isLoading
                ? Center(child: CircularProgressIndicator())
                : _items.isEmpty
                    ? Center(
                        child: Text(
                          _error != null ? 'تعذر تحميل البيانات' : 'لا توجد بيانات',
                        ),
                      )
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
                                  '${c['label']}: ${item[c['field']] ?? '-'}'
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

  Widget _errorBanner() {
    return Container(
      width: double.infinity,
      margin: EdgeInsets.symmetric(horizontal: 8),
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
            onPressed: _loadData,
            tooltip: 'إعادة المحاولة',
          ),
        ],
      ),
    );
  }
}
