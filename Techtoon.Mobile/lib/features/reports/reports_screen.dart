import 'package:flutter/material.dart';

class ReportsScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('التقارير'), backgroundColor: Colors.blueGrey),
      body: Center(
        child: Padding(
          padding: EdgeInsets.all(24),
          child: Text('سيتم إضافة التقارير قريباً', style: TextStyle(fontSize: 18, color: Colors.grey)),
        ),
      ),
    );
  }
}