# -*- coding: utf-8 -*-
"""
════════════════════════════════════════════════════════════════════
 apply_patch.py — تطبيق تعديلات Techtoon Accounting (v2) على مشروعك
════════════════════════════════════════════════════════════════════

 الاستخدام من VS Code Terminal:

     python apply_patch.py --target "D:\\PROG\\Accounting Prog\\Techtoon Accounting"

 ماذا يفعل؟
   1. يتأكد أن المسار الهدف صحيح (يبحث عن app.py)
   2. يأخذ نسخة احتياطية من كل ملف سيُعدَّل → _patch_backup_<التاريخ>/
   3. ينسخ الملفات المُصلحة من مجلد fixed_files إلى مشروعك
   4. يعرض ملخص العملية والخطوات التالية

 آمن 100%: لا يحذف أي شيء، وكل ملف قديم يُحفظ في النسخة الاحتياطية.
 لا يمس: قاعدة البيانات، .env، صفحاتك، أو أي ملف غير مذكور في CHANGES.md
"""
import argparse
import shutil
import sys
from datetime import datetime
from pathlib import Path

# ── الملفات التي ستُستبدل/تُضاف (مسارات نسبية من جذر المشروع) ──
PATCH_FILES = [
    "services.py",                  # ⭐ إعادة هيكلة ترحيل الفواتير + إصلاح تحديث الأسعار
    "cache_helpers.py",             # إصلاح لوحة التحكم (KPIs + ملخص المخزون)
    "audit_log.py",                 # إصلاح NameError في ملخص السجل
    "export_excel.py",              # إصلاح التصدير إلى Excel (كان مكسوراً)
    "sidebar.py",                   # إصلاح أنبوب الحالة المعلقة (تعديل الفواتير)
    "invoice_page_common.py",       # ⭐ ميزة تلميح المخزون تحت الصنف
    "requirements.txt",             # pywebview + wmi
    "README.txt",                   # تصحيح النصوص + قسم GitHub/الموبايل
    "pages/3_👥_عملاء وموردين.py",  # إصلاح الكراش عند التنقل من شجرة الحسابات
    ".gitignore",                   # جديد — لأجل الرفع على GitHub
]

PATCH_DIR = Path(__file__).resolve().parent / "fixed_files"


def main():
    parser = argparse.ArgumentParser(description="تطبيق تعديلات Techtoon v2")
    parser.add_argument("--target", required=True,
                        help='مسار مجلد المشروع، مثال: "D:\\PROG\\Accounting Prog\\Techtoon Accounting"')
    parser.add_argument("--no-backup", action="store_true",
                        help="عدم أخذ نسخة احتياطية (غير مستحسن)")
    args = parser.parse_args()

    target = Path(args.target).expanduser().resolve()
    print("═" * 60)
    print("  Techtoon Accounting — تطبيق التعديلات v2")
    print("═" * 60)

    # 1) التحقق من المسار
    if not target.exists():
        print(f"❌ المسار غير موجود: {target}")
        sys.exit(1)
    if not (target / "app.py").exists():
        print(f"❌ لا يوجد app.py في: {target}")
        print("   تأكد أن المسار هو جذر مشروع Techtoon Accounting")
        sys.exit(1)
    print(f"✔ المسار الهدف: {target}")

    if not PATCH_DIR.exists():
        print(f"❌ مجلد fixed_files غير موجود بجانب السكربت: {PATCH_DIR}")
        sys.exit(1)

    # 2) النسخة الاحتياطية
    if not args.no_backup:
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        backup_root = target / f"_patch_backup_{stamp}"
        backup_root.mkdir(exist_ok=True)
        print(f"✔ النسخ الاحتياطي → {backup_root.name}/")
    else:
        backup_root = None
        print("⚠ وضع عدم النسخ الاحتياطي — الملفات القديمة ستُستبدل نهائياً")

    # 3) النسخ
    print("\nجاري التطبيق:")
    ok, fail = [], []
    for rel in PATCH_FILES:
        src = PATCH_DIR / rel
        dst = target / rel
        try:
            if not src.exists():
                raise FileNotFoundError(f"غير موجود في fixed_files: {rel}")
            dst.parent.mkdir(parents=True, exist_ok=True)

            # نسخة احتياطية قبل الاستبدال
            if backup_root is not None and dst.exists():
                bdst = backup_root / rel
                bdst.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(dst, bdst)

            shutil.copy2(src, dst)
            ok.append(rel)
            print(f"  ✔ {rel}")
        except Exception as e:
            fail.append((rel, str(e)))
            print(f"  ❌ {rel} — {e}")

    # 4) الملخص
    print("\n" + "═" * 60)
    print(f"نجح: {len(ok)} | فشل: {len(fail)}")
    if fail:
        for rel, err in fail:
            print(f"  ❌ {rel}: {err}")
        sys.exit(1)

    print("""
✅ تم تطبيق جميع التعديلات!

الخطوات التالية:
  1. حدّث المكتبات (لو لم تكن مثبتة):
       pip install -r requirements.txt
  2. أعد تشغيل البرنامج وأجرّب:
       • فاتورة بيع بعدة أصناف + ضريبة 14% → ستُحفظ بقيود متوازنة
       • لوحة التحكم → ستعمل بدون أخطاء
       • فواتير البيع/الشراء → سطر خافت يعرض الكمية المتاحة تحت الصنف
  3. للرفع على GitHub والوصول من الموبايل:
       راجع GITHUB_SETUP.md

ملاحظة: الفواتير المحفوظة سابقاً وقيودها لم تتغير إطلاقاً.
        القيد الجديد المتوازن يُطبَّق على الفواتير الجديدة فقط.
""")


if __name__ == "__main__":
    main()
