# -*- coding: utf-8 -*-
"""
apply_hotfix_reports.py — إصلاح عاجل: دقة التقارير المالية + ظهور الأصناف
═══════════════════════════════════════════════════════════════════════════
المشكلتان:

1) تقرير الأرباح والخسائر والميزانية يعرضان أرقاماً متضخمة:
   السبب الجذري: النسخ القديمة من create_invoice كانت تنشئ قيد الفاتورة
   داخل حلقة الأسطر — قيد كامل بمبلغ الفاتورة الكامل لكل سطر! ففاتورة بيع
   من 10 أسطر تسجّل الإيراد 10 مرات، فتضخم التقارير المبنية على القيود
   (التقارير > الأرباح والخسائر، الميزانية، ميزان المراجعة) بعدد الأسطر.

2) الأصناف لا تظهر في صفحة الأصناف ولوحة التحكم أصفار، رغم ظهورها بالفواتير:
   السبب الجذري: صفحة الأصناف تقرأ من المرآة SQLite المحلية (الفارغة)،
   بينما الفواتير تقرأ مباشرة من قاعدة البيانات المحلية (PostgreSQL) التي
   تحتوي البيانات فعلاً — تعارض مصادر قراءة.

الإصلاح (بدون أي تغيير في النسق):

  database.py:
    + إذا كان DATABASE_URL يشير لقاعدة محلية (localhost / شبكة محلية)
      → القراءة المباشرة منها (أسرع وأدق — بلا مرآة نهائياً)
    + حماية جديدة: لو كانت المرآة فارغة → قراءة مؤقتة من المصدر مباشرة
      مع إطلاق مزامنة خلفية، فلا تظهر صفحات فارغة أبداً

  services.py:
    + حماية _dedupe_invoice_journals داخل create_invoice: قيد واحد فقط
      لكل فاتورة (يمنع تضخم التقارير نهائياً حتى مع النسخ القديمة)
    + _after_write تتخطى مزامنة المرآة إذا كانت القاعدة نفسها محلية
      (تسريع كبير بعد كل فاتورة/دفعة)
    + الميزانية العمومية: إظهار الأرصدة السالبة بدل حذفها + أرباح محتجزة
      صحيحة (الإيراد − تكلفة المبيعات − المصروفات − الرواتب − الإهلاك)

  cache_helpers.py:
    + تخطي مزامنة المرآة إذا كانت قاعدة البيانات نفسها محلية

  sync_manager.py:
    + تسجيل نتائج/أخطاء المزامنة في data/sync.log بدل ابتلاعها صامتاً

مهم بعد هذا الباتش:
    شغّل fix_journal_duplicates.py مرة واحدة لتنظيف القيود المكررة
    التاريخية (التي تسبب رقم 45,819 الخاطئ) — راجع README.

طريقة التنفيذ (VS Code Terminal):
    cd "D:\\PROG\\Accounting Prog\\Techtoon Accounting"
    python apply_hotfix_reports.py

    السكربت يأخذ نسخة احتياطية تلقائية إلى backups/reports_hotfix_<وقت>/
    ويمكن تشغيله أكثر من مرة بأمان (يتخطى ما طُبّق مسبقاً).
"""
import argparse
import re
import shutil
import sys
from datetime import datetime
from pathlib import Path

# ═══════════════════════════════════════════════════════════
#  الكود الجديد الذي سيُضاف
# ═══════════════════════════════════════════════════════════

DEDUPE_HELPER = '''
def _dedupe_invoice_journals(db, invoice_id):
    """
    ⭐ حماية من تكرار قيود الفواتير:
    النسخ القديمة كانت تنشئ قيد الفاتورة داخل حلقة الأسطر (قيد كامل لكل
    سطر) مما يضخم التقارير المحاسبية بعدد أسطر كل فاتورة. هذه الدالة
    تحتفظ بأول قيد لكل فاتورة وتحذف القيود المطابقة تاماً فقط.
    """
    try:
        entries = db.query(models.JournalEntry).filter(
            models.JournalEntry.reference_type == "invoice",
            models.JournalEntry.reference_id == invoice_id
        ).order_by(models.JournalEntry.id).all()
        if len(entries) <= 1:
            return 0

        def _sig(e):
            rows = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == e.id
            ).order_by(models.JournalLine.account_id,
                       models.JournalLine.debit,
                       models.JournalLine.credit).all()
            return (
                (e.description or ""), str(e.date),
                tuple((r.account_id, round(float(r.debit or 0), 2),
                       round(float(r.credit or 0), 2)) for r in rows),
            )

        keep_sig = _sig(entries[0])
        removed = 0
        for extra in entries[1:]:
            if _sig(extra) == keep_sig:
                db.query(models.JournalLine).filter(
                    models.JournalLine.entry_id == extra.id
                ).delete(synchronize_session=False)
                db.delete(extra)
                removed += 1
        if removed:
            db.flush()
        return removed
    except Exception:
        return 0


'''

NEW_READ_BLOCK_LINES = [
    "_READ_SESSION_FACTORY = None",
    "_READ_INIT_DONE = False",
    "",
    "",
    "def _is_local_db_url(url) -> bool:",
    '    """هل رابط قاعدة البيانات يشير إلى جهاز محلي (وليس سحابي)؟"""',
    "    try:",
    "        import re as _re",
    '        _u = str(url or "").lower()',
    '        if _u.startswith("sqlite"):',
    "            return True",
    '        if "@" not in _u:',
    "            return False",
    '        _host = _u.split("@", 1)[1].split("/", 1)[0]',
    '        if ":" in _host:',
    '            _host = _host.split(":", 1)[0]',
    '        _host = _host.strip("[]() ")',
    "        if not _host:",
    "            return False",
    '        if _host in ("localhost", "127.0.0.1", "::1", "host.docker.internal"):',
    "            return True",
    '        if _re.match(r"^192\\.168\\.\\d+\\.\\d+$", _host):',
    "            return True",
    '        if _re.match(r"^10\\.\\d+\\.\\d+\\.\\d+$", _host):',
    "            return True",
    '        if _re.match(r"^172\\.(1[6-9]|2[0-9]|3[01])\\.\\d+\\.\\d+$", _host):',
    "            return True",
    "        return False",
    "    except Exception:",
    "        return False",
    "",
    "",
    "# ⭐ قاعدة البيانات نفسها محلية → القراءة المباشرة أسرع وأدق من أي مرآة",
    "DATABASE_URL_IS_LOCAL = _is_local_db_url(DATABASE_URL)",
    "",
    "",
    "def _mirror_has_data() -> bool:",
    '    """فحص سريع: هل تحتوي المرآة المحلية على بيانات فعلية؟"""',
    "    try:",
    "        from sqlalchemy import func as _func",
    "        import models as _models",
    "        from local_mirror import LocalSession as _LS",
    "        _db = _LS()",
    "        try:",
    "            for _m in (_models.User, _models.Account, _models.Item,",
    "                       _models.Party, _models.Invoice):",
    "                try:",
    '                    _c = _db.query(_func.count(getattr(_m, "id"))).scalar()',
    "                    if _c and int(_c) > 0:",
    "                        return True",
    "                except Exception:",
    "                    continue",
    "            return False",
    "        finally:",
    "            _db.close()",
    "    except Exception:",
    "        return False",
    "",
    "",
    "_MIRROR_FALLBACK_NOTED = False",
    "",
    "",
    "def get_read_session():",
    '    """',
    "    جلسة قراءة:",
    "    - قاعدة بيانات محلية (localhost/شبكة محلية) → قراءة مباشرة (الأسرع والأدق)",
    "    - سحابية + وضع محلي + مرآة تحتوي بيانات → المرآة (سريع)",
    "    - سحابية + وضع محلي + مرآة فارغة → المصدر مباشرة (حماية من بيانات ناقصة)",
    '    """',
    "    global _READ_SESSION_FACTORY, _READ_INIT_DONE, _MIRROR_FALLBACK_NOTED",
    "    if not _READ_INIT_DONE:",
    "        _READ_INIT_DONE = True",
    "        if is_local_mode() and not DATABASE_URL_IS_LOCAL:",
    "            _READ_SESSION_FACTORY = _try_local()",
    "    if _READ_SESSION_FACTORY is not None:",
    "        # ⭐ حماية: مرآة فارغة → اقرأ من المصدر مباشرة وابدأ مزامنة خلفية",
    "        if _mirror_has_data():",
    "            return _READ_SESSION_FACTORY()",
    "        if not _MIRROR_FALLBACK_NOTED:",
    "            _MIRROR_FALLBACK_NOTED = True",
    "            try:",
    '                print("⚠️ المرآة المحلية فارغة — قراءة مؤقتة من المصدر مع مزامنة خلفية")',
    "            except Exception:",
    "                pass",
    "            try:",
    "                import threading as _th",
    "                from sync_manager import full_sync as _fs",
    '                _th.Thread(target=_fs, kwargs={"verbose": False}, daemon=True).start()',
    "            except Exception:",
    "                pass",
    "        return SessionLocal()",
    "    return SessionLocal()",
    "",
    "",
]

SYNC_LOG_BLOCK_LINES = [
    "logger = logging.getLogger(__name__)",
    "",
    "",
    "# ⭐ تسجيل نتائج/أخطاء المزامنة في ملف مرئي بدل ابتلاعها صامتاً",
    "def _ensure_file_logging():",
    "    try:",
    '        if getattr(logger, "_tt_file_handler", False):',
    "            return",
    '        _dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "data")',
    "        os.makedirs(_dir, exist_ok=True)",
    '        _fh = logging.FileHandler(os.path.join(_dir, "sync.log"), encoding="utf-8")',
    '        _fh.setFormatter(logging.Formatter("%(asctime)s | %(levelname)s | %(message)s"))',
    "        logger.addHandler(_fh)",
    "        if logger.level == logging.NOTSET:",
    "            logger.setLevel(logging.INFO)",
    "        logger._tt_file_handler = True",
    "    except Exception:",
    "        pass",
    "",
    "_ensure_file_logging()",
    "",
]

BALANCE_RETAINED_LINES = [
    "{i1}# ⭐ أرباح محتجزة صحيحة: الإيراد − تكلفة المبيعات − المصروفات التشغيلية − الرواتب − الإهلاك",
    "{i1}revenue = db.query(func.sum(models.InvoiceLine.total)).join(",
    "{i1}    models.Invoice",
    "{i1}).filter(",
    "{i1}    models.Invoice.type == 'sale',",
    "{i1}    models.Invoice.status != 'cancelled',",
    "{i1}    models.Invoice.date <= as_of_date",
    "{i1}).scalar() or 0.0",
    "",
    "{i1}cogs = db.query(func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).join(",
    "{i1}    models.Invoice",
    "{i1}).filter(",
    "{i1}    models.Invoice.type == 'sale',",
    "{i1}    models.Invoice.status != 'cancelled',",
    "{i1}    models.Invoice.date <= as_of_date",
    "{i1}).scalar() or 0.0",
    "",
    "{i1}op_exp = db.query(func.sum(Expense.amount)).filter(",
    "{i1}    Expense.date <= as_of_date",
    "{i1}).scalar() or 0.0",
    "",
    "{i1}sal_exp = db.query(func.sum(SalaryRecord.gross_salary)).filter(",
    "{i1}    SalaryRecord.payment_date <= as_of_date",
    "{i1}).scalar() or 0.0",
    "",
    "{i1}dep_exp = db.query(func.sum(DepreciationRecord.depreciation_amount)).filter(",
    "{i1}    DepreciationRecord.date <= as_of_date",
    "{i1}).scalar() or 0.0",
    "",
    "{i1}equity['الأرباح المحتجزة'] = revenue - cogs - op_exp - sal_exp - dep_exp",
    "",
]

# ═══════════════════════════════════════════════════════════
#  قواعد الاستبدال
# ═══════════════════════════════════════════════════════════

DATABASE_RULES = [
    {
        "name": "get_read_session: قراءة مباشرة للقاعدة المحلية + حماية المرآة الفارغة",
        "marker": "DATABASE_URL_IS_LOCAL",
        "pattern": r"_READ_SESSION_FACTORY = None[\s\S]*?(?=\r?\ndef get_write_session)",
        "replace_lines": NEW_READ_BLOCK_LINES,
    },
]

SERVICES_RULES = [
    {
        "name": "create_invoice: حماية قيد واحد لكل فاتورة",
        "marker": "_dedupe_invoice_journals(db, invoice.id)",
        "pattern": (r"(?m)^([ \t]*)db\.commit\(\)[ \t]*\r?\n"
                    r"([ \t]*)_after_write\(\)[ \t]*\r?\n"
                    r"([ \t]*)return invoice[ \t]*\r?\n"),
        "replace_lines": [
            "{i1}# ⭐ حماية: قيد واحد فقط لكل فاتورة (يمنع تضخم التقارير نهائياً)",
            "{i1}try:",
            "{i1}    _dedupe_invoice_journals(db, invoice.id)",
            "{i1}except Exception:",
            "{i1}    pass",
            "{i1}db.commit()",
            "{i1}_after_write()",
            "{i1}return invoice",
            "",
        ],
    },
    {
        "name": "_after_write: تخطي مزامنة المرآة مع قاعدة محلية",
        "marker": "DATABASE_URL_IS_LOCAL as _db_is_local",
        "pattern": (r"(?m)^([ \t]*)if not is_local_mode\(\):[ \t]*\r?\n"
                    r"([ \t]*)return[ \t]*\r?\n"
                    r"([ \t]*)if _sync_pending\.is_set\(\):"),
        "replace_lines": [
            "{i1}if not is_local_mode():",
            "{i2}return",
            "{i1}try:",
            "{i1}    from database import DATABASE_URL_IS_LOCAL as _db_is_local",
            "{i1}except Exception:",
            "{i1}    _db_is_local = False",
            "{i1}if _db_is_local:",
            "{i1}    return  # ⭐ القاعدة نفسها محلية — لا حاجة لمرآة أو مزامنة",
            "{i3}if _sync_pending.is_set():",
        ],
    },
    {
        "name": "الميزانية: إظهار الأرصدة السالبة بدل حذفها",
        "marker": "if b != 0:  # ⭐",
        "pattern": (r"(?m)^([ \t]*)if b > 0:[ \t]*\r?\n"
                    r"([ \t]*)result\[acc\.name\] = b[ \t]*\r?$"),
        "replace_lines": [
            "{i1}if b != 0:  # ⭐ إظهار كل الأرصدة (بما فيها السالبة) بدل حذفها من التقرير",
            "{i2}result[acc.name] = b",
        ],
    },
    {
        "name": "الميزانية: أرباح محتجزة صحيحة (إيراد − COGS − مصروفات − رواتب − إهلاك)",
        "marker": "equity['الأرباح المحتجزة'] = revenue - cogs - op_exp - sal_exp - dep_exp",
        "pattern": (r"(?m)^([ \t]*)revenue = db\.query\(func\.sum\(models\.InvoiceLine\.total\)\)\.join\(\r?\n"
                    r"[ \t]*models\.Invoice\r?\n"
                    r"[ \t]*\)\.filter\(\r?\n"
                    r"[ \t]*models\.Invoice\.type == 'sale',\r?\n"
                    r"[ \t]*models\.Invoice\.status != 'cancelled',\r?\n"
                    r"[ \t]*models\.Invoice\.date <= as_of_date\r?\n"
                    r"[ \t]*\)\.scalar\(\) or 0\.0\r?\n"
                    r"\r?\n"
                    r"[ \t]*expenses = db\.query\(func\.sum\(Expense\.amount\)\)\.filter\(\r?\n"
                    r"[ \t]*Expense\.date <= as_of_date\r?\n"
                    r"[ \t]*\)\.scalar\(\) or 0\.0\r?\n"
                    r"\r?\n"
                    r"[ \t]*equity\['الأرباح المحتجزة'\] = revenue - expenses[ \t]*\r?\n"),
        "replace_lines": BALANCE_RETAINED_LINES,
    },
]

CACHE_RULES = [
    {
        "name": "invalidate_and_sync: تخطي المزامنة مع قاعدة محلية",
        "marker": "DATABASE_URL_IS_LOCAL as _db_local",
        "pattern": (r"(?m)^([ \t]*)from database import is_local_mode[ \t]*\r?\n"
                    r"([ \t]*)if is_local_mode\(\):[ \t]*\r?\n"
                    r"([ \t]*)import threading[ \t]*\r?\n"
                    r"([ \t]*)from sync_manager import full_sync[ \t]*\r?\n"
                    r"([ \t]*)threading\.Thread\(target=full_sync, daemon=True\)\.start\(\)[ \t]*\r?\n"
                    r"([ \t]*)except Exception:[ \t]*\r?\n"
                    r"([ \t]*)pass[ \t]*\r?$"),
        "replace_lines": [
            "{i1}from database import is_local_mode",
            "{i1}try:",
            "{i1}    from database import DATABASE_URL_IS_LOCAL as _db_local",
            "{i1}except Exception:",
            "{i1}    _db_local = False",
            "{i2}if is_local_mode() and not _db_local:  # ⭐ تخطَّ مزامنة المرآة إذا كانت القاعدة نفسها محلية",
            "{i3}import threading",
            "{i4}from sync_manager import full_sync",
            "{i5}threading.Thread(target=full_sync, daemon=True).start()",
            "{i6}except Exception:",
            "{i7}pass",
            "",
        ],
    },
]

SYNC_RULES = [
    {
        "name": "sync_manager: تسجيل المزامنة في data/sync.log",
        "marker": "_ensure_file_logging",
        "pattern": r"(?m)^logger = logging\.getLogger\(__name__\)[ \t]*\r?\n",
        "replace_lines": SYNC_LOG_BLOCK_LINES,
    },
]

# ═══════════════════════════════════════════════════════════
#  محرك التطبيق
# ═══════════════════════════════════════════════════════════

def apply_rules(content, rules, filename, results):
    """يطبّق قواعد regex مع الحفاظ على نهايات الأسطر الأصلية (CRLF/LF)."""
    for rule in rules:
        marker = rule.get("marker")
        if marker and marker in content:
            results.append(("SKIP", filename, rule["name"], "مطبّق مسبقاً"))
            continue
        m = re.search(rule["pattern"], content)
        if not m:
            results.append(("FAIL", filename, rule["name"],
                            "النمط غير موجود — نسخة مختلفة؟ أرسل هذا الخطأ للمطور"))
            continue
        region = content[m.start():m.end()]
        eol = "\r\n" if "\r\n" in region else "\n"
        if "replace_lines" in rule:
            groups = {f"i{k}": (m.group(k) or "") for k in range(1, (m.re.groups or 0) + 1)}
            lines = []
            for ln in rule["replace_lines"]:
                ln = ln.replace("{ind}", groups.get("i1", ""))
                for gname, gval in groups.items():
                    ln = ln.replace("{" + gname + "}", gval)
                lines.append(ln)
            block = eol.join(lines)
            content = content[:m.start()] + block + content[m.end():]
        else:
            content = content[:m.start()] + rule["replace_text"] + content[m.end():]
        results.append(("OK", filename, rule["name"], ""))
    return content


def insert_dedupe_helper(content, filename, results):
    """يضيف _dedupe_invoice_journals قبل def create_invoice."""
    if "_dedupe_invoice_journals" in content:
        results.append(("SKIP", filename, "إضافة دالة حماية القيود", "موجودة مسبقاً"))
        return content
    m = re.search(r'(?m)^def create_invoice\(', content)
    if not m:
        results.append(("FAIL", filename, "إضافة دالة حماية القيود",
                        "لم أجد نقطة الإدخال def create_invoice"))
        return content
    eol = "\r\n" if "\r\n" in content else "\n"
    helper = DEDUPE_HELPER.replace("\n", eol).strip(eol) + eol * 2
    content = content[:m.start()] + helper + content[m.start():]
    results.append(("OK", filename, "إضافة دالة حماية القيود", ""))
    return content


def main():
    ap = argparse.ArgumentParser(description="إصلاح دقة التقارير + ظهور الأصناف")
    ap.add_argument("--target", default=None,
                    help="مجلد البرنامج (افتراضياً: مجلد هذا السكربت)")
    args = ap.parse_args()
    target = Path(args.target).resolve() if args.target else Path(__file__).resolve().parent

    files = {
        "database.py": target / "database.py",
        "services.py": target / "services.py",
        "cache_helpers.py": target / "cache_helpers.py",
        "sync_manager.py": target / "sync_manager.py",
    }
    missing = [n for n, p in files.items() if not p.exists()]
    if missing:
        print("❌ لم أجد الملفات التالية في:", target)
        for m_ in missing:
            print("   -", m_)
        print("شغّل السكربت من داخل مجلد البرنامج أو استخدم --target")
        sys.exit(1)

    print("═" * 62)
    print("🔧 إصلاح دقة التقارير المالية + ظهور الأصناف — Techtoon Accounting")
    print("   المجلد:", target)
    print("═" * 62)

    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_dir = target / "backups" / f"reports_hotfix_{stamp}"
    backup_dir.mkdir(parents=True, exist_ok=True)
    for p in files.values():
        shutil.copy2(p, backup_dir / p.name)
    print("🗂 نسخة احتياطية:", backup_dir)

    results = []

    text = files["database.py"].read_bytes().decode("utf-8")
    text = apply_rules(text, DATABASE_RULES, "database.py", results)
    files["database.py"].write_bytes(text.encode("utf-8"))

    text = files["services.py"].read_bytes().decode("utf-8")
    text = insert_dedupe_helper(text, "services.py", results)
    text = apply_rules(text, SERVICES_RULES, "services.py", results)
    files["services.py"].write_bytes(text.encode("utf-8"))

    text = files["cache_helpers.py"].read_bytes().decode("utf-8")
    text = apply_rules(text, CACHE_RULES, "cache_helpers.py", results)
    files["cache_helpers.py"].write_bytes(text.encode("utf-8"))

    text = files["sync_manager.py"].read_bytes().decode("utf-8")
    text = apply_rules(text, SYNC_RULES, "sync_manager.py", results)
    files["sync_manager.py"].write_bytes(text.encode("utf-8"))

    # ── تحقق تركيبي ──
    import py_compile
    compile_ok = True
    for p in files.values():
        try:
            py_compile.compile(str(p), doraise=True)
            print(f"✅ فحص تركيبي سليم: {p.name}")
        except py_compile.PyCompileError as e:
            compile_ok = False
            print(f"❌ خطأ تركيبي في {p.name}:")
            print(e)

    print("─" * 62)
    fails = 0
    for status, fname, name, note in results:
        icon = {"OK": "✅", "SKIP": "⏭️", "FAIL": "❌"}[status]
        if status == "FAIL":
            fails += 1
        line = f"{icon} [{fname}] {name}"
        if note:
            line += f" — {note}"
        print(line)
    print("─" * 62)

    if not compile_ok or fails:
        print("\n❌ هناك بنود لم تُطبَّق — استعد النسخ الاحتياطية من مجلد backups")
        print("   وأرسل مخرجات هذا السكربت للمطور قبل أي خطوة أخرى.")
        sys.exit(2)

    print("""
✅ تم تطبيق الباتش بنجاح!

⚠️ خطوة إلزامية تالية — تنظيف القيود المكررة التاريخية:
   python fix_journal_duplicates.py
   (هي التي ستُصلح رقم 45,819 الخاطئ في تقرير الأرباح)

ثم أعد تشغيل البرنامج:
   1. أغلق نافذة البرنامج تماماً
   2. شغّل Techtoon.bat من جديد

بعد إعادة التشغيل ستحصل على:
   • الأصناف تظهر في صفحة الأصناف ولوحة التحكم تعمل (قراءة مباشرة من قاعدتك المحلية)
   • الفواتير الجديدة تُرحَّل بقيد واحد فقط لكل فاتورة
   • الميزانية تعرض الأرصدة السالبة وأرباحاً محتجزة صحيحة
   • أي خطأ مزامنة يُسجَّل في data/sync.log بدل أن يبقى صامتاً
""")


if __name__ == "__main__":
    main()
