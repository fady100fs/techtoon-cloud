# -*- coding: utf-8 -*-
"""
apply_hotfix_numbering.py — إصلاح عاجل: تضارب ترقيم الفواتير (UniqueViolation)
═══════════════════════════════════════════════════════════════════════════
المشكلة:
    IntegrityError: duplicate key value violates unique constraint
    "invoices_invoice_number_key" — Key (invoice_number)=(INV-000001) already exists.

السبب الجذري:
    1) قاعدة البيانات تفرض UNIQUE على invoice_number عالمياً (بيع + شراء معاً)
    2) صفحة الفواتير كانت تحسب الرقم التالي لكل نوع على حدة
       (used_invoice_numbers(db, invoice_type=...)) → أول فاتورة شراء بعد
       فاتورة بيع INV-000001 تقترح INV-000001 مرة أخرى → انهيار
    3) احتياطي services كان count()+1 وهو خاطئ بعد حذف أي فاتورة
       (العدّ ينقص لكن الأرقام القديمة ما زالت موجودة)

الإصلاح (ملفان فقط، بدون أي تغيير في النسق):
    services.py:
      + دالة _ensure_unique_invoice_number() — تولّد أصغر رقم حر عالمياً،
        وترفع الرقم تلقائياً عند التعارض بدل الانهيار
      * استبدال احتياطي count()+1 في create_invoice
      * استبدال ترقيم الفواتير المتكررة بنفس الدالة الموحدة
    invoice_page_common.py:
      * الترقيم المقترح والفحص المسبق يصبحان عالمياً (بيع+شراء معاً)
      * رسالة النجاح ومرجع الدفعة تعرضان الرقم النهائي الفعلي

طريقة التنفيذ (VS Code Terminal):
    cd "D:\\PROG\\Accounting Prog\\Techtoon Accounting"
    python apply_hotfix_numbering.py

    السكربت يأخذ نسخة احتياطية تلقائية إلى backups/numbering_hotfix_<وقت>/
    ويمكن تشغيله أكثر من مرة بأمان (يتخطى ما طُبّق مسبقاً).
"""
import argparse
import re
import shutil
import sys
from datetime import datetime
from pathlib import Path

# ═══════════════════════════════════════════════════════════
#  الدالة الجديدة التي ستُضاف إلى services.py
# ═══════════════════════════════════════════════════════════
HELPER_CODE = '''
def _ensure_unique_invoice_number(db, desired=None):
    """
    ⭐ يرجع رقم فاتورة غير مستخدم (فريد عالمياً عبر بيع + شراء معاً).

    - لو desired فارغ: يولّد (أكبر رقم موجود + 1) عالمياً وليس count()+1،
      لأن count() ينقص عند حذف فواتير فيقترح أرقاماً موجودة مسبقاً.
    - لو desired موجود ومستخدماً: يرفع الرقم تلقائياً لأول رقم حر
      بدلاً من الفشل بخطأ UniqueViolation.
    """
    import re as _re

    def _bump(base, offset):
        m = _re.search(r"(\\d+)$", base)
        if m:
            width = len(m.group(1))
            return base[:m.start()] + str(int(m.group(1)) + offset).zfill(width)
        return f"{base}-{offset}"

    candidate = str(desired).strip() if desired else None

    if not candidate:
        max_n = 0
        for (num,) in db.query(models.Invoice.invoice_number).all():
            m = _re.search(r"(\\d+)$", str(num or ""))
            if m:
                try:
                    max_n = max(max_n, int(m.group(1)))
                except ValueError:
                    pass
        candidate = f"INV-{max_n + 1:06d}"

    # ⭐ الفحص عالمي (كل الأنواع) لمطابقة قيد UNIQUE في قاعدة البيانات
    def _taken(num):
        return db.query(models.Invoice.id).filter(
            models.Invoice.invoice_number == num
        ).first() is not None

    if not _taken(candidate):
        return candidate

    base = candidate
    offset = 1
    while True:
        candidate = _bump(base, offset)
        if not _taken(candidate):
            return candidate
        offset += 1
        if offset > 100000:
            raise ValueError("تعذر توليد رقم فاتورة فريد — راجع ترقيم الفواتير")


'''

# ═══════════════════════════════════════════════════════════
#  قواعد الاستبدال لكل ملف
# ═══════════════════════════════════════════════════════════
SERVICES_RULES = [
    {
        "name": "create_invoice: استبدال احتياطي count()+1 بالدالة الآمنة",
        "pattern": r'(?m)^(?P<ind>[ \t]*)if not invoice_number:[ \t]*\r?\n'
                   r'[ \t]*invoice_number = f"INV-\{db\.query\(models\.Invoice\)\.count\(\) \+ 1:06d\}"[ \t]*\r?$',
        "replace_lines": [
            "{ind}# ⭐ ترقيم آمن فريد عالمياً (بيع+شراء) — يرفع الرقم تلقائياً عند التعارض",
            "{ind}invoice_number = _ensure_unique_invoice_number(db, invoice_number)",
        ],
    },
    {
        "name": "الفواتير المتكررة: ترقيم موحد آمن",
        "pattern": r'(?m)^(?P<ind>[ \t]*)invoice_count = db\.query\(models\.Invoice\)\.count\(\)[ \t]*\r?\n'
                   r'[ \t]*new_invoice_number = f"INV-\{invoice_count \+ 1:06d\}"[ \t]*\r?\n'
                   r'[ \t]*while db\.query\(models\.Invoice\)\.filter\([ \t]*\r?\n'
                   r'[ \t]*models\.Invoice\.invoice_number == new_invoice_number[ \t]*\r?\n'
                   r'[ \t]*\)\.first\(\):[ \t]*\r?\n'
                   r'[ \t]*invoice_count \+= 1[ \t]*\r?\n'
                   r'[ \t]*new_invoice_number = f"INV-\{invoice_count \+ 1:06d\}"[ \t]*\r?
        "replace_lines": [
            "{ind}# ⭐ ترقيم آمن عبر الدالة الموحدة (فريد عالمياً)",
            "{ind}new_invoice_number = _ensure_unique_invoice_number(db, None)",
        ],
    },
]

PAGE_RULES = [
    {
        "name": "الفحص المسبق والترقيم المقترح يصبحان عالميين (بيع+شراء معاً)",
        "pattern": r'used_numbers = used_invoice_numbers\(db, invoice_type=invoice_type\)',
        "replace_text": "used_numbers = used_invoice_numbers(db)  # ⭐ فريد عالمياً لمطابقة قيد UNIQUE",
    },
    {
        "name": "رسالة النجاح تعرض الرقم النهائي الفعلي",
        "pattern": r'saved_msg = f"✅ تم \{action_word\} فاتورة \{type_ar\} رقم \{current_num_str\} بنجاح!"',
        "replace_text": "saved_msg = f\"✅ تم {action_word} فاتورة {type_ar} رقم {getattr(invoice, 'invoice_number', current_num_str)} بنجاح!\"",
    },
    {
        "name": "مرجع الدفعة يستخدم الرقم النهائي الفعلي",
        "pattern": r'reference_number=current_num_str,',
        "replace_text": 'reference_number=getattr(invoice, "invoice_number", current_num_str),',
    },
]

MARKER_SERVICES = "_ensure_unique_invoice_number"
MARKER_PAGE = "used_invoice_numbers(db)  # ⭐ فريد عالمياً"


# ═══════════════════════════════════════════════════════════
#  محرك التطبيق
# ═══════════════════════════════════════════════════════════
def apply_rules(content, rules, filename, results):
    """يطبّق قواعد regex مع الحفاظ على نهايات الأسطر الأصلية (CRLF/LF)."""
    for rule in rules:
        m = re.search(rule["pattern"], content)
        if not m:
            results.append(("SKIP", filename, rule["name"],
                            "النمط غير موجود (ربما طُبّق سابقاً أو نسخة مختلفة)"))
            continue
        if "replace_lines" in rule:
            ind = m.group("ind") or ""
            lines = [ln.replace("{ind}", ind) for ln in rule["replace_lines"]]
            eol = "\r\n" if "\r\n" in content[m.start():m.end()] else "\n"
            block = eol.join(lines)
            content = content[:m.start()] + block + content[m.end():]
        else:
            content = content[:m.start()] + rule["replace_text"] + content[m.end():]
        results.append(("OK", filename, rule["name"], ""))
    return content


def insert_helper(content, filename, results):
    """يضيف _ensure_unique_invoice_number قبل def create_invoice."""
    if MARKER_SERVICES in content:
        results.append(("SKIP", filename, "إضافة دالة الترقيم الآمن",
                        "موجودة مسبقاً"))
        return content
    m = re.search(r'(?m)^def create_invoice\(', content)
    if not m:
        results.append(("FAIL", filename, "إضافة دالة الترقيم الآمن",
                        "لم أجد نقطة الإدخال def create_invoice"))
        return content
    eol = "\r\n" if "\r\n" in content else "\n"
    helper = HELPER_CODE.replace("\n", eol).strip(eol) + eol * 2
    content = content[:m.start()] + helper + content[m.start():]
    results.append(("OK", filename, "إضافة دالة الترقيم الآمن", ""))
    return content


def main():
    ap = argparse.ArgumentParser(description="إصلاح ترقيم الفواتير")
    ap.add_argument("--target", default=None,
                    help="مجلد البرنامج (افتراضياً: مجلد هذا السكربت)")
    args = ap.parse_args()
    target = Path(args.target).resolve() if args.target else Path(__file__).resolve().parent

    svc = target / "services.py"
    page = target / "invoice_page_common.py"

    missing = [str(p) for p in (svc, page) if not p.exists()]
    if missing:
        print("❌ لم أجد الملفات التالية في:", target)
        for m_ in missing:
            print("   -", m_)
        print("شغّل السكربت من داخل مجلد البرنامج أو استخدم --target")
        sys.exit(1)

    print("═" * 60)
    print("🔧 إصلاح ترقيم الفواتير — Techtoon Accounting")
    print("   المجلد:", target)
    print("═" * 60)

    # نسخة احتياطية
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_dir = target / "backups" / f"numbering_hotfix_{stamp}"
    backup_dir.mkdir(parents=True, exist_ok=True)

    results = []

    for path in (svc, page):
        shutil.copy2(path, backup_dir / path.name)

    # ── services.py ──
    text = svc.read_bytes().decode("utf-8")
    text = insert_helper(text, "services.py", results)
    text = apply_rules(text, SERVICES_RULES, "services.py", results)
    svc.write_bytes(text.encode("utf-8"))

    # ── invoice_page_common.py ──
    text = page.read_bytes().decode("utf-8")
    if MARKER_PAGE in text:
        results.append(("SKIP", "invoice_page_common.py",
                        "قواعد الصفحة", "مطبّقة مسبقاً"))
    else:
        text = apply_rules(text, PAGE_RULES, "invoice_page_common.py", results)
        page.write_bytes(text.encode("utf-8"))

    # ── تحقق تركيبي ──
    import py_compile
    compile_ok = True
    for path in (svc, page):
        try:
            py_compile.compile(str(path), doraise=True)
            print(f"✅ فحص تركيبي سليم: {path.name}")
        except py_compile.PyCompileError as e:
            compile_ok = False
            print(f"❌ خطأ تركيبي في {path.name}:")
            print(e)

    print("─" * 60)
    for status, fname, name, note in results:
        icon = {"OK": "✅", "SKIP": "⏭️", "FAIL": "❌"}[status]
        line = f"{icon} [{fname}] {name}"
        if note:
            line += f" — {note}"
        print(line)
    print("─" * 60)
    print("🗂 النسخة الاحتياطية:", backup_dir)

    if not compile_ok:
        print("\n❌ حدث خطأ تركيبي — استعد النسخ الاحتياطية من مجلد backups وأرسل لي رسالة الخطأ")
        sys.exit(2)

    print("""
✅ تم الإصلاح بنجاح!

الخطوة الأخيرة — أعد تشغيل البرنامج:
  1. أغلق نافذة البرنامج تماماً
  2. شغّل Techtoon.bat من جديد

بعد الإصلاح:
  • الترقيم المقترح في صفحة الفواتير يصبح فريداً عالمياً (بيع+شراء)
  • أي رقم متعارض يُرفع تلقائياً لأول رقم حر بدل رسالة الخطأ
  • الفواتير القديمة وترقيمها لا تتأثر إطلاقاً
""")


if __name__ == "__main__":
    main()
,
        "replace_lines": [
            "{ind}# ⭐ ترقيم آمن عبر الدالة الموحدة (فريد عالمياً)",
            "{ind}new_invoice_number = _ensure_unique_invoice_number(db, None)",
        ],
    },
]

PAGE_RULES = [
    {
        "name": "الفحص المسبق والترقيم المقترح يصبحان عالميين (بيع+شراء معاً)",
        "pattern": r'used_numbers = used_invoice_numbers\(db, invoice_type=invoice_type\)',
        "replace_text": "used_numbers = used_invoice_numbers(db)  # ⭐ فريد عالمياً لمطابقة قيد UNIQUE",
    },
    {
        "name": "رسالة النجاح تعرض الرقم النهائي الفعلي",
        "pattern": r'saved_msg = f"✅ تم \{action_word\} فاتورة \{type_ar\} رقم \{current_num_str\} بنجاح!"',
        "replace_text": "saved_msg = f\"✅ تم {action_word} فاتورة {type_ar} رقم {getattr(invoice, 'invoice_number', current_num_str)} بنجاح!\"",
    },
    {
        "name": "مرجع الدفعة يستخدم الرقم النهائي الفعلي",
        "pattern": r'reference_number=current_num_str,',
        "replace_text": 'reference_number=getattr(invoice, "invoice_number", current_num_str),',
    },
]

MARKER_SERVICES = "_ensure_unique_invoice_number"
MARKER_PAGE = "used_invoice_numbers(db)  # ⭐ فريد عالمياً"


# ═══════════════════════════════════════════════════════════
#  محرك التطبيق
# ═══════════════════════════════════════════════════════════
def apply_rules(content, rules, filename, results):
    """يطبّق قواعد regex مع الحفاظ على نهايات الأسطر الأصلية (CRLF/LF)."""
    for rule in rules:
        m = re.search(rule["pattern"], content)
        if not m:
            results.append(("SKIP", filename, rule["name"],
                            "النمط غير موجود (ربما طُبّق سابقاً أو نسخة مختلفة)"))
            continue
        if "replace_lines" in rule:
            ind = m.group("ind") or ""
            lines = [ln.replace("{ind}", ind) for ln in rule["replace_lines"]]
            eol = "\r\n" if "\r\n" in content[m.start():m.end()] else "\n"
            block = eol.join(lines)
            content = content[:m.start()] + block + content[m.end():]
        else:
            content = content[:m.start()] + rule["replace_text"] + content[m.end():]
        results.append(("OK", filename, rule["name"], ""))
    return content


def insert_helper(content, filename, results):
    """يضيف _ensure_unique_invoice_number قبل def create_invoice."""
    if MARKER_SERVICES in content:
        results.append(("SKIP", filename, "إضافة دالة الترقيم الآمن",
                        "موجودة مسبقاً"))
        return content
    m = re.search(r'(?m)^def create_invoice\(', content)
    if not m:
        results.append(("FAIL", filename, "إضافة دالة الترقيم الآمن",
                        "لم أجد نقطة الإدخال def create_invoice"))
        return content
    eol = "\r\n" if "\r\n" in content else "\n"
    helper = HELPER_CODE.replace("\n", eol).strip(eol) + eol * 2
    content = content[:m.start()] + helper + content[m.start():]
    results.append(("OK", filename, "إضافة دالة الترقيم الآمن", ""))
    return content


def main():
    ap = argparse.ArgumentParser(description="إصلاح ترقيم الفواتير")
    ap.add_argument("--target", default=None,
                    help="مجلد البرنامج (افتراضياً: مجلد هذا السكربت)")
    args = ap.parse_args()
    target = Path(args.target).resolve() if args.target else Path(__file__).resolve().parent

    svc = target / "services.py"
    page = target / "invoice_page_common.py"

    missing = [str(p) for p in (svc, page) if not p.exists()]
    if missing:
        print("❌ لم أجد الملفات التالية في:", target)
        for m_ in missing:
            print("   -", m_)
        print("شغّل السكربت من داخل مجلد البرنامج أو استخدم --target")
        sys.exit(1)

    print("═" * 60)
    print("🔧 إصلاح ترقيم الفواتير — Techtoon Accounting")
    print("   المجلد:", target)
    print("═" * 60)

    # نسخة احتياطية
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_dir = target / "backups" / f"numbering_hotfix_{stamp}"
    backup_dir.mkdir(parents=True, exist_ok=True)

    results = []

    for path in (svc, page):
        shutil.copy2(path, backup_dir / path.name)

    # ── services.py ──
    text = svc.read_bytes().decode("utf-8")
    text = insert_helper(text, "services.py", results)
    text = apply_rules(text, SERVICES_RULES, "services.py", results)
    svc.write_bytes(text.encode("utf-8"))

    # ── invoice_page_common.py ──
    text = page.read_bytes().decode("utf-8")
    if MARKER_PAGE in text:
        results.append(("SKIP", "invoice_page_common.py",
                        "قواعد الصفحة", "مطبّقة مسبقاً"))
    else:
        text = apply_rules(text, PAGE_RULES, "invoice_page_common.py", results)
        page.write_bytes(text.encode("utf-8"))

    # ── تحقق تركيبي ──
    import py_compile
    compile_ok = True
    for path in (svc, page):
        try:
            py_compile.compile(str(path), doraise=True)
            print(f"✅ فحص تركيبي سليم: {path.name}")
        except py_compile.PyCompileError as e:
            compile_ok = False
            print(f"❌ خطأ تركيبي في {path.name}:")
            print(e)

    print("─" * 60)
    for status, fname, name, note in results:
        icon = {"OK": "✅", "SKIP": "⏭️", "FAIL": "❌"}[status]
        line = f"{icon} [{fname}] {name}"
        if note:
            line += f" — {note}"
        print(line)
    print("─" * 60)
    print("🗂 النسخة الاحتياطية:", backup_dir)

    if not compile_ok:
        print("\n❌ حدث خطأ تركيبي — استعد النسخ الاحتياطية من مجلد backups وأرسل لي رسالة الخطأ")
        sys.exit(2)

    print("""
✅ تم الإصلاح بنجاح!

الخطوة الأخيرة — أعد تشغيل البرنامج:
  1. أغلق نافذة البرنامج تماماً
  2. شغّل Techtoon.bat من جديد

بعد الإصلاح:
  • الترقيم المقترح في صفحة الفواتير يصبح فريداً عالمياً (بيع+شراء)
  • أي رقم متعارض يُرفع تلقائياً لأول رقم حر بدل رسالة الخطأ
  • الفواتير القديمة وترقيمها لا تتأثر إطلاقاً
""")


if __name__ == "__main__":
    main()