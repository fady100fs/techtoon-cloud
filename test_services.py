# test_services.py
"""
ملف اختبار الدوال
"""

from services import create_item, create_party_with_opening_balance, create_kit_item, create_invoice

print("=" * 60)
print(" اختبار نظام المحاسبة")
print("=" * 60)

# 1. إنشاء أصناف عادية
print("\n 1. إنشاء الأصناف...")
create_item(name="ورق A4", cost_price=50.0, sell_price=75.0, barcode="123456")
create_item(name="فايل حفظ", cost_price=10.0, sell_price=15.0, barcode="789012")

# 2. إنشاء صنف مدمج
print("\n📦 2. إنشاء صنف مدمج (إقرار ضريبي)...")
create_kit_item(
    kit_name="إقرار ضريبي",
    components=[
        {'item_name': 'ورق A4', 'quantity': 5},
        {'item_name': 'فايل حفظ', 'quantity': 1}
    ],
    sell_price=100.0
)

# 3. إنشاء عميل مع رصيد افتتاحي
print("\n👥 3. إنشاء عميل مع رصيد افتتاحي...")
create_party_with_opening_balance(
    name="شركة الأمل",
    party_type="customer",
    opening_balance=5000.0,
    phone="0501234567",
    address="الرياض"
)

# 4. إنشاء فاتورة بيع
print("\n🧾 4. إنشاء فاتورة بيع...")
create_invoice(
    party_name="شركة الأمل",
    party_type="customer",
    invoice_type="sale",
    items=[
        {'item_name': 'ورق A4', 'quantity': 10, 'price': 75.0},
        {'item_name': 'فايل حفظ', 'quantity': 5, 'price': 15.0}
    ],
    invoice_number="INV-000001"
)

print("\n" + "=" * 60)
print("✅ تم الانتهاء من جميع الاختبارات بنجاح!")
print("=" * 60)