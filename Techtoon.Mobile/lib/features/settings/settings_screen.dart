import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/services/api_service.dart';

class SettingsScreen extends StatefulWidget {
  @override
  _SettingsScreenState createState() => _SettingsScreenState();
}

class _SettingsScreenState extends State<SettingsScreen> {
  bool _isChecking = true;
  bool _isConnected = false;
  String? _error;
  String _health = '';

  @override
  void initState() {
    super.initState();
    _checkConnection();
  }

  /// Calls the real health endpoint of the API the app is configured to use.
  Future<void> _checkConnection() async {
    setState(() {
      _isChecking = true;
      _error = null;
    });
    final api = Provider.of<ApiService>(context, listen: false);
    try {
      final body = await api.checkHealth();
      if (!mounted) return;
      setState(() {
        _isConnected = true;
        _health = body.trim();
        _isChecking = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isConnected = false;
        _health = '';
        _error = e is ApiException ? e.message : 'تعذر الاتصال بالخادم';
        _isChecking = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final baseUrl = ApiService.baseUrl;

    return Scaffold(
      appBar: AppBar(title: Text('الإعدادات'), backgroundColor: Colors.blueGrey),
      body: ListView(
        children: [
          ListTile(
            leading: Icon(Icons.cloud, color: Colors.blueGrey),
            title: Text('حالة الاتصال بالخادم'),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('الخادم: $baseUrl'),
                SizedBox(height: 4),
                if (_isChecking)
                  Row(
                    children: [
                      SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2)),
                      SizedBox(width: 8),
                      Text('جارٍ فحص الاتصال...', style: TextStyle(color: Colors.grey[600])),
                    ],
                  )
                else if (_isConnected)
                  Text(
                    'متصل بالخادم ($_health)',
                    style: TextStyle(color: Colors.green, fontWeight: FontWeight.bold),
                  )
                else
                  Text(
                    _error ?? 'فشل الاتصال بالخادم',
                    style: TextStyle(color: Colors.red, fontWeight: FontWeight.bold),
                  ),
              ],
            ),
            trailing: _isChecking
                ? SizedBox(width: 24, height: 24, child: CircularProgressIndicator(strokeWidth: 2))
                : Icon(
                    _isConnected ? Icons.check_circle : Icons.error,
                    color: _isConnected ? Colors.green : Colors.red,
                  ),
            onTap: _checkConnection,
          ),
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16),
            child: OutlinedButton.icon(
              icon: Icon(Icons.refresh),
              label: Text('إعادة فحص الاتصال'),
              onPressed: _isChecking ? null : _checkConnection,
            ),
          ),
          Divider(),
          ListTile(
            leading: Icon(Icons.update, color: Colors.blueGrey),
            title: Text('تحديث البيانات'),
            subtitle: Text('تحديث الصفحات يدوياً (Pull to Refresh)'),
          ),
          Divider(),
          ListTile(
            leading: Icon(Icons.info, color: Colors.blueGrey),
            title: Text('حول التطبيق'),
            subtitle: Text('Techtoon Mobile v1.0.0\nFlutter + ASP.NET Core API'),
          ),
        ],
      ),
    );
  }
}
