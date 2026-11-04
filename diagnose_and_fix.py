# diagnose_and_fix.py
# يشخّص سطور الفواتير ويصلح التكلفة من بيانات الأصناف الحالية
# + يصلح دالة get_monthly_profitability في services.py

import re
import shutil
from pathlib import Path

from database import SessionLocal
import models
from sqlalchemy import func

# ==========================================
# المرحلة 1: التشخيص
# ==========================================
print("=" * 60)
print("🔍 المرحلة 1: تشخيص بيانات الفواتير")
print("=" * 60)

db = SessionLocal()
try:
    lines = db.query(models.InvoiceLine).all()
    print(f"\n📋 إجمالي سطور الفواتير: {len(lines)}")

    zero_cost_count = 0
    for ln in lines[:20]:  # نطبع أول 20 بس
        item = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        item_cost = float(item.cost_price or 0) if item else 0
        line_cost = float(ln.cost_price or 0)
        flag = "⚠️" if line_cost == 0 and item_cost > 0 else "  "
        print(f"  {flag} فاتورة#{ln.invoice_id} | صنف: {item.name if item else '?'} | "
              f"الكمية: {ln.quantity} | سعر البيع: {ln.price} | "
              f"تكلفة السطر: {line_cost} | تكلفة الصنف الحالية: {item_cost}")

    zero_cost_count = sum(
        1 for ln in lines
        if float(ln.cost_price or 0) == 0 and ln.item_id
    )
    print(f"\n⚠️  سطور بتكلفة صفر: {zero_cost_count} من {len(lines)}")
finally:
    db.close()

# ==========================================
# المرحلة 2: إصلاح البيانات التاريخية
# ==========================================
print()
print("=" * 60)
print("🔧 المرحلة 2: تصحيح تكلفة سطور الفواتير من الأصناف")
print("=" * 60)

db = SessionLocal()
try:
    lines = db.query(models.InvoiceLine).all()
    fixed = 0
    for ln in lines:
        item = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        if not item:
            continue
        current_cost = float(item.cost_price or 0)
        line_cost = float(ln.cost_price or 0)
        # لو السطر صفر والصنف له تكلفة → نحدّث
        if line_cost == 0 and current_cost > 0:
            ln.cost_price = current_cost
            fixed += 1
    db.commit()
    print(f"✅ تم تصحيح {fixed} سطر")
finally:
    db.close()

# ==========================================
# المرحلة 3: إصلاح دالة get_monthly_profitability في services.py
# ==========================================
print()
print("=" * 60)
print("🔧 المرحلة 3: تحديث دالة get_monthly_profitability")
print("=" * 60)

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_cost_fix")

if not SERVICES.exists():
    print("❌ services.py غير موجود")
    exit(1)

shutil.copy(SERVICES, BACKUP)
print(f"✅ نسخة احتياطية: {BACKUP.name}")

content = SERVICES.read_text(encoding="utf-8")

# دالة جديدة تستخدم Item.cost_price (مصدر موثوق) بدل InvoiceLine.cost_price
NEW_FUNC = '''def get_monthly_profitability(year=None):
    """الربحية الشهرية — نسخة موثوقة تستخدم تكلفة الأصناف الحالية."""
    db = SessionLocal()
    try:
        if year is None:
            year = datetime.now().year

        # ✅ الإيرادات: subquery عشان نتجنب تكرار net_amount بسبب JOIN
        month_expr_inv = func.to_char(models.Invoice.date, literal_column("'MM'"))
        rev_sub = db.query(
            month_expr_inv.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            func.to_char(models.Invoice.date, literal_column("'YYYY'")) == str(year),
        ).group_by(month_expr_inv).subquery()

        # ✅ التكلفة: نجمع من سطور الفواتير + الأصناف (COALESCE لحماية القيم الفارغة)
        month_expr_line = func.to_char(models.Invoice.date, literal_column("'MM'"))
        cost_sub = db.query(
            month_expr_line.label('month'),
            func.sum(
                models.InvoiceLine.quantity *
                func.coalesce(models.InvoiceLine.cost_price, models.Item.cost_price, 0)
            ).label('cost'),
        ).join(
            models.Item, models.InvoiceLine.item_id == models.Item.id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            func.to_char(models.Invoice.date, literal_column("'YYYY'")) == str(year),
        ).group_by(month_expr_line).subquery()

        # نجمع الاتنين
        all_months = db.query(
            func.coalesce(rev_sub.c.month, cost_sub.c.month).label('month'),
            func.coalesce(rev_sub.c.revenue, 0).label('revenue'),
            func.coalesce(cost_sub.c.cost, 0).label('cost'),
        ).select_from(
            rev_sub.outerjoin(cost_sub, rev_sub.c.month == cost_sub.c.month)
        ).all()

        monthly_data = []
        for r in all_months:
            revenue = float(r.revenue or 0)
            cost = float(r.cost or 0)
            profit = revenue - cost
            profit_margin = (profit / revenue * 100) if revenue > 0 else 0
            monthly_data.append({
                'month': r.month,
                'month_name': datetime.strptime(r.month, '%m').strftime('%B'),
                'revenue': revenue,
                'cost': cost,
                'profit': profit,
                'profit_margin': profit_margin,
            })
        return monthly_data
    finally:
        db.close()
'''

# استبدال الدالة
pattern = rf"def get_monthly_profitability\([^)]*\):.*?(?=\ndef |\Z)"
new_content, count = re.subn(pattern, NEW_FUNC + "\n\n", content, count=1, flags=re.DOTALL)

if count == 0:
    print("⚠️ لم نجد get_monthly_profitability — تحقق يدويًا")
else:
    SERVICES.write_text(new_content, encoding="utf-8")
    print("✅ تم تحديث get_monthly_profitability")

print()
print("=" * 60)
print("🎉 تم الانتهاء!")
print(f"💾 نسخة احتياطية: {BACKUP.name}")
print("=" * 60)