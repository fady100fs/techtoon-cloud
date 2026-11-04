# period_guard.py
"""
حارس الفترات المحاسبية — يمنع التعديل/الحذف في فترات مغلقة.

الاستخدام:
    from period_guard import check_period_open, check_period_open_for_entry

    # عند إنشاء/تعديل حركة بتاريخ معين:
    check_period_open(datetime.now())         # يرفع ValueError لو الفترة مغلقة
    check_period_open(date, entity="الفاتورة")

    # عند تعديل/حذف قيد موجود:
    check_period_open_for_entry(entry_id)     # يفحص تاريخ القيد
    check_period_open_for_entry(entry_id, entity="حركة الخزينة")
"""

from datetime import datetime

from database import SessionLocal
import models
from models import AccountingPeriod, PeriodStatus


def _period_is_locked(period):
    """هل الفترة مغلقة أو مقفلة؟"""
    if period is None:
        return False
    return period.status in (PeriodStatus.CLOSED, PeriodStatus.LOCKED)


def get_period_for_date(date):
    """يرجع الفترة المحاسبية التي يقع فيها التاريخ، أو None."""
    if date is None:
        date = datetime.now()

    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= date,
            AccountingPeriod.end_date >= date,
        ).first()
        return period
    finally:
        db.close()


def check_period_open(date, entity="العملية"):
    """يرفع ValueError إذا كانت الفترة مغلقة.

    Args:
        date: التاريخ المطلوب التحقق منه.
        entity: اسم العملية (لرسالة الخطأ).

    Raises:
        ValueError: إذا كانت الفترة مغلقة/مقفلة.
    """
    if date is None:
        date = datetime.now()

    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= date,
            AccountingPeriod.end_date >= date,
        ).first()

        if period and _period_is_locked(period):
            raise ValueError(
                f"⛔ لا يمكن تنفيذ {entity}: "
                f"الفترة '{period.period_name}' {period.status.value}. "
                f"افتح الفترة أولاً من صفحة الإغلاق المحاسبي."
            )
    finally:
        db.close()


def check_period_open_for_entry(entry_id, entity="هذا السجل"):
    """يفحص تاريخ JournalEntry ويرفض لو الفترة مغلقة.

    Args:
        entry_id: معرف القيد في جدول journal_entries.
        entity: اسم العملية (لرسالة الخطأ).

    Raises:
        ValueError: إذا كانت الفترة مغلقة/مقفلة.
    """
    if entry_id is None:
        return

    db = SessionLocal()
    try:
        entry = db.query(models.JournalEntry).filter(
            models.JournalEntry.id == entry_id
        ).first()
        if not entry or not entry.date:
            return
        entry_date = entry.date
    finally:
        db.close()

    check_period_open(entry_date, entity=entity)


def check_period_open_for_invoice(invoice_id):
    """فحص فترة الفاتورة (يستدعي check_period_open_for_entry)."""
    db = SessionLocal()
    try:
        inv = db.query(models.Invoice).filter(
            models.Invoice.id == invoice_id
        ).first()
        if not inv or not inv.date:
            return
        inv_date = inv.date
    finally:
        db.close()

    check_period_open(inv_date, entity="الفاتورة")


def check_period_open_for_payment(payment_id):
    """فحص فترة الدفعة."""
    db = SessionLocal()
    try:
        pay = db.query(models.Payment).filter(
            models.Payment.id == payment_id
        ).first()
        if not pay or not pay.date:
            return
        pay_date = pay.date
    finally:
        db.close()

    check_period_open(pay_date, entity="الدفعة")


def check_period_open_for_expense(expense_id):
    """فحص فترة المصروف."""
    db = SessionLocal()
    try:
        exp = db.query(models.Expense).filter(
            models.Expense.id == expense_id
        ).first()
        if not exp or not exp.date:
            return
        exp_date = exp.date
    finally:
        db.close()

    check_period_open(exp_date, entity="المصروف")


def is_period_open(date):
    """يرجع True/False بدون رفع استثناء (للعرض فقط)."""
    try:
        check_period_open(date)
        return True
    except ValueError:
        return False