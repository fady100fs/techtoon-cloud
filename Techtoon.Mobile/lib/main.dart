import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/services/api_service.dart';
import 'features/auth/login_screen.dart';

void main() {
  runApp(MyApp());
}

class MyApp extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Provider(
      create: (_) => ApiService(),
      child: MaterialApp(
        title: 'Techtoon Mobile',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          primarySwatch: Colors.blueGrey,
          fontFamily: 'Arial',
        ),
        home: LoginScreen(),
      ),
    );
  }
}