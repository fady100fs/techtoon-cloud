#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
check_item_19008.py — تشخيص: لماذا لا يظهر الصنف «ورق تصوير A4» (كود 19008) في قائمة الأصناف؟
═════════════════════════════════════════════════════════════════════════════════════════════
فحص للقراءة فقط — لا يعدل أي ملف ولا أي بيانات.

الخلفية من الكود:
  • فواتير البيع/الشراء وخانة "اختيار صنف للتعديل" تقرأ من قاعدة المصدر مباشرة
    (invoice_page_common.py ← SessionLocal) → لذلك الصنف "متوفر" هناك.
  • قائمة الأصناف (التبويب "قائمة الأصناف / تعديل") تقرأ عبر get_items_with_stock()
    التي كانت تقرأ من المرآة المحلية SQLite (data/local_mirror.db) → إذا كانت
    المرآة قديمة/ناقصة يختفي الصنف من القائمة رغم وجوده في المصدر.
  • حزمة apply_hotfix_stock_reports.py تجعل القائمة تقرأ من المصدر مباشرة.

الاستخدام من داخل مجلد البرنامج:
    python check_item_19008.py
"""
import os
import re
import sys

# ضمان ظهور العربية في PowerShell
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

SEARCH_CODE = "19008"
NAME_HINTS = ["ورق", "A4", "a4"]
BAR = "=" * 70


def p(t=""):
    print(t)


def mask_url(url):
    """إخفاء كلمة المرور من رابط قاعدة البيانات قبل طباعته."""
    try:
        m = re.match(r"^([^:/@]+://[^:/@]+:)([^@]+)(@.*)$", url or "")
        if m:
            return m.group(1) + "****" + m.group(3)
        return url or "?"
    except Exception:
        return "?"


def find_in_source():
    """البحث عن الصنف في قاعدة المصدر (نفس مصدر الفواتير) — عبر ORM."""
    import models
    from sqlalchemy import or_, func, case
    from database import SessionLocal

    db = SessionLocal()
    try:
        total = db.query(func.count(models.Item.id)).scalar()
        conds = [models.Item.barcode == SEARCH_CODE]
        for h in NAME_HINTS:
            conds.append(models.Item.name.ilike(f"%{h}%"))
        try:
            conds.append(models.Item.id == int(SEARCH_CODE))
        except Exception:
            pass
        rows = db.query(models.Item).filter(or_(*conds)).all()

        result = []
        for r in rows:
            balance = None
            movements = 0
            try:
                mv = db.query(models.InventoryMovement).filter(
                    models.InventoryMovement.item_id == r.id
                ).all()
                movements = len(mv)
                balance = sum(
                    float(m.quantity or 0) if (m.type or "") == "in"
                    else -float(m.quantity or 0)
                    for m in mv
                )
            except Exception:
                pass
            result.append({
                "id": r.id,
                "name": r.name,
                "barcode": r.barcode,
                "is_kit": bool(r.is_kit),
                "category_id": r.category_id,
                "cost": float(r.cost_price or 0),
                "sell": float(r.sell_price or 0),
                "movements": movements,
                "stock_balance": balance,
            })
        return total, result, None
    finally:
        db.close()


def find_in_mirror(base_dir):
    """البحث عن الصنف في المرآة المحلية SQLite (مصدر قائمة الأصناف قبل الإصلاح)."""
    import sqlite3
    mirror_path = os.path.join(base_dir, "data", "local_mirror.db")
    if not os.path.exists(mirror_path):
        return None, None, "الملف غير موجود: data/local_mirror.db"

    con = sqlite3.connect(mirror_path)
    con.row_factory = sqlite3.Row
    try:
        cur = con.cursor()
        tables = {row[0] for row in cur.execute(
            "SELECT name FROM sqlite_master WHERE type='table'").fetchall()}
        if "items" not in tables:
            return None, None, "جدول items غير موجود في المرآة"

        total = cur.execute("SELECT COUNT(*) FROM items").fetchone()[0]
        like_conds = " OR ".join(
            [f"barcode = ?", f"id = ?"] +
            [f"name LIKE ?" for _ in NAME_HINTS]
        )
        params = [SEARCH_CODE, int(SEARCH_CODE)] + [f"%{h}%" for h in NAME_HINTS]
        rows = cur.execute(
            f"SELECT id, name, barcode, is_kit, category_id, cost_price, sell_price "
            f"FROM items WHERE {like_conds}", params
        ).fetchall()

        result = []
        for r in rows:
            balance, movements = None, 0
            if "inventory_movements" in tables:
                mv = cur.execute(
                    "SELECT type, quantity FROM inventory_movements WHERE item_id = ?",
                    (r["id"],)
                ).fetchall()
                movements = len(mv)
                balance = sum(
                    float(m["quantity"] or 0) if (m["type"] or "") == "in"
                    else -float(m["quantity"] or 0)
                    for m in mv
                )
            result.append({
                "id": r["id"],
                "name": r["name"],
                "barcode": r["barcode"],
                "movements": movements,
                "stock_balance": balance,
            })
        return total, result, None
    finally:
        con.close()


def file_has(base_dir, filename, needle):
    try:
        with open(os.path.join(base_dir, filename), "r", encoding="utf-8", errors="ignore") as f:
            return needle in f.read()
    except Exception:
        return None


def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    os.chdir(base_dir)

    p(BAR)
    p(f"  تشخيص اختفاء الصنف «ورق تصوير A4» (كود {SEARCH_CODE}) من قائمة الأصناف")
    p(BAR)
    p(f"المجلد: {base_dir}")

    # ─── [1] الإعدادات ───
    p(f"\n[1] الإعدادات")
    deploy_mode = "?"
    try:
        with open(os.path.join(base_dir, ".env"), "r", encoding="utf-8", errors="ignore") as f:
            for line in f:
                line = line.strip()
                if line.startswith("DEPLOY_MODE="):
                    deploy_mode = line.split("=", 1)[1].strip()
    except Exception:
        pass
    effective_url = None
    try:
        from database import DATABASE_URL
        effective_url = DATABASE_URL
    except Exception as e:
        p(f"  ⚠️ تعذر استيراد database.py: {e}")
        p("     (تأكد من تشغيل الفحص من داخل مجلد البرنامج)")
    p(f"  DEPLOY_MODE = {deploy_mode}")
    p(f"  قاعدة المصدر الفعلية = {mask_url(effective_url) if effective_url else 'غير معروفة'}")

    # ─── [2] البحث في قاعدة المصدر ───
    p(f"\n[2] البحث عن الصنف في قاعدة المصدر (نفس مصدر الفواتير)")
    src_total, src_rows, src_err = None, [], None
    try:
        src_total, src_rows, src_err = find_in_source()
        p(f"  إجمالي الأصناف في المصدر: {src_total}")
        if src_err:
            p(f"  ❌ خطأ: {src_err}")
        elif not src_rows:
            p(f"  ❌ لم يُعثر على صنف يطابق «{SEARCH_CODE}» أو يحوي (ورق/A4) في المصدر!")
        else:
            for r in src_rows:
                p(f"  ✅ id={r['id']} | باركود={r['barcode']} | {r['name']} | "
                  f"حركات={r['movements']} | رصيد={r['stock_balance']}")
    except Exception as e:
        p(f"  ❌ فشل الاتصال بالمصدر: {e}")

    # ─── [3] البحث في المرآة المحلية ───
    p(f"\n[3] البحث في المرآة المحلية data/local_mirror.db (مصدر القائمة قبل الإصلاح)")
    mir_total, mir_rows, mir_err = find_in_mirror(base_dir)
    if mir_err:
        p(f"  ⚠️ {mir_err}")
    else:
        p(f"  إجمالي الأصناف في المرآة: {mir_total}")
        if not mir_rows:
            p(f"  ❌ الصنف غير موجود في المرآة — المرآة قديمة (هذا سبب الاختفاء من القائمة)")
        else:
            for r in mir_rows:
                p(f"  ✅ id={r['id']} | باركود={r['barcode']} | {r['name']} | "
                  f"حركات={r['movements']} | رصيد={r['stock_balance']}")
    if src_total is not None and mir_total is not None:
        gap = src_total - mir_total
        if gap > 0:
            p(f"  ⚠️ المرآة متأخرة عن المصدر بـ {gap} صنف")

    # ─── [4] حالة الإصلاحات ───
    p(f"\n[4] حالة إصلاحات الحزمة الأخيرة (apply_hotfix_stock_reports)")
    m1 = file_has(base_dir, "cache_helpers.py", "def _get_fresh_session():")
    m2 = file_has(base_dir, "sync_manager.py", '"KitComponent", "InventoryMovement",')
    m3 = file_has(base_dir, "services.py", "def _month_expr(db, col):")
    applied = all(m is True for m in (m1, m2, m3))
    p(f"  {'✅' if m1 else '❌'} قائمة الأصناف تقرأ من المصدر مباشرة (cache_helpers)")
    p(f"  {'✅' if m2 else '❌'} حركات المخزون تُزامَن للمرآة (sync_manager)")
    p(f"  {'✅' if m3 else '❌'} توافق دوال الربحية مع SQLite (services)")

    # ─── الخلاصة ───
    p("\n" + BAR)
    p("الخلاصة والتوصية:")
    if not applied:
        p("  • إصلاح «القائمة تقرأ من المرآة القديمة» غير مطبق على هذا الجهاز بعد.")
        p("  ➜ الحل: شغّل ثم أعد تشغيل البرنامج بالكامل (أغلق كل نوافذه وافتحه من جديد):")
        p("       python apply_hotfix_stock_reports.py")
    elif src_rows:
        p("  • الإصلاح مطبق والصنف موجود في قاعدة المصدر — كل شيء جاهز.")
        p("  ➜ أعد تشغيل البرنامج بالكامل، ثم افتح «قائمة الأصناف / تعديل»")
        p("     مع التصفية «الكل» وتنقّل بين الصفحات (50 صنف/صفحة).")
        p("  ➜ للاختيار السريع: استخدم خانة «اختيار صنف للتعديل أو الحذف» واكتب 19008.")
        if mir_rows is not None and not mir_rows:
            p("  • ملاحظة: المرآة المحلية ما تزال قديمة — الإصلاح يجعل القائمة لا تعتمد عليها،")
            p("    وستتحدث تلقائياً مع المزامنة بعد تشغيل البرنامج.")
    else:
        p("  • الصنف غير موجود في قاعدة المصدر أصلاً! أرسل لي مخرجات هذا الفحص كاملة.")
    p(BAR)


if __name__ == "__main__":
    main()