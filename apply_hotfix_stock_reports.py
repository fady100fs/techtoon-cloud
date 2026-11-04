#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
apply_hotfix_stock_reports.py — حزمة إصلاح المخزون وتقارير الربحية
════════════════════════════════════════════════════════════════════
المشاكل التي تصلحها هذه الحزمة:
 1) المخزون يظهر صفراً في: تلميح الفواتير + قائمة الأصناف + لوحة التحكم +
    تقرير المخزون — رغم وجود فواتير شراء فعلية.
    السبب الجذري: جدول InventoryMovement (حركات المخزون) كان غائباً تماماً
    عن قائمة جداول المزامنة في sync_manager.py → المرآة المحلية التي تقرأ
    منها الصفحات لا تحتوي أي حركة مخزون أبداً.
 2) انهيار صفحة «تحليل الربحية»:
    sqlite3.OperationalError: no such function: to_char
    السبب الجذري: get_monthly_profitability + get_budget_vs_actual +
    record_annual_depreciation تستخدم to_char (دالة PostgreSQL فقط)
    وتنهار عند القراءة من مرآة SQLite.
 3) تقادم الأرقام بعد المزامنة: الكاش لم يكن يُمسح بعد نجاح المزامنة
    فتبقى القيم القديمة معروضة حتى انتهاء مدة الـ TTL.

الملفات المعدلة: sync_manager.py — services.py — cache_helpers.py
الآمان: نسخة احتياطية تلقائية قبل أي تعديل + يمكن تشغيله أكثر من مرة
        (idempotent) + لا يلمس قاعدة البيانات أو الفواتير إطلاقاً.

الاستخدام داخل مجلد البرنامج:
    python apply_hotfix_stock_reports.py
أو لتحديد مجلد البرنامج يدوياً:
    python apply_hotfix_stock_reports.py --target "D:\\PROG\\Accounting Prog\\Techtoon Accounting"
"""
import argparse
import datetime
import os
import re
import shutil
import sys

# ═══════════════════════════════════════════════════════════════════
#  أدوات مساعدة
# ═══════════════════════════════════════════════════════════════════

def read_file(path):
    with open(path, "r", encoding="utf-8") as f:
        return f.read()


def write_file(path, content):
    eol = "\r\n" if "\r\n" in content else "\n"
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(content)
    return eol


def eol_of(content):
    return "\r\n" if "\r\n" in content else "\n"


class Patcher:
    def __init__(self, target_dir):
        self.target_dir = target_dir
        self.backup_dir = os.path.join(
            target_dir,
            "backup_hotfix_stock_" + datetime.datetime.now().strftime("%Y%m%d_%H%M%S"),
        )
        self.results = {}   # filename -> list of (rule, status)
        self.failures = []

    def backup(self, filename):
        os.makedirs(self.backup_dir, exist_ok=True)
        src = os.path.join(self.target_dir, filename)
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(self.backup_dir, filename))

    def apply(self, filename, rules):
        """rules: list of dicts:
             name     — وصف القاعدة
             marker   — نص يدل على أن الإصلاح مطبق مسبقاً (idempotency)
             pattern  — regex أو نص قديم
             replace  — النص الجديد
             kind     — "str" (استبدال نصي) أو "regex"
             required — هل فشلها يوقف الحزمة؟ (افتراضي True)
        """
        path = os.path.join(self.target_dir, filename)
        if not os.path.exists(path):
            self.results[filename] = [("الملف نفسه", "❌ غير موجود")]
            self.failures.append(f"{filename}: الملف غير موجود")
            return None

        content = read_file(path)
        original = content
        statuses = []

        for rule in rules:
            name = rule["name"]
            marker = rule.get("marker")
            if marker and marker in content:
                statuses.append((name, "⏭️ مطبق مسبقاً"))
                continue

            kind = rule.get("kind", "str")
            if kind == "str":
                old, new = rule["pattern"], rule["replace"]
                count = content.count(old)
                if count == 0:
                    statuses.append((name, "❌ لم أجد النقطة المطلوبة"))
                    if rule.get("required", True):
                        self.failures.append(f"{filename}: {name}")
                    continue
                content = content.replace(old, new)
                statuses.append((name, f"✅ تم ({count} موضع)"))
            else:  # regex
                new_content, n = re.subn(rule["pattern"], rule["replace"], content)
                if n == 0:
                    statuses.append((name, "❌ لم أجد النقطة المطلوبة"))
                    if rule.get("required", True):
                        self.failures.append(f"{filename}: {name}")
                    continue
                content = new_content
                statuses.append((name, f"✅ تم ({n} موضع)"))

        if content != original:
            self.backup(filename)
            write_file(path, content)

        self.results[filename] = statuses
        return content


def replace_in_function(content, func_name, old, new):
    """استبدال نصي محصور داخل دالة واحدة فقط (حتى لا يمس دوالاً مشابهة)."""
    m = re.search(rf"(?m)^def {re.escape(func_name)}\(", content)
    if not m:
        return content, "not_found"
    start = m.start()
    nxt = re.search(r"(?m)^(?:@st\.cache_data|def )", content[m.end():])
    end = m.end() + (nxt.start() if nxt else len(content) - m.end())
    block = content[start:end]
    if new in block and old not in block:
        return content, "already"
    if old not in block:
        return content, "missing"
    return content[:start] + block.replace(old, new) + content[end:], "done"


# ═══════════════════════════════════════════════════════════════════
#  قواعد الإصلاح
# ═══════════════════════════════════════════════════════════════════

SYNC_MANAGER_RULES = [
    {
        "name": "إضافة InventoryMovement إلى جداول المزامنة (السبب الجذري للمخزون صفر)",
        "marker": '"KitComponent", "InventoryMovement",',
        "kind": "str",
        "pattern": '"KitComponent",',
        "replace": '"KitComponent", "InventoryMovement",',
        "required": True,
    },
    {
        "name": "إضافة جداول الفواتير المتكررة إلى المزامنة",
        "marker": '"RecurringInvoiceTemplate", "RecurringInvoiceLine",',
        "kind": "regex",
        "pattern": r'"Invoice", "InvoiceLine", "CostHistory",',
        "replace": (
            '"Invoice", "InvoiceLine",{EOL}        '
            '"RecurringInvoiceTemplate", "RecurringInvoiceLine", "CostHistory",'
        ),
        "required": False,
    },
    {
        "name": "مسح كاش Streamlit بعد نجاح المزامنة (منع الأرقام القديمة)",
        "marker": "بعد نجاح المزامنة نمسح الكاش",
        "kind": "regex",
        "pattern": (
            r'(        res = _pull_all_from_neon\(verbose=verbose\))'
            r'(\r?\n)'
            r'(        duration = time\.time\(\) - start)'
        ),
        "replace": (
            '\\1\\2'
            '        # ⭐ بعد نجاح المزامنة نمسح الكاش حتى ترى الصفحات البيانات\\2'
            '        #    المُزامنة فوراً (كانت تبقى قديمة حتى انتهاء مدة الكاش)\\2'
            '        try:\\2'
            '            import streamlit as _st_clear\\2'
            '            _st_clear.cache_data.clear()\\2'
            '        except Exception:\\2'
            '            pass\\2'
            '\\3'
        ),
        "required": True,
    },
]

HELPER_BLOCK = '''

def _month_expr(db, col):
    """تعبير الشهر (01..12) — يعمل على PostgreSQL و SQLite معاً.

    كانت الدوال تستخدم to_char (PostgreSQL فقط) فتنهار على المرآة المحلية:
    sqlite3.OperationalError: no such function: to_char
    """
    try:
        if db.get_bind().dialect.name == "sqlite":
            return func.strftime("%m", col)
    except Exception:
        pass
    return func.to_char(col, literal_column("'MM'"))


def _year_expr(db, col):
    """تعبير السنة (YYYY) — يعمل على PostgreSQL و SQLite معاً."""
    try:
        if db.get_bind().dialect.name == "sqlite":
            return func.strftime("%Y", col)
    except Exception:
        pass
    return func.to_char(col, literal_column("'YYYY'"))

'''

SERVICES_RULES = [
    {
        "name": "إضافة دالتي شهر/سنة محمولتين (_month_expr/_year_expr)",
        "marker": "def _month_expr(db, col):",
        "kind": "regex",
        "pattern": r"(?m)^(?=def get_monthly_profitability\(year=None\):)",
        "replace": "{HELPER}{EOL}",
        "required": True,
    },
    # ─── get_monthly_profitability (صفحة تحليل الربحية — الكراش المُبلَّغ عنه) ───
    {
        "name": "get_monthly_profitability: شهر الإيرادات",
        "marker": "month_expr_inv = _month_expr(db, models.Invoice.date)",
        "kind": "str",
        "pattern": "month_expr_inv = func.to_char(models.Invoice.date, literal_column(\"'MM'\"))",
        "replace": "month_expr_inv = _month_expr(db, models.Invoice.date)",
        "required": True,
    },
    {
        "name": "get_monthly_profitability: سنة الإيرادات",
        "marker": "year_expr_inv = _year_expr(db, models.Invoice.date)",
        "kind": "str",
        "pattern": "year_expr_inv = func.to_char(models.Invoice.date, literal_column(\"'YYYY'\"))",
        "replace": "year_expr_inv = _year_expr(db, models.Invoice.date)",
        "required": True,
    },
    {
        "name": "get_monthly_profitability: شهر الأسطر",
        "marker": "month_expr_line = _month_expr(db, models.Invoice.date)",
        "kind": "str",
        "pattern": "month_expr_line = func.to_char(models.Invoice.date, literal_column(\"'MM'\"))",
        "replace": "month_expr_line = _month_expr(db, models.Invoice.date)",
        "required": True,
    },
    {
        "name": "get_monthly_profitability: سنة الأسطر",
        "marker": "year_expr_line = _year_expr(db, models.Invoice.date)",
        "kind": "str",
        "pattern": "year_expr_line = func.to_char(models.Invoice.date, literal_column(\"'YYYY'\"))",
        "replace": "year_expr_line = _year_expr(db, models.Invoice.date)",
        "required": True,
    },
    # ─── get_budget_vs_actual (الموازنات) ───
    {
        "name": "get_budget_vs_actual: تعبير الشهر",
        "marker": "month_expr = _month_expr(db, models.JournalEntry.date)",
        "kind": "str",
        "pattern": "month_expr = func.to_char(models.JournalEntry.date, literal_column(\"'MM'\"))",
        "replace": "month_expr = _month_expr(db, models.JournalEntry.date)",
        "required": True,
    },
    {
        "name": "get_budget_vs_actual: تعبير السنة",
        "marker": "year_expr = _year_expr(db, models.JournalEntry.date)",
        "kind": "str",
        "pattern": "year_expr = func.to_char(models.JournalEntry.date, literal_column(\"'YYYY'\"))",
        "replace": "year_expr = _year_expr(db, models.JournalEntry.date)",
        "required": True,
    },
    # ─── record_annual_depreciation (الإهلاك) ───
    {
        "name": "record_annual_depreciation: تعبير السنة",
        "marker": "year_expr = _year_expr(db, DepreciationRecord.date)",
        "kind": "str",
        "pattern": "year_expr = func.to_char(DepreciationRecord.date, literal_column(\"'YYYY'\"))",
        "replace": "year_expr = _year_expr(db, DepreciationRecord.date)",
        "required": True,
    },
]

FRESH_SESSION_BLOCK = '''

def _get_fresh_session():
    """⭐ جلسة قراءة من قاعدة البيانات المصدر مباشرة (نفس مصدر الكتابة).

    تُستخدم لأرقام المخزون الحرجة (تلميح الفواتير، قائمة الأصناف، لوحة
    التحكم) حتى تظهر الكميات فوراً بعد حفظ فواتير البيع/الشراء دون
    انتظار المزامنة — كان الاعتماد على المرآة يعرض رصيداً صفراً أو قديماً.
    """
    from database import SessionLocal
    return SessionLocal()

'''

STOCK_FUNCTIONS = ["get_stock_map", "get_inventory_summary", "get_items_with_stock"]

CACHE_HELPERS_RULES = [
    {
        "name": "إضافة _get_fresh_session (قراءة المصدر مباشرة)",
        "marker": "def _get_fresh_session():",
        "kind": "regex",
        "pattern": (
            r"(def _get_read_session\(\):\r?\n"
            r"    from database import get_read_session\r?\n"
            r"    return get_read_session\(\)\r?\n)"
        ),
        "replace": "\\1{FRESH}{EOL}",
        "required": True,
    },
    {
        "name": "تخفيض مدة الكاش المحلي من 1800 إلى 120 ثانية",
        "marker": 'os.getenv("LOCAL_CACHE_TTL", "120")',
        "kind": "str",
        "pattern": 'os.getenv("LOCAL_CACHE_TTL", "1800")',
        "replace": 'os.getenv("LOCAL_CACHE_TTL", "120")',
        "required": False,
    },
]


def apply_cache_helpers_stock(patch_dir, content):
    """القواعد الخاصة بدوال المخزون الثلاث (استبدال محصور داخل كل دالة)."""
    statuses = []
    for fn in STOCK_FUNCTIONS:
        content, status = replace_in_function(
            content, fn, "db = _get_read_session()", "db = _get_fresh_session()"
        )
        label = {
            "done": f"✅ {fn}: القراءة من المصدر مباشرة",
            "already": f"⏭️ {fn}: مطبق مسبقاً",
            "missing": f"❌ {fn}: لم أجد جلسة القراءة داخل الدالة",
            "not_found": f"⏭️ {fn}: غير موجودة في هذه النسخة — تخطٍ آمن",
        }[status]
        statuses.append((label, ""))
        if status == "missing":
            patch_dir.failures.append(f"cache_helpers.py: {fn}")
    return content, statuses


# ═══════════════════════════════════════════════════════════════════
#  التنفيذ
# ═══════════════════════════════════════════════════════════════════

def main():
    parser = argparse.ArgumentParser(description="إصلاح المخزون وتقارير الربحية")
    parser.add_argument("--target", default=os.path.dirname(os.path.abspath(__file__)),
                        help="مجلد البرنامج (افتراضياً: مجلد هذا السكربت)")
    args = parser.parse_args()

    target = os.path.abspath(args.target)
    print("=" * 68)
    print("  حزمة إصلاح المخزون وتقارير الربحية — Techtoon Accounting")
    print("=" * 68)
    print(f"المجلد المستهدف: {target}\n")

    if not os.path.exists(os.path.join(target, "services.py")):
        print("❌ لم أجد services.py في المجلد المستهدف — تأكد من وضع السكربت داخل مجلد البرنامج")
        sys.exit(2)

    patcher = Patcher(target)
    eol = None

    # 1) sync_manager.py
    content = read_file(os.path.join(target, "sync_manager.py"))
    eol = eol_of(content)
    rules = []
    for r in SYNC_MANAGER_RULES:
        r = dict(r)
        if isinstance(r.get("replace"), str):
            r["replace"] = r["replace"].replace("{EOL}", eol)
        rules.append(r)
    patcher.apply("sync_manager.py", rules)

    # 2) services.py
    content = read_file(os.path.join(target, "services.py"))
    eol = eol_of(content)
    rules = []
    for r in SERVICES_RULES:
        r = dict(r)
        if isinstance(r.get("replace"), str):
            r["replace"] = r["replace"].replace("{EOL}", eol).replace("{HELPER}", HELPER_BLOCK)
        rules.append(r)
    patcher.apply("services.py", rules)

    # 3) cache_helpers.py
    content = read_file(os.path.join(target, "cache_helpers.py"))
    eol = eol_of(content)
    rules = []
    for r in CACHE_HELPERS_RULES:
        r = dict(r)
        if isinstance(r.get("replace"), str):
            r["replace"] = r["replace"].replace("{EOL}", eol).replace("{FRESH}", FRESH_SESSION_BLOCK)
        rules.append(r)
    patcher.apply("cache_helpers.py", rules)
    # ⭐ إعادة القراءة بعد تطبيق القواعد ثم استبدالات دوال المخزون ثم الحفظ مرة واحدة
    content = read_file(os.path.join(target, "cache_helpers.py"))
    content, extra = apply_cache_helpers_stock(patcher, content)
    patcher.results["cache_helpers.py"].extend([x for x in extra])
    if any("❌" in s for s, _ in extra):
        patcher.failures.append("cache_helpers.py: دوال المخزون")
    write_file(os.path.join(target, "cache_helpers.py"), content)

    # ─── التقرير ───
    print("نتيجة الإصلاح:")
    print("-" * 68)
    for filename, statuses in patcher.results.items():
        print(f"\n📄 {filename}")
        for name, status in statuses:
            print(f"   {status}  {name}" if status else f"   {name}")

    print("\n" + "-" * 68)
    if patcher.failures:
        print("⚠️  قواعد لم تكتمل (راجع الأعلى):")
        for f in patcher.failures:
            print(f"   - {f}")
        print("النسخة الاحتياطية محفوظة في:", patcher.backup_dir)
        sys.exit(1)

    # فحص الترجمة
    print("\nفحص الترجمة (py_compile):")
    import py_compile
    ok = True
    for filename in ("sync_manager.py", "services.py", "cache_helpers.py"):
        try:
            py_compile.compile(os.path.join(target, filename), doraise=True)
            print(f"   ✅ {filename}")
        except Exception as e:
            ok = False
            print(f"   ❌ {filename}: {e}")
    if not ok:
        print("⚠️ يوجد ملف به خطأ ترجمة — استعد النسخة الاحتياطية من:", patcher.backup_dir)
        sys.exit(1)

    print("\n✅ تم الإصلاح بنجاح — النسخة الاحتياطية في:", patcher.backup_dir)
    print("\nالخطوة الأخيرة: أعد تشغيل البرنامج بالكامل (أغلقه وافتحه من جديد).")
    print("بعد إعادة التشغيل: ستجد مخزون الأصناف صحيحاً، وصفحة تحليل الربحية تعمل،")
    print("والتلميح تحت الصنف يعرض الكمية المتاحة مباشرة بعد حفظ أي فاتورة.")


if __name__ == "__main__":
    main()