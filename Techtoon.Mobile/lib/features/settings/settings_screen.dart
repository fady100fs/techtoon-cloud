import 'package:flutter/material.dart';

class SettingsScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('الإعدادات'), backgroundColor: Colors.blueGrey),
      body: ListView(
        children: [
          ListTile(
            leading: Icon(Icons.cloud, color: Colors.blueGrey),
            title: Text('حالة الاتصال بالخادم'),
            subtitle: Text('متصل بـ https://techtoon-api.fly.dev'),
            trailing: Icon(Icons.check_circle, color: Colors.green),
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