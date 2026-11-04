# test_serial.py
from services import movement_serial, parse_movement_serial, movement_serial_with_year

print("🧪 اختبار دالة المسلسل الرقمي")
print("=" * 50)

# اختبار 1: مسلسل عادي
print(f"movement_serial('CM', 45)     = {movement_serial('CM', 45)}")
print(f"movement_serial('PAY', 123)   = {movement_serial('PAY', 123)}")
print(f"movement_serial('TRF', 7)     = {movement_serial('TRF', 7)}")
print(f"movement_serial('EXP', 999999)= {movement_serial('EXP', 999999)}")

# اختبار 2: قيم غريبة
print(f"movement_serial('CM', None)   = {movement_serial('CM', None)}")
print(f"movement_serial('PAY', 'abc') = {movement_serial('PAY', 'abc')}")

# اختبار 3: عكس المسلسل
print(f"parse_movement_serial('CM-000045')   = {parse_movement_serial('CM-000045')}")
print(f"parse_movement_serial('PAY-123')     = {parse_movement_serial('PAY-123')}")
print(f"parse_movement_serial('غير صحيح')    = {parse_movement_serial('غير صحيح')}")

# اختبار 4: مع السنة
print(f"movement_serial_with_year('PAY', 123, 2026) = {movement_serial_with_year('PAY', 123, 2026)}")

print("=" * 50)
print("✅ انتهى الاختبار")