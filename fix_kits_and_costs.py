# fix_kits_and_costs.py
# حل شامل لمشكلة تكلفة الأصناف المدمجة (Kits) في الفواتير والربحية

import re
import shutil
from pathlib import Path
from database import SessionLocal
import models
from sqlalchemy import func

# ==========================================================
# دالة مساعدة: حساب تكلفة صنف مدمج من مكوناته
# ==========================================================
def calculate_kit_cost(db, kit_item_id):
    """يحسب تكلفة الـ Kit = مجموع (كمية المكون × تكلفة المكون)"""
    total = 0.0
    components = db.query(models.KitComponent).filter(
        models.KitComponent.kit_item_id == kit_item_id
    ).all()
    for comp in components:
        comp_item = db.query(models.Item).filter(
            models.Item.id == comp.component_item_id
        ).first()
        if comp_item:
            comp_cost = float(comp_item.cost_price or 0)
            comp_qty = float(comp.quantity or 0)
            total += comp_qty * comp_cost
    return round(total, 4)

# ==========================================================
# المرحلة 1: تشخيص - عرض الأصناف المدمجة وتكاليفها
# ==========================================================
print("=" * 80)
print("🔍 المرحلة 1: كل الأصناف المدمجة (Kits) وتكلفتها الفعلية")
print("=" * 80)

db = SessionLocal()
fixed_kits = 0
try:
    kits = db.query(models.Item).filter(models.Item.is_kit == True).all()
    print(f"\n📦 عدد الأصناف المدمجة: {len(kits)}\n")

    for kit in kits:
        current_cost = float(kit.cost_price or 0)
        real_cost = calculate_kit_cost(db, kit.id)
        flag = "⚠️" if current_cost != real_cost else "✅"
        print(f"  {flag} [{kit.id}] {kit.name[:40]:40}")
        print(f"      cost_price الحالي: {current_cost:.4f}")
        print(f"      التكلفة الحقيقية:  {real_cost:.4f}")

        # نعرض المكونات
        components = db.query(models.KitComponent).filter(
            models.KitComponent.kit_item_id == kit.id
        ).all()
        for comp in components:
            comp_item = db.query(models.Item).filter(
                models.Item.id == comp.component_item_id
            ).first()
            if comp_item:
                print(f"         ↳ {comp_item.name[:30]:30} | "
                      f"qty={float(comp.quantity or 0):.2f} × "
                      f"cost={float(comp_item.cost_price or 0):.4f}")

        # ✅ إصلاح cost_price للـ Kit
        if current_cost != real_cost:
            kit.cost_price = real_cost
            if hasattr(kit, 'avg_cost_price'):
                kit.avg_cost_price = real_cost
            fixed_kits += 1
            print(f"      ✅ تم تحديث cost_price: {current_cost:.4f} → {real_cost:.4f}")
        print()

    if fixed_kits:
        db.commit()
        print(f"✅ تم تصحيح {fixed_kits} صنف مدمج")
finally:
    db.close()

# ==========================================================
# المرحلة 2: تصحيح InvoiceLine.cost_price
# ==========================================================
print()
print("=" * 80)
print("🔧 المرحلة 2: تصحيح تكلفة سطور الفواتير")
print("=" * 80)

db = SessionLocal()
fixed_lines = 0
try:
    lines = db.query(models.InvoiceLine).all()
    for ln in lines:
        item = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        if not item:
            continue

        # التكلفة الصحيحة
        if item.is_kit:
            correct_cost = calculate_kit_cost(db, item.id)
        else:
            correct_cost = float(item.cost_price or 0)
            # لو صفر نستخدم avg
            if correct_cost == 0:
                correct_cost = float(getattr(item, 'avg_cost_price', 0) or 0)

        current_cost = float(ln.cost_price or 0)

        if current_cost != correct_cost and correct_cost > 0:
            ln.cost_price = correct_cost
            fixed_lines += 1
            print(f"  ✅ فاتورة#{ln.invoice_id} | {item.name[:30]:30} | "
                  f"{current_cost:.4f} → {correct_cost:.4f}")

    if fixed_lines:
        db.commit()
        print(f"\n✅ تم تصحيح {fixed_lines} سطر")
    else:
        print("ℹ️ مفيش سطور محتاجة تصحيح")
finally:
    db.close()

# ==========================================================
# المرحلة 3: تحديث services.py لضمان الحفظ مستقبلاً
# ==========================================================
print()
print("=" * 80)
print("🔧 المرحلة 3: تحديث services.py")
print("=" * 80)

SERVICES = Path(__file__).parent / "services.py"
BACKUP = SERVICES.with_suffix(".py.before_kits_fix")

if SERVICES.exists():
    shutil.copy(SERVICES, BACKUP)
    print(f"✅ نسخة احتياطية: {BACKUP.name}")

    content = SERVICES.read_text(encoding="utf-8")

    # =========================================================
    # 3.1) إضافة دالة مساعدة لحساب تكلفة الـ Kit
    # =========================================================
    if "def _calc_item_cost_for_invoice" not in content:
        helper_func = '''
def _calc_item_cost_for_invoice(db, item):
    """يحسب التكلفة الصحيحة لصنف عند إضافته للفاتورة.
    - للصنف العادي: cost_price أو avg_cost_price
    - للصنف المدمج: مجموع (كمية المكون × تكلفة المكون)
    """
    if not item:
        return 0.0
    if getattr(item, "is_kit", False):
        total = 0.0
        components = db.query(models.KitComponent).filter(
            models.KitComponent.kit_item_id == item.id
        ).all()
        for comp in components:
            comp_item = db.query(models.Item).filter(
                models.Item.id == comp.component_item_id
            ).first()
            if comp_item:
                total += float(comp.quantity or 0) * float(comp_item.cost_price or 0)
        return round(total, 4)
    # صنف عادي
    cost = float(item.cost_price or 0)
    if cost == 0:
        cost = float(getattr(item, "avg_cost_price", 0) or 0)
    return cost


'''
        # نحط الدالة قبل create_invoice
        marker = "def create_invoice("
        if marker in content:
            content = content.replace(marker, helper_func + marker, 1)
            print("✅ أضفنا دالة _calc_item_cost_for_invoice")
        else:
            print("⚠️ مش لاقيين create_invoice — هنستمر في المحاولة")

    # =========================================================
    # 3.2) استبدال السطر اللي بيحفظ cost_price في InvoiceLine
    # =========================================================
    old_line = "total=line_total, cost_price=item.cost_price"
    new_line = "total=line_total, cost_price=_calc_item_cost_for_invoice(db, item)"
    if old_line in content:
        content = content.replace(old_line, new_line)
        print("✅ عدّلنا حفظ cost_price في InvoiceLine")
    else:
        # بدائل محتملة
        old_line2 = 'cost_price=item.cost_price'
        if old_line2 in content:
            content = content.replace(old_line2, 'cost_price=_calc_item_cost_for_invoice(db, item)')
            print("✅ عدّلنا حفظ cost_price (صيغة بديلة)")
        else:
            print("⚠️ مش لاقيين سطر حفظ cost_price — تحقق يدويًا")

    # =========================================================
    # 3.3) تحديث get_monthly_profitability لتحسب تكلفة الـ Kits
    # =========================================================
    NEW_FUNC = '''def get_monthly_profitability(year=None):
    """الربحية الشهرية — تحسب تكلفة الأصناف المدمجة من مكوناتها."""
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

        # التكلفة - نحسبها برمجيًا لكل سطر ونرمزها
        # (لأن تكلفة الـ Kits محتاجة حساب من المكونات)
        month_expr_line = func.to_char(models.Invoice.date, literal_column("'MM'"))
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
            func.to_char(models.Invoice.date, literal_column("'YYYY'")) == str(year),
        ).all()

        # تجميع التكلفة لكل شهر
        cost_by_month = {}
        for row in invoice_lines:
            month = row.month
            qty = float(row.quantity or 0)

            # تحديد التكلفة الوحدة
            if row.is_kit:
                # للـ Kit: نحسب من المكونات
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
                # صنف عادي: cost_price أو avg
                unit_cost = float(row.cost_price or 0)
                if unit_cost == 0:
                    unit_cost = float(row.item_cost or 0)
                if unit_cost == 0:
                    unit_cost = float(row.avg_cost_price or 0)

            cost_by_month[month] = cost_by_month.get(month, 0.0) + (qty * unit_cost)

        # نتائج الإيرادات
        rev_result = {r.month: float(r.revenue or 0) for r in rev_sub}

        # ندمج الأشهر
        all_months = sorted(set(list(rev_result.keys()) + list(cost_by_month.keys())))
        monthly_data = []
        for m in all_months:
            revenue = rev_result.get(m, 0.0)
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

    pattern = rf"def get_monthly_profitability\([^)]*\):.*?(?=\ndef |\Z)"
    new_content, count = re.subn(pattern, NEW_FUNC + "\n\n", content, count=1, flags=re.DOTALL)
    if count:
        content = new_content
        print("✅ حدّثنا get_monthly_profitability")
    else:
        print("⚠️ لم نحدّث get_monthly_profitability")

    SERVICES.write_text(content, encoding="utf-8")
    print("✅ تم حفظ services.py")

print()
print("=" * 80)
print("🎉 انتهى!")
print("=" * 80)
print()
print("الخطوة التالية:")
print("  1) اقفل Streamlit (Ctrl+C)")
print("  2) شغّل: streamlit run app.py")
print("  3) افتح 'تحليل الربحية' → 'الربحية الشهرية'")