# -*- coding: utf-8 -*-
"""
================================================================
  Techtoon Accounting — سكربت تشخيص وإصلاح مخزون أصناف الدمج
  diagnose_kit_sales.py
================================================================

المشكلة:
  أنت بِعت أصنافاً مدمجة (kit) في فواتير سابقة، لكن مخزون
  المكوّنات (الورق مثلاً) لم يَنقص. السبب أن دالة create_invoice
  كانت تتخطّى أصناف الدمج ولا تُنشئ InventoryMovement للمكوّنات.

  بعد تطبيق apply_techtoon_fix.py (الإصلاح 2)، الفواتير الجديدة
  ستُنشئ حركات مخزون للمكوّنات تلقائياً. لكن الفواتير القديمة
  تحتاج إلى هذا السكربت لإنشاء الحركات المفقودة بأثر رجعي.

الوظائف:
  1) تشخيص: يعرض كل أصناف الدمج ومكوّناتها، وكل فواتير البيع
     التي تحتوي على أصناف دمج، ويُحدد أيها ينقصه InventoryMovement
     للمكوّنات.
  2) إصلاح (optional بـ --apply): يُنشئ InventoryMovement المفقود
     للمكوّنات بأثر رجعي — Idempotent (آمن تشغيله أكثر من مرة).

الاستخدام:
    # تشخيص فقط (لا يُعدّل شيئاً):
    python diagnose_kit_sales.py

    # تشخيص + تطبيق الإصلاح:
    python diagnose_kit_sales.py --apply

    # تحديد مجلد المشروع يدوياً:
    python diagnose_kit_sales.py --project "D:\\PROG\\Accounting Prog\\Techtoon Accounting"

ملاحظة عن المخزون في صفحة إدارة المخزون (23):
  صفحة "الأصناف" (2) تستخدم InventoryMovement (تنعكس فيها المبيعات).
  صفحة "إدارة المخزون" (23) تستخدم StockLevel (نظام منفصل لا تتأثر
  فيه المبيعات تلقائياً). بعد --apply، افتح صفحة الأصناف (2) لرؤية
  المخزون المُحدَّث. لتحديث StockLevel، استخدم زر "حفظ الجرد" داخل
  صفحة إدارة المخزون (23).
================================================================
"""

import argparse
import os
import sys
from datetime import datetime


# ====================================================================
#  الذهاب إلى مجلد المشروع قبل أي import
# ====================================================================

def _bootstrap(project_dir):
    """أضف مجلد المشروع إلى sys.path ثم استورد db/models."""
    project_dir = os.path.abspath(project_dir)
    if not os.path.isdir(project_dir):
        print(f"❌ مجلد المشروع غير موجود: {project_dir}")
        sys.exit(1)
    # تأكد أن cwd = project_dir حتى يقرأ dotenv ملف .env الصحيح
    os.chdir(project_dir)
    if project_dir not in sys.path:
        sys.path.insert(0, project_dir)
    # التحقق من وجود database.py
    if not os.path.isfile(os.path.join(project_dir, "database.py")):
        print(f"❌ database.py غير موجود في: {project_dir}")
        print("   هذا السكربت يجب أن يُشغّل من جذر مشروع Techtoon.")
        sys.exit(1)


# ====================================================================
#  المنطق الأساسي
# ====================================================================

def diagnose_and_fix(apply_changes=False):
    """
    1) تشخيص كل أصناف الدمج ومكوّناتها
    2) تشخيص كل فواتير البيع التي تحتوي أصناف دمج
    3) (optional) إنشاء InventoryMovement المفقود للمكوّنات
    4) تقرير تناقضات بين InventoryMovement و StockLevel
    """
    import models
    from database import SessionLocal
    from sqlalchemy import func

    db = SessionLocal()
    try:
        # ---------- (1) قائمة كل أصناف الدمج ومكوّناتها ----------
        print("\n" + "=" * 64)
        print("  (1) أصناف الدمج (Kit Items) ومكوّناتها")
        print("=" * 64)

        kit_items = db.query(models.Item).filter(
            models.Item.is_kit == True  # noqa: E712
        ).order_by(models.Item.id).all()

        if not kit_items:
            print("  ℹ️  لا توجد أصناف دمج (is_kit=True) في قاعدة البيانات.")
            print("      إذا كنت تظن أن لديك أصناف دمج، فتحقق من علامة is_kit في صفحة الأصناف.")
            return

        print(f"  عدد أصناف الدمج: {len(kit_items)}\n")

        # خريطة kit_id -> components
        kit_components_map = {}
        for kit in kit_items:
            comps = db.query(models.KitComponent).filter(
                models.KitComponent.kit_item_id == kit.id
            ).all()
            kit_components_map[kit.id] = comps
            comp_lines = []
            for c in comps:
                comp_item = db.query(models.Item).filter(
                    models.Item.id == c.component_item_id
                ).first()
                comp_name = comp_item.name if comp_item else f"<محذوف #{c.component_item_id}>"
                comp_lines.append(f"    • {comp_name} × {float(c.quantity or 0):.4g}")
            print(f"  • #{kit.id}  {kit.name}")
            if comp_lines:
                for ln in comp_lines:
                    print(ln)
            else:
                print("    ⚠️  لا توجد مكوّنات مسجّلة لهذا الصنف المدمج!")
        print()

        # ---------- (2) فواتير البيع التي تحتوي على أصناف دمج ----------
        print("=" * 64)
        print("  (2) فواتير البيع التي تحتوي على أصناف دمج")
        print("=" * 64)

        kit_ids = list(kit_components_map.keys())
        # ابحث عن InvoiceLine فيها item_id ينتمي لـ kit_ids، ونوع الفاتورة sale
        sale_lines = (
            db.query(models.InvoiceLine, models.Invoice)
            .join(models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id)
            .filter(
                models.InvoiceLine.item_id.in_(kit_ids),
                models.Invoice.type == "sale",
            )
            .order_by(models.Invoice.id)
            .all()
        )

        if not sale_lines:
            print("  ℹ️  لا توجد فواتير بيع تحتوي على أصناف دمج.")
            return

        print(f"  عدد سطور أصناف الدمج في فواتير البيع: {len(sale_lines)}\n")

        # ---------- (3) تحديد الحركات المفقودة ----------
        # لكل سطر فاتورة (kit sale): تحقق هل توجد InventoryMovement
        # لكل مكوّن بنفس reference_id == invoice.id ونوع 'out' و item_id == comp.component_item_id
        missing_count = 0
        existing_count = 0
        missing_details = []  # list of (invoice_id, kit_item_id, kit_qty, comp)

        for line, invoice in sale_lines:
            kit_id = line.item_id
            kit_qty = float(line.quantity or 0)
            comps = kit_components_map.get(kit_id, [])
            for comp in comps:
                comp_item_id = comp.component_item_id
                expected_qty = float(comp.quantity or 0) * kit_qty
                # ابحث عن حركة 'out' موجودة بنفس المرجع ونفس المكوّن
                existing = db.query(models.InventoryMovement).filter(
                    models.InventoryMovement.reference_id == invoice.id,
                    models.InventoryMovement.item_id == comp_item_id,
                    models.InventoryMovement.type == "out",
                ).first()

                if existing is None:
                    missing_count += 1
                    missing_details.append({
                        "invoice_id": invoice.id,
                        "invoice_number": invoice.invoice_number,
                        "invoice_date": invoice.date,
                        "kit_item_id": kit_id,
                        "kit_item_name": next(
                            (k.name for k in kit_items if k.id == kit_id), f"#{kit_id}"),
                        "kit_qty": kit_qty,
                        "comp_item_id": comp_item_id,
                        "comp_quantity_per_kit": float(comp.quantity or 0),
                        "expected_movement_qty": expected_qty,
                    })
                else:
                    existing_count += 1

        print(f"  حركات مخزون موجودة بالفعل للمكوّنات: {existing_count}")
        print(f"  حركات مخزون مفقودة (تحتاج إنشاء):    {missing_count}\n")

        if missing_count == 0:
            print("  ✅ كل حركات المخزون للمكوّنات موجودة. لا حاجة لإصلاح.")
        else:
            print("  تفاصيل الحركات المفقودة (أول 20):")
            for i, m in enumerate(missing_details[:20], start=1):
                comp_item = db.query(models.Item).filter(
                    models.Item.id == m["comp_item_id"]
                ).first()
                comp_name = comp_item.name if comp_item else f"<محذوف #{m['comp_item_id']}>"
                print(
                    f"  {i:>3}. فاتورة #{m['invoice_id']} ({m['invoice_number']}) "
                    f"| {m['kit_item_name']} × {m['kit_qty']:g} "
                    f"⇒ ينقص {comp_name} بمقدار {m['expected_movement_qty']:g}"
                )
            if missing_count > 20:
                print(f"  ... و {missing_count - 20} سجل آخر.")
            print()

        # ---------- (4) تطبيق الإصلاح (إن طُلب) ----------
        if not apply_changes:
            print("=" * 64)
            print("  وضع التشخيص فقط — لم يتم تعديل أي شيء.")
            print("  لتطبيق الإصلاح فعلاً، أعد التشغيل بـ: --apply")
            print("=" * 64)
            return

        if missing_count == 0:
            print("  لا توجد حركات مفقودة لإنشائها. ✅")
            return

        print("=" * 64)
        print("  (3) تطبيق الإصلاح — إنشاء InventoryMovement المفقود")
        print("=" * 64)

        created = 0
        skipped = 0
        for m in missing_details:
            # فحص إضافي للأمان (Idempotency)
            already = db.query(models.InventoryMovement).filter(
                models.InventoryMovement.reference_id == m["invoice_id"],
                models.InventoryMovement.item_id == m["comp_item_id"],
                models.InventoryMovement.type == "out",
            ).first()
            if already is not None:
                skipped += 1
                continue

            # تأكد أن المكوّن ما زال موجوداً في قاعدة البيانات
            comp_item = db.query(models.Item).filter(
                models.Item.id == m["comp_item_id"]
            ).first()
            if comp_item is None:
                print(f"  ⚠️  تخطّي: المكوّن #{m['comp_item_id']} محذوف. (فاتورة #{m['invoice_id']})")
                skipped += 1
                continue

            # التاريخ: نستخدم تاريخ الفاتورة (لو معروف) وإلا الآن
            mv_date = m["invoice_date"] or datetime.now()

            db.add(models.InventoryMovement(
                item_id=m["comp_item_id"],
                date=mv_date,
                type="out",
                quantity=m["expected_movement_qty"],
                reference_id=m["invoice_id"],
            ))
            created += 1

        if created > 0:
            try:
                db.commit()
                print(f"  ✅ تم إنشاء {created} حركة مخزون 'out' للمكوّنات.")
            except Exception as e:
                db.rollback()
                print(f"  ❌ فشل commit: {e}")
                return
        if skipped > 0:
            print(f"  ℹ️  تم تخطّي {skipped} سجل (موجود مسبقاً أو مكوّن محذوف).")

        # ---------- (5) تقرير تناقضات InventoryMovement vs StockLevel ----------
        print("\n" + "=" * 64)
        print("  (4) تقرير تناقضات بين InventoryMovement و StockLevel")
        print("=" * 64)
        print("  ⚠️  ملاحظة معمارية:")
        print("       صفحة الأصناف (2) تقرأ من InventoryMovement.")
        print("       صفحة إدارة المخزون (23) تقرأ من StockLevel (نظام منفصل).")
        print("       المبيعات تُنشئ InventoryMovement فقط؛ لا تُحدّث StockLevel.")
        print("       لذلك قد تختلف قيمتاهما. هذا تقرير بالتناقضات لأي صنف:\n")

        try:
            from sqlalchemy import case
            mv_rows = db.query(
                models.InventoryMovement.item_id,
                func.sum(case(
                    (models.InventoryMovement.type == "in",
                     models.InventoryMovement.quantity),
                    else_=-models.InventoryMovement.quantity)
                ).label("balance"),
            ).group_by(models.InventoryMovement.item_id).all()
            mv_balance = {iid: float(b or 0) for iid, b in mv_rows}

            stock_levels = db.query(
                models.StockLevel.item_id,
                func.sum(models.StockLevel.quantity),
            ).group_by(models.StockLevel.item_id).all()
            sl_balance = {iid: float(q or 0) for iid, q in stock_levels}

            all_item_ids = set(mv_balance.keys()) | set(sl_balance.keys())
            discrepancies = []
            for iid in all_item_ids:
                mv = mv_balance.get(iid, 0.0)
                sl = sl_balance.get(iid, 0.0)
                if abs(mv - sl) > 0.01:
                    discrepancies.append((iid, mv, sl))

            if not discrepancies:
                print("  ✅ لا توجد تناقضات بين النظامين.")
            else:
                print(f"  عدد الأصناف ذات التناقض: {len(discrepancies)} (أول 15):")
                print(f"  {'#':>4}  {'الصنف':<30}  {'Inventory':>12}  {'StockLevel':>12}")
                for i, (iid, mv, sl) in enumerate(discrepancies[:15], start=1):
                    item = db.query(models.Item).filter(models.Item.id == iid).first()
                    nm = (item.name[:28] if item else f"<#{iid}>")
                    print(f"  {i:>4}  {nm:<30}  {mv:>12.4g}  {sl:>12.4g}")
                if len(discrepancies) > 15:
                    print(f"  ... و {len(discrepancies) - 15} سجل آخر.")

                print("\n  💡 لمعالجة التناقض في صفحة المخزون (23):")
                print("       استخدم زر 'حفظ الجرد' داخل تبويب 'جرد مخزن' بعد إدخال الكمية الفعلية.")
        except Exception as e:
            print(f"  ⚠️  تعذّر حساب التناقضات: {e}")

    finally:
        db.close()


# ====================================================================
#  main
# ====================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Techtoon — تشخيص وإصلاح مخزون أصناف الدمج (Kit Items)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--project", default=None,
        help="مسار جذر مشروع Techtoon (افتراضياً المجلد الحالي)",
    )
    parser.add_argument(
        "--apply", action="store_true",
        help="طبّق الإصلاح فعلاً (أنشئ InventoryMovement المفقود). "
             "بدونه: وضع تشخيص فقط.",
    )
    args = parser.parse_args()

    project_dir = args.project or os.getcwd()
    print("=" * 64)
    print("  Techtoon — تشخيص وإصلاح مخزون أصناف الدمج")
    print("=" * 64)
    print(f"  مجلد المشروع: {os.path.abspath(project_dir)}")
    print(f"  وضع:          {'تطبيق' if args.apply else 'تشخيص فقط'}")

    _bootstrap(project_dir)

    try:
        diagnose_and_fix(apply_changes=args.apply)
    except Exception as e:
        print(f"\n❌ خطأ غير متوقع: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(2)


if __name__ == "__main__":
    main()
