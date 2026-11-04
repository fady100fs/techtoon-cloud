# -*- coding: utf-8 -*-
"""
test_all_features.py — اختبار شامل لكل ميزات Techtoon

يفحص:
1. سلامة قاعدة البيانات
2. توازن القيود المحاسبية
3. صحة الفواتير
4. المخزون
5. ملفات الواجهة
6. نظام التراخيص
7. الإصلاحات المطبّقة

طريقة الاستخدام:
  python test_all_features.py
"""
import sys
import os
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
os.chdir(Path(__file__).resolve().parent)

PASS = 0
FAIL = 0
WARN = 0


def ok(msg):
    global PASS
    PASS += 1
    print(f"  ✅ {msg}")


def fail(msg):
    global FAIL
    FAIL += 1
    print(f"  ❌ {msg}")


def warn(msg):
    global WARN
    WARN += 1
    print(f"  ⚠️  {msg}")


def section(title):
    print(f"\n{'='*50}")
    print(f"  {title}")
    print(f"{'='*50}\n")


# ═══════════════════════════════════════════════════════
# 1. فحص ملفات الواجهة
# ═══════════════════════════════════════════════════════
section("1. فحص ملفات الواجهة")

ui_files = [
    "sticky_topbar.py", "quick_actions.py", "global_search.py",
    "quick_stats.py", "smart_alerts.py", "theme_toggle.py",
    "print_styles.py", "ui_enhancements.py", "recently_viewed.py",
    "confirm_dialog.py", "draft_autosave.py", "invoice_cart_badge.py",
    "loading_indicator.py", "shortcuts.py",
]

for f in ui_files:
    if Path(f).exists():
        try:
            import py_compile
            py_compile.compile(f, doraise=True)
            ok(f"{f}")
        except Exception as e:
            fail(f"{f}: {e}")
    else:
        warn(f"{f} غير موجود")

# ═══════════════════════════════════════════════════════
# 2. فحص الملفات الأساسية
# ═══════════════════════════════════════════════════════
section("2. فحص الملفات الأساسية")

core_files = [
    "services.py", "cache_helpers.py", "sync_manager.py",
    "sidebar.py", "app.py", "invoice_page_common.py",
    "database.py", "models.py", "license_manager.py",
]

for f in core_files:
    if Path(f).exists():
        try:
            import py_compile
            py_compile.compile(f, doraise=True)
            ok(f"{f}")
        except Exception as e:
            fail(f"{f}: {e}")
    else:
        fail(f"{f} غير موجود!")

# ═══════════════════════════════════════════════════════
# 3. فحص استدعاءات app.py
# ═══════════════════════════════════════════════════════
section("3. فحص استدعاءات app.py")

try:
    src = Path("app.py").read_text(encoding="utf-8")
    
    expected_imports = [
        "render_topbar", "render_enhancements", "render_quick_stats",
        "render_smart_alerts", "render_theme_toggle", "render_print_styles",
    ]
    
    for imp in expected_imports:
        if imp in src:
            ok(f"app.py يستدعي {imp}")
        else:
            warn(f"app.py لا يستدعي {imp}")
except Exception as e:
    fail(f"خطأ في قراءة app.py: {e}")

# ═══════════════════════════════════════════════════════
# 4. فحص قاعدة البيانات
# ═══════════════════════════════════════════════════════
section("4. فحص قاعدة البيانات")

try:
    from database import SessionLocal
    from sqlalchemy import text
    
    db = SessionLocal()
    
    # 4أ) الفواتير
    print("  --- الفواتير ---")
    rows = db.execute(text("SELECT type, COUNT(*) FROM invoices GROUP BY type")).fetchall()
    for r in rows:
        ok(f"{r[0]}: {r[1]} فاتورة")
    
    # 4ب) فواتير INV- متبقية
    inv_count = db.execute(text("SELECT COUNT(*) FROM invoices WHERE invoice_number LIKE 'INV-%'")).scalar()
    if inv_count == 0:
        ok("لا فواتير بصيغة INV-")
    else:
        fail(f"{inv_count} فاتورة بصيغة INV- (يجب ترحيلها)")
    
    # 4ج) الفهارس
    idx_count = db.execute(text("SELECT COUNT(*) FROM pg_indexes WHERE schemaname='public'")).scalar()
    if idx_count >= 100:
        ok(f"{idx_count} فهرس نشط")
    else:
        warn(f"فقط {idx_count} فهرس (المتوقع 100+)")
    
    # 4د) الـ Views
    views = db.execute(text("SELECT viewname FROM pg_views WHERE schemaname='public'")).fetchall()
    ok(f"{len(views)} views نشطة")
    
    db.close()
except Exception as e:
    fail(f"خطأ في الاتصال بـ DB: {e}")

# ═══════════════════════════════════════════════════════
# 5. فحص توازن القيود المحاسبية
# ═══════════════════════════════════════════════════════
section("5. فحص توازن القيود المحاسبية")

try:
    db = SessionLocal()
    
    # كل قيد يجب أن يكون: SUM(debit) = SUM(credit)
    unbalanced = db.execute(text("""
        SELECT je.id, je.description, 
               COALESCE(SUM(jl.debit), 0) as total_debit,
               COALESCE(SUM(jl.credit), 0) as total_credit
        FROM journal_entries je
        LEFT JOIN journal_lines jl ON jl.entry_id = je.id
        GROUP BY je.id, je.description
        HAVING ABS(COALESCE(SUM(jl.debit), 0) - COALESCE(SUM(jl.credit), 0)) > 0.01
    """)).fetchall()
    
    if not unbalanced:
        ok("كل القيود متوازنة (مدين = دائن)")
    else:
        fail(f"{len(unbalanced)} قيد غير متوازن:")
        for r in unbalanced[:5]:
            print(f"      قيد #{r[0]}: مدين={r[2]:.2f}, دائن={r[3]:.2f}")
    
    db.close()
except Exception as e:
    fail(f"خطأ في فحص القيود: {e}")

# ═══════════════════════════════════════════════════════
# 6. فحص الفواتير
# ═══════════════════════════════════════════════════════
section("6. فحص الفواتير")

try:
    db = SessionLocal()
    
    # 6أ) فواتير بدون أسطر
    empty_inv = db.execute(text("""
        SELECT i.id, i.invoice_number
        FROM invoices i
        LEFT JOIN invoice_lines il ON il.invoice_id = i.id
        WHERE il.id IS NULL
    """)).fetchall()
    
    if not empty_inv:
        ok("كل الفواتير لها أسطر")
    else:
        warn(f"{len(empty_inv)} فاتورة بدون أسطر")
    
    # 6ب) فواتير بدون طرف
    no_party = db.execute(text("""
        SELECT id, invoice_number FROM invoices WHERE party_id IS NULL
    """)).fetchall()
    
    if not no_party:
        ok("كل الفواتير لها طرف")
    else:
        fail(f"{len(no_party)} فاتورة بدون طرف")
    
    # 6ج) مسلسلات مكررة
    dupes = db.execute(text("""
        SELECT invoice_number, COUNT(*) 
        FROM invoices 
        GROUP BY invoice_number 
        HAVING COUNT(*) > 1
    """)).fetchall()
    
    if not dupes:
        ok("لا مسلسلات مكررة")
    else:
        fail(f"{len(dupes)} مسلسل مكرر")
    
    db.close()
except Exception as e:
    fail(f"خطأ في فحص الفواتير: {e}")

# ═══════════════════════════════════════════════════════
# 7. فحص المخزون
# ═══════════════════════════════════════════════════════
section("7. فحص المخزون")

try:
    db = SessionLocal()
    
    # 7أ) أصناف بدون تصنيف
    no_cat = db.execute(text("""
        SELECT COUNT(*) FROM items WHERE category_id IS NULL
    """)).scalar()
    
    if no_cat == 0:
        ok("كل الأصناف لها تصنيف")
    else:
        warn(f"{no_cat} صنف بدون تصنيف")
    
    # 7ب) أصناف مدمجة بدون مكوّنات
    empty_kits = db.execute(text("""
        SELECT i.id, i.name
        FROM items i
        LEFT JOIN kit_components kc ON kc.kit_item_id = i.id
        WHERE i.is_kit = true AND kc.id IS NULL
    """)).fetchall()
    
    if not empty_kits:
        ok("كل الأصناف المدمجة لها مكوّنات")
    else:
        warn(f"{len(empty_kits)} صنف مدمج بدون مكوّنات")
    
    db.close()
except Exception as e:
    fail(f"خطأ في فحص المخزون: {e}")

# ═══════════════════════════════════════════════════════
# 8. فحص نظام التراخيص
# ═══════════════════════════════════════════════════════
section("8. فحص نظام التراخيص")

try:
    from license_manager import get_license_status, get_machine_id
    
    status = get_license_status()
    mid = get_machine_id()
    
    if mid:
        ok(f"machine_id: {mid}")
    else:
        warn("تعذّر قراءة machine_id")
    
    if status["valid"]:
        ok(f"الرخصة صالحة: {status['message']}")
    else:
        warn(f"الرخصة: {status['message']}")
    
    # فحص مولّد الرخص
    gen_path = Path("Licence/generate_license.py")
    if gen_path.exists():
        ok("مولّد الرخص موجود")
    else:
        warn("مولّد الرخص غير موجود في Licence/")
except Exception as e:
    fail(f"خطأ في فحص التراخيص: {e}")

# ═══════════════════════════════════════════════════════
# 9. فحص الإصلاحات المطبّقة
# ═══════════════════════════════════════════════════════
section("9. فحص الإصلاحات المطبّقة")

try:
    # 9أ) _after_write يستخدم targeted clear
    src = Path("services.py").read_text(encoding="utf-8")
    if "get_invoices_index.clear()" in src:
        ok("_after_write يستخدم targeted cache clear")
    else:
        warn("_after_write قد لا يستخدم targeted clear")
    
    # 9ب) quick_sync موجودة
    if "def quick_sync" in src or "from sync_manager import quick_sync" in src:
        ok("quick_sync موجودة")
    else:
        warn("quick_sync غير موجودة")
    
    # 9ج) atexit للمزامنة عند الإغلاق
    if "atexit" in src:
        ok("atexit للمزامنة عند الإغلاق")
    else:
        warn("atexit غير موجود")
    
    # 9د) NullPool للوضع المحلي
    db_src = Path("database.py").read_text(encoding="utf-8")
    if "NullPool" in db_src:
        ok("NullPool للوضع المحلي")
    else:
        warn("NullPool غير موجود")
    
    # 9هـ) format_invoice_number بنوع SAL/PUR
    inv_src = Path("invoice_page_common.py").read_text(encoding="utf-8")
    if "SAL" in inv_src and "PUR" in inv_src:
        ok("format_invoice_number يدعم SAL-/PUR-")
    else:
        warn("format_invoice_number قد لا يدعم SAL-/PUR-")
    
except Exception as e:
    fail(f"خطأ في فحص الإصلاحات: {e}")

# ═══════════════════════════════════════════════════════
# النتيجة النهائية
# ═══════════════════════════════════════════════════════
section("النتيجة النهائية")

total = PASS + FAIL + WARN
print(f"  ✅ نجح: {PASS}/{total}")
print(f"  ⚠️  تحذير: {WARN}/{total}")
print(f"  ❌ فشل: {FAIL}/{total}")

if FAIL == 0:
    print(f"\n  🎉 كل الفحوصات نجحت!")
elif FAIL <= 3:
    print(f"\n  ✅ معظم الفحوصات نجحت ({FAIL} مشاكل)")
else:
    print(f"\n  ⚠️  يوجد {FAIL} مشاكل تحتاج إصلاح")

print(f"\n{'='*50}")