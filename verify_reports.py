# -*- coding: utf-8 -*-
"""
verify_reports.py — فحص سريع: هل التقارير المالية سليمة؟
═══════════════════════════════════════════════════════════════════════════
يقارن بين مصدري الأرقام:
  (أ) القيود المحاسبية — الطريقة التي يستخدمها تقرير «الأرباح والخسائر»
      و«الميزانية» في صفحة التقارير
  (ب) الفواتير نفسها — الطريقة التي يستخدمها «تقرير المنتج»

لو الرقمان متطابقان تقريباً → التقارير سليمة.
لو الرقم (أ) أكبر بكثير → عندك قيود مكررة، شغّل fix_journal_duplicates.py

طريقة التنفيذ (VS Code Terminal):
    cd "D:\\PROG\\Accounting Prog\\Techtoon Accounting"
    python verify_reports.py
"""
import sys
from pathlib import Path

APP_DIR = Path(__file__).resolve().parent
if str(APP_DIR) not in sys.path:
    sys.path.insert(0, str(APP_DIR))

from database import SessionLocal  # noqa: E402


def main():
    from sqlalchemy import func
    import models

    db = SessionLocal()
    try:
        print("═" * 62)
        print("🩺 فحص سلامة التقارير المالية — Techtoon Accounting")
        print("═" * 62)

        # (أ) طريقة القيود
        rev_j = float(db.query(func.sum(models.JournalLine.credit)).join(
            models.Account
        ).filter(
            models.Account.type == 'REVENUE',
            models.JournalLine.credit > 0
        ).scalar() or 0)

        exp_j = float(db.query(func.sum(models.JournalLine.debit)).join(
            models.Account
        ).filter(
            models.Account.type == 'EXPENSE',
            models.JournalLine.debit > 0
        ).scalar() or 0)

        # (ب) طريقة الفواتير
        rev_i = float(db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled'
        ).scalar() or 0)

        # عدد القيود لكل فاتورة (كشف التكرار)
        counts = db.query(
            models.JournalEntry.reference_id,
            func.count(models.JournalEntry.id)
        ).filter(
            models.JournalEntry.reference_type == "invoice"
        ).group_by(models.JournalEntry.reference_id).all()

        n_invoices = len(counts)
        multi = [(iid, c) for iid, c in counts if c > 1]
        max_c = max((c for _, c in counts), default=0)

        print(f"\n(أ) الإيراد حسب القيود المحاسبية:      {rev_j:,.2f}")
        print(f"(ب) الإيراد حسب الفواتير (تقرير المنتج): {rev_i:,.2f}")
        print(f"    المصروفات حسب القيود:               {exp_j:,.2f}")
        print(f"    صافي الربح حسب القيود:              {rev_j - exp_j:,.2f}")
        print(f"\n📋 فواتير لها قيود: {n_invoices} | فواتير بأكثر من قيد: {len(multi)} | أقصى تكرار: {max_c}")
        if multi:
            ids = [iid for iid, _ in multi[:10]]
            nums = dict(db.query(models.Invoice.id, models.Invoice.invoice_number).filter(
                models.Invoice.id.in_(ids)).all()) if ids else {}
            print("   أمثلة:")
            for iid, c in multi[:10]:
                print(f"   - {nums.get(iid, '#' + str(iid))}: {c} قيود")

        print("─" * 62)
        diff = rev_j - rev_i
        tol = max(1.0, rev_i * 0.02)
        if abs(diff) <= tol:
            print("✅ النتيجة: الإيرادان متطابقان — التقارير المالية سليمة.")
        elif diff > 0:
            print(f"⚠️ النتيجة: القيود تضخم الإيراد بمقدار {diff:,.2f}.")
            print("   الحل: python fix_journal_duplicates.py")
        else:
            print(f"ℹ️ النتيجة: القيود أقل من الفواتير بمقدار {abs(diff):,.2f} —")
            print("   قد تكون فواتير أُدخلت دون ترحيل (نسخة قديمة) — راجع المطور.")
        print("═" * 62)
    finally:
        db.close()


if __name__ == "__main__":
    main()
