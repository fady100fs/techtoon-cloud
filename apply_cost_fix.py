# apply_cost_fix.py
# يفرض تحديث get_monthly_profitability في services.py بالقوة
import shutil
import re
from pathlib import Path

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_force_fix")

shutil.copy(SERVICES, BACKUP)
print(f"✅ نسخة احتياطية: {BACKUP.name}")

content = SERVICES.read_text(encoding="utf-8")

# نتحقق: هل الدالة موجودة أصلاً؟
if "def get_monthly_profitability" not in content:
    print("❌ دالة get_monthly_profitability مش موجودة!")
    exit(1)

# نطبع رقم السطر اللي فيه الدالة
lines_arr = content.split("\n")
for i, line in enumerate(lines_arr):
    if "def get_monthly_profitability" in line:
        print(f"📍 الدالة بتبدأ في السطر: {i + 1}")
        # نطبع أول 20 سطر منها عشان نتأكد شكلها
        print("\n--- أول 20 سطر من الدالة الحالية ---")
        for j in range(i, min(i + 20, len(lines_arr))):
            print(f"  {j+1}: {lines_arr[j]}")
        break

print("\n" + "=" * 60)
print("🚀 جاري استبدال الدالة...")
print("=" * 60)

NEW_FUNC = '''def get_monthly_profitability(year=None):
    """الربحية الشهرية — تستخدم تكلفة الأصناف الحالية (fallback)."""
    db = SessionLocal()
    try:
        if year is None:
            year = datetime.now().year

        # الإيرادات
        month_expr_inv = func.to_char(models.Invoice.date, literal_column("'MM'"))
        rev_sub = db.query(
            month_expr_inv.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            func.to_char(models.Invoice.date, literal_column("'YYYY'")) == str(year),
        ).group_by(month_expr_inv).subquery()

        # التكلفة
        month_expr_line = func.to_char(models.Invoice.date, literal_column("'MM'"))
        cost_sub = db.query(
            month_expr_line.label('month'),
            func.sum(
                models.InvoiceLine.quantity *
                func.coalesce(
                    func.nullif(models.InvoiceLine.cost_price, 0),
                    models.Item.cost_price,
                    0
                )
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

# استبدال قوي
pattern = rf"def get_monthly_profitability\([^)]*\):.*?(?=\ndef |\Z)"
new_content, count = re.subn(pattern, NEW_FUNC + "\n\n", content, count=1, flags=re.DOTALL)

if count == 0:
    print("❌ فشل الاستبدال — الدالة مش موجودة أو النمط مختلف")
else:
    SERVICES.write_text(new_content, encoding="utf-8")
    print(f"✅ تم استبدال الدالة بنجاح")

# تأكيد
content2 = SERVICES.read_text(encoding="utf-8")
if "coalesce" in content2.lower() and "item.cost_price" in content2:
    print("✅ الدالة الجديدة مكتوبة في الملف بشكل صحيح")
else:
    print("⚠️ الملف مكتوب بس ما لقيناش العلامات — تحقق يدويًا")

print("\n" + "=" * 60)
print("🎉 انتهى!")
print("=" * 60)