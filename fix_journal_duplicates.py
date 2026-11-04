# -*- coding: utf-8 -*-
"""
fix_journal_duplicates.py — تنظيف القيود المحاسبية المكررة (لمرة واحدة)
═══════════════════════════════════════════════════════════════════════════
لماذا هذا السكربت؟
    النسخ القديمة من البرنامج كانت تنشئ قيد الفاتورة داخل حلقة الأسطر:
    فاتورة من 10 أسطر = 10 قيود مطابقة، كل قيد بمبلغ الفاتورة الكامل!
    لذلك يظهر تقرير الأرباح والخسائر برقم متضخم (مثال: 45,819 بدل ~4,000)
    وتكون أرصدة العملاء/الموردين/المخزون في الميزانية مضخمة بنفس النسبة.

ماذا يفعل هذا السكربت؟
    1) يأخذ نسخة احتياطية JSON كاملة لجدولي القيود (journals_backup)
    2) يجمع قيود كل فاتورة (reference_type = 'invoice')
    3) يحذف النسخ المطابقة تاماً فقط — ويحتفظ بأول قيد لكل فاتورة
       (القيود غير المطابقة تامة لا تُمس وتُعرض في تقرير "مراجعة يدوية")
    4) يعرض ملخص الإيرادات قبل/بعد للتأكد من نجاح التنظيف

آمن للتشغيل المتكرر: التشغيل الثاني لا يحذف شيئاً.
الفواتير نفسها (بنودها ومدفوعاتها) لا تُمس إطلاقاً — فقط القيود المكررة.

طريقة التنفيذ (VS Code Terminal):
    cd "D:\\PROG\\Accounting Prog\\Techtoon Accounting"
    python fix_journal_duplicates.py             # تفاعلي (يسألك قبل الحذف)
    python fix_journal_duplicates.py --dry-run   # عرض فقط بدون أي تغيير
    python fix_journal_duplicates.py --yes       # تنفيذ مباشر بدون سؤال
"""
import argparse
import json
import sys
from datetime import date, datetime
from pathlib import Path

# تشغيل السكربت من داخل مجلد البرنامج
APP_DIR = Path(__file__).resolve().parent
if str(APP_DIR) not in sys.path:
    sys.path.insert(0, str(APP_DIR))


def _json_default(o):
    if isinstance(o, (datetime, date)):
        return o.isoformat()
    return str(o)


def _dump_rows(db, model):
    cols = [c.key for c in model.__table__.columns]
    rows = db.query(model).all()
    out = []
    for r in rows:
        d = {}
        for c in cols:
            v = getattr(r, c, None)
            d[c] = v
        out.append(d)
    return out


def entry_signature(db, e):
    """بصمة القيد: الوصف + التاريخ + الأسطر مرتبة — لتطابق التام فقط."""
    from models import JournalLine
    rows = db.query(JournalLine).filter(
        JournalLine.entry_id == e.id
    ).order_by(JournalLine.account_id, JournalLine.debit, JournalLine.credit).all()
    return (
        (e.description or ""),
        str(e.date),
        tuple((r.account_id, round(float(r.debit or 0), 2),
               round(float(r.credit or 0), 2)) for r in rows),
    )


def journal_revenue(db):
    """مجموع الإيرادات حسب القيود (نفس طريقة صفحة التقارير)."""
    from sqlalchemy import func
    from models import Account, JournalLine
    return float(db.query(func.sum(JournalLine.credit)).join(
        Account
    ).filter(
        Account.type == 'REVENUE',
        JournalLine.credit > 0
    ).scalar() or 0)


def invoice_revenue(db):
    """مجموع المبيعات حسب الفواتير نفسها (نفس طريقة تقرير المنتج)."""
    from sqlalchemy import func
    from models import Invoice, InvoiceLine
    return float(db.query(func.sum(InvoiceLine.total)).join(
        Invoice
    ).filter(
        Invoice.type == 'sale',
        Invoice.status != 'cancelled'
    ).scalar() or 0)


def main():
    ap = argparse.ArgumentParser(description="تنظيف قيود الفواتير المكررة")
    ap.add_argument("--yes", action="store_true", help="تنفيذ مباشر بدون سؤال")
    ap.add_argument("--dry-run", action="store_true", help="عرض فقط بدون أي تغيير")
    args = ap.parse_args()

    from database import SessionLocal
    import models

    db = SessionLocal()
    try:
        print("═" * 62)
        print("🧹 تنظيف قيود الفواتير المكررة — Techtoon Accounting")
        print("═" * 62)

        rev_before = journal_revenue(db)
        inv_rev = invoice_revenue(db)
        print(f"📊 الإيرادات حسب القيود (طريقة تقرير الأرباح): {rev_before:,.2f}")
        print(f"📊 المبيعات حسب الفواتير (طريقة تقرير المنتج): {inv_rev:,.2f}")

        entries = db.query(models.JournalEntry).filter(
            models.JournalEntry.reference_type == "invoice"
        ).order_by(models.JournalEntry.reference_id, models.JournalEntry.id).all()

        groups = {}
        for e in entries:
            groups.setdefault(e.reference_id, []).append(e)

        dup_groups = {k: v for k, v in groups.items() if len(v) > 1}
        total_entries = len(entries)
        print(f"📋 قيود الفواتير: {total_entries} قيداً على {len(groups)} فاتورة")
        print(f"🔎 فواتير بها أكثر من قيد: {len(dup_groups)}")

        to_remove = []
        review = []
        for inv_id, ents in sorted(dup_groups.items(), key=lambda kv: (kv[0] is None, kv[0])):
            base_sig = entry_signature(db, ents[0])
            for e in ents[1:]:
                if entry_signature(db, e) == base_sig:
                    to_remove.append(e)
                else:
                    review.append((inv_id, e.id))

        # أسماء الفواتير للتقرير
        inv_ids = [k for k in dup_groups.keys() if k is not None]
        num_map = {}
        if inv_ids:
            num_map = dict(db.query(
                models.Invoice.id, models.Invoice.invoice_number
            ).filter(models.Invoice.id.in_(inv_ids)).all())

        if not to_remove and not review:
            print("\n✅ لا توجد قيود مكررة — قاعدة بياناتك نظيفة، لا شيء للإصلاح.")
            return

        if to_remove:
            print(f"\n🗑 قيود مكررة مطابقة تاماً ستُحذف: {len(to_remove)}")
            shown = 0
            for e in to_remove:
                if shown < 15:
                    label = num_map.get(e.reference_id, f"#{e.reference_id}")
                    print(f"   - قيد #{e.id} ({e.date:%Y-%m-%d}) — فاتورة {label}")
                    shown += 1
            if len(to_remove) > 15:
                print(f"   ... و{len(to_remove) - 15} قيداً آخر")

        if review:
            print(f"\n⚠️ قيود غير مطابقة تاماً (لن تُحذف — مراجعة يدوية): {len(review)}")
            for inv_id, eid in review[:10]:
                label = num_map.get(inv_id, f"#{inv_id}")
                print(f"   - قيد #{eid} — فاتورة {label}")

        # تقدير الإيراد بعد الحذف
        removed_lines = db.query(models.JournalLine).filter(
            models.JournalLine.entry_id.in_([e.id for e in to_remove])
        ).all() if to_remove else []

        rev_acct_ids = set(db.query(models.Account.id).filter(
            models.Account.type == 'REVENUE').all())
        rev_removed = 0.0
        for ln in removed_lines:
            if ln.account_id in rev_acct_ids:
                rev_removed += float(ln.credit or 0)

        rev_after_est = rev_before - rev_removed
        print("\n" + "─" * 62)
        print(f"📈 الإيراد الحالي (قيود): {rev_before:,.2f}")
        print(f"📉 الإيراد المتوقع بعد التنظيف: {rev_after_est:,.2f}")
        print(f"🎯 مرجع المقارنة (مبيعات الفواتير): {inv_rev:,.2f}")
        print("─" * 62)

        if args.dry_run:
            print("\n👁 وضع العرض فقط — لم يُغيَّر أي شيء. شغّل من جديد بدون --dry-run للتنفيذ.")
            return

        if not to_remove:
            print("\n⏭ لا توجد قيود مطابقة تامة للحذف (فقط بنود مراجعة يدوية أعلاه).")
            return

        if not args.yes:
            ans = input("\n❓ اكتب [نعم] لتنفيذ التنظيف الآن: ").strip().lower()
            if ans not in ("نعم", "n3m", "yes", "y"):
                print("🚫 تم الإلغاء — لم يُغيَّر أي شيء.")
                return

        # 1) نسخة احتياطية كاملة قبل أي حذف
        stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        backup_dir = APP_DIR / "backups" / f"journals_backup_{stamp}"
        backup_dir.mkdir(parents=True, exist_ok=True)
        backup = {
            "created_at": stamp,
            "journal_entries": _dump_rows(db, models.JournalEntry),
            "journal_lines": _dump_rows(db, models.JournalLine),
        }
        backup_path = backup_dir / "journals_full_backup.json"
        with open(backup_path, "w", encoding="utf-8") as f:
            json.dump(backup, f, ensure_ascii=False, indent=1, default=_json_default)
        print(f"\n🗂 نسخة احتياطية كاملة: {backup_path}")

        # 2) الحذف (يُحذف القيد كاملاً بأسطره — ونحتفظ بأول قيد لكل فاتورة)
        removed_count = 0
        removed_lines_count = 0
        for e in to_remove:
            n = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == e.id
            ).delete(synchronize_session=False)
            db.delete(e)
            removed_count += 1
            removed_lines_count += n
        db.commit()
        print(f"🗑 تم حذف {removed_count} قيداً مكرراً ({removed_lines_count} سطراً)")

        # 3) التحقق بعد التنظيف
        rev_after = journal_revenue(db)
        print("\n" + "═" * 62)
        print(f"✅ الإيراد حسب القيود بعد التنظيف: {rev_after:,.2f}")
        print(f"✅ مرجع المقارنة (مبيعات الفواتير): {inv_rev:,.2f}")
        if abs(rev_after - inv_rev) <= max(1.0, inv_rev * 0.02):
            print("🎉 الإيرادان متطابقان تقريباً — التقرير المالي أصبح سليماً!")
        else:
            print("ℹ️ يوجد فرق متبقٍ — راجع بنود «مراجعة يدوية» أعلاه أو أرسل")
            print("   مخرجات هذا السكربت للمطور.")

        # فحص توازن القيود المتبقية
        from sqlalchemy import func
        bal = db.query(
            models.JournalLine.entry_id,
            func.sum(models.JournalLine.debit - models.JournalLine.credit)
        ).group_by(models.JournalLine.entry_id).all()
        imbalanced = [eid for eid, diff in bal if abs(float(diff or 0)) > 0.05]
        if imbalanced:
            print(f"⚠️ قيود غير متوازنة موجودة مسبقاً (لم تُمس): {len(imbalanced)}")
        else:
            print("✅ جميع القيود المتبقية متوازنة.")
        print("═" * 62)
        print("\nالخطوة الأخيرة: أعد تشغيل البرنامج ثم افتح التقارير.")

    finally:
        db.close()


if __name__ == "__main__":
    main()
