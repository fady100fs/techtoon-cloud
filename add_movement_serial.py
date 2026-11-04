# add_movement_serial.py
# يضيف دوال المسلسل الرقمي إلى نهاية services.py
# آمن: يتحقق أولاً إذا كانت الدوال موجودة لتجنّب التكرار

from pathlib import Path
import shutil
from datetime import datetime

SERVICES = Path(__file__).parent / "services.py"

if not SERVICES.exists():
    print("❌ services.py غير موجود")
    exit(1)

content = SERVICES.read_text(encoding="utf-8")

# ✅ تحقق إذا كانت الدالة موجودة مسبقاً
if "def movement_serial(" in content:
    print("ℹ️  الدالة 'movement_serial' موجودة مسبقاً — لا حاجة للتعديل")
    exit(0)

# 💾 نسخة احتياطية
backup = SERVICES.with_name(
    f"services.py.backup_{datetime.now():%Y%m%d_%H%M%S}"
)
shutil.copy2(SERVICES, backup)
print(f"💾 نسخة احتياطية: {backup.name}")

# 📄 الكود الجديد
NEW_CODE = '''

# ==========================================
# 26. المسلسل الرقمي الموحّد للحركات (جديد)
# ==========================================
# البادئات المستخدمة:
#   CM  = حركة خزينة (Cash Movement)
#   PAY = دفعة (Payment)
#   TRF = تحويل بين الخزائن (Transfer)
#   EXP = مصروف (Expense)
#   INV = فاتورة (Invoice) — موجود مسبقاً
#   LOAN = قرض — موجود مسبقاً
#
# الفكرة: نستخدم `id` الموجود في قاعدة البيانات كأساس للمسلسل.
# لا نحتاج أي تعديل على قاعدة البيانات، ولا أعمدة جديدة.
# ==========================================


def movement_serial(prefix, id_value, digits=6):
    """يبني مسلسلاً مقروءًا من id رقمي موجود في قاعدة البيانات."""
    if id_value is None:
        return f"{prefix}-?"
    try:
        return f"{prefix}-{int(id_value):0{digits}d}"
    except (TypeError, ValueError):
        return f"{prefix}-?"


def parse_movement_serial(serial):
    """يستخرج id الرقمي من مسلسل نصي."""
    if not serial or "-" not in str(serial):
        return None
    try:
        parts = str(serial).strip().split("-", 1)
        if len(parts) != 2:
            return None
        prefix = parts[0].strip().upper()
        id_part = parts[1].strip()
        if not id_part.isdigit():
            return None
        return {"prefix": prefix, "id": int(id_part)}
    except Exception:
        return None


def movement_serial_with_year(prefix, id_value, year=None, digits=6):
    """نسخة اختيارية: مسلسل يشمل السنة."""
    if year is None:
        from datetime import datetime as _dt
        year = _dt.now().year
    return movement_serial(f"{prefix}-{year}", id_value, digits)
'''

# ✍️ إضافة الكود
new_content = content.rstrip() + NEW_CODE + "\n"
SERVICES.write_text(new_content, encoding="utf-8")

# ✅ تأكيد
verify = SERVICES.read_text(encoding="utf-8")
if "def movement_serial(" in verify and "def parse_movement_serial(" in verify:
    print("✅ تمت إضافة الدوال بنجاح إلى services.py")
    print(f"📏 الحجم الجديد: {len(verify):,} حرف")
    print()
    print("الدوال المضافة:")
    print("  • movement_serial(prefix, id_value, digits=6)")
    print("  • parse_movement_serial(serial)")
    print("  • movement_serial_with_year(prefix, id_value, year=None, digits=6)")
else:
    print("⚠️  تحقق يدوياً من services.py")