# fix_profitability_v2.py
# يستبدل دالة get_monthly_profitability بنسخة صحيحة (بدون Subquery iteration)

import re
import shutil
from pathlib import Path

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_v2_fix")

if not SERVICES.exists():
    print("❌ services.py غير موجود")
    exit(1)

shutil.copy(SERVICES, BACKUP)
print(f"✅ نسخة احتياطية: {BACKUP.name}")

content = SERVICES.read_text(encoding="utf-8")

# ============================================================
# الدالة الجديدة الصحيحة
# ============================================================
NEW_FUNC = '''def get_monthly_profitability(year=None):
    """الربحية الشهرية — تحسب تكلفة الأصناف المدمجة (Kits) من مكوناتها."""
    db = SessionLocal()
    try:
        if year is None:
            year = datetime.now().year

        # ✅ الإيرادات: نستعلم مباشرة (بدون subquery iteration)
        month_expr_inv = func.to_char(models.Invoice.date, literal_column("'MM'"))
        year_expr_inv = func.to_char(models.Invoice.date, literal_column("'YYYY'"))

        revenue_rows = db.query(
            month_expr_inv.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr_inv == str(year),
        ).group_by(month_expr_inv).all()

        revenue_by_month = {
            r.month: float(r.revenue or 0)
            for r in revenue_rows
        }

        # ✅ التكلفة: نحسبها برمجيًا لكل سطر
        month_expr_line = func.to_char(models.Invoice.date, literal_column("'MM'"))
        year_expr_line = func.to_char(models.Invoice.date, literal_column("'YYYY'"))

        invoice_lines = db.query(
            month_expr_line.label('month'),
            models.InvoiceLine.quantity,
            models.InvoiceLine.cost_price,
            models.Item.id.label('item_id'),
            models.Item.is_kit,
            models.Item.cost_price.label('item_cost'),
            models.Item.avg_cost_price,
        ).join(
            models.Item, models.InvoiceLine.item_id == models.Item.id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr_line == str(year),
        ).all()

        # تجميع التكلفة لكل شهر
        cost_by_month = {}
        for row in invoice_lines:
            month = row.month
            qty = float(row.quantity or 0)

            if row.is_kit:
                # صنف مدمج: نحسب من المكونات
                unit_cost = 0.0
                components = db.query(models.KitComponent).filter(
                    models.KitComponent.kit_item_id == row.item_id
                ).all()
                for comp in components:
                    comp_item = db.query(models.Item).filter(
                        models.Item.id == comp.component_item_id
                    ).first()
                    if comp_item:
                        unit_cost += float(comp.quantity or 0) * float(comp_item.cost_price or 0)
            else:
                # صنف عادي
                unit_cost = float(row.cost_price or 0)
                if unit_cost == 0:
                    unit_cost = float(row.item_cost or 0)
                if unit_cost == 0:
                    unit_cost = float(row.avg_cost_price or 0)

            cost_by_month[month] = cost_by_month.get(month, 0.0) + (qty * unit_cost)

        # دمج الأشهر
        all_months = sorted(set(list(revenue_by_month.keys()) + list(cost_by_month.keys())))
        monthly_data = []
        for m in all_months:
            revenue = revenue_by_month.get(m, 0.0)
            cost = cost_by_month.get(m, 0.0)
            profit = revenue - cost
            profit_margin = (profit / revenue * 100) if revenue > 0 else 0
            monthly_data.append({
                'month': m,
                'month_name': datetime.strptime(m, '%m').strftime('%B'),
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
    print("❌ لم نجد get_monthly_profitability")
else:
    SERVICES.write_text(new_content, encoding="utf-8")
    print("✅ تم استبدال الدالة بنجاح")

# تأكيد
content2 = SERVICES.read_text(encoding="utf-8")
if "for r in revenue_rows" in content2:
    print("✅ الدالة الجديدة مكتوبة في الملف بشكل صحيح")
else:
    print("⚠️ تحقق يدويًا من الملف")

print()
print("=" * 60)
print("🎉 انتهى!")
print("=" * 60)
print()
print("الخطوة التالية:")
print("  1) اقفل Streamlit (Ctrl+C)")
print("  2) شغّل: streamlit run app.py")
print("  3) افتح 'تحليل الربحية' → 'الربحية الشهرية'")