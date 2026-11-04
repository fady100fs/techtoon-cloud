import 'package:flutter/material.dart';
import '../invoices/invoices_screen.dart';

class SalesScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return InvoicesScreen(type: 'sales');
  }
}