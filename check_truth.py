# check_truth.py
# تشخيص صادق: يقرأ البيانات الفعلية من قاعدة البيانات
from database import SessionLocal
import models

db = SessionLocal()
try:
    print("=" * 70)
    print("🔍 الأصناف في قاعدة البيانات:")
    print("=" * 70)
    items = db.query(models.Item).filter(models.Item.is_kit == False).all()
    for it in items:
        print(f"  [{it.id}] {it.name}")
        print(f"      cost_price = {it.cost_price}  |  sell_price = {it.sell_price}")

    print()
    print("=" * 70)
    print("🔍 سطور الفواتير في قاعدة البيانات:")
    print("=" * 70)
    lines = db.query(models.InvoiceLine).all()
    for ln in lines:
        item = db.query(models.Item).filter(models.Item.id == ln.item_id).first()
        item_cost = item.cost_price if item else "N/A"
        line_cost = ln.cost_price
        match = "✅" if (line_cost or 0) == (item_cost or 0) else "❌"
        print(f"  {match} فاتورة#{ln.invoice_id} | item_id={ln.item_id}")
        print(f"      quantity={ln.quantity} | price={ln.price}")
        print(f"      line.cost_price = {line_cost}  |  item.cost_price = {item_cost}")

    print()
    print("=" * 70)
    print("🔍 هل دالة get_monthly_profitability اتحدثت في services.py؟")
    print("=" * 70)
    with open("services.py", "r", encoding="utf-8") as f:
        content = f.read()
    if "coalesce" in content.lower() and "item.cost_price" in content:
        print("  ✅ الدالة اتحدثت (فيها coalesce + item.cost_price)")
    elif "to_char" in content:
        print("  ⚠️ الدالة اتحدثت جزئيًا بس مش فيها coalesce")
    else:
        print("  ❌ الدالة لسه القديمة")
finally:
    db.close()