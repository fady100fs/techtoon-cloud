# services.py
"""
ملف المنطق البرمجي (Business Logic)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
+ Period Guard (فحص الفترات المحاسبية)
+ Journal Validation (فحص توازن القيود)
+ Movement Serial (المسلسل الرقمي الموحّد)
+ Recurring Invoices (الفواتير المتكررة)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
⭐ النسخة المحدّثة لتعمل مع SQLite Mirror:
   • القراءات → get_read_session() = SQLite محلياً
   • الكتابات → get_write_session() = Neon دائماً
   • بعد كل كتابة: مزامنة خلفية + مسح كاش
"""
import os
import random
import shutil
import threading
import time
from datetime import datetime, timedelta

import pandas as pd
from sqlalchemy import func, literal_column, case
from dateutil.relativedelta import relativedelta

import models
from models import (
    AccountType, CostHistory, CashBox, CashBoxType, CashTransfer,
    Category, ExpenseCategory, Expense, Warehouse, StockLevel,
    WarehouseTransfer, StockCount, FixedAsset, DepreciationRecord, DepreciationMethod,
    Loan, LoanInstallment, LoanType, LoanStatus,
    Employee, SalaryRecord, EmploymentStatus, Budget,
    AccountingPeriod, PeriodStatus,
    RecurringInvoiceTemplate, RecurringInvoiceLine, RecurrenceFrequency,
)
from models import CostCenter, CostAllocation, CostCenterTransaction

# ✅ Period Guard
from period_guard import check_period_open, check_period_open_for_entry



# ⭐ مزامنة عند إغلاق البرنامج
import atexit
import signal

def _sync_on_exit(signum=None, frame=None):
    """مزامنة كاملة عند إغلاق البرنامج — تضمن أن كل الكتابات في SQLite."""
    try:
        if is_local_mode():
            from sync_manager import full_sync
            full_sync(verbose=False)
    except Exception:
        pass

atexit.register(_sync_on_exit)

try:
    signal.signal(signal.SIGTERM, _sync_on_exit)
except Exception:
    pass


# ═══════════════════════════════════════════════════════
#  🔌 طبقة الجلسات (Session Layer)
# ═══════════════════════════════════════════════════════
try:
    from database import (
        SessionLocal,
        get_read_session,
        get_write_session,
        is_local_mode,
    )
except ImportError:
    # توافقية عكسية — لو database.py لم يُحدَّث بعد
    from database import SessionLocal
    def get_read_session():  return SessionLocal()
    def get_write_session(): return SessionLocal()
    def is_local_mode():     return False


def _read():
    """جلسة قراءة — SQLite محلياً، Neon سحابياً."""
    return get_read_session()


def _write():
    """جلسة كتابة — Neon دائماً."""
    return get_write_session()


# 🔁 Debounce: منع تشغيل مزامنات متعددة متزامنة
_sync_pending = threading.Event()


def _after_write():
    """
    بعد أي كتابة ناجحة:
    1) امسح كاش Streamlit (لتحديث العرض)
    2) ابدأ مزامنة خلفية (Debounced) محلياً فقط
    """
    # 1) مسح مستهدف للكاش (فقط الدوال المتأثرة بالكتابة)
    #    لا نمسح كل الكاش — نتركه دافئاً لتسريع التنقل
    try:
        from cache_helpers import (
            get_invoices_index, get_payments_index,
            get_dashboard_kpis, get_items_with_stock,
            get_stock_map, get_inventory_summary,
        )
        get_invoices_index.clear()
        get_payments_index.clear()
        get_dashboard_kpis.clear()
        get_items_with_stock.clear()
        get_stock_map.clear()
        get_inventory_summary.clear()
    except Exception:
        pass

    # 2) لا مزامنة بعد كل حفظ — اعتمد على المزامنة الدورية (كل 30 دقيقة)
    #    هذا يمنع البطء بعد كل حفظ. الكاش يُحدّث عبر المسح المستهدف أعلاه.
    return  # تعطيل المزامنة بعد كل حفظ
    
    try:
        if not is_local_mode():
            return
        try:
            from database import DATABASE_URL_IS_LOCAL as _db_is_local
        except Exception:
            _db_is_local = False
        if _db_is_local:
            return  # ⭐ القاعدة نفسها محلية — لا حاجة لمرآة أو مزامنة
        if _sync_pending.is_set():
            return  # مزامنة قيد الانتظار بالفعل
        _sync_pending.set()

        def _do_sync():
            time.sleep(30)  # نافذة تجميع لعدة كتابات سريعة
            try:
                from sync_manager import quick_sync
                quick_sync()
            except Exception:
                pass
            finally:
                _sync_pending.clear()

        threading.Thread(target=_do_sync, daemon=True).start()
    except Exception:
        pass


# ═══════════════════════════════════════════════════════
#  🏦 دوال الخزائن
# ═══════════════════════════════════════════════════════
def create_cash_box(name, code, box_type, account_id,
                    responsible_person=None, max_limit=None, notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(CashBox).filter(CashBox.code == code).first()
        if existing:
            raise ValueError(f"كود الخزينة '{code}' موجود مسبقاً!")

        cash_box = CashBox(
            name=name, code=code, type=box_type, account_id=account_id,
            responsible_person=responsible_person, max_limit=max_limit,
            notes=notes, is_active=True
        )
        db.add(cash_box)
        db.commit()
        db.refresh(cash_box)
        _after_write()
        return cash_box
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_cash_box_balance(cash_box_id, db=None):
    """رصيد الخزينة — يقرأ من SQLite محلياً."""
    _owned = db is None
    if _owned:
        db = _read()
    try:
        cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
        if not cash_box:
            return 0.0

        debit_total = db.query(func.sum(models.JournalLine.debit)).filter(
            models.JournalLine.account_id == cash_box.account_id
        ).scalar() or 0.0

        credit_total = db.query(func.sum(models.JournalLine.credit)).filter(
            models.JournalLine.account_id == cash_box.account_id
        ).scalar() or 0.0

        return debit_total - credit_total
    finally:
        if _owned:
            db.close()


def transfer_between_cash_boxes(from_cash_box_id, to_cash_box_id, amount,
                                 notes=None, created_by=None):
    db = _write()
    try:
        if from_cash_box_id == to_cash_box_id:
            raise ValueError("لا يمكن التحويل بين نفس الخزينة!")

        from_box = db.query(CashBox).filter(CashBox.id == from_cash_box_id).first()
        to_box = db.query(CashBox).filter(CashBox.id == to_cash_box_id).first()

        if not from_box or not to_box:
            raise ValueError("إحدى الخزائن غير موجودة!")

        # ⚠️ نستخدم db نفسه لضمان قراءة الحالة بعد أي كتابات معلقة
        current_balance = get_cash_box_balance(from_cash_box_id, db=db)
        if current_balance < amount:
            raise ValueError(f"رصيد الخزينة غير كافٍ! الرصيد الحالي: {current_balance:,.2f}")

        check_period_open(datetime.now(), entity="تحويل بين الخزائن")

        transfer = CashTransfer(
            date=datetime.now(), from_cash_box_id=from_cash_box_id,
            to_cash_box_id=to_cash_box_id, amount=amount, notes=notes, created_by=created_by
        )
        db.add(transfer)

        journal_entry = models.JournalEntry(
            date=datetime.now(),
            description=f"تحويل من {from_box.name} إلى {to_box.name} - {amount:,.2f}",
            reference_type="cash_transfer", reference_id=None, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=to_box.account_id,
                                   debit=amount, credit=0.0))
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=from_box.account_id,
                                   debit=0.0, credit=amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)
        db.commit()
        _after_write()
        return transfer
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  💲 تحديث أسعار الصنف (يُستدعى داخل create_invoice)
# ═══════════════════════════════════════════════════════
def update_item_prices(item_id, db=None):
    """
    يحدّث أسعار الصنف من CostHistory.
    إذا مُرِّر db، يستخدمه (بدون commit/close). وإلا يفتح جلسة كتابة.
    """
    _owned = db is None
    if _owned:
        db = _write()
    try:
        item = db.query(models.Item).filter(models.Item.id == item_id).first()
        if not item:
            return

        purchase_records = db.query(CostHistory).filter(
            CostHistory.item_id == item_id
        ).order_by(CostHistory.purchase_date.desc()).all()

        if not purchase_records:
            return

        last_purchase = purchase_records[0]
        item.cost_price = last_purchase.purchase_price

        total_cost = sum(record.total_cost for record in purchase_records)
        total_qty = sum(record.quantity for record in purchase_records)

        if total_qty > 0:
            item.avg_cost_price = total_cost / total_qty
            item.total_purchased_qty = total_qty

        if item.avg_cost_price > 0:
            item.sell_price = item.avg_cost_price * 1.25

        if _owned:
            db.commit()
            _after_write()
    except Exception:
        if _owned:
            db.rollback()
        raise
    finally:
        if _owned:
            db.close()


# ═══════════════════════════════════════════════════════
#  1. إضافة عميل أو مورد
# ═══════════════════════════════════════════════════════
def create_party_with_opening_balance(name, party_type, opening_balance=0.0,
                                       phone=None, address=None, created_by=None):
    db = _write()
    try:
        if party_type == 'customer':
            parent_account = db.query(models.Account).filter(models.Account.code == "1201").first()
        else:
            parent_account = db.query(models.Account).filter(models.Account.code == "2101").first()

        if not parent_account:
            raise ValueError("الحساب الأب غير موجود!")

        party_account = models.Account(
            code=f"{parent_account.code}{db.query(models.Account).count() + 1:04d}",
            name=name, type=parent_account.type, parent_id=parent_account.id
        )
        db.add(party_account)
        db.flush()

        party = models.Party(
            name=name, type=party_type, phone=phone, address=address,
            account_id=party_account.id
        )
        db.add(party)

        if opening_balance != 0.0:
            opening_account = db.query(models.Account).filter(models.Account.code == "3101").first()
            if not opening_account:
                raise ValueError("حساب الأرصدة الافتتاحية غير موجود!")

            journal_entry = models.JournalEntry(
                date=datetime.now(), description=f"رصيد افتتاحي لـ {party_type}: {name}",
                reference_type="opening_balance", reference_id=None, created_by=created_by
            )
            db.add(journal_entry)
            db.flush()

            if party_type == 'customer':
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id,
                                           debit=opening_balance, credit=0.0))
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id,
                                           debit=0.0, credit=opening_balance))
            else:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id,
                                           debit=opening_balance, credit=0.0))
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id,
                                           debit=0.0, credit=opening_balance))

            db.flush()
            validate_journal_entry(db, journal_entry.id)

        db.commit()
        _after_write()
        return party
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  2. حساب تكلفة صنف للفاتورة (مساعد)
# ═══════════════════════════════════════════════════════
def _calc_item_cost_for_invoice(db, item):
    if not item:
        return 0.0
    if getattr(item, "is_kit", False):
        total = 0.0
        components = db.query(models.KitComponent).filter(
            models.KitComponent.kit_item_id == item.id
        ).all()
        for comp in components:
            comp_item = db.query(models.Item).filter(
                models.Item.id == comp.component_item_id
            ).first()
            if comp_item:
                total += float(comp.quantity or 0) * float(comp_item.cost_price or 0)
        return round(total, 4)
    cost = float(item.cost_price or 0)
    if cost == 0:
        cost = float(getattr(item, "avg_cost_price", 0) or 0)
    return cost


# ═══════════════════════════════════════════════════════
#  ⭐ ضمان وجود حساب ضريبة القيمة المضافة (2102)
# ═══════════════════════════════════════════════════════
def _ensure_tax_account(db):
    """يُرجع حساب ضريبة القيمة المضافة (كود 2102) وينشئه تلقائياً إن لم يوجد.

    يُستخدم في ترحيل الفواتير:
      - بيع:  دائن (ضريبة مخرجات مستحقة)
      - شراء: مدين (ضريبة مدخلات قابلة للخصم)
    """
    try:
        acc = db.query(models.Account).filter(models.Account.code == "2102").first()
        if acc:
            return acc
        parent = db.query(models.Account).filter(models.Account.code == "2000").first()
        acc = models.Account(
            code="2102",
            name="ضريبة القيمة المضافة",
            type=AccountType.LIABILITY,
            parent_id=parent.id if parent else None,
        )
        db.add(acc)
        db.flush()
        return acc
    except Exception:
        try:
            db.rollback()
        except Exception:
            pass
        return None


# ═══════════════════════════════════════════════════════
#  2b. إنشاء فاتورة
# ═══════════════════════════════════════════════════════
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
        m = _re.search(r"(\d+)$", base)
        if m:
            width = len(m.group(1))
            return base[:m.start()] + str(int(m.group(1)) + offset).zfill(width)
        return f"{base}-{offset}"

    candidate = str(desired).strip() if desired else None

    if not candidate:
        max_n = 0
        for (num,) in db.query(models.Invoice.invoice_number).all():
            m = _re.search(r"(\d+)$", str(num or ""))
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

def create_invoice(party_id, invoice_type, items, invoice_number=None, currency_id=1,
                   invoice_date=None, discount_percentage=0.0, tax_rate=14.0, created_by=None):
    """إنشاء فاتورة — الترحيل المحاسبي المُعاد هيكلته (⭐ v2).

    قبل: كان يُنشأ قيد لكل سطر بمبالغ مستوى الفاتورة (ترحيل مضاعف بعدد الأسطر)
         ولا يوجد سطر ضريبة (قيود غير متوازنة عند الضريبة > 0).

    الآن — لكل فاتورة قيد واحد متوازن فقط:
      بيع:   مدين/حساب العميل = الصافي
             دائن/4101 الإيرادات = المبلغ بعد الخصم
             دائن/2102 الضريبة  = مبلغ الضريبة (إن وجد)
      شراء:  مدين/1301 المخزون = المبلغ بعد الخصم
             مدين/2102 الضريبة  = مبلغ الضريبة (إن وجد)
             دائن/حساب المورد = الصافي
      وقيد COGS واحد للبيع (5101 مدين / 1301 دائن) بإجمالي تكلفة الأسطر.

    لا تغيير على: الأسطر، حركات المخزون، سجل التكاليف، ترقيم الفواتير.
    ⭐ الفواتير المحفوظة سابقاً وقيودها لا تتأثر إطلاقاً — المنطق الجديد
       يُطبَّق على الفواتير الجديدة فقط.
    """
    db = _write()
    try:
        check_period_open(invoice_date or datetime.now(), entity="إنشاء فاتورة")
        period = get_period_for_date(invoice_date or datetime.now())

        party = db.query(models.Party).filter(models.Party.id == party_id).first()
        if not party:
            raise ValueError(f"العميل/المورد برقم {party_id} غير موجود!")

        # ⭐ ترقيم آمن فريد عالمياً (بيع+شراء) — يرفع الرقم تلقائياً عند التعارض
        invoice_number = _ensure_unique_invoice_number(db, invoice_number)
        if invoice_date is None:
            invoice_date = datetime.now()

        subtotal = sum(item['quantity'] * item['price'] for item in items)
        discount_amount = subtotal * (discount_percentage / 100)
        amount_after_discount = subtotal - discount_amount
        tax_amount = amount_after_discount * (tax_rate / 100)
        net_amount = amount_after_discount + tax_amount

        invoice = models.Invoice(
            invoice_number=invoice_number, date=invoice_date, party_id=party.id,
            type=invoice_type, total_amount=net_amount, status='pending',
            currency_id=currency_id, discount_amount=discount_amount,
            discount_percentage=discount_percentage, tax_rate=tax_rate,
            tax_amount=tax_amount, net_amount=net_amount, created_by=created_by
        )
        db.add(invoice)
        db.flush()

        line_costs_total = 0.0   # ⭐ إجمالي تكلفة الأسطر لقيد COGS الموحّد

        for item_data in items:
            item = db.query(models.Item).filter(models.Item.name == item_data['item_name']).first()
            if not item:
                raise ValueError(f"الصنف '{item_data['item_name']}' غير موجود!")

            line_total = item_data['quantity'] * item_data['price']
            line_cost = _calc_item_cost_for_invoice(db, item)
            invoice_line = models.InvoiceLine(
                invoice_id=invoice.id, item_id=item.id,
                quantity=item_data['quantity'], price=item_data['price'],
                total=line_total, cost_price=line_cost
            )
            db.add(invoice_line)

            # ⭐ إصلاح أصناف الدمج (kit): عند بيع/شراء صنف مدمج، يجب خصم/إضافة
            #    كميات مكوّناته من مخزون كل صنف مكوّن (وليس من مخزون الصنف المدمج نفسه
            #    الذي لا يحتفظ بمخزون مستقل). الحل: إنشاء InventoryMovement لكل مكوّن
            #    بمقدار (component.quantity × كمية الصنف المدمج في الفاتورة).
            #    الصنف العادي يحافظ على سلوكه القديم بدون أي تعديل.
            if item.is_kit:
                kit_qty = float(item_data['quantity'] or 0)
                kit_components = db.query(models.KitComponent).filter(
                    models.KitComponent.kit_item_id == item.id
                ).all()
                movement_type = 'in' if invoice_type == 'purchase' else 'out'
                for comp in kit_components:
                    comp_qty = float(comp.quantity or 0) * kit_qty
                    if comp_qty <= 0:
                        continue
                    db.add(models.InventoryMovement(
                        item_id=comp.component_item_id, date=invoice_date,
                        type=movement_type, quantity=comp_qty, reference_id=invoice.id
                    ))
            else:
                movement_type = 'in' if invoice_type == 'purchase' else 'out'
                movement = models.InventoryMovement(
                    item_id=item.id, date=invoice_date, type=movement_type,
                    quantity=item_data['quantity'], reference_id=invoice.id
                )
                db.add(movement)

                if invoice_type == 'purchase':
                    purchase_price = item_data['price']
                    total_cost = item_data['quantity'] * purchase_price
                    cost_record = CostHistory(
                        item_id=item.id, invoice_id=invoice.id,
                        purchase_price=purchase_price, quantity=item_data['quantity'],
                        total_cost=total_cost, purchase_date=invoice_date, supplier_id=party.id
                    )
                    db.add(cost_record)
                    # ⭐ الإصلاح: flush حتى يرى تحديث الأسطر سجل التكلفة الجديد
                    #    (الجلسة autoflush=False وكان السجل المعلق غير مرئي للاستعلام)
                    db.flush()
                    # ⭐ نمرر نفس db لتفادي جلسة متداخلة منفصلة
                    update_item_prices(item.id, db=db)

            if invoice_type == 'sale':
                line_costs_total += float(item_data['quantity'] or 0) * float(line_cost or 0)

        # ═════════════════════════════════════════════════
        #  ⭐ الترحيل المحاسبي — قيد واحد متوازن للفاتورة كاملة
        # ═════════════════════════════════════════════════
        journal_entry = models.JournalEntry(
            date=invoice_date, description=f"فاتورة {invoice_type} رقم {invoice_number}",
            reference_type="invoice", reference_id=invoice.id, created_by=created_by,
            period_id=period.id if period else None
        )
        db.add(journal_entry)
        db.flush()

        # تقريب القيم لضمان التوازن التام بين الطرفين
        net_r = round(float(net_amount or 0), 2)
        aad_r = round(float(amount_after_discount or 0), 2)
        tax_r = round(float(tax_amount or 0), 2)

        tax_account = _ensure_tax_account(db) if tax_r > 0 else None

        if invoice_type == 'sale':
            # بيع: دائن الإيرادات + دائن الضريبة | مدين العميل بالصافي
            credit_rev = aad_r
            credit_tax = tax_r if (tax_r > 0 and tax_account is not None) else 0.0
            debit_party = round(credit_rev + credit_tax, 2)

            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id,
                                       debit=debit_party, credit=0.0))
            revenue_account = db.query(models.Account).filter(models.Account.code == "4101").first()
            if revenue_account:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=revenue_account.id,
                                           debit=0.0, credit=credit_rev))
            if credit_tax > 0 and tax_account is not None:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=tax_account.id,
                                           debit=0.0, credit=credit_tax))
        else:
            # شراء: مدين المخزون + مدين ضريبة المدخلات | دائن المورد بالصافي
            credit_party = net_r
            debit_inventory = aad_r
            debit_tax = 0.0
            if tax_account is not None:
                debit_tax = max(0.0, round(credit_party - debit_inventory, 2))
            if debit_tax <= 0:
                # لا ضريبة أو لا حساب ضريبة → المخزون يتحمل الصافي كاملاً (متوازن)
                debit_inventory = credit_party

            inventory_account = db.query(models.Account).filter(models.Account.code == "1301").first()
            if inventory_account:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=inventory_account.id,
                                           debit=debit_inventory, credit=0.0))
            if debit_tax > 0 and tax_account is not None:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=tax_account.id,
                                           debit=debit_tax, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id,
                                       debit=0.0, credit=credit_party))

        db.flush()
        validate_journal_entry(db, journal_entry.id)

        # ═══ قيد تكلفة البضاعة المباعة — واحد للفاتورة كاملة (بيع فقط) ═══
        if invoice_type == 'sale' and line_costs_total > 0:
            cogs_total = round(line_costs_total, 2)
            cogs_entry = models.JournalEntry(
                date=invoice_date,
                description=f"تكلفة البضاعة المباعة - فاتورة {invoice_number}",
                reference_type="cogs", reference_id=invoice.id, created_by=created_by,
                period_id=period.id if period else None
            )
            db.add(cogs_entry)
            db.flush()
            cogs_account = db.query(models.Account).filter(models.Account.code == "5101").first()
            inventory_account = db.query(models.Account).filter(models.Account.code == "1301").first()
            if cogs_account and inventory_account:
                db.add(models.JournalLine(entry_id=cogs_entry.id, account_id=cogs_account.id,
                                           debit=cogs_total, credit=0.0))
                db.add(models.JournalLine(entry_id=cogs_entry.id, account_id=inventory_account.id,
                                           debit=0.0, credit=cogs_total))
            db.flush()
            validate_journal_entry(db, cogs_entry.id)

        # ⭐ حماية: قيد واحد فقط لكل فاتورة (يمنع تضخم التقارير نهائياً)
        try:
            _dedupe_invoice_journals(db, invoice.id)
        except Exception:
            pass
        db.commit()
        _after_write()
        return invoice
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  3. إنشاء صنف مدمج
# ═══════════════════════════════════════════════════════
def create_kit_item(kit_name, components, sell_price, created_by=None):
    db = _write()
    try:
        kit_item = models.Item(name=kit_name, sell_price=sell_price, is_kit=True, barcode=None)
        db.add(kit_item)
        db.flush()

        for comp_data in components:
            component_item = db.query(models.Item).filter(
                models.Item.name == comp_data['item_name']
            ).first()
            if not component_item:
                raise ValueError(f"الصنف المكون '{comp_data['item_name']}' غير موجود!")
            db.add(models.KitComponent(
                kit_item_id=kit_item.id, component_item_id=component_item.id,
                quantity=comp_data['quantity']
            ))

        db.commit()
        _after_write()
        return kit_item
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  4. إضافة صنف عادي
# ═══════════════════════════════════════════════════════
def create_item(name, cost_price, sell_price, barcode=None, created_by=None):
    db = _write()
    try:
        if not barcode or barcode.strip() == "":
            barcode = f"ITEM{random.randint(100000, 999999)}"
        if sell_price == 0.0 and cost_price > 0:
            sell_price = cost_price * 1.25

        item = models.Item(
            name=name, cost_price=cost_price, sell_price=sell_price,
            avg_cost_price=cost_price, barcode=barcode, is_kit=False
        )
        db.add(item)
        db.commit()
        db.refresh(item)
        _after_write()
        return item
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  5. تسجيل دفعة
# ═══════════════════════════════════════════════════════
def create_payment(party_id, amount, payment_type, payment_method='cash',
                   reference_number=None, notes=None, currency_id=1,
                   payment_date=None, created_by=None, cash_box_id=1):
    db = _write()
    try:
        check_period_open(payment_date or datetime.now(), entity="تسجيل دفعة")
        period = get_period_for_date(payment_date or datetime.now())

        party = db.query(models.Party).filter(models.Party.id == party_id).first()
        if not party:
            raise ValueError(f"العميل/المورد برقم {party_id} غير موجود!")

        cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
        if not cash_box:
            raise ValueError("الخزينة المحددة غير موجودة!")

        if payment_date is None:
            payment_date = datetime.now()

        payment = models.Payment(
            date=payment_date, party_id=party.id, amount=amount,
            payment_type=payment_type, payment_method=payment_method,
            reference_number=reference_number, notes=notes,
            currency_id=currency_id, cash_box_id=cash_box_id
        )
        db.add(payment)

        journal_entry = models.JournalEntry(
            date=payment_date,
            description=f"دفعة {payment_type} لـ {party.name} - {reference_number or ''}",
            reference_type="payment", reference_id=None, created_by=created_by,
            period_id=period.id if period else None
        )
        db.add(journal_entry)
        db.flush()

        if payment_type == 'receipt':
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id,
                                       debit=amount, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id,
                                       debit=0.0, credit=amount))
        else:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id,
                                       debit=amount, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id,
                                       debit=0.0, credit=amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)

        db.commit()
        _after_write()
        return payment
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  6. استيراد الأصناف
# ═══════════════════════════════════════════════════════
def import_items_from_file(file_path, created_by=None):
    db = _write()
    imported_count = 0
    errors = []

    try:
        if file_path.endswith('.csv'):
            df = pd.read_csv(file_path, encoding='utf-8')
        else:
            df = pd.read_excel(file_path)

        for index, row in df.iterrows():
            try:
                name = str(row.get('name', '')).strip()
                barcode = str(row.get('barcode', '')).strip() if pd.notna(row.get('barcode')) else None
                cost_price = float(row.get('cost_price', 0))
                sell_price = float(row.get('sell_price', 0))
                category_name = str(row.get('category', '')).strip() if pd.notna(row.get('category')) else None
                current_stock = float(row.get('current_stock', 0)) if pd.notna(row.get('current_stock')) else 0.0
                min_stock = float(row.get('min_stock', 10)) if pd.notna(row.get('min_stock')) else 10.0
                is_kit_str = str(row.get('is_kit', 'عادي')).strip()
                is_kit = True if 'مدمج' in is_kit_str else False

                if not name:
                    errors.append(f"السطر {index + 1}: اسم الصنف فارغ")
                    continue

                existing = db.query(models.Item).filter(models.Item.name == name).first()
                if existing:
                    errors.append(f"السطر {index + 1}: الصنف '{name}' موجود مسبقاً")
                    continue

                if not barcode:
                    barcode = f"ITEM{random.randint(100000, 999999)}"
                if sell_price == 0.0 and cost_price > 0:
                    sell_price = cost_price * 1.25

                category_id = None
                if category_name:
                    category = db.query(Category).filter(Category.name == category_name).first()
                    if not category:
                        category = Category(name=category_name, is_active=True, created_at=datetime.now())
                        db.add(category)
                        db.flush()
                    category_id = category.id

                item = models.Item(
                    name=name, cost_price=cost_price, sell_price=sell_price,
                    avg_cost_price=cost_price, barcode=barcode, is_kit=is_kit,
                    min_stock=min_stock, category_id=category_id
                )
                db.add(item)
                db.flush()

                if current_stock > 0:
                    db.add(models.InventoryMovement(
                        item_id=item.id, date=datetime.now(),
                        type='in', quantity=current_stock, reference_id=None
                    ))
                    item.total_purchased_qty = current_stock

                imported_count += 1
            except Exception as e:
                db.rollback()
                errors.append(f"السطر {index + 1}: {str(e)}")

        db.commit()
        _after_write()
        return imported_count, errors
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  7. استيراد العملاء/الموردين
# ═══════════════════════════════════════════════════════
def import_parties_from_file(file_path, party_type, created_by=None):
    db = _write()
    imported_count = 0
    errors = []

    try:
        if file_path.endswith('.csv'):
            df = pd.read_csv(file_path, encoding='utf-8')
        else:
            df = pd.read_excel(file_path)

        for index, row in df.iterrows():
            try:
                name = str(row.get('name', '')).strip()
                phone = str(row.get('phone', '')).strip() if pd.notna(row.get('phone')) else None
                address = str(row.get('address', '')).strip() if pd.notna(row.get('address')) else None
                opening_balance = float(row.get('opening_balance', 0)) if pd.notna(row.get('opening_balance')) else 0.0

                if not name:
                    errors.append(f"السطر {index + 1}: الاسم فارغ")
                    continue

                existing = db.query(models.Party).filter(
                    models.Party.name == name, models.Party.type == party_type
                ).first()
                if existing:
                    errors.append(f"السطر {index + 1}: '{name}' موجود مسبقاً")
                    continue

                if party_type == 'customer':
                    parent_account = db.query(models.Account).filter(models.Account.code == "1201").first()
                else:
                    parent_account = db.query(models.Account).filter(models.Account.code == "2101").first()

                if not parent_account:
                    errors.append(f"السطر {index + 1}: الحساب الأب غير موجود")
                    continue

                party_account = models.Account(
                    code=f"{parent_account.code}{db.query(models.Account).count() + 1:04d}",
                    name=name, type=parent_account.type, parent_id=parent_account.id
                )
                db.add(party_account)
                db.flush()

                party = models.Party(
                    name=name, type=party_type, phone=phone, address=address,
                    account_id=party_account.id
                )
                db.add(party)

                if opening_balance != 0.0:
                    opening_account = db.query(models.Account).filter(models.Account.code == "3101").first()
                    if opening_account:
                        journal_entry = models.JournalEntry(
                            date=datetime.now(),
                            description=f"رصيد افتتاحي لـ {party_type}: {name}",
                            reference_type="opening_balance", reference_id=None, created_by=created_by
                        )
                        db.add(journal_entry)
                        db.flush()
                        if party_type == 'customer':
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id,
                                                       debit=opening_balance, credit=0.0))
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id,
                                                       debit=0.0, credit=opening_balance))
                        else:
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id,
                                                       debit=opening_balance, credit=0.0))
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id,
                                                       debit=0.0, credit=opening_balance))

                imported_count += 1
            except Exception as e:
                errors.append(f"السطر {index + 1}: {str(e)}")

        db.commit()
        _after_write()
        return imported_count, errors
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  8. النسخ الاحتياطي (ملفات — لا يعتمد على DB session)
# ═══════════════════════════════════════════════════════
def create_backup(destination_folder):
    from pathlib import Path
    db_path = Path(__file__).parent / "accounting.db"
    if not db_path.exists():
        raise FileNotFoundError("قاعدة البيانات غير موجودة!")
    if not os.path.exists(destination_folder):
        os.makedirs(destination_folder)
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    backup_filename = f"Techtoon_Accounting_Backup_{timestamp}.db"
    backup_path = os.path.join(destination_folder, backup_filename)
    shutil.copy2(str(db_path), backup_path)
    return backup_path


def restore_backup(backup_file_path):
    from pathlib import Path
    if not os.path.exists(backup_file_path):
        raise FileNotFoundError("ملف النسخة الاحتياطية غير موجود!")
    db_path = Path(__file__).parent / "accounting.db"
    if db_path.exists():
        backup_current = str(db_path) + ".before_restore"
        shutil.copy2(str(db_path), backup_current)
    shutil.copy2(backup_file_path, str(db_path))
    return True


# ═══════════════════════════════════════════════════════
#  9. تصدير قالب Excel (بدون DB)
# ═══════════════════════════════════════════════════════
def export_import_template(template_type, output_path=None):
    if not output_path:
        output_path = f"template_{template_type}.xlsx"

    if template_type == 'items':
        df = pd.DataFrame({
            'name': ['ورق A4', 'فايل حفظ', 'طابعة ليزر'],
            'barcode': ['ITEM001', 'ITEM002', 'ITEM003'],
            'cost_price': [50.0, 10.0, 1500.0],
            'sell_price': [75.0, 15.0, 2000.0],
            'is_kit': ['عادي', 'عادي', 'عادي'],
            'category': ['أدوات مكتبية', 'أدوات مكتبية', 'أجهزة'],
            'min_stock': [100, 50, 5],
            'current_stock': [500, 200, 10]
        })
    elif template_type == 'categories':
        df = pd.DataFrame({
            'name': ['أدوات مكتبية', 'أجهزة', 'ورق A4'],
            'description': ['مستلزمات مكتبية', 'أجهزة إلكترونية', 'ورق طباعة'],
            'parent_category': ['', '', 'أدوات مكتبية']
        })
    elif template_type == 'customers':
        df = pd.DataFrame({
            'name': ['شركة الأمل', 'محمد أحمد'],
            'phone': ['01012345678', '01198765432'],
            'address': ['القاهرة', 'الإسكندرية'],
            'opening_balance': [0.0, 1000.0]
        })
    elif template_type == 'suppliers':
        df = pd.DataFrame({
            'name': ['مورد الورق', 'مورد الأثاث'],
            'phone': ['01011111111', '01022222222'],
            'address': ['الجيزة', 'المنصورة'],
            'opening_balance': [0.0, 5000.0]
        })
    else:
        raise ValueError("نوع القالب غير صحيح!")

    with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
        df.to_excel(writer, sheet_name='Template', index=False)
        workbook = writer.book
        worksheet = writer.sheets['Template']
        header_format = workbook.add_format({
            'bold': True, 'bg_color': '#4472C4', 'font_color': 'white', 'border': 1
        })
        for col_num, value in enumerate(df.columns.values):
            worksheet.write(0, col_num, value, header_format)
        for i in range(len(df.columns)):
            worksheet.set_column(i, i, 20)
    return output_path


# ═══════════════════════════════════════════════════════
#  10. اسم المستخدم
# ═══════════════════════════════════════════════════════
def get_user_name_by_id(user_id):
    if user_id is None:
        return "النظام"
    db = _read()
    try:
        user = db.query(models.User).filter(models.User.id == user_id).first()
        return user.full_name if user else "مستخدم غير معروف"
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  11. حساب الربح الفعلي للصنف
# ═══════════════════════════════════════════════════════
def calculate_item_profit(item_id):
    db = _read()
    try:
        item = db.query(models.Item).filter(models.Item.id == item_id).first()
        if not item:
            return None

        sales = db.query(models.InvoiceLine).join(models.Invoice).filter(
            models.InvoiceLine.item_id == item_id, models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled'
        ).all()
        total_sales_qty = sum(s.quantity for s in sales)
        total_sales_revenue = sum(s.total for s in sales)

        purchases = db.query(CostHistory).filter(CostHistory.item_id == item_id).all()
        total_purchase_qty = sum(p.quantity for p in purchases)
        total_purchase_cost = sum(p.total_cost for p in purchases)

        profit = total_sales_revenue - total_purchase_cost
        profit_margin = (profit / total_sales_revenue * 100) if total_sales_revenue > 0 else 0

        return {
            'item': item, 'total_sales_qty': total_sales_qty,
            'total_sales_revenue': total_sales_revenue,
            'total_purchase_qty': total_purchase_qty,
            'total_purchase_cost': total_purchase_cost,
            'profit': profit, 'profit_margin': profit_margin
        }
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  12. تصدير الأصناف إلى Excel
# ═══════════════════════════════════════════════════════
def export_items_to_excel(output_path=None):
    db = _read()
    try:
        if not output_path:
            output_path = f"Items_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"

        items = db.query(models.Item).all()
        categories_map = {c.id: c.name for c in db.query(Category).all()}

        rows = db.query(
            models.InventoryMovement.item_id,
            func.sum(
                case(
                    (models.InventoryMovement.type == 'in', models.InventoryMovement.quantity),
                    else_=-models.InventoryMovement.quantity
                )
            ).label('balance')
        ).group_by(models.InventoryMovement.item_id).all()
        stock_map = {iid: float(b or 0) for iid, b in rows}

        data = []
        for item in items:
            current_stock = stock_map.get(item.id, 0.0)
            data.append({
                'id': item.id, 'name': item.name, 'barcode': item.barcode or '',
                'cost_price': item.cost_price, 'sell_price': item.sell_price,
                'avg_cost_price': item.avg_cost_price,
                'is_kit': 'مدمج' if item.is_kit else 'عادي',
                'category': categories_map.get(item.category_id, ''),
                'min_stock': item.min_stock, 'current_stock': current_stock,
                'total_purchased_qty': item.total_purchased_qty
            })

        df = pd.DataFrame(data)
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            df.to_excel(writer, sheet_name='الأصناف', index=False)
            wb = writer.book
            ws = writer.sheets['الأصناف']
            hf = wb.add_format({'bold': True, 'bg_color': '#4472C4', 'font_color': 'white', 'border': 1})
            for i, v in enumerate(df.columns.values):
                ws.write(0, i, v, hf)
            for i in range(len(df.columns)):
                ws.set_column(i, i, 20)
        return output_path, len(items)
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  13. تصدير التصنيفات إلى Excel
# ═══════════════════════════════════════════════════════
def export_categories_to_excel(output_path=None):
    db = _read()
    try:
        if not output_path:
            output_path = f"Categories_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"

        categories = db.query(Category).all()
        cat_map = {c.id: c.name for c in categories}
        item_counts = db.query(
            models.Item.category_id, func.count(models.Item.id)
        ).group_by(models.Item.category_id).all()
        count_map = {cid: cnt for cid, cnt in item_counts}

        data = [{
            'id': cat.id, 'name': cat.name,
            'description': cat.description or '',
            'parent_category': cat_map.get(cat.parent_id, ''),
            'is_active': 'نشط' if cat.is_active else 'معطل',
            'items_count': count_map.get(cat.id, 0)
        } for cat in categories]

        df = pd.DataFrame(data)
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            df.to_excel(writer, sheet_name='التصنيفات', index=False)
            wb = writer.book
            ws = writer.sheets['التصنيفات']
            hf = wb.add_format({'bold': True, 'bg_color': '#4472C4', 'font_color': 'white', 'border': 1})
            for i, v in enumerate(df.columns.values):
                ws.write(0, i, v, hf)
            for i in range(len(df.columns)):
                ws.set_column(i, i, 20)
        return output_path, len(categories)
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  14. تصدير العملاء/الموردين إلى Excel
# ═══════════════════════════════════════════════════════
def export_parties_to_excel(party_type, output_path=None):
    db = _read()
    try:
        if not output_path:
            type_ar = 'Customers' if party_type == 'customer' else 'Suppliers'
            output_path = f"{type_ar}_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"

        parties = db.query(models.Party).filter(models.Party.type == party_type).all()
        party_ids = [p.id for p in parties]

        accounts_map = {}
        if party_ids:
            account_ids = list({p.account_id for p in parties if p.account_id})
            if account_ids:
                accs = db.query(models.Account).filter(models.Account.id.in_(account_ids)).all()
                accounts_map = {a.id: a.code for a in accs}

        inv_type = 'sale' if party_type == 'customer' else 'purchase'
        invoices_agg = db.query(
            models.Invoice.party_id,
            func.sum(models.Invoice.net_amount).label('total')
        ).filter(
            models.Invoice.party_id.in_(party_ids),
            models.Invoice.type == inv_type
        ).group_by(models.Invoice.party_id).all()
        invoiced_map = {pid: float(t or 0) for pid, t in invoices_agg}

        pay_type = 'receipt' if party_type == 'customer' else 'payment'
        payments_agg = db.query(
            models.Payment.party_id,
            func.sum(models.Payment.amount).label('total')
        ).filter(
            models.Payment.party_id.in_(party_ids),
            models.Payment.payment_type == pay_type
        ).group_by(models.Payment.party_id).all()
        paid_map = {pid: float(t or 0) for pid, t in payments_agg}

        data = []
        for party in parties:
            ti = invoiced_map.get(party.id, 0.0)
            tp = paid_map.get(party.id, 0.0)
            data.append({
                'id': party.id, 'name': party.name,
                'phone': party.phone or '', 'address': party.address or '',
                'account_code': accounts_map.get(party.account_id, ''),
                'credit_limit': party.credit_limit,
                'total_invoiced': ti, 'total_paid': tp, 'balance': ti - tp
            })

        df = pd.DataFrame(data)
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            sheet_name = 'العملاء' if party_type == 'customer' else 'الموردين'
            df.to_excel(writer, sheet_name=sheet_name, index=False)
            wb = writer.book
            ws = writer.sheets[sheet_name]
            hf = wb.add_format({'bold': True, 'bg_color': '#4472C4', 'font_color': 'white', 'border': 1})
            for i, v in enumerate(df.columns.values):
                ws.write(0, i, v, hf)
            for i in range(len(df.columns)):
                ws.set_column(i, i, 20)
        return output_path, len(parties)
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  15. استيراد التصنيفات
# ═══════════════════════════════════════════════════════
def import_categories_from_file(file_path, created_by=None):
    db = _write()
    imported_count = 0
    errors = []
    try:
        if file_path.endswith('.csv'):
            df = pd.read_csv(file_path, encoding='utf-8')
        else:
            df = pd.read_excel(file_path)

        for index, row in df.iterrows():
            try:
                name = str(row.get('name', '')).strip()
                description = str(row.get('description', '')).strip() if pd.notna(row.get('description')) else None
                parent_name = str(row.get('parent_category', '')).strip() if pd.notna(row.get('parent_category')) else None

                if not name:
                    errors.append(f"السطر {index + 1}: اسم التصنيف فارغ")
                    continue

                existing = db.query(Category).filter(Category.name == name).first()
                if existing:
                    errors.append(f"السطر {index + 1}: التصنيف '{name}' موجود مسبقاً")
                    continue

                parent_id = None
                if parent_name:
                    parent = db.query(Category).filter(Category.name == parent_name).first()
                    if parent:
                        parent_id = parent.id
                    else:
                        errors.append(f"السطر {index + 1}: التصنيف الأب '{parent_name}' غير موجود")
                        continue

                db.add(Category(
                    name=name, description=description,
                    parent_id=parent_id, is_active=True, created_at=datetime.now()
                ))
                imported_count += 1
            except Exception as e:
                errors.append(f"السطر {index + 1}: {str(e)}")

        db.commit()
        _after_write()
        return imported_count, errors
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  16. المصروفات
# ═══════════════════════════════════════════════════════
def create_expense_category(name, description, account_id, created_by=None):
    db = _write()
    try:
        existing = db.query(ExpenseCategory).filter(ExpenseCategory.name == name).first()
        if existing:
            raise ValueError(f"تصنيف '{name}' موجود مسبقاً!")

        category = ExpenseCategory(
            name=name, description=description, account_id=account_id,
            is_active=True, created_at=datetime.now()
        )
        db.add(category)
        db.commit()
        db.refresh(category)
        _after_write()
        return category
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def create_expense(category_id, amount, description, payment_method,
                   cash_box_id, reference_number=None, notes=None,
                   expense_date=None, created_by=None):
    db = _write()
    try:
        check_period_open(expense_date or datetime.now(), entity="تسجيل مصروف")
        period = get_period_for_date(expense_date or datetime.now())

        category = db.query(ExpenseCategory).filter(ExpenseCategory.id == category_id).first()
        if not category:
            raise ValueError("تصنيف المصروف غير موجود!")

        cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
        if not cash_box:
            raise ValueError("الخزينة المحددة غير موجودة!")

        if expense_date is None:
            expense_date = datetime.now()

        expense = Expense(
            date=expense_date, category_id=category_id, amount=amount,
            description=description, payment_method=payment_method,
            cash_box_id=cash_box_id, reference_number=reference_number,
            notes=notes, created_by=created_by
        )
        db.add(expense)

        journal_entry = models.JournalEntry(
            date=expense_date,
            description=f"مصروف: {description} ({category.name})",
            reference_type="expense", reference_id=None, created_by=created_by,
            period_id=period.id if period else None
        )
        db.add(journal_entry)
        db.flush()

        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=category.account_id,
                                   debit=amount, credit=0.0))
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id,
                                   debit=0.0, credit=amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)
        db.commit()
        _after_write()
        return expense
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_expenses_summary(start_date=None, end_date=None):
    db = _read()
    try:
        query = db.query(
            ExpenseCategory.name,
            func.sum(Expense.amount).label('total')
        ).join(Expense, ExpenseCategory.id == Expense.category_id)

        if start_date:
            query = query.filter(Expense.date >= start_date)
        if end_date:
            query = query.filter(Expense.date <= end_date)

        results = query.group_by(ExpenseCategory.name).all()
        total_expenses = sum(r.total for r in results) if results else 0.0
        return {'by_category': results, 'total': total_expenses}
    finally:
        db.close()


def delete_expense(expense_id):
    db = _write()
    try:
        expense = db.query(Expense).filter(Expense.id == expense_id).first()
        if not expense:
            raise ValueError("المصروف غير موجود!")

        if expense.date:
            check_period_open(expense.date, entity="حذف مصروف")

        journal_entry = db.query(models.JournalEntry).filter(
            models.JournalEntry.reference_type == "expense",
            models.JournalEntry.description.contains(expense.description)
        ).first()

        if journal_entry:
            db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == journal_entry.id
            ).delete()
            db.delete(journal_entry)

        db.delete(expense)
        db.commit()
        _after_write()
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  17. المخازن
# ═══════════════════════════════════════════════════════
def create_warehouse(name, code, location=None, responsible_person=None, notes=None):
    db = _write()
    try:
        existing = db.query(Warehouse).filter(Warehouse.code == code).first()
        if existing:
            raise ValueError(f"كود المخزن '{code}' موجود مسبقاً!")

        warehouse = Warehouse(
            name=name, code=code, location=location,
            responsible_person=responsible_person, notes=notes, is_active=True
        )
        db.add(warehouse)
        db.commit()
        db.refresh(warehouse)
        _after_write()
        return warehouse
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_stock_level(warehouse_id, item_id, db=None):
    """رصيد المخزون — يقبل db اختياري للاستخدام داخل معاملة كتابة."""
    _owned = db is None
    if _owned:
        db = _read()
    try:
        stock = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.item_id == item_id
        ).first()
        return stock.quantity if stock else 0.0
    finally:
        if _owned:
            db.close()


def update_stock_level(warehouse_id, item_id, quantity, db=None):
    """تحديث المخزون — يقبل db اختياري."""
    _owned = db is None
    if _owned:
        db = _write()
    try:
        stock = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.item_id == item_id
        ).first()

        if stock:
            stock.quantity = quantity
            stock.last_updated = datetime.now()
        else:
            db.add(StockLevel(
                warehouse_id=warehouse_id, item_id=item_id, quantity=quantity
            ))

        if _owned:
            db.commit()
            _after_write()
    except Exception:
        if _owned:
            db.rollback()
        raise
    finally:
        if _owned:
            db.close()


def transfer_between_warehouses(from_warehouse_id, to_warehouse_id, item_id, quantity,
                                 notes=None, created_by=None):
    db = _write()
    try:
        if from_warehouse_id == to_warehouse_id:
            raise ValueError("لا يمكن التحويل بين نفس المخزن!")

        # ⭐ كلها بنفس db (نفس المعاملة)
        current_stock = get_stock_level(from_warehouse_id, item_id, db=db)
        if current_stock < quantity:
            raise ValueError(f"الرصيد غير كافٍ! الرصيد الحالي: {current_stock}")

        transfer = WarehouseTransfer(
            date=datetime.now(),
            from_warehouse_id=from_warehouse_id,
            to_warehouse_id=to_warehouse_id,
            item_id=item_id, quantity=quantity,
            notes=notes, created_by=created_by, status='completed'
        )
        db.add(transfer)

        update_stock_level(from_warehouse_id, item_id, current_stock - quantity, db=db)
        to_stock = get_stock_level(to_warehouse_id, item_id, db=db)
        update_stock_level(to_warehouse_id, item_id, to_stock + quantity, db=db)

        db.commit()
        _after_write()
        return transfer
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def create_stock_count(warehouse_id, item_id, counted_quantity, counted_by=None, notes=None):
    db = _write()
    try:
        system_quantity = get_stock_level(warehouse_id, item_id, db=db)
        difference = counted_quantity - system_quantity

        stock_count = StockCount(
            date=datetime.now(), warehouse_id=warehouse_id, item_id=item_id,
            system_quantity=system_quantity, counted_quantity=counted_quantity,
            difference=difference, notes=notes, counted_by=counted_by
        )
        db.add(stock_count)
        update_stock_level(warehouse_id, item_id, counted_quantity, db=db)

        db.commit()
        _after_write()
        return stock_count
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_warehouse_stock_report(warehouse_id):
    db = _read()
    try:
        stocks = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.quantity > 0
        ).all()

        item_ids = [s.item_id for s in stocks]
        items_map = {}
        if item_ids:
            items_map = {it.id: it for it in db.query(models.Item).filter(
                models.Item.id.in_(item_ids)
            ).all()}

        report = []
        for stock in stocks:
            item = items_map.get(stock.item_id)
            if item:
                report.append({
                    'item_id': item.id, 'item_name': item.name,
                    'barcode': item.barcode, 'quantity': stock.quantity,
                    'min_stock': item.min_stock,
                    'status': 'منخفض' if stock.quantity <= item.min_stock else 'جيد'
                })
        return report
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  18. الأصول الثابتة
# ═══════════════════════════════════════════════════════
def create_fixed_asset(name, code, category, purchase_date, purchase_cost,
                        salvage_value, useful_life_years, depreciation_method,
                        location=None, responsible_person=None, notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(FixedAsset).filter(FixedAsset.code == code).first()
        if existing:
            raise ValueError(f"كود الأصل '{code}' موجود مسبقاً!")

        asset = FixedAsset(
            name=name, code=code, category=category,
            purchase_date=purchase_date, purchase_cost=purchase_cost,
            salvage_value=salvage_value, useful_life_years=useful_life_years,
            depreciation_method=depreciation_method,
            accumulated_depreciation=0.0, net_book_value=purchase_cost,
            location=location, responsible_person=responsible_person,
            notes=notes, status='active', created_by=created_by
        )
        db.add(asset)
        db.commit()
        db.refresh(asset)
        _after_write()
        return asset
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def calculate_annual_depreciation(asset):
    depreciable_amount = asset.purchase_cost - asset.salvage_value
    if asset.depreciation_method == DepreciationMethod.STRAIGHT_LINE:
        return depreciable_amount / asset.useful_life_years
    elif asset.depreciation_method == DepreciationMethod.DECLINING_BALANCE:
        rate = 2.0 / asset.useful_life_years
        return asset.net_book_value * rate
    return 0.0


def run_annual_depreciation(asset_id, year, created_by=None):
    db = _write()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            raise ValueError("الأصل غير موجود!")
        if asset.status != 'active':
            raise ValueError("الأصل غير نشط!")

        year_end = datetime(year, 12, 31)
        check_period_open(year_end, entity="تسجيل إهلاك سنوي")

        year_expr = _year_expr(db, DepreciationRecord.date)
        existing = db.query(DepreciationRecord).filter(
            DepreciationRecord.asset_id == asset_id,
            year_expr == str(year)
        ).first()
        if existing:
            raise ValueError(f"تم تسجيل إهلاك سنة {year} مسبقاً!")

        depreciation_amount = calculate_annual_depreciation(asset)
        if asset.net_book_value - depreciation_amount < asset.salvage_value:
            depreciation_amount = asset.net_book_value - asset.salvage_value

        asset.accumulated_depreciation += depreciation_amount
        asset.net_book_value -= depreciation_amount

        record = DepreciationRecord(
            asset_id=asset_id, date=year_end,
            depreciation_amount=depreciation_amount,
            accumulated_depreciation=asset.accumulated_depreciation,
            net_book_value=asset.net_book_value,
            notes=f"إهلاك سنة {year}", created_by=created_by
        )
        db.add(record)

        journal_entry = models.JournalEntry(
            date=year_end,
            description=f"إهلاك سنوي {year} - {asset.name} ({asset.code})",
            reference_type="depreciation", reference_id=asset_id, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        dep_exp_acc = db.query(models.Account).filter(models.Account.code == "5201").first()
        accum_dep_acc = db.query(models.Account).filter(models.Account.code == "1501").first()

        if dep_exp_acc and accum_dep_acc:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=dep_exp_acc.id,
                                       debit=depreciation_amount, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=accum_dep_acc.id,
                                       debit=0.0, credit=depreciation_amount))
            db.flush()
            validate_journal_entry(db, journal_entry.id)

        db.commit()
        _after_write()
        return record
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_asset_depreciation_schedule(asset_id):
    db = _read()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            return None

        schedule = []
        current_value = asset.purchase_cost
        annual_dep = (asset.purchase_cost - asset.salvage_value) / asset.useful_life_years

        for year in range(1, asset.useful_life_years + 1):
            depreciation = annual_dep
            if current_value - depreciation < asset.salvage_value:
                depreciation = current_value - asset.salvage_value
            current_value -= depreciation
            schedule.append({
                'السنة': year, 'مبلغ الإهلاك': depreciation,
                'مجمع الإهلاك': asset.purchase_cost - current_value,
                'صافي القيمة الدفترية': current_value
            })
        return {'asset': asset, 'schedule': schedule}
    finally:
        db.close()


def dispose_asset(asset_id, disposal_date, disposal_value, notes=None, created_by=None):
    db = _write()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            raise ValueError("الأصل غير موجود!")

        check_period_open(disposal_date, entity="التخلص من أصل")

        gain_loss = disposal_value - asset.net_book_value
        asset.status = 'sold' if disposal_value > 0 else 'scrapped'

        journal_entry = models.JournalEntry(
            date=disposal_date,
            description=f"التخلص من الأصل: {asset.name} ({asset.code})",
            reference_type="asset_disposal", reference_id=asset_id, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        fa_acc = db.query(models.Account).filter(models.Account.code == "1500").first()
        accum_dep_acc = db.query(models.Account).filter(models.Account.code == "1501").first()

        if fa_acc and accum_dep_acc:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=accum_dep_acc.id,
                                       debit=asset.accumulated_depreciation, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=fa_acc.id,
                                       debit=0.0, credit=asset.purchase_cost))

        db.commit()
        _after_write()
        return {'asset': asset, 'gain_loss': gain_loss}
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  19. تحليل الربحية
# ═══════════════════════════════════════════════════════
def get_profitability_by_item(start_date=None, end_date=None):
    db = _read()
    try:
        query = db.query(
            models.Item.id, models.Item.name, models.Item.category_id,
            func.sum(models.InvoiceLine.quantity).label('total_qty'),
            func.sum(models.InvoiceLine.total).label('total_revenue'),
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price).label('total_cost')
        ).join(
            models.InvoiceLine, models.Item.id == models.InvoiceLine.item_id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale', models.Invoice.status != 'cancelled',
            models.Item.is_kit == False
        )
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)

        results = query.group_by(
            models.Item.id, models.Item.name, models.Item.category_id
        ).order_by(
            func.sum(models.InvoiceLine.total * (1 - models.InvoiceLine.cost_price / models.InvoiceLine.price)).desc()
        ).all()

        cat_ids = list({r.category_id for r in results if r.category_id})
        cats_map = {}
        if cat_ids:
            cats_map = {c.id: c.name for c in db.query(Category).filter(
                Category.id.in_(cat_ids)
            ).all()}

        data = []
        for r in results:
            profit = r.total_revenue - r.total_cost
            margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            data.append({
                'item_id': r.id, 'item_name': r.name,
                'category': cats_map.get(r.category_id, 'بدون'),
                'quantity_sold': r.total_qty, 'revenue': r.total_revenue,
                'cost': r.total_cost, 'profit': profit, 'profit_margin': margin
            })
        return data
    finally:
        db.close()


def get_profitability_by_customer(start_date=None, end_date=None):
    db = _read()
    try:
        query = db.query(
            models.Party.id, models.Party.name,
            func.sum(models.Invoice.net_amount).label('total_revenue'),
            func.count(models.Invoice.id).label('invoice_count')
        ).join(
            models.Invoice, models.Party.id == models.Invoice.party_id
        ).filter(
            models.Invoice.type == 'sale', models.Invoice.status != 'cancelled',
            models.Party.type == 'customer'
        )
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)

        results = query.group_by(models.Party.id, models.Party.name).order_by(
            func.sum(models.Invoice.net_amount).desc()
        ).all()

        data = []
        for r in results:
            cost_q = db.query(
                func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)
            ).join(
                models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
            ).filter(
                models.Invoice.party_id == r.id,
                models.Invoice.type == 'sale',
                models.Invoice.status != 'cancelled'
            )
            if start_date:
                cost_q = cost_q.filter(models.Invoice.date >= start_date)
            if end_date:
                cost_q = cost_q.filter(models.Invoice.date <= end_date)
            tc = cost_q.scalar() or 0.0
            profit = r.total_revenue - tc
            margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            data.append({
                'customer_id': r.id, 'customer_name': r.name,
                'invoice_count': r.invoice_count,
                'revenue': r.total_revenue, 'cost': tc,
                'profit': profit, 'profit_margin': margin
            })
        return data
    finally:
        db.close()


def get_profitability_by_category(start_date=None, end_date=None):
    db = _read()
    try:
        query = db.query(
            Category.id, Category.name,
            func.sum(models.InvoiceLine.quantity).label('total_qty'),
            func.sum(models.InvoiceLine.total).label('total_revenue'),
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price).label('total_cost')
        ).join(
            models.Item, Category.id == models.Item.category_id
        ).join(
            models.InvoiceLine, models.Item.id == models.InvoiceLine.item_id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale', models.Invoice.status != 'cancelled'
        )
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)

        results = query.group_by(Category.id, Category.name).order_by(
            func.sum(models.InvoiceLine.total).desc()
        ).all()

        data = []
        for r in results:
            profit = r.total_revenue - r.total_cost
            margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            data.append({
                'category_id': r.id, 'category_name': r.name,
                'quantity_sold': r.total_qty,
                'revenue': r.total_revenue, 'cost': r.total_cost,
                'profit': profit, 'profit_margin': margin
            })
        return data
    finally:
        db.close()




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


def get_monthly_profitability(year=None):
    db = _read()
    try:
        if year is None:
            year = datetime.now().year

        month_expr_inv = _month_expr(db, models.Invoice.date)
        year_expr_inv = _year_expr(db, models.Invoice.date)

        revenue_rows = db.query(
            month_expr_inv.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr_inv == str(year),
        ).group_by(month_expr_inv).all()
        revenue_by_month = {r.month: float(r.revenue or 0) for r in revenue_rows}

        month_expr_line = _month_expr(db, models.Invoice.date)
        year_expr_line = _year_expr(db, models.Invoice.date)

        invoice_lines = db.query(
            month_expr_line.label('month'),
            models.InvoiceLine.quantity, models.InvoiceLine.cost_price,
            models.Item.id.label('item_id'), models.Item.is_kit,
            models.Item.cost_price.label('item_cost'),
            models.Item.avg_cost_price,
        ).join(
            models.Item, models.InvoiceLine.item_id == models.Item.id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr_line == str(year),
        ).all()

        cost_by_month = {}
        for row in invoice_lines:
            month = row.month
            qty = float(row.quantity or 0)
            if row.is_kit:
                unit_cost = 0.0
                components = db.query(models.KitComponent).filter(
                    models.KitComponent.kit_item_id == row.item_id
                ).all()
                for comp in components:
                    comp_item = db.query(models.Item).filter(
                        models.Item.id == comp.component_item_id
                    ).first()
                    if comp_item:
                        unit_cost += float(comp.quantity or 0) * float(comp_item.cost_price or 0)
            else:
                unit_cost = float(row.cost_price or 0) or float(row.item_cost or 0) or float(row.avg_cost_price or 0)
            cost_by_month[month] = cost_by_month.get(month, 0.0) + (qty * unit_cost)

        all_months = sorted(set(list(revenue_by_month.keys()) + list(cost_by_month.keys())))
        monthly_data = []
        for m in all_months:
            revenue = revenue_by_month.get(m, 0.0)
            cost = cost_by_month.get(m, 0.0)
            profit = revenue - cost
            margin = (profit / revenue * 100) if revenue > 0 else 0
            monthly_data.append({
                'month': m,
                'month_name': datetime.strptime(m, '%m').strftime('%B'),
                'revenue': revenue, 'cost': cost, 'profit': profit,
                'profit_margin': margin,
            })
        return monthly_data
    finally:
        db.close()


def compare_periods(period1_start, period1_end, period2_start, period2_end):
    db = _read()
    try:
        def get_period_data(start, end):
            revenue = db.query(func.sum(models.Invoice.net_amount)).filter(
                models.Invoice.type == 'sale',
                models.Invoice.status != 'cancelled',
                models.Invoice.date >= start,
                models.Invoice.date <= end
            ).scalar() or 0.0

            cost = db.query(
                func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)
            ).join(
                models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
            ).filter(
                models.Invoice.type == 'sale',
                models.Invoice.status != 'cancelled',
                models.Invoice.date >= start,
                models.Invoice.date <= end
            ).scalar() or 0.0

            profit = revenue - cost
            margin = (profit / revenue * 100) if revenue > 0 else 0
            return {'revenue': revenue, 'cost': cost, 'profit': profit, 'profit_margin': margin}

        p1 = get_period_data(period1_start, period1_end)
        p2 = get_period_data(period2_start, period2_end)

        rev_change = ((p1['revenue'] - p2['revenue']) / p2['revenue'] * 100) if p2['revenue'] > 0 else 0
        prof_change = ((p1['profit'] - p2['profit']) / p2['profit'] * 100) if p2['profit'] > 0 else 0

        return {
            'period1': p1, 'period2': p2,
            'revenue_change': rev_change, 'profit_change': prof_change
        }
    finally:
        db.close()


def get_top_profitable_items(limit=10, start_date=None, end_date=None):
    db = _read()
    try:
        query = db.query(
            models.Item.name,
            func.sum(models.InvoiceLine.quantity).label('qty'),
            func.sum(models.InvoiceLine.total).label('revenue'),
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price).label('cost')
        ).join(
            models.InvoiceLine, models.Item.id == models.InvoiceLine.item_id
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Item.is_kit == False
        )
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)

        results = query.group_by(models.Item.name).order_by(
            func.sum(models.InvoiceLine.total - (models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).desc()
        ).limit(limit).all()

        out = []
        for r in results:
            profit = r.revenue - r.cost
            margin = (profit / r.revenue * 100) if r.revenue > 0 else 0
            out.append({
                'item_name': r.name, 'quantity': r.qty,
                'revenue': r.revenue, 'cost': r.cost,
                'profit': profit, 'profit_margin': margin
            })
        return out
    finally:
        db.close()


def get_profitability_summary(start_date=None, end_date=None):
    db = _read()
    try:
        revenue = db.query(func.sum(models.Invoice.net_amount)).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled'
        )
        if start_date:
            revenue = revenue.filter(models.Invoice.date >= start_date)
        if end_date:
            revenue = revenue.filter(models.Invoice.date <= end_date)
        total_revenue = revenue.scalar() or 0.0

        cost = db.query(
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)
        ).join(
            models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
        ).filter(
            models.Invoice.type == 'sale', models.Invoice.status != 'cancelled'
        )
        if start_date:
            cost = cost.filter(models.Invoice.date >= start_date)
        if end_date:
            cost = cost.filter(models.Invoice.date <= end_date)
        total_cost = cost.scalar() or 0.0

        expenses = db.query(func.sum(Expense.amount))
        if start_date:
            expenses = expenses.filter(Expense.date >= start_date)
        if end_date:
            expenses = expenses.filter(Expense.date <= end_date)
        total_expenses = expenses.scalar() or 0.0

        gp = total_revenue - total_cost
        np_ = gp - total_expenses
        return {
            'revenue': total_revenue, 'cost': total_cost,
            'gross_profit': gp, 'expenses': total_expenses,
            'net_profit': np_,
            'gross_margin': (gp / total_revenue * 100) if total_revenue > 0 else 0,
            'net_margin': (np_ / total_revenue * 100) if total_revenue > 0 else 0
        }
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  20. القروض
# ═══════════════════════════════════════════════════════
def calculate_loan_installments(principal_amount, annual_interest_rate, term_months):
    monthly_rate = annual_interest_rate / 100 / 12
    if monthly_rate == 0:
        monthly_payment = principal_amount / term_months
        total_interest = 0
    else:
        monthly_payment = principal_amount * (
            monthly_rate * (1 + monthly_rate) ** term_months
        ) / ((1 + monthly_rate) ** term_months - 1)
        total_interest = (monthly_payment * term_months) - principal_amount
    return monthly_payment, total_interest


def generate_installment_schedule(principal_amount, annual_interest_rate, term_months, loan_date):
    monthly_payment, total_interest = calculate_loan_installments(
        principal_amount, annual_interest_rate, term_months
    )
    monthly_rate = annual_interest_rate / 100 / 12
    schedule = []
    remaining = principal_amount
    for i in range(1, term_months + 1):
        interest = remaining * monthly_rate
        principal = monthly_payment - interest
        remaining -= principal
        schedule.append({
            'installment_number': i,
            'due_date': loan_date + relativedelta(months=i),
            'principal_amount': principal,
            'interest_amount': interest,
            'total_amount': monthly_payment,
            'remaining_balance': max(0, remaining)
        })
    return schedule, monthly_payment, total_interest


def create_loan(loan_type, borrower_name, borrower_type, principal_amount,
                annual_interest_rate, loan_date, term_months,
                cash_box_id=None, notes=None, created_by=None):
    db = _write()
    try:
        check_period_open(loan_date, entity="إنشاء قرض")

        loan_number = f"LOAN-{db.query(Loan).count() + 1:06d}"
        schedule, monthly_payment, total_interest = generate_installment_schedule(
            principal_amount, annual_interest_rate, term_months, loan_date
        )
        total_amount = principal_amount + total_interest

        loan = Loan(
            loan_number=loan_number, loan_type=loan_type,
            borrower_name=borrower_name, borrower_type=borrower_type,
            principal_amount=principal_amount, interest_rate=annual_interest_rate,
            loan_date=loan_date,
            maturity_date=loan_date + relativedelta(months=term_months),
            term_months=term_months, installment_amount=monthly_payment,
            total_interest=total_interest, total_amount=total_amount,
            paid_amount=0.0, remaining_amount=total_amount,
            status=LoanStatus.ACTIVE, cash_box_id=cash_box_id,
            notes=notes, created_by=created_by
        )
        db.add(loan)
        db.flush()

        for inst in schedule:
            db.add(LoanInstallment(
                loan_id=loan.id,
                installment_number=inst['installment_number'],
                due_date=inst['due_date'],
                principal_amount=inst['principal_amount'],
                interest_amount=inst['interest_amount'],
                total_amount=inst['total_amount'],
                paid_amount=0.0, status='pending'
            ))

        journal_entry = models.JournalEntry(
            date=loan_date,
            description=f"قرض {loan_type.value} رقم {loan_number} - {borrower_name}",
            reference_type="loan", reference_id=loan.id, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        if loan_type == LoanType.RECEIVED:
            if cash_box_id:
                cb = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cb:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cb.account_id,
                                               debit=principal_amount, credit=0.0))
            loans_payable = db.query(models.Account).filter(models.Account.code == "2301").first()
            if loans_payable:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=loans_payable.id,
                                           debit=0.0, credit=principal_amount))
        else:
            loans_recv = db.query(models.Account).filter(models.Account.code == "1250").first()
            if loans_recv:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=loans_recv.id,
                                           debit=principal_amount, credit=0.0))
            if cash_box_id:
                cb = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cb:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cb.account_id,
                                               debit=0.0, credit=principal_amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)
        db.commit()
        _after_write()
        return loan
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def pay_loan_installment(installment_id, payment_date=None, cash_box_id=None,
                          notes=None, created_by=None):
    db = _write()
    try:
        installment = db.query(LoanInstallment).filter(
            LoanInstallment.id == installment_id
        ).first()
        if not installment:
            raise ValueError("القسط غير موجود!")
        if installment.status == 'paid':
            raise ValueError("هذا القسط مدفوع مسبقاً!")

        if payment_date is None:
            payment_date = datetime.now()
        check_period_open(payment_date, entity="سداد قسط")

        installment.paid_amount = installment.total_amount
        installment.paid_date = payment_date
        installment.status = 'paid'

        loan = db.query(Loan).filter(Loan.id == installment.loan_id).first()
        loan.paid_amount += installment.total_amount
        loan.remaining_amount -= installment.total_amount
        if loan.remaining_amount <= 0.01:
            loan.status = LoanStatus.COMPLETED
            loan.remaining_amount = 0.0

        journal_entry = models.JournalEntry(
            date=payment_date,
            description=f"سداد قسط {installment.installment_number} - قرض {loan.loan_number}",
            reference_type="loan_payment", reference_id=loan.id, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        if loan.loan_type == LoanType.RECEIVED:
            loans_payable = db.query(models.Account).filter(models.Account.code == "2301").first()
            if loans_payable:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=loans_payable.id,
                                           debit=installment.principal_amount, credit=0.0))
            int_exp = db.query(models.Account).filter(models.Account.code == "5301").first()
            if int_exp and installment.interest_amount > 0:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=int_exp.id,
                                           debit=installment.interest_amount, credit=0.0))
            if cash_box_id:
                cb = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cb:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cb.account_id,
                                               debit=0.0, credit=installment.total_amount))
        else:
            if cash_box_id:
                cb = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cb:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cb.account_id,
                                               debit=installment.total_amount, credit=0.0))
            loans_recv = db.query(models.Account).filter(models.Account.code == "1250").first()
            if loans_recv:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=loans_recv.id,
                                           debit=0.0, credit=installment.principal_amount))
            int_inc = db.query(models.Account).filter(models.Account.code == "4201").first()
            if int_inc and installment.interest_amount > 0:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=int_inc.id,
                                           debit=0.0, credit=installment.interest_amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)
        db.commit()
        _after_write()
        return installment
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_loans_summary(loan_type=None):
    db = _read()
    try:
        query = db.query(Loan)
        if loan_type:
            query = query.filter(Loan.loan_type == loan_type)
        loans = query.all()

        return {
            'loans': loans,
            'total_principal': sum(l.principal_amount for l in loans),
            'total_interest': sum(l.total_interest for l in loans),
            'total_amount': sum(l.total_amount for l in loans),
            'total_paid': sum(l.paid_amount for l in loans),
            'total_remaining': sum(l.remaining_amount for l in loans),
            'active_count': len([l for l in loans if l.status == LoanStatus.ACTIVE]),
            'completed_count': len([l for l in loans if l.status == LoanStatus.COMPLETED])
        }
    finally:
        db.close()


def get_loan_schedule(loan_id):
    db = _read()
    try:
        loan = db.query(Loan).filter(Loan.id == loan_id).first()
        if not loan:
            return None

        installments = db.query(LoanInstallment).filter(
            LoanInstallment.loan_id == loan_id
        ).order_by(LoanInstallment.installment_number).all()

        schedule = [{
            'id': i.id, 'number': i.installment_number,
            'due_date': i.due_date, 'principal': i.principal_amount,
            'interest': i.interest_amount, 'total': i.total_amount,
            'paid': i.paid_amount, 'paid_date': i.paid_date, 'status': i.status
        } for i in installments]
        return {'loan': loan, 'schedule': schedule}
    finally:
        db.close()


def check_overdue_installments():
    db = _read()
    try:
        today = datetime.now()
        overdue = db.query(LoanInstallment).join(Loan).filter(
            LoanInstallment.status == 'pending',
            LoanInstallment.due_date < today,
            Loan.status == LoanStatus.ACTIVE
        ).all()

        out = []
        for inst in overdue:
            loan = db.query(Loan).filter(Loan.id == inst.loan_id).first()
            days = (today - inst.due_date).days
            out.append({
                'loan_number': loan.loan_number,
                'borrower': loan.borrower_name,
                'loan_type': loan.loan_type.value,
                'installment_number': inst.installment_number,
                'due_date': inst.due_date,
                'amount': inst.total_amount,
                'days_overdue': days
            })
        return out
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  21. الموظفين والرواتب
# ═══════════════════════════════════════════════════════
def create_employee(employee_code, full_name, job_title, hire_date, basic_salary,
                    national_id=None, phone=None, email=None, address=None,
                    department=None, allowances=0.0, deductions=0.0,
                    social_insurance=0.0, tax_rate=0.0,
                    bank_account=None, bank_name=None,
                    notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(Employee).filter(Employee.employee_code == employee_code).first()
        if existing:
            raise ValueError(f"كود الموظف '{employee_code}' موجود مسبقاً!")

        emp = Employee(
            employee_code=employee_code, full_name=full_name,
            national_id=national_id, phone=phone, email=email, address=address,
            job_title=job_title, department=department, hire_date=hire_date,
            basic_salary=basic_salary, allowances=allowances, deductions=deductions,
            social_insurance=social_insurance, tax_rate=tax_rate,
            bank_account=bank_account, bank_name=bank_name,
            status=EmploymentStatus.ACTIVE, notes=notes, created_by=created_by
        )
        db.add(emp)
        db.commit()
        db.refresh(emp)
        _after_write()
        return emp
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def calculate_monthly_salary(employee_id, month, year, overtime=0.0, bonus=0.0, other_deductions=0.0):
    db = _read()
    try:
        emp = db.query(Employee).filter(Employee.id == employee_id).first()
        if not emp:
            raise ValueError("الموظف غير موجود!")
        if emp.status != EmploymentStatus.ACTIVE:
            raise ValueError("الموظف غير نشط!")

        gross = emp.basic_salary + emp.allowances + overtime + bonus
        si = gross * (emp.social_insurance / 100)
        tax = gross * (emp.tax_rate / 100)
        total_ded = si + tax + emp.deductions + other_deductions
        net = gross - total_ded

        return {
            'employee': emp, 'month': month, 'year': year,
            'basic_salary': emp.basic_salary, 'allowances': emp.allowances,
            'overtime': overtime, 'bonus': bonus, 'gross_salary': gross,
            'social_insurance': si, 'tax': tax,
            'other_deductions': other_deductions,
            'total_deductions': total_ded, 'net_salary': net
        }
    finally:
        db.close()


def process_monthly_salary(employee_id, month, year, overtime=0.0, bonus=0.0,
                           other_deductions=0.0, payment_date=None,
                           payment_method='bank_transfer', notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(SalaryRecord).filter(
            SalaryRecord.employee_id == employee_id,
            SalaryRecord.month == month,
            SalaryRecord.year == year
        ).first()
        if existing:
            raise ValueError(f"تم تسجيل راتب شهر {month}/{year} مسبقاً!")

        salary_data = calculate_monthly_salary(
            employee_id, month, year, overtime, bonus, other_deductions
        )

        if payment_date is None:
            payment_date = datetime.now()
        check_period_open(payment_date, entity="صرف راتب")

        salary_record = SalaryRecord(
            employee_id=employee_id, month=month, year=year,
            basic_salary=salary_data['basic_salary'],
            allowances=salary_data['allowances'],
            overtime=overtime, bonus=bonus,
            gross_salary=salary_data['gross_salary'],
            social_insurance=salary_data['social_insurance'],
            tax=salary_data['tax'], other_deductions=other_deductions,
            total_deductions=salary_data['total_deductions'],
            net_salary=salary_data['net_salary'],
            payment_date=payment_date, payment_method=payment_method,
            status='paid', notes=notes, created_by=created_by
        )
        db.add(salary_record)

        journal_entry = models.JournalEntry(
            date=payment_date,
            description=f"راتب شهر {month}/{year} - {salary_data['employee'].full_name}",
            reference_type="salary", reference_id=employee_id, created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        sal_exp = db.query(models.Account).filter(models.Account.code == "5401").first()
        if sal_exp:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=sal_exp.id,
                                       debit=salary_data['gross_salary'], credit=0.0))

        if salary_data['social_insurance'] > 0:
            si_acc = db.query(models.Account).filter(models.Account.code == "2401").first()
            if si_acc:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=si_acc.id,
                                           debit=0.0, credit=salary_data['social_insurance']))

        if salary_data['tax'] > 0:
            tax_acc = db.query(models.Account).filter(models.Account.code == "2402").first()
            if tax_acc:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=tax_acc.id,
                                           debit=0.0, credit=salary_data['tax']))

        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=1,
                                   debit=0.0, credit=salary_data['net_salary']))

        db.commit()
        _after_write()
        return salary_record
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_employee_salary_history(employee_id):
    db = _read()
    try:
        records = db.query(SalaryRecord).filter(
            SalaryRecord.employee_id == employee_id
        ).order_by(SalaryRecord.year.desc(), SalaryRecord.month.desc()).all()

        return [{
            'id': r.id, 'month': r.month, 'year': r.year,
            'gross_salary': r.gross_salary,
            'deductions': r.total_deductions,
            'net_salary': r.net_salary,
            'payment_date': r.payment_date, 'status': r.status
        } for r in records]
    finally:
        db.close()


def get_monthly_payroll(month=None, year=None):
    db = _read()
    try:
        if month is None:
            month = datetime.now().month
        if year is None:
            year = datetime.now().year

        records = db.query(SalaryRecord).filter(
            SalaryRecord.month == month, SalaryRecord.year == year
        ).all()

        emp_ids = list({r.employee_id for r in records if r.employee_id})
        emp_map = {}
        if emp_ids:
            emp_map = {e.id: e for e in db.query(Employee).filter(
                Employee.id.in_(emp_ids)
            ).all()}

        payroll = []
        tg = td = tn = 0
        for r in records:
            e = emp_map.get(r.employee_id)
            payroll.append({
                'employee_code': e.employee_code if e else '-',
                'employee_name': e.full_name if e else '-',
                'job_title': e.job_title if e else '-',
                'department': e.department if e else '-',
                'basic_salary': r.basic_salary, 'allowances': r.allowances,
                'overtime': r.overtime, 'bonus': r.bonus,
                'gross_salary': r.gross_salary,
                'social_insurance': r.social_insurance, 'tax': r.tax,
                'other_deductions': r.other_deductions,
                'total_deductions': r.total_deductions,
                'net_salary': r.net_salary, 'status': r.status
            })
            tg += r.gross_salary
            td += r.total_deductions
            tn += r.net_salary

        return {
            'payroll': payroll,
            'total_gross': tg, 'total_deductions': td, 'total_net': tn,
            'month': month, 'year': year
        }
    finally:
        db.close()


def get_employees_summary():
    db = _read()
    try:
        employees = db.query(Employee).all()
        active = [e for e in employees if e.status == EmploymentStatus.ACTIVE]
        total_salaries = sum(e.basic_salary + e.allowances for e in active)

        departments = {}
        for emp in employees:
            if emp.department:
                departments[emp.department] = departments.get(emp.department, 0) + 1

        return {
            'total_employees': len(employees),
            'active_employees': len(active),
            'total_monthly_salaries': total_salaries,
            'departments': departments
        }
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  22. التقارير المالية
# ═══════════════════════════════════════════════════════
def get_income_statement(start_date, end_date):
    db = _read()
    try:
        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0

        cogs = db.query(func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0

        gp = revenue - cogs

        op_exp = db.query(func.sum(Expense.amount)).filter(
            Expense.date >= start_date, Expense.date <= end_date
        ).scalar() or 0.0

        sal_exp = db.query(func.sum(SalaryRecord.gross_salary)).filter(
            SalaryRecord.payment_date >= start_date,
            SalaryRecord.payment_date <= end_date
        ).scalar() or 0.0

        dep_exp = db.query(func.sum(DepreciationRecord.depreciation_amount)).filter(
            DepreciationRecord.date >= start_date,
            DepreciationRecord.date <= end_date
        ).scalar() or 0.0

        total_exp = op_exp + sal_exp + dep_exp
        np_ = gp - total_exp

        return {
            'revenue': revenue, 'cogs': cogs, 'gross_profit': gp,
            'operating_expenses': op_exp, 'salary_expenses': sal_exp,
            'depreciation_expenses': dep_exp, 'total_expenses': total_exp,
            'net_profit': np_,
            'gross_margin': (gp / revenue * 100) if revenue > 0 else 0,
            'net_margin': (np_ / revenue * 100) if revenue > 0 else 0
        }
    finally:
        db.close()


def get_balance_sheet(as_of_date):
    db = _read()
    try:
        def balance_for_type(acc_type, is_asset=True):
            result = {}
            accounts = db.query(models.Account).filter(models.Account.type == acc_type).all()
            for acc in accounts:
                if acc.parent_id is None:
                    continue
                d = db.query(func.sum(models.JournalLine.debit)).filter(
                    models.JournalLine.account_id == acc.id,
                    models.JournalEntry.date <= as_of_date
                ).join(models.JournalEntry).scalar() or 0.0
                c = db.query(func.sum(models.JournalLine.credit)).filter(
                    models.JournalLine.account_id == acc.id,
                    models.JournalEntry.date <= as_of_date
                ).join(models.JournalEntry).scalar() or 0.0
                b = (d - c) if is_asset else (c - d)
                if b != 0:  # ⭐ إظهار كل الأرصدة (بما فيها السالبة) بدل حذفها من التقرير
                    result[acc.name] = b
            return result

        assets = balance_for_type(AccountType.ASSET, True)
        liabilities = balance_for_type(AccountType.LIABILITY, False)
        equity = balance_for_type(AccountType.EQUITY, False)

        # ⭐ أرباح محتجزة صحيحة: الإيراد − تكلفة المبيعات − المصروفات التشغيلية − الرواتب − الإهلاك
        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date <= as_of_date
        ).scalar() or 0.0

        cogs = db.query(func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date <= as_of_date
        ).scalar() or 0.0

        op_exp = db.query(func.sum(Expense.amount)).filter(
            Expense.date <= as_of_date
        ).scalar() or 0.0

        sal_exp = db.query(func.sum(SalaryRecord.gross_salary)).filter(
            SalaryRecord.payment_date <= as_of_date
        ).scalar() or 0.0

        dep_exp = db.query(func.sum(DepreciationRecord.depreciation_amount)).filter(
            DepreciationRecord.date <= as_of_date
        ).scalar() or 0.0

        equity['الأرباح المحتجزة'] = revenue - cogs - op_exp - sal_exp - dep_exp

        return {
            'assets': assets, 'total_assets': sum(assets.values()),
            'liabilities': liabilities, 'total_liabilities': sum(liabilities.values()),
            'equity': equity, 'total_equity': sum(equity.values())
        }
    finally:
        db.close()


def get_trial_balance(start_date, end_date):
    db = _read()
    try:
        accounts = db.query(models.Account).all()

        debit_agg = db.query(
            models.JournalLine.account_id,
            func.sum(models.JournalLine.debit).label('total')
        ).join(models.JournalEntry).filter(
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).group_by(models.JournalLine.account_id).all()
        debit_map = {aid: float(t or 0) for aid, t in debit_agg}

        credit_agg = db.query(
            models.JournalLine.account_id,
            func.sum(models.JournalLine.credit).label('total')
        ).join(models.JournalEntry).filter(
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).group_by(models.JournalLine.account_id).all()
        credit_map = {aid: float(t or 0) for aid, t in credit_agg}

        tb = []
        td = tc = 0
        for acc in accounts:
            d = debit_map.get(acc.id, 0.0)
            c = credit_map.get(acc.id, 0.0)
            if d > 0 or c > 0:
                tb.append({
                    'account_code': acc.code, 'account_name': acc.name,
                    'debit': d, 'credit': c, 'balance': d - c, 'type': acc.type.value
                })
                td += d
                tc += c

        return {'accounts': tb, 'total_debit': td, 'total_credit': tc, 'difference': td - tc}
    finally:
        db.close()


def get_cash_flow_statement(start_date, end_date):
    db = _read()
    try:
        cfo = db.query(func.sum(models.Payment.amount)).filter(
            models.Payment.payment_type == 'receipt',
            models.Payment.date >= start_date, models.Payment.date <= end_date
        ).scalar() or 0.0

        cto = db.query(func.sum(models.Payment.amount)).filter(
            models.Payment.payment_type == 'payment',
            models.Payment.date >= start_date, models.Payment.date <= end_date
        ).scalar() or 0.0

        cfi = db.query(func.sum(models.JournalLine.debit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('15%'))
            ),
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).join(models.JournalEntry).scalar() or 0.0

        cff = db.query(func.sum(models.JournalLine.credit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('23%'))
            ),
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).join(models.JournalEntry).scalar() or 0.0

        return {
            'cash_from_operations': cfo, 'cash_to_operations': cto,
            'cash_from_investing': cfi, 'cash_from_financing': cff,
            'net_cash_flow': cfo - cto + cfi + cff
        }
    finally:
        db.close()


def get_financial_ratios(start_date, end_date):
    db = _read()
    try:
        ca = db.query(func.sum(models.JournalLine.debit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('11%'))
            )
        ).scalar() or 0.0

        cl = db.query(func.sum(models.JournalLine.credit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('21%'))
            )
        ).scalar() or 0.0

        cr = ca / cl if cl > 0 else 0

        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0

        np_ = db.query(func.sum(
            models.InvoiceLine.total - models.InvoiceLine.quantity * models.InvoiceLine.cost_price
        )).join(models.Invoice).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0

        pm = (np_ / revenue * 100) if revenue > 0 else 0

        inventory = db.query(func.sum(StockLevel.quantity * models.Item.cost_price)).join(
            models.Item
        ).scalar() or 0.0

        cogs = db.query(func.sum(
            models.InvoiceLine.quantity * models.InvoiceLine.cost_price
        )).join(models.Invoice).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0

        it = cogs / inventory if inventory > 0 else 0

        return {
            'current_ratio': cr, 'profit_margin': pm,
            'inventory_turnover': it, 'revenue': revenue, 'net_profit': np_
        }
    finally:
        db.close()


def create_budget(account_id, month, year, budgeted_amount, notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(Budget).filter(
            Budget.account_id == account_id,
            Budget.month == month, Budget.year == year
        ).first()

        if existing:
            existing.budgeted_amount = budgeted_amount
            existing.notes = notes
            db.commit()
            _after_write()
            return existing

        budget = Budget(
            account_id=account_id, month=month, year=year,
            budgeted_amount=budgeted_amount, notes=notes, created_by=created_by
        )
        db.add(budget)
        db.commit()
        db.refresh(budget)
        _after_write()
        return budget
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_budget_vs_actual(month, year):
    db = _read()
    try:
        budgets = db.query(Budget).filter(
            Budget.month == month, Budget.year == year
        ).all()

        comparison = []
        month_expr = _month_expr(db, models.JournalEntry.date)
        year_expr = _year_expr(db, models.JournalEntry.date)

        for budget in budgets:
            account = db.query(models.Account).filter(
                models.Account.id == budget.account_id
            ).first()

            actual = db.query(
                func.sum(models.JournalLine.debit - models.JournalLine.credit)
            ).filter(
                models.JournalLine.account_id == budget.account_id,
                month_expr == f"{month:02d}",
                year_expr == str(year),
            ).join(models.JournalEntry).scalar() or 0.0

            variance = budget.budgeted_amount - actual
            vp = (variance / budget.budgeted_amount * 100) if budget.budgeted_amount > 0 else 0

            comparison.append({
                'account_code': account.code if account else '-',
                'account_name': account.name if account else '-',
                'budgeted': budget.budgeted_amount, 'actual': actual,
                'variance': variance, 'variance_percentage': vp,
            })
        return comparison
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  23. الإغلاق المحاسبي
# ═══════════════════════════════════════════════════════
def create_accounting_period(period_name, start_date, end_date, is_fiscal_year=False, created_by=None):
    db = _write()
    try:
        existing = db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= end_date,
            AccountingPeriod.end_date >= start_date
        ).first()
        if existing:
            raise ValueError(f"توجد فترة محاسبية متداخلة: '{existing.period_name}'")

        period = AccountingPeriod(
            period_name=period_name, start_date=start_date, end_date=end_date,
            is_fiscal_year=is_fiscal_year, status=PeriodStatus.OPEN,
            created_by=created_by
        )
        db.add(period)
        db.commit()
        db.refresh(period)
        _after_write()
        return period
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_period_for_date(date):
    db = _read()
    try:
        return db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= date,
            AccountingPeriod.end_date >= date
        ).first()
    finally:
        db.close()


def get_all_periods():
    db = _read()
    try:
        return db.query(AccountingPeriod).order_by(
            AccountingPeriod.start_date.desc()
        ).all()
    finally:
        db.close()


def close_accounting_period(period_id, closing_notes=None, closed_by=None):
    db = _write()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            raise ValueError("الفترة المحاسبية غير موجودة!")
        if period.status == PeriodStatus.CLOSED:
            raise ValueError("هذه الفترة مغلقة بالفعل!")

        pending = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date,
            models.Invoice.status == 'pending'
        ).count()
        if pending > 0:
            raise ValueError(f"يوجد {pending} فاتورة معلقة في هذه الفترة! يجب تسويتها أولاً.")

        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).scalar() or 0.0

        expenses = db.query(func.sum(Expense.amount)).filter(
            Expense.date >= period.start_date,
            Expense.date <= period.end_date
        ).scalar() or 0.0

        np_ = revenue - expenses

        closing_entry = models.JournalEntry(
            date=period.end_date,
            description=f"إغلاق فترة '{period.period_name}' - ترحيل صافي الربح/الخسارة",
            reference_type="period_closing", reference_id=period_id,
            created_by=closed_by, period_id=period_id
        )
        db.add(closing_entry)
        db.flush()

        re_acc = db.query(models.Account).filter(models.Account.code == "3101").first()
        if not re_acc:
            raise ValueError("حساب الأرباح المحتجزة غير موجود!")

        pl_acc = db.query(models.Account).filter(models.Account.code == "3201").first()

        if np_ > 0:
            if pl_acc:
                db.add(models.JournalLine(entry_id=closing_entry.id, account_id=pl_acc.id,
                                           debit=np_, credit=0.0))
                db.add(models.JournalLine(entry_id=closing_entry.id, account_id=re_acc.id,
                                           debit=0.0, credit=np_))
        else:
            if pl_acc:
                db.add(models.JournalLine(entry_id=closing_entry.id, account_id=re_acc.id,
                                           debit=abs(np_), credit=0.0))
                db.add(models.JournalLine(entry_id=closing_entry.id, account_id=pl_acc.id,
                                           debit=0.0, credit=abs(np_)))

        period.status = PeriodStatus.CLOSED
        period.closing_date = datetime.now()
        period.closing_notes = closing_notes
        period.closed_by = closed_by

        db.commit()
        _after_write()
        return {'period': period, 'net_profit': np_, 'revenue': revenue, 'expenses': expenses}
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def reopen_accounting_period(period_id, reopened_by=None):
    db = _write()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            raise ValueError("الفترة المحاسبية غير موجودة!")
        if period.status == PeriodStatus.OPEN:
            raise ValueError("هذه الفترة مفتوحة بالفعل!")

        closing_entry = db.query(models.JournalEntry).filter(
            models.JournalEntry.reference_type == "period_closing",
            models.JournalEntry.reference_id == period_id
        ).first()

        if closing_entry:
            db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == closing_entry.id
            ).delete()
            db.delete(closing_entry)

        period.status = PeriodStatus.OPEN
        period.closing_date = None
        period.closing_notes = None
        period.closed_by = None

        db.commit()
        _after_write()
        return period
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_period_summary(period_id):
    db = _read()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            return None

        inv_count = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).count()

        ts = db.query(func.sum(models.Invoice.net_amount)).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).scalar() or 0.0

        tp = db.query(func.sum(models.Invoice.net_amount)).filter(
            models.Invoice.type == 'purchase',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).scalar() or 0.0

        te = db.query(func.sum(Expense.amount)).filter(
            Expense.date >= period.start_date, Expense.date <= period.end_date
        ).scalar() or 0.0

        je_count = db.query(models.JournalEntry).filter(
            models.JournalEntry.period_id == period_id
        ).count()

        return {
            'period': period, 'invoices_count': inv_count,
            'total_sales': ts, 'total_purchases': tp,
            'total_expenses': te, 'journal_entries_count': je_count
        }
    finally:
        db.close()


def check_period_integrity(period_id):
    db = _read()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            return None

        issues = []

        pending = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date,
            models.Invoice.status == 'pending'
        ).count()
        if pending > 0:
            issues.append(f"⚠️ يوجد {pending} فاتورة معلقة")

        entries = db.query(models.JournalEntry).filter(
            models.JournalEntry.period_id == period_id
        ).all()
        unbalanced = 0
        for entry in entries:
            lines = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == entry.id
            ).all()
            td = sum(l.debit for l in lines)
            tc = sum(l.credit for l in lines)
            if abs(td - tc) > 0.01:
                unbalanced += 1
        if unbalanced > 0:
            issues.append(f"⚠️ يوجد {unbalanced} قيد غير متوازن")

        # مخزون منخفض
        rows = db.query(
            models.InventoryMovement.item_id,
            func.sum(
                case(
                    (models.InventoryMovement.type == 'in', models.InventoryMovement.quantity),
                    else_=-models.InventoryMovement.quantity
                )
            ).label('balance')
        ).group_by(models.InventoryMovement.item_id).all()
        stock_map = {iid: float(b or 0) for iid, b in rows}

        items = db.query(models.Item).filter(models.Item.is_kit == False).all()
        low = sum(1 for it in items if stock_map.get(it.id, 0.0) <= (it.min_stock or 0))
        if low > 0:
            issues.append(f"⚠️ يوجد {low} صنف تحت الحد الأدنى للمخزون")

        return {
            'period': period, 'issues': issues,
            'is_ready_to_close': len(issues) == 0
        }
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  24. مراكز التكلفة
# ═══════════════════════════════════════════════════════
def create_cost_center(code, name, description=None, manager_name=None,
                        parent_id=None, budget_limit=None, notes=None, created_by=None):
    db = _write()
    try:
        existing = db.query(CostCenter).filter(CostCenter.code == code).first()
        if existing:
            raise ValueError(f"كود مركز التكلفة '{code}' موجود مسبقاً!")

        cc = CostCenter(
            code=code, name=name, description=description,
            manager_name=manager_name, parent_id=parent_id,
            budget_limit=budget_limit, notes=notes,
            is_active=True, created_by=created_by
        )
        db.add(cc)
        db.commit()
        db.refresh(cc)
        _after_write()
        return cc
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_cost_centers_tree():
    db = _read()
    try:
        centers = db.query(CostCenter).filter(CostCenter.is_active == True).all()
        tree = []
        for c in centers:
            if c.parent_id is None:
                children = [x for x in centers if x.parent_id == c.id]
                tree.append({
                    'id': c.id, 'code': c.code, 'name': c.name,
                    'manager': c.manager_name, 'budget_limit': c.budget_limit,
                    'children': [{
                        'id': ch.id, 'code': ch.code, 'name': ch.name,
                        'manager': ch.manager_name, 'budget_limit': ch.budget_limit
                    } for ch in children]
                })
        return tree
    finally:
        db.close()


def allocate_cost_to_center(cost_center_id, account_id, amount, description,
                             allocation_date=None, reference_type=None,
                             reference_id=None, created_by=None):
    db = _write()
    try:
        cc = db.query(CostCenter).filter(CostCenter.id == cost_center_id).first()
        if not cc:
            raise ValueError("مركز التكلفة غير موجود!")
        if not cc.is_active:
            raise ValueError("مركز التكلفة غير نشط!")

        if allocation_date is None:
            allocation_date = datetime.now()
        check_period_open(allocation_date, entity="تخصيص تكلفة")

        transaction = CostCenterTransaction(
            cost_center_id=cost_center_id, date=allocation_date,
            description=description, amount=amount,
            transaction_type='expense',
            reference_type=reference_type, reference_id=reference_id,
            created_by=created_by
        )
        db.add(transaction)

        journal_entry = models.JournalEntry(
            date=allocation_date,
            description=f"تخصيص تكلفة لـ {cc.name}: {description}",
            reference_type="cost_allocation", reference_id=cost_center_id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()

        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=account_id,
                                   debit=amount, credit=0.0))

        cb = db.query(CashBox).filter(CashBox.is_active == True).first()
        if cb:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cb.account_id,
                                       debit=0.0, credit=amount))

        db.flush()
        validate_journal_entry(db, journal_entry.id)
        db.commit()
        _after_write()
        return transaction
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_cost_center_report(cost_center_id, start_date=None, end_date=None):
    db = _read()
    try:
        cc = db.query(CostCenter).filter(CostCenter.id == cost_center_id).first()
        if not cc:
            return None

        q = db.query(CostCenterTransaction).filter(
            CostCenterTransaction.cost_center_id == cost_center_id
        )
        if start_date:
            q = q.filter(CostCenterTransaction.date >= start_date)
        if end_date:
            q = q.filter(CostCenterTransaction.date <= end_date)

        transactions = q.order_by(CostCenterTransaction.date.desc()).all()
        te = sum(t.amount for t in transactions if t.transaction_type == 'expense')
        tr = sum(t.amount for t in transactions if t.transaction_type == 'revenue')

        children = db.query(CostCenter).filter(CostCenter.parent_id == cost_center_id).all()
        children_data = []
        for child in children:
            cts = db.query(CostCenterTransaction).filter(
                CostCenterTransaction.cost_center_id == child.id
            ).all()
            children_data.append({
                'id': child.id, 'name': child.name,
                'total': sum(t.amount for t in cts)
            })

        return {
            'cost_center': cc, 'transactions': transactions,
            'total_expenses': te, 'total_revenues': tr,
            'net_amount': tr - te, 'children': children_data
        }
    finally:
        db.close()


def get_all_cost_centers_summary(start_date=None, end_date=None):
    db = _read()
    try:
        centers = db.query(CostCenter).filter(CostCenter.is_active == True).all()
        summary = []
        for cc in centers:
            q = db.query(CostCenterTransaction).filter(
                CostCenterTransaction.cost_center_id == cc.id
            )
            if start_date:
                q = q.filter(CostCenterTransaction.date >= start_date)
            if end_date:
                q = q.filter(CostCenterTransaction.date <= end_date)
            total = sum(t.amount for t in q.all())
            summary.append({
                'id': cc.id, 'code': cc.code, 'name': cc.name,
                'manager': cc.manager_name, 'budget_limit': cc.budget_limit,
                'total_spent': total,
                'remaining_budget': (cc.budget_limit - total) if cc.budget_limit else None,
                'usage_percentage': (total / cc.budget_limit * 100) if cc.budget_limit and cc.budget_limit > 0 else 0
            })
        return summary
    finally:
        db.close()


def transfer_between_cost_centers(from_center_id, to_center_id, amount,
                                   description, transfer_date=None, created_by=None):
    db = _write()
    try:
        if from_center_id == to_center_id:
            raise ValueError("لا يمكن التحويل بين نفس مركز التكلفة!")

        fc = db.query(CostCenter).filter(CostCenter.id == from_center_id).first()
        tc = db.query(CostCenter).filter(CostCenter.id == to_center_id).first()
        if not fc or not tc:
            raise ValueError("أحد مراكز التكلفة غير موجود!")

        if transfer_date is None:
            transfer_date = datetime.now()

        db.add(CostCenterTransaction(
            cost_center_id=from_center_id, date=transfer_date,
            description=f"تحويل إلى {tc.name}: {description}",
            amount=amount, transaction_type='transfer_out', created_by=created_by
        ))
        db.add(CostCenterTransaction(
            cost_center_id=to_center_id, date=transfer_date,
            description=f"تحويل من {fc.name}: {description}",
            amount=amount, transaction_type='transfer_in', created_by=created_by
        ))

        db.commit()
        _after_write()
        return {'from': fc.name, 'to': tc.name, 'amount': amount}
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  25. تصنيفات المصروفات
# ═══════════════════════════════════════════════════════
def get_expense_categories(include_inactive=False):
    db = _read()
    try:
        q = db.query(ExpenseCategory)
        if not include_inactive:
            q = q.filter(ExpenseCategory.is_active == True)
        return q.order_by(ExpenseCategory.name).all()
    finally:
        db.close()


def get_expense_category_by_id(category_id):
    db = _read()
    try:
        return db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
    finally:
        db.close()


def update_expense_category(category_id, name=None, description=None,
                             account_id=None, is_active=None):
    db = _write()
    try:
        cat = db.query(ExpenseCategory).filter(ExpenseCategory.id == category_id).first()
        if not cat:
            raise ValueError("تصنيف المصروف غير موجود!")

        if name is not None:
            name = name.strip()
            if not name:
                raise ValueError("اسم التصنيف مطلوب!")
            existing = db.query(ExpenseCategory).filter(
                ExpenseCategory.name == name,
                ExpenseCategory.id != category_id
            ).first()
            if existing:
                raise ValueError(f"يوجد تصنيف آخر باسم '{name}'!")
            cat.name = name

            if cat.account_id:
                acc = db.query(models.Account).filter(models.Account.id == cat.account_id).first()
                if acc:
                    acc.name = name

        if description is not None:
            cat.description = description.strip() if description else None

        if account_id is not None:
            acc = db.query(models.Account).filter(models.Account.id == account_id).first()
            if not acc:
                raise ValueError("الحساب غير موجود!")
            cat.account_id = account_id

        if is_active is not None:
            cat.is_active = bool(is_active)

        db.commit()
        _after_write()
        return cat
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def delete_expense_category(category_id, force=False):
    db = _write()
    try:
        cat = db.query(ExpenseCategory).filter(ExpenseCategory.id == category_id).first()
        if not cat:
            raise ValueError("تصنيف المصروف غير موجود!")

        account_id = cat.account_id

        count = db.query(Expense).filter(Expense.category_id == category_id).count()
        if count > 0:
            if not force:
                raise ValueError(f"لا يمكن الحذف: يوجد {count} مصروف مرتبط.")
            expenses_list = db.query(Expense).filter(Expense.category_id == category_id).all()
            for exp in expenses_list:
                je = db.query(models.JournalEntry).filter(
                    models.JournalEntry.reference_type == "expense",
                    models.JournalEntry.description.contains(exp.description or ""),
                ).first()
                if je:
                    db.query(models.JournalLine).filter(
                        models.JournalLine.entry_id == je.id
                    ).delete(synchronize_session=False)
                    db.delete(je)
                db.delete(exp)

        db.delete(cat)
        db.flush()

        if account_id:
            other = db.query(ExpenseCategory).filter(
                ExpenseCategory.account_id == account_id
            ).first()
            if not other:
                acc = db.query(models.Account).filter(models.Account.id == account_id).first()
                if acc:
                    db.delete(acc)

        db.commit()
        _after_write()
        return True
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def deactivate_expense_category(category_id):
    return update_expense_category(category_id, is_active=False)


def activate_expense_category(category_id):
    return update_expense_category(category_id, is_active=True)


def get_expense_category_usage(category_id):
    db = _read()
    try:
        count = db.query(Expense).filter(Expense.category_id == category_id).count()
        total = db.query(func.sum(Expense.amount)).filter(
            Expense.category_id == category_id
        ).scalar() or 0.0
        return {"count": count, "total": float(total)}
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  26. المسلسل الرقمي الموحّد
# ═══════════════════════════════════════════════════════
def movement_serial(prefix, id_value, digits=6):
    if id_value is None:
        return f"{prefix}-?"
    try:
        return f"{prefix}-{int(id_value):0{digits}d}"
    except (TypeError, ValueError):
        return f"{prefix}-?"


def parse_movement_serial(serial):
    if not serial or "-" not in str(serial):
        return None
    try:
        parts = str(serial).strip().split("-", 1)
        if len(parts) != 2:
            return None
        prefix = parts[0].strip().upper()
        id_part = parts[1].strip()
        if not id_part.isdigit():
            return None
        return {"prefix": prefix, "id": int(id_part)}
    except Exception:
        return None


def movement_serial_with_year(prefix, id_value, year=None, digits=6):
    if year is None:
        year = datetime.now().year
    return movement_serial(f"{prefix}-{year}", id_value, digits)


# ═══════════════════════════════════════════════════════
#  27. حارس توازن القيد (يأخذ db كمعامل — لا يفتح جلسة)
# ═══════════════════════════════════════════════════════
def validate_journal_entry(db, entry_id):
    lines = db.query(models.JournalLine).filter(
        models.JournalLine.entry_id == entry_id
    ).all()

    td = sum(float(l.debit or 0) for l in lines)
    tc = sum(float(l.credit or 0) for l in lines)

    if abs(td - tc) > 0.01:
        raise ValueError(
            f"⛔ قيد غير متوازن (ID={entry_id}): "
            f"مدين={td:,.2f} | دائن={tc:,.2f} | فرق={td - tc:,.2f}"
        )
    return True


def validate_all_journal_entries():
    db = _read()
    try:
        entries = db.query(models.JournalEntry).all()
        unbalanced = []
        for entry in entries:
            lines = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == entry.id
            ).all()
            td = sum(float(l.debit or 0) for l in lines)
            tc = sum(float(l.credit or 0) for l in lines)
            if abs(td - tc) > 0.01:
                unbalanced.append({
                    "id": entry.id, "date": entry.date,
                    "description": entry.description,
                    "debit": td, "credit": tc, "diff": td - tc,
                })
        return {
            "total_entries": len(entries),
            "unbalanced_count": len(unbalanced),
            "unbalanced_list": unbalanced,
        }
    finally:
        db.close()


def check_accounting_integrity():
    db = _read()
    try:
        report = {
            "unbalanced_entries": [],
            "orphan_journal_lines": 0,
            "duplicate_invoice_numbers": [],
            "low_stock_count": 0,
        }

        entries = db.query(models.JournalEntry).all()
        for entry in entries:
            lines = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == entry.id
            ).all()
            td = sum(float(l.debit or 0) for l in lines)
            tc = sum(float(l.credit or 0) for l in lines)
            if abs(td - tc) > 0.01:
                report["unbalanced_entries"].append({
                    "id": entry.id, "date": entry.date, "diff": td - tc
                })

        entry_ids = {e.id for e in entries}
        all_lines = db.query(models.JournalLine).all()
        report["orphan_journal_lines"] = sum(
            1 for l in all_lines if l.entry_id not in entry_ids
        )

        dupes = db.query(
            models.Invoice.invoice_number,
            func.count(models.Invoice.id).label('cnt')
        ).group_by(models.Invoice.invoice_number).having(
            func.count(models.Invoice.id) > 1
        ).all()
        report["duplicate_invoice_numbers"] = [{"number": n, "count": c} for n, c in dupes]

        rows = db.query(
            models.InventoryMovement.item_id,
            func.sum(
                case(
                    (models.InventoryMovement.type == 'in', models.InventoryMovement.quantity),
                    else_=-models.InventoryMovement.quantity
                )
            ).label('balance')
        ).group_by(models.InventoryMovement.item_id).all()
        stock_map = {iid: float(b or 0) for iid, b in rows}

        items = db.query(models.Item).filter(models.Item.is_kit == False).all()
        report["low_stock_count"] = sum(
            1 for it in items if stock_map.get(it.id, 0.0) <= (it.min_stock or 0)
        )

        return report
    finally:
        db.close()


# ═══════════════════════════════════════════════════════
#  28. الفواتير المتكررة
# ═══════════════════════════════════════════════════════
def _calc_next_run_date(current_date, frequency):
    if frequency == RecurrenceFrequency.DAILY:
        return current_date + timedelta(days=1)
    elif frequency == RecurrenceFrequency.WEEKLY:
        return current_date + timedelta(weeks=1)
    elif frequency == RecurrenceFrequency.BIWEEKLY:
        return current_date + timedelta(weeks=2)
    elif frequency == RecurrenceFrequency.MONTHLY:
        return current_date + relativedelta(months=1)
    elif frequency == RecurrenceFrequency.QUARTERLY:
        return current_date + relativedelta(months=3)
    elif frequency == RecurrenceFrequency.SEMIANNUAL:
        return current_date + relativedelta(months=6)
    elif frequency == RecurrenceFrequency.ANNUAL:
        return current_date + relativedelta(years=1)
    return current_date + relativedelta(months=1)


def create_recurring_template(name, party_id, invoice_type, frequency,
                               start_date, lines, end_date=None,
                               discount_percentage=0.0, tax_rate=14.0,
                               notes=None, auto_post=False, created_by=None):
    db = _write()
    try:
        check_period_open(start_date, entity="إنشاء قالب فاتورة متكررة")

        party = db.query(models.Party).filter(models.Party.id == party_id).first()
        if not party:
            raise ValueError("العميل/المورد غير موجود!")
        if not lines or len(lines) == 0:
            raise ValueError("يجب إضافة سطر واحد على الأقل")

        template = RecurringInvoiceTemplate(
            name=name.strip(), party_id=party_id, invoice_type=invoice_type,
            frequency=frequency, start_date=start_date, end_date=end_date,
            next_run_date=start_date, discount_percentage=discount_percentage,
            tax_rate=tax_rate, notes=notes, is_active=True,
            auto_post=auto_post, created_by=created_by,
        )
        db.add(template)
        db.flush()

        for ln in lines:
            item = db.query(models.Item).filter(models.Item.name == ln['item_name']).first()
            if not item:
                raise ValueError(f"الصنف '{ln['item_name']}' غير موجود!")
            db.add(RecurringInvoiceLine(
                template_id=template.id, item_id=item.id,
                quantity=float(ln['quantity']), price=float(ln['price']),
            ))

        db.commit()
        _after_write()
        return template
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def get_recurring_templates(only_active=False):
    db = _read()
    try:
        q = db.query(RecurringInvoiceTemplate)
        if only_active:
            q = q.filter(RecurringInvoiceTemplate.is_active == True)
        return q.order_by(RecurringInvoiceTemplate.next_run_date).all()
    finally:
        db.close()


def get_recurring_template(template_id):
    db = _read()
    try:
        return db.query(RecurringInvoiceTemplate).filter(
            RecurringInvoiceTemplate.id == template_id
        ).first()
    finally:
        db.close()


def get_recurring_template_lines(template_id):
    db = _read()
    try:
        lines = db.query(RecurringInvoiceLine).filter(
            RecurringInvoiceLine.template_id == template_id
        ).all()
        if not lines:
            return []

        item_ids = [l.item_id for l in lines]
        items_map = {i.id: i for i in db.query(models.Item).filter(
            models.Item.id.in_(item_ids)
        ).all()}

        return [{
            "line_id": l.id, "item_id": l.item_id,
            "item_name": items_map[l.item_id].name if l.item_id in items_map else "—",
            "quantity": float(l.quantity or 0),
            "price": float(l.price or 0),
        } for l in lines]
    finally:
        db.close()


def update_recurring_template(template_id, **kwargs):
    db = _write()
    try:
        t = db.query(RecurringInvoiceTemplate).filter(
            RecurringInvoiceTemplate.id == template_id
        ).first()
        if not t:
            raise ValueError("القالب غير موجود!")

        if 'name' in kwargs and kwargs['name']:
            t.name = kwargs['name'].strip()
        if 'frequency' in kwargs and kwargs['frequency']:
            t.frequency = kwargs['frequency']
        if 'discount_percentage' in kwargs:
            t.discount_percentage = float(kwargs['discount_percentage'])
        if 'tax_rate' in kwargs:
            t.tax_rate = float(kwargs['tax_rate'])
        if 'notes' in kwargs:
            t.notes = kwargs['notes']
        if 'is_active' in kwargs:
            t.is_active = bool(kwargs['is_active'])
        if 'auto_post' in kwargs:
            t.auto_post = bool(kwargs['auto_post'])
        if 'next_run_date' in kwargs and kwargs['next_run_date']:
            t.next_run_date = kwargs['next_run_date']
        if 'end_date' in kwargs:
            t.end_date = kwargs['end_date']

        db.commit()
        _after_write()
        return t
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def delete_recurring_template(template_id):
    db = _write()
    try:
        t = db.query(RecurringInvoiceTemplate).filter(
            RecurringInvoiceTemplate.id == template_id
        ).first()
        if not t:
            raise ValueError("القالب غير موجود!")
        db.delete(t)
        db.commit()
        _after_write()
        return True
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def run_recurring_template(template_id, force=False, created_by=None):
    db = _write()
    try:
        t = db.query(RecurringInvoiceTemplate).filter(
            RecurringInvoiceTemplate.id == template_id
        ).first()
        if not t:
            raise ValueError("القالب غير موجود!")
        if not t.is_active:
            raise ValueError("القالب غير نشط!")

        if not force and t.next_run_date and t.next_run_date > datetime.now():
            raise ValueError(
                f"موعد التنفيذ التالي: {t.next_run_date.strftime('%Y-%m-%d')}"
            )

        lines = db.query(RecurringInvoiceLine).filter(
            RecurringInvoiceLine.template_id == t.id
        ).all()
        if not lines:
            raise ValueError("القالب بلا سطور!")

        items_data = []
        for l in lines:
            item = db.query(models.Item).filter(models.Item.id == l.item_id).first()
            if not item:
                continue
            items_data.append({
                'item_name': item.name,
                'quantity': float(l.quantity),
                'price': float(l.price),
            })
        if not items_data:
            raise ValueError("لا توجد أصناف صالحة!")

        # ⚠️ نغلق هذه الجلسة قبل استدعاء create_invoice (يتطلب جلسة مستقلة)
        # ⭐ ترقيم آمن عبر الدالة الموحدة (فريد عالمياً)
        new_invoice_number = _ensure_unique_invoice_number(db, None)

        t_id = t.id
        t_created = t.created_by
        t_party = t.party_id
        t_type = t.invoice_type
        t_disc = float(t.discount_percentage or 0)
        t_tax = float(t.tax_rate or 0)

        db.close()
        db = None

        # إنشاء الفاتورة (بجلسة كتابة مستقلة داخل create_invoice)
        invoice = create_invoice(
            party_id=t_party, invoice_type=t_type, items=items_data,
            invoice_number=new_invoice_number,
            discount_percentage=t_disc, tax_rate=t_tax,
            created_by=created_by or t_created,
        )

        # جلسة جديدة لتحديث القالب
        db2 = _write()
        try:
            t2 = db2.query(RecurringInvoiceTemplate).filter(
                RecurringInvoiceTemplate.id == t_id
            ).first()
            t2.last_run_date = datetime.now()
            t2.runs_count = (t2.runs_count or 0) + 1
            t2.next_run_date = _calc_next_run_date(
                t2.next_run_date or datetime.now(), t2.frequency
            )
            if t2.end_date and t2.next_run_date > t2.end_date:
                t2.is_active = False
            db2.commit()
            _after_write()
        finally:
            db2.close()

        return {
            'invoice': invoice, 'template': t,
            'invoice_number': new_invoice_number,
        }
    except Exception:
        if db is not None:
            db.rollback()
        raise
    finally:
        if db is not None:
            db.close()


def get_due_recurring_templates():
    db = _read()
    try:
        now = datetime.now()
        return db.query(RecurringInvoiceTemplate).filter(
            RecurringInvoiceTemplate.is_active == True,
            RecurringInvoiceTemplate.next_run_date <= now,
        ).order_by(RecurringInvoiceTemplate.next_run_date).all()
    finally:
        db.close()


def process_due_recurring_templates(auto_run=False, created_by=None):
    due = get_due_recurring_templates()

    if not auto_run:
        return {
            'total': len(due), 'processed': 0, 'failed': 0,
            'results': [{'template': t, 'status': 'pending'} for t in due]
        }

    processed = 0
    failed = 0
    results = []

    for t in due:
        try:
            r = run_recurring_template(t.id, force=True, created_by=created_by)
            processed += 1
            results.append({
                'template': t, 'status': 'success',
                'invoice_number': r['invoice_number'],
            })
        except Exception as e:
            failed += 1
            results.append({
                'template': t, 'status': 'failed', 'error': str(e),
            })

    return {
        'total': len(due), 'processed': processed,
        'failed': failed, 'results': results,
    }