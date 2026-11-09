# INCOMPLETE FEATURES

> لا حذف بدون إثبات وموافقة. Flutter/Streamlit مهملان (قرار مالك).

| # | الميزة | الوضع | السبب | ما يحتاج |
|---|--------|-------|-------|----------|
| 1 | RecurringInvoicesView | stub (نص "قيد التطوير") | لا VM/خدمة | VM + جدول/خدمة (قرار معماري) |
| 2 | Expenses PUT (API) | مفقود (Mobile PUT=405) | Service بلا Update | UpdateExpenseAsync + PUT |
| 3 | Employees/Warehouses/Salaries/Loans write | read-only | P2.19 stubs | Controllers + Mobile forms (Flutter غير مطلوب) |
| 4 | FinancialService | orphan (0 callers) | لا أحد يستدعيه | Wire إلى Transfer/Close UI أو حذف (قرار) |
| 5 | LoanEditWindow | orphan duplicate | LoansEditWindow المستخدم | حذف أو توثيق (قرار) |
| 6 | 54 sync files | تدريجي | P0-4 batches (8 hot paths تمت) | Batches لاحقة |
| 7 | 106 in-progress | محلي فقط | عمل ميزات قيد التطوير | إتمام + موافقة قبل push |
| 8 | Mobile create (invoices/cashboxes) | read-only | Flutter SDK غير متوفر + link-only | قرار (تخطي مقصود) |
| 9 | Deleted Invoice/Return ledger reversal | partial | يحتاج قرار محاسبي (credit-note vs cash) | موافقة P1 |
| 10 | Tests (WPF UI) | manual | لا أتمتة سطح مكتب | بيئة اختبار يدوية |

## قرارات مطلوبة من المالك
- D1-D6 (تدوين في TECHTOON_PENDING_DECISIONS.md).
- Cash auto-refund للمرتجعات.
- Streamlit/Flutter مهملان (مؤكد).
