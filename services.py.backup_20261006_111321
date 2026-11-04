# services.py
"""
ملف المنطق البرمجي (Business Logic)
يحتوي على جميع الدوال الأساسية للبرنامج
"""

from database import SessionLocal
import models
from models import (
    AccountType, CostHistory, CashBox, CashBoxType, CashTransfer,
    Category, ExpenseCategory, Expense, Warehouse, StockLevel,
    WarehouseTransfer, StockCount, FixedAsset, DepreciationRecord, DepreciationMethod,
    Loan, LoanInstallment, LoanType, LoanStatus,
    Employee, SalaryRecord, EmploymentStatus, Budget,
    AccountingPeriod, PeriodStatus
)
from datetime import datetime, timedelta
from dateutil.relativedelta import relativedelta
import random
import shutil
import os
import pandas as pd
from sqlalchemy import func, literal_column
from models import CostCenter, CostAllocation, CostCenterTransaction

# ==========================================
# دوال الخزائن
# ==========================================
def create_cash_box(name, code, box_type, account_id, 
                    responsible_person=None, max_limit=None, notes=None, created_by=None):
    db = SessionLocal()
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
        return cash_box
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_cash_box_balance(cash_box_id):
    db = SessionLocal()
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
        db.close()


def transfer_between_cash_boxes(from_cash_box_id, to_cash_box_id, amount, notes=None, created_by=None):
    db = SessionLocal()
    try:
        if from_cash_box_id == to_cash_box_id:
            raise ValueError("لا يمكن التحويل بين نفس الخزينة!")
        
        from_box = db.query(CashBox).filter(CashBox.id == from_cash_box_id).first()
        to_box = db.query(CashBox).filter(CashBox.id == to_cash_box_id).first()
        
        if not from_box or not to_box:
            raise ValueError("إحدى الخزائن غير موجودة!")
        
        current_balance = get_cash_box_balance(from_cash_box_id)
        if current_balance < amount:
            raise ValueError(f"رصيد الخزينة غير كافٍ! الرصيد الحالي: {current_balance:,.2f}")
        
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
        
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=to_box.account_id, debit=amount, credit=0.0))
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=from_box.account_id, debit=0.0, credit=amount))
        
        db.commit()
        return transfer
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# دالة تحديث أسعار الصنف تلقائياً
# ==========================================
def update_item_prices(item_id):
    db = SessionLocal()
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
        
        db.commit()
    except Exception as e:
        db.rollback()
    finally:
        db.close()


# ==========================================
# 1. دالة إضافة عميل أو مورد
# ==========================================
def create_party_with_opening_balance(name, party_type, opening_balance=0.0,
                                       phone=None, address=None, created_by=None):
    db = SessionLocal()
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
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id, debit=opening_balance, credit=0.0))
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id, debit=0.0, credit=opening_balance))
            else:
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id, debit=opening_balance, credit=0.0))
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id, debit=0.0, credit=opening_balance))
        
        db.commit()
        return party
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 2. دالة إنشاء فاتورة
# ==========================================

def _calc_item_cost_for_invoice(db, item):
    """يحسب التكلفة الصحيحة لصنف عند إضافته للفاتورة.
    - للصنف العادي: cost_price أو avg_cost_price
    - للصنف المدمج: مجموع (كمية المكون × تكلفة المكون)
    """
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
    # صنف عادي
    cost = float(item.cost_price or 0)
    if cost == 0:
        cost = float(getattr(item, "avg_cost_price", 0) or 0)
    return cost


def create_invoice(party_id, invoice_type, items, invoice_number=None, currency_id=1,
                   invoice_date=None, discount_percentage=0.0, tax_rate=14.0, created_by=None):
    db = SessionLocal()
    try:
        # التحقق من أن الفترة المحاسبية مفتوحة
        period = get_period_for_date(invoice_date or datetime.now())
        if period and period.status == PeriodStatus.CLOSED:
            raise ValueError(f"الفترة المحاسبية '{period.period_name}' مغلقة! لا يمكن إنشاء فواتير.")
        
        party = db.query(models.Party).filter(models.Party.id == party_id).first()
        if not party:
            raise ValueError(f"العميل/المورد برقم {party_id} غير موجود!")
        
        if not invoice_number:
            invoice_number = f"INV-{db.query(models.Invoice).count() + 1:06d}"
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
        
        for item_data in items:
            item = db.query(models.Item).filter(models.Item.name == item_data['item_name']).first()
            if not item:
                raise ValueError(f"الصنف '{item_data['item_name']}' غير موجود!")
            
            line_total = item_data['quantity'] * item_data['price']
            invoice_line = models.InvoiceLine(
                invoice_id=invoice.id, item_id=item.id,
                quantity=item_data['quantity'], price=item_data['price'],
                total=line_total, cost_price=_calc_item_cost_for_invoice(db, item)
            )
            db.add(invoice_line)
            
            if not item.is_kit:
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
                    update_item_prices(item.id)
            
            journal_entry = models.JournalEntry(
                date=invoice_date, description=f"فاتورة {invoice_type} رقم {invoice_number}",
                reference_type="invoice", reference_id=invoice.id, created_by=created_by,
                period_id=period.id if period else None
            )
            db.add(journal_entry)
            db.flush()
            
            if invoice_type == 'sale':
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id, debit=net_amount, credit=0.0))
                revenue_account = db.query(models.Account).filter(models.Account.code == "4101").first()
                if revenue_account:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=revenue_account.id, debit=0.0, credit=amount_after_discount))
                
                if item.cost_price > 0:
                    cogs_entry = models.JournalEntry(
                        date=invoice_date, description=f"تكلفة البضاعة المباعة - فاتورة {invoice_number}",
                        reference_type="cogs", reference_id=invoice.id, created_by=created_by,
                        period_id=period.id if period else None
                    )
                    db.add(cogs_entry)
                    db.flush()
                    cogs_account = db.query(models.Account).filter(models.Account.code == "5101").first()
                    inventory_account = db.query(models.Account).filter(models.Account.code == "1301").first()
                    if cogs_account and inventory_account:
                        total_cost = item_data['quantity'] * item.cost_price
                        db.add(models.JournalLine(entry_id=cogs_entry.id, account_id=cogs_account.id, debit=total_cost, credit=0.0))
                        db.add(models.JournalLine(entry_id=cogs_entry.id, account_id=inventory_account.id, debit=0.0, credit=total_cost))
            else:
                inventory_account = db.query(models.Account).filter(models.Account.code == "1301").first()
                if inventory_account:
                    db.add(models.JournalLine(entry_id=journal_entry.id, account_id=inventory_account.id, debit=amount_after_discount, credit=0.0))
                db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id, debit=0.0, credit=net_amount))
        
        db.commit()
        return invoice
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 3. دالة إنشاء صنف مدمج
# ==========================================
def create_kit_item(kit_name, components, sell_price, created_by=None):
    db = SessionLocal()
    try:
        kit_item = models.Item(name=kit_name, sell_price=sell_price, is_kit=True, barcode=None)
        db.add(kit_item)
        db.flush()
        
        for comp_data in components:
            component_item = db.query(models.Item).filter(models.Item.name == comp_data['item_name']).first()
            if not component_item:
                raise ValueError(f"الصنف المكون '{comp_data['item_name']}' غير موجود!")
            kit_component = models.KitComponent(
                kit_item_id=kit_item.id, component_item_id=component_item.id,
                quantity=comp_data['quantity']
            )
            db.add(kit_component)
        
        db.commit()
        return kit_item
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 4. دالة إضافة صنف عادي
# ==========================================
def create_item(name, cost_price, sell_price, barcode=None, created_by=None):
    db = SessionLocal()
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
        return item
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 5. دالة تسجيل المدفوعات
# ==========================================
def create_payment(party_id, amount, payment_type, payment_method='cash',
                   reference_number=None, notes=None, currency_id=1,
                   payment_date=None, created_by=None, cash_box_id=1):
    db = SessionLocal()
    try:
        # التحقق من الفترة المحاسبية
        period = get_period_for_date(payment_date or datetime.now())
        if period and period.status == PeriodStatus.CLOSED:
            raise ValueError(f"الفترة المحاسبية '{period.period_name}' مغلقة!")
        
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
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id, debit=amount, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id, debit=0.0, credit=amount))
        else:
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party.account_id, debit=amount, credit=0.0))
            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id, debit=0.0, credit=amount))
        
        db.commit()
        return payment
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 6. استيراد الأصناف
# ==========================================
def import_items_from_file(file_path, created_by=None):
    db = SessionLocal()
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
                    opening_movement = models.InventoryMovement(
                        item_id=item.id, date=datetime.now(),
                        type='in', quantity=current_stock, reference_id=None
                    )
                    db.add(opening_movement)
                    item.total_purchased_qty = current_stock
                
                imported_count += 1
            except Exception as e:
                db.rollback()
                errors.append(f"السطر {index + 1}: {str(e)}")
        
        db.commit()
        return imported_count, errors
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 7. استيراد العملاء/الموردين
# ==========================================
def import_parties_from_file(file_path, party_type, created_by=None):
    db = SessionLocal()
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
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id, debit=opening_balance, credit=0.0))
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id, debit=0.0, credit=opening_balance))
                        else:
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=opening_account.id, debit=opening_balance, credit=0.0))
                            db.add(models.JournalLine(entry_id=journal_entry.id, account_id=party_account.id, debit=0.0, credit=opening_balance))
                
                imported_count += 1
            except Exception as e:
                errors.append(f"السطر {index + 1}: {str(e)}")
        
        db.commit()
        return imported_count, errors
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 8. النسخ الاحتياطي
# ==========================================
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


# ==========================================
# 9. تصدير قالب Excel
# ==========================================
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
            'bold': True, 'bg_color': '#4472C4',
            'font_color': 'white', 'border': 1
        })
        for col_num, value in enumerate(df.columns.values):
            worksheet.write(0, col_num, value, header_format)
        for i in range(len(df.columns)):
            worksheet.set_column(i, i, 20)
    return output_path


# ==========================================
# 10. دالة مساعدة للحصول على اسم المستخدم
# ==========================================
def get_user_name_by_id(user_id):
    if user_id is None:
        return "النظام"
    db = SessionLocal()
    try:
        user = db.query(models.User).filter(models.User.id == user_id).first()
        if user:
            return user.full_name
        return "مستخدم غير معروف"
    finally:
        db.close()


# ==========================================
# 11. دالة حساب الربح الفعلي للصنف
# ==========================================
def calculate_item_profit(item_id):
    db = SessionLocal()
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


# ==========================================
# 12. تصدير الأصناف إلى Excel
# ==========================================
def export_items_to_excel(output_path=None):
    db = SessionLocal()
    try:
        if not output_path:
            output_path = f"Items_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"
        
        items = db.query(models.Item).all()
        
        data = []
        for item in items:
            category = db.query(Category).filter(Category.id == item.category_id).first()
            
            movements = db.query(models.InventoryMovement).filter(
                models.InventoryMovement.item_id == item.id
            ).all()
            current_stock = sum(m.quantity if m.type == 'in' else -m.quantity for m in movements)
            
            data.append({
                'id': item.id,
                'name': item.name,
                'barcode': item.barcode or '',
                'cost_price': item.cost_price,
                'sell_price': item.sell_price,
                'avg_cost_price': item.avg_cost_price,
                'is_kit': 'مدمج' if item.is_kit else 'عادي',
                'category': category.name if category else '',
                'min_stock': item.min_stock,
                'current_stock': current_stock,
                'total_purchased_qty': item.total_purchased_qty
            })
        
        df = pd.DataFrame(data)
        
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            df.to_excel(writer, sheet_name='الأصناف', index=False)
            
            workbook = writer.book
            worksheet = writer.sheets['الأصناف']
            
            header_format = workbook.add_format({
                'bold': True, 'bg_color': '#4472C4',
                'font_color': 'white', 'border': 1
            })
            
            for col_num, value in enumerate(df.columns.values):
                worksheet.write(0, col_num, value, header_format)
            
            for i in range(len(df.columns)):
                worksheet.set_column(i, i, 20)
        
        return output_path, len(items)
    finally:
        db.close()


# ==========================================
# 13. تصدير التصنيفات إلى Excel
# ==========================================
def export_categories_to_excel(output_path=None):
    db = SessionLocal()
    try:
        if not output_path:
            output_path = f"Categories_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"
        
        categories = db.query(Category).all()
        
        data = []
        for cat in categories:
            parent = db.query(Category).filter(Category.id == cat.parent_id).first()
            items_count = db.query(models.Item).filter(models.Item.category_id == cat.id).count()
            data.append({
                'id': cat.id,
                'name': cat.name,
                'description': cat.description or '',
                'parent_category': parent.name if parent else '',
                'is_active': 'نشط' if cat.is_active else 'معطل',
                'items_count': items_count
            })
        
        df = pd.DataFrame(data)
        
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            df.to_excel(writer, sheet_name='التصنيفات', index=False)
            
            workbook = writer.book
            worksheet = writer.sheets['التصنيفات']
            
            header_format = workbook.add_format({
                'bold': True, 'bg_color': '#4472C4',
                'font_color': 'white', 'border': 1
            })
            
            for col_num, value in enumerate(df.columns.values):
                worksheet.write(0, col_num, value, header_format)
            
            for i in range(len(df.columns)):
                worksheet.set_column(i, i, 20)
        
        return output_path, len(categories)
    finally:
        db.close()


# ==========================================
# 14. تصدير العملاء/الموردين إلى Excel
# ==========================================
def export_parties_to_excel(party_type, output_path=None):
    db = SessionLocal()
    try:
        if not output_path:
            type_ar = 'Customers' if party_type == 'customer' else 'Suppliers'
            output_path = f"{type_ar}_Export_{datetime.now().strftime('%Y%m%d_%H%M%S')}.xlsx"
        
        parties = db.query(models.Party).filter(models.Party.type == party_type).all()
        
        data = []
        for party in parties:
            account = db.query(models.Account).filter(models.Account.id == party.account_id).first()
            
            invoices = db.query(models.Invoice).filter(
                models.Invoice.party_id == party.id,
                models.Invoice.type == ('sale' if party_type == 'customer' else 'purchase')
            ).all()
            total_invoiced = sum(inv.net_amount for inv in invoices)
            
            payments = db.query(models.Payment).filter(
                models.Payment.party_id == party.id,
                models.Payment.payment_type == ('receipt' if party_type == 'customer' else 'payment')
            ).all()
            total_paid = sum(p.amount for p in payments)
            
            balance = total_invoiced - total_paid
            
            data.append({
                'id': party.id,
                'name': party.name,
                'phone': party.phone or '',
                'address': party.address or '',
                'account_code': account.code if account else '',
                'credit_limit': party.credit_limit,
                'total_invoiced': total_invoiced,
                'total_paid': total_paid,
                'balance': balance
            })
        
        df = pd.DataFrame(data)
        
        with pd.ExcelWriter(output_path, engine='xlsxwriter') as writer:
            sheet_name = 'العملاء' if party_type == 'customer' else 'الموردين'
            df.to_excel(writer, sheet_name=sheet_name, index=False)
            
            workbook = writer.book
            worksheet = writer.sheets[sheet_name]
            
            header_format = workbook.add_format({
                'bold': True, 'bg_color': '#4472C4',
                'font_color': 'white', 'border': 1
            })
            
            for col_num, value in enumerate(df.columns.values):
                worksheet.write(0, col_num, value, header_format)
            
            for i in range(len(df.columns)):
                worksheet.set_column(i, i, 20)
        
        return output_path, len(parties)
    finally:
        db.close()


# ==========================================
# 15. استيراد التصنيفات
# ==========================================
def import_categories_from_file(file_path, created_by=None):
    db = SessionLocal()
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
                
                category = Category(
                    name=name, description=description,
                    parent_id=parent_id, is_active=True,
                    created_at=datetime.now()
                )
                db.add(category)
                imported_count += 1
            except Exception as e:
                errors.append(f"السطر {index + 1}: {str(e)}")
        
        db.commit()
        return imported_count, errors
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 16. دوال إدارة المصروفات
# ==========================================
def create_expense_category(name, description, account_id, created_by=None):
    db = SessionLocal()
    try:
        existing = db.query(ExpenseCategory).filter(
            ExpenseCategory.name == name
        ).first()
        if existing:
            raise ValueError(f"تصنيف '{name}' موجود مسبقاً!")
        
        category = ExpenseCategory(
            name=name, description=description,
            account_id=account_id, is_active=True,
            created_at=datetime.now()
        )
        db.add(category)
        db.commit()
        return category
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def create_expense(category_id, amount, description, payment_method,
                   cash_box_id, reference_number=None, notes=None,
                   expense_date=None, created_by=None):
    db = SessionLocal()
    try:
        # التحقق من الفترة المحاسبية
        period = get_period_for_date(expense_date or datetime.now())
        if period and period.status == PeriodStatus.CLOSED:
            raise ValueError(f"الفترة المحاسبية '{period.period_name}' مغلقة!")
        
        category = db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
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
        
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=category.account_id, debit=amount, credit=0.0))
        db.add(models.JournalLine(entry_id=journal_entry.id, account_id=cash_box.account_id, debit=0.0, credit=amount))
        
        db.commit()
        return expense
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_expenses_summary(start_date=None, end_date=None):
    db = SessionLocal()
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
    db = SessionLocal()
    try:
        expense = db.query(Expense).filter(Expense.id == expense_id).first()
        if not expense:
            raise ValueError("المصروف غير موجود!")
        
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
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 17. دوال إدارة المخازن
# ==========================================
def create_warehouse(name, code, location=None, responsible_person=None, notes=None):
    db = SessionLocal()
    try:
        existing = db.query(Warehouse).filter(Warehouse.code == code).first()
        if existing:
            raise ValueError(f"كود المخزن '{code}' موجود مسبقاً!")
        
        warehouse = Warehouse(
            name=name, code=code, location=location,
            responsible_person=responsible_person,
            notes=notes, is_active=True
        )
        db.add(warehouse)
        db.commit()
        return warehouse
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_stock_level(warehouse_id, item_id):
    db = SessionLocal()
    try:
        stock = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.item_id == item_id
        ).first()
        return stock.quantity if stock else 0.0
    finally:
        db.close()


def update_stock_level(warehouse_id, item_id, quantity):
    db = SessionLocal()
    try:
        stock = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.item_id == item_id
        ).first()
        
        if stock:
            stock.quantity = quantity
            stock.last_updated = datetime.now()
        else:
            stock = StockLevel(
                warehouse_id=warehouse_id,
                item_id=item_id,
                quantity=quantity
            )
            db.add(stock)
        
        db.commit()
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def transfer_between_warehouses(from_warehouse_id, to_warehouse_id, item_id, quantity, notes=None, created_by=None):
    db = SessionLocal()
    try:
        if from_warehouse_id == to_warehouse_id:
            raise ValueError("لا يمكن التحويل بين نفس المخزن!")
        
        current_stock = get_stock_level(from_warehouse_id, item_id)
        if current_stock < quantity:
            raise ValueError(f"الرصيد غير كافٍ! الرصيد الحالي: {current_stock}")
        
        transfer = WarehouseTransfer(
            date=datetime.now(),
            from_warehouse_id=from_warehouse_id,
            to_warehouse_id=to_warehouse_id,
            item_id=item_id,
            quantity=quantity,
            notes=notes,
            created_by=created_by,
            status='completed'
        )
        db.add(transfer)
        
        update_stock_level(from_warehouse_id, item_id, current_stock - quantity)
        
        to_stock = get_stock_level(to_warehouse_id, item_id)
        update_stock_level(to_warehouse_id, item_id, to_stock + quantity)
        
        db.commit()
        return transfer
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def create_stock_count(warehouse_id, item_id, counted_quantity, counted_by=None, notes=None):
    db = SessionLocal()
    try:
        system_quantity = get_stock_level(warehouse_id, item_id)
        difference = counted_quantity - system_quantity
        
        stock_count = StockCount(
            date=datetime.now(),
            warehouse_id=warehouse_id,
            item_id=item_id,
            system_quantity=system_quantity,
            counted_quantity=counted_quantity,
            difference=difference,
            notes=notes,
            counted_by=counted_by
        )
        db.add(stock_count)
        
        update_stock_level(warehouse_id, item_id, counted_quantity)
        
        db.commit()
        return stock_count
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_warehouse_stock_report(warehouse_id):
    db = SessionLocal()
    try:
        stocks = db.query(StockLevel).filter(
            StockLevel.warehouse_id == warehouse_id,
            StockLevel.quantity > 0
        ).all()
        
        report = []
        for stock in stocks:
            item = db.query(models.Item).filter(models.Item.id == stock.item_id).first()
            if item:
                report.append({
                    'item_id': item.id,
                    'item_name': item.name,
                    'barcode': item.barcode,
                    'quantity': stock.quantity,
                    'min_stock': item.min_stock,
                    'status': 'منخفض' if stock.quantity <= item.min_stock else 'جيد'
                })
        
        return report
    finally:
        db.close()


# ==========================================
# 18. دوال إدارة الأصول الثابتة
# ==========================================
def create_fixed_asset(
    name, code, category, purchase_date, purchase_cost,
    salvage_value, useful_life_years, depreciation_method,
    location=None, responsible_person=None, notes=None, created_by=None
):
    db = SessionLocal()
    try:
        existing = db.query(FixedAsset).filter(FixedAsset.code == code).first()
        if existing:
            raise ValueError(f"كود الأصل '{code}' موجود مسبقاً!")
        
        net_book_value = purchase_cost
        
        asset = FixedAsset(
            name=name, code=code, category=category,
            purchase_date=purchase_date, purchase_cost=purchase_cost,
            salvage_value=salvage_value, useful_life_years=useful_life_years,
            depreciation_method=depreciation_method,
            accumulated_depreciation=0.0, net_book_value=net_book_value,
            location=location, responsible_person=responsible_person,
            notes=notes, status='active', created_by=created_by
        )
        db.add(asset)
        db.commit()
        return asset
    except Exception as e:
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
    else:
        return 0.0


def run_annual_depreciation(asset_id, year, created_by=None):
    db = SessionLocal()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            raise ValueError("الأصل غير موجود!")
        
        if asset.status != 'active':
            raise ValueError("الأصل غير نشط!")
        
        year_expr = func.to_char(DepreciationRecord.date, literal_column("'YYYY'"))
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
            asset_id=asset_id, date=datetime(year, 12, 31),
            depreciation_amount=depreciation_amount,
            accumulated_depreciation=asset.accumulated_depreciation,
            net_book_value=asset.net_book_value,
            notes=f"إهلاك سنة {year}", created_by=created_by
        )
        db.add(record)
        
        journal_entry = models.JournalEntry(
            date=datetime(year, 12, 31),
            description=f"إهلاك سنوي {year} - {asset.name} ({asset.code})",
            reference_type="depreciation", reference_id=asset_id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        depreciation_expense_account = db.query(models.Account).filter(
            models.Account.code == "5201"
        ).first()
        
        accumulated_depreciation_account = db.query(models.Account).filter(
            models.Account.code == "1501"
        ).first()
        
        if depreciation_expense_account and accumulated_depreciation_account:
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=depreciation_expense_account.id,
                debit=depreciation_amount, credit=0.0
            ))
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=accumulated_depreciation_account.id,
                debit=0.0, credit=depreciation_amount
            ))
        
        db.commit()
        return record
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_asset_depreciation_schedule(asset_id):
    db = SessionLocal()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            return None
        
        schedule = []
        current_value = asset.purchase_cost
        annual_depreciation = (asset.purchase_cost - asset.salvage_value) / asset.useful_life_years
        
        for year in range(1, asset.useful_life_years + 1):
            depreciation = annual_depreciation
            if current_value - depreciation < asset.salvage_value:
                depreciation = current_value - asset.salvage_value
            
            current_value -= depreciation
            
            schedule.append({
                'السنة': year,
                'مبلغ الإهلاك': depreciation,
                'مجمع الإهلاك': asset.purchase_cost - current_value,
                'صافي القيمة الدفترية': current_value
            })
        
        return {'asset': asset, 'schedule': schedule}
    finally:
        db.close()


def dispose_asset(asset_id, disposal_date, disposal_value, notes=None, created_by=None):
    db = SessionLocal()
    try:
        asset = db.query(FixedAsset).filter(FixedAsset.id == asset_id).first()
        if not asset:
            raise ValueError("الأصل غير موجود!")
        
        gain_loss = disposal_value - asset.net_book_value
        asset.status = 'sold' if disposal_value > 0 else 'scrapped'
        
        journal_entry = models.JournalEntry(
            date=disposal_date,
            description=f"التخلص من الأصل: {asset.name} ({asset.code})",
            reference_type="asset_disposal", reference_id=asset_id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        fixed_asset_account = db.query(models.Account).filter(
            models.Account.code == "1500"
        ).first()
        
        accumulated_depreciation_account = db.query(models.Account).filter(
            models.Account.code == "1501"
        ).first()
        
        if fixed_asset_account and accumulated_depreciation_account:
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=accumulated_depreciation_account.id,
                debit=asset.accumulated_depreciation, credit=0.0
            ))
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=fixed_asset_account.id,
                debit=0.0, credit=asset.purchase_cost
            ))
        
        db.commit()
        return {'asset': asset, 'gain_loss': gain_loss}
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 19. دوال تحليل الربحية المتقدم
# ==========================================

def get_profitability_by_item(start_date=None, end_date=None):
    db = SessionLocal()
    try:
        query = db.query(
            models.Item.id,
            models.Item.name,
            models.Item.category_id,
            func.sum(models.InvoiceLine.quantity).label('total_qty'),
            func.sum(models.InvoiceLine.total).label('total_revenue'),
            func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price).label('total_cost')
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
        
        results = query.group_by(
            models.Item.id, models.Item.name, models.Item.category_id
        ).order_by(
            func.sum(models.InvoiceLine.total * (1 - models.InvoiceLine.cost_price / models.InvoiceLine.price)).desc()
        ).all()
        
        profitability_data = []
        for r in results:
            profit = r.total_revenue - r.total_cost
            profit_margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            
            category = db.query(Category).filter(Category.id == r.category_id).first()
            
            profitability_data.append({
                'item_id': r.id,
                'item_name': r.name,
                'category': category.name if category else 'بدون',
                'quantity_sold': r.total_qty,
                'revenue': r.total_revenue,
                'cost': r.total_cost,
                'profit': profit,
                'profit_margin': profit_margin
            })
        
        return profitability_data
    finally:
        db.close()


def get_profitability_by_customer(start_date=None, end_date=None):
    db = SessionLocal()
    try:
        query = db.query(
            models.Party.id,
            models.Party.name,
            func.sum(models.Invoice.net_amount).label('total_revenue'),
            func.count(models.Invoice.id).label('invoice_count')
        ).join(
            models.Invoice, models.Party.id == models.Invoice.party_id
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Party.type == 'customer'
        )
        
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)
        
        results = query.group_by(
            models.Party.id, models.Party.name
        ).order_by(
            func.sum(models.Invoice.net_amount).desc()
        ).all()
        
        customer_data = []
        for r in results:
            cost_query = db.query(
                func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)
            ).join(
                models.Invoice, models.InvoiceLine.invoice_id == models.Invoice.id
            ).filter(
                models.Invoice.party_id == r.id,
                models.Invoice.type == 'sale',
                models.Invoice.status != 'cancelled'
            )
            
            if start_date:
                cost_query = cost_query.filter(models.Invoice.date >= start_date)
            if end_date:
                cost_query = cost_query.filter(models.Invoice.date <= end_date)
            
            total_cost = cost_query.scalar() or 0.0
            profit = r.total_revenue - total_cost
            profit_margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            
            customer_data.append({
                'customer_id': r.id,
                'customer_name': r.name,
                'invoice_count': r.invoice_count,
                'revenue': r.total_revenue,
                'cost': total_cost,
                'profit': profit,
                'profit_margin': profit_margin
            })
        
        return customer_data
    finally:
        db.close()


def get_profitability_by_category(start_date=None, end_date=None):
    db = SessionLocal()
    try:
        query = db.query(
            Category.id,
            Category.name,
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
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled'
        )
        
        if start_date:
            query = query.filter(models.Invoice.date >= start_date)
        if end_date:
            query = query.filter(models.Invoice.date <= end_date)
        
        results = query.group_by(
            Category.id, Category.name
        ).order_by(
            func.sum(models.InvoiceLine.total).desc()
        ).all()
        
        category_data = []
        for r in results:
            profit = r.total_revenue - r.total_cost
            profit_margin = (profit / r.total_revenue * 100) if r.total_revenue > 0 else 0
            
            category_data.append({
                'category_id': r.id,
                'category_name': r.name,
                'quantity_sold': r.total_qty,
                'revenue': r.total_revenue,
                'cost': r.total_cost,
                'profit': profit,
                'profit_margin': profit_margin
            })
        
        return category_data
    finally:
        db.close()


def get_monthly_profitability(year=None):
    """الربحية الشهرية — تحسب تكلفة الأصناف المدمجة (Kits) من مكوناتها."""
    db = SessionLocal()
    try:
        if year is None:
            year = datetime.now().year

        # ✅ الإيرادات: نستعلم مباشرة (بدون subquery iteration)
        month_expr_inv = func.to_char(models.Invoice.date, literal_column("'MM'"))
        year_expr_inv = func.to_char(models.Invoice.date, literal_column("'YYYY'"))

        revenue_rows = db.query(
            month_expr_inv.label('month'),
            func.sum(models.Invoice.net_amount).label('revenue'),
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            year_expr_inv == str(year),
        ).group_by(month_expr_inv).all()

        revenue_by_month = {
            r.month: float(r.revenue or 0)
            for r in revenue_rows
        }

        # ✅ التكلفة: نحسبها برمجيًا لكل سطر
        month_expr_line = func.to_char(models.Invoice.date, literal_column("'MM'"))
        year_expr_line = func.to_char(models.Invoice.date, literal_column("'YYYY'"))

        invoice_lines = db.query(
            month_expr_line.label('month'),
            models.InvoiceLine.quantity,
            models.InvoiceLine.cost_price,
            models.Item.id.label('item_id'),
            models.Item.is_kit,
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

        # تجميع التكلفة لكل شهر
        cost_by_month = {}
        for row in invoice_lines:
            month = row.month
            qty = float(row.quantity or 0)

            if row.is_kit:
                # صنف مدمج: نحسب من المكونات
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
                # صنف عادي
                unit_cost = float(row.cost_price or 0)
                if unit_cost == 0:
                    unit_cost = float(row.item_cost or 0)
                if unit_cost == 0:
                    unit_cost = float(row.avg_cost_price or 0)

            cost_by_month[month] = cost_by_month.get(month, 0.0) + (qty * unit_cost)

        # دمج الأشهر
        all_months = sorted(set(list(revenue_by_month.keys()) + list(cost_by_month.keys())))
        monthly_data = []
        for m in all_months:
            revenue = revenue_by_month.get(m, 0.0)
            cost = cost_by_month.get(m, 0.0)
            profit = revenue - cost
            profit_margin = (profit / revenue * 100) if revenue > 0 else 0
            monthly_data.append({
                'month': m,
                'month_name': datetime.strptime(m, '%m').strftime('%B'),
                'revenue': revenue,
                'cost': cost,
                'profit': profit,
                'profit_margin': profit_margin,
            })
        return monthly_data
    finally:
        db.close()



def compare_periods(period1_start, period1_end, period2_start, period2_end):
    db = SessionLocal()
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
            profit_margin = (profit / revenue * 100) if revenue > 0 else 0
            
            return {
                'revenue': revenue,
                'cost': cost,
                'profit': profit,
                'profit_margin': profit_margin
            }
        
        period1 = get_period_data(period1_start, period1_end)
        period2 = get_period_data(period2_start, period2_end)
        
        revenue_change = ((period1['revenue'] - period2['revenue']) / period2['revenue'] * 100) if period2['revenue'] > 0 else 0
        profit_change = ((period1['profit'] - period2['profit']) / period2['profit'] * 100) if period2['profit'] > 0 else 0
        
        return {
            'period1': period1,
            'period2': period2,
            'revenue_change': revenue_change,
            'profit_change': profit_change
        }
    finally:
        db.close()


def get_top_profitable_items(limit=10, start_date=None, end_date=None):
    db = SessionLocal()
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
        
        results = query.group_by(
            models.Item.name
        ).order_by(
            func.sum(models.InvoiceLine.total - (models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).desc()
        ).limit(limit).all()
        
        top_items = []
        for r in results:
            profit = r.revenue - r.cost
            profit_margin = (profit / r.revenue * 100) if r.revenue > 0 else 0
            
            top_items.append({
                'item_name': r.name,
                'quantity': r.qty,
                'revenue': r.revenue,
                'cost': r.cost,
                'profit': profit,
                'profit_margin': profit_margin
            })
        
        return top_items
    finally:
        db.close()


def get_profitability_summary(start_date=None, end_date=None):
    db = SessionLocal()
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
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled'
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
        
        gross_profit = total_revenue - total_cost
        net_profit = gross_profit - total_expenses
        gross_margin = (gross_profit / total_revenue * 100) if total_revenue > 0 else 0
        net_margin = (net_profit / total_revenue * 100) if total_revenue > 0 else 0
        
        return {
            'revenue': total_revenue,
            'cost': total_cost,
            'gross_profit': gross_profit,
            'expenses': total_expenses,
            'net_profit': net_profit,
            'gross_margin': gross_margin,
            'net_margin': net_margin
        }
    finally:
        db.close()


# ==========================================
# 20. دوال إدارة القروض
# ==========================================

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
    remaining_balance = principal_amount
    
    for i in range(1, term_months + 1):
        interest_amount = remaining_balance * monthly_rate
        principal_payment = monthly_payment - interest_amount
        remaining_balance -= principal_payment
        
        due_date = loan_date + relativedelta(months=i)
        
        schedule.append({
            'installment_number': i,
            'due_date': due_date,
            'principal_amount': principal_payment,
            'interest_amount': interest_amount,
            'total_amount': monthly_payment,
            'remaining_balance': max(0, remaining_balance)
        })
    
    return schedule, monthly_payment, total_interest


def create_loan(
    loan_type, borrower_name, borrower_type, principal_amount,
    annual_interest_rate, loan_date, term_months,
    cash_box_id=None, notes=None, created_by=None
):
    db = SessionLocal()
    try:
        loan_count = db.query(Loan).count()
        loan_number = f"LOAN-{loan_count + 1:06d}"
        
        schedule, monthly_payment, total_interest = generate_installment_schedule(
            principal_amount, annual_interest_rate, term_months, loan_date
        )
        
        total_amount = principal_amount + total_interest
        
        loan = Loan(
            loan_number=loan_number,
            loan_type=loan_type,
            borrower_name=borrower_name,
            borrower_type=borrower_type,
            principal_amount=principal_amount,
            interest_rate=annual_interest_rate,
            loan_date=loan_date,
            maturity_date=loan_date + relativedelta(months=term_months),
            term_months=term_months,
            installment_amount=monthly_payment,
            total_interest=total_interest,
            total_amount=total_amount,
            paid_amount=0.0,
            remaining_amount=total_amount,
            status=LoanStatus.ACTIVE,
            cash_box_id=cash_box_id,
            notes=notes,
            created_by=created_by
        )
        db.add(loan)
        db.flush()
        
        for inst_data in schedule:
            installment = LoanInstallment(
                loan_id=loan.id,
                installment_number=inst_data['installment_number'],
                due_date=inst_data['due_date'],
                principal_amount=inst_data['principal_amount'],
                interest_amount=inst_data['interest_amount'],
                total_amount=inst_data['total_amount'],
                paid_amount=0.0,
                status='pending'
            )
            db.add(installment)
        
        journal_entry = models.JournalEntry(
            date=loan_date,
            description=f"قرض {loan_type.value} رقم {loan_number} - {borrower_name}",
            reference_type="loan",
            reference_id=loan.id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        if loan_type == LoanType.RECEIVED:
            if cash_box_id:
                cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cash_box:
                    db.add(models.JournalLine(
                        entry_id=journal_entry.id,
                        account_id=cash_box.account_id,
                        debit=principal_amount,
                        credit=0.0
                    ))
            
            loans_payable_account = db.query(models.Account).filter(
                models.Account.code == "2301"
            ).first()
            
            if loans_payable_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=loans_payable_account.id,
                    debit=0.0,
                    credit=principal_amount
                ))
        else:
            loans_receivable_account = db.query(models.Account).filter(
                models.Account.code == "1250"
            ).first()
            
            if loans_receivable_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=loans_receivable_account.id,
                    debit=principal_amount,
                    credit=0.0
                ))
            
            if cash_box_id:
                cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cash_box:
                    db.add(models.JournalLine(
                        entry_id=journal_entry.id,
                        account_id=cash_box.account_id,
                        debit=0.0,
                        credit=principal_amount
                    ))
        
        db.commit()
        return loan
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def pay_loan_installment(installment_id, payment_date=None, cash_box_id=None, notes=None, created_by=None):
    db = SessionLocal()
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
            reference_type="loan_payment",
            reference_id=loan.id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        if loan.loan_type == LoanType.RECEIVED:
            loans_payable_account = db.query(models.Account).filter(
                models.Account.code == "2301"
            ).first()
            
            if loans_payable_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=loans_payable_account.id,
                    debit=installment.principal_amount,
                    credit=0.0
                ))
            
            interest_expense_account = db.query(models.Account).filter(
                models.Account.code == "5301"
            ).first()
            
            if interest_expense_account and installment.interest_amount > 0:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=interest_expense_account.id,
                    debit=installment.interest_amount,
                    credit=0.0
                ))
            
            if cash_box_id:
                cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cash_box:
                    db.add(models.JournalLine(
                        entry_id=journal_entry.id,
                        account_id=cash_box.account_id,
                        debit=0.0,
                        credit=installment.total_amount
                    ))
        else:
            if cash_box_id:
                cash_box = db.query(CashBox).filter(CashBox.id == cash_box_id).first()
                if cash_box:
                    db.add(models.JournalLine(
                        entry_id=journal_entry.id,
                        account_id=cash_box.account_id,
                        debit=installment.total_amount,
                        credit=0.0
                    ))
            
            loans_receivable_account = db.query(models.Account).filter(
                models.Account.code == "1250"
            ).first()
            
            if loans_receivable_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=loans_receivable_account.id,
                    debit=0.0,
                    credit=installment.principal_amount
                ))
            
            interest_income_account = db.query(models.Account).filter(
                models.Account.code == "4201"
            ).first()
            
            if interest_income_account and installment.interest_amount > 0:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=interest_income_account.id,
                    debit=0.0,
                    credit=installment.interest_amount
                ))
        
        db.commit()
        return installment
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_loans_summary(loan_type=None):
    db = SessionLocal()
    try:
        query = db.query(Loan)
        
        if loan_type:
            query = query.filter(Loan.loan_type == loan_type)
        
        loans = query.all()
        
        total_principal = sum(l.principal_amount for l in loans)
        total_interest = sum(l.total_interest for l in loans)
        total_amount = sum(l.total_amount for l in loans)
        total_paid = sum(l.paid_amount for l in loans)
        total_remaining = sum(l.remaining_amount for l in loans)
        
        active_count = len([l for l in loans if l.status == LoanStatus.ACTIVE])
        completed_count = len([l for l in loans if l.status == LoanStatus.COMPLETED])
        
        return {
            'loans': loans,
            'total_principal': total_principal,
            'total_interest': total_interest,
            'total_amount': total_amount,
            'total_paid': total_paid,
            'total_remaining': total_remaining,
            'active_count': active_count,
            'completed_count': completed_count
        }
    finally:
        db.close()


def get_loan_schedule(loan_id):
    db = SessionLocal()
    try:
        loan = db.query(Loan).filter(Loan.id == loan_id).first()
        if not loan:
            return None
        
        installments = db.query(LoanInstallment).filter(
            LoanInstallment.loan_id == loan_id
        ).order_by(LoanInstallment.installment_number).all()
        
        schedule = []
        for inst in installments:
            schedule.append({
                'id': inst.id,
                'number': inst.installment_number,
                'due_date': inst.due_date,
                'principal': inst.principal_amount,
                'interest': inst.interest_amount,
                'total': inst.total_amount,
                'paid': inst.paid_amount,
                'paid_date': inst.paid_date,
                'status': inst.status
            })
        
        return {'loan': loan, 'schedule': schedule}
    finally:
        db.close()


def check_overdue_installments():
    db = SessionLocal()
    try:
        today = datetime.now()
        
        overdue = db.query(LoanInstallment).join(Loan).filter(
            LoanInstallment.status == 'pending',
            LoanInstallment.due_date < today,
            Loan.status == LoanStatus.ACTIVE
        ).all()
        
        overdue_list = []
        for inst in overdue:
            loan = db.query(Loan).filter(Loan.id == inst.loan_id).first()
            days_overdue = (today - inst.due_date).days
            
            overdue_list.append({
                'loan_number': loan.loan_number,
                'borrower': loan.borrower_name,
                'loan_type': loan.loan_type.value,
                'installment_number': inst.installment_number,
                'due_date': inst.due_date,
                'amount': inst.total_amount,
                'days_overdue': days_overdue
            })
        
        return overdue_list
    finally:
        db.close()


# ==========================================
# 21. دوال إدارة الموظفين والرواتب
# ==========================================

def create_employee(
    employee_code, full_name, job_title, hire_date, basic_salary,
    national_id=None, phone=None, email=None, address=None,
    department=None, allowances=0.0, deductions=0.0,
    social_insurance=0.0, tax_rate=0.0,
    bank_account=None, bank_name=None,
    notes=None, created_by=None
):
    db = SessionLocal()
    try:
        existing = db.query(Employee).filter(Employee.employee_code == employee_code).first()
        if existing:
            raise ValueError(f"كود الموظف '{employee_code}' موجود مسبقاً!")
        
        employee = Employee(
            employee_code=employee_code,
            full_name=full_name,
            national_id=national_id,
            phone=phone,
            email=email,
            address=address,
            job_title=job_title,
            department=department,
            hire_date=hire_date,
            basic_salary=basic_salary,
            allowances=allowances,
            deductions=deductions,
            social_insurance=social_insurance,
            tax_rate=tax_rate,
            bank_account=bank_account,
            bank_name=bank_name,
            status=EmploymentStatus.ACTIVE,
            notes=notes,
            created_by=created_by
        )
        db.add(employee)
        db.commit()
        return employee
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def calculate_monthly_salary(employee_id, month, year, overtime=0.0, bonus=0.0, other_deductions=0.0):
    db = SessionLocal()
    try:
        employee = db.query(Employee).filter(Employee.id == employee_id).first()
        if not employee:
            raise ValueError("الموظف غير موجود!")
        
        if employee.status != EmploymentStatus.ACTIVE:
            raise ValueError("الموظف غير نشط!")
        
        gross_salary = employee.basic_salary + employee.allowances + overtime + bonus
        
        social_insurance_amount = gross_salary * (employee.social_insurance / 100)
        tax_amount = gross_salary * (employee.tax_rate / 100)
        total_deductions = social_insurance_amount + tax_amount + employee.deductions + other_deductions
        
        net_salary = gross_salary - total_deductions
        
        return {
            'employee': employee,
            'month': month,
            'year': year,
            'basic_salary': employee.basic_salary,
            'allowances': employee.allowances,
            'overtime': overtime,
            'bonus': bonus,
            'gross_salary': gross_salary,
            'social_insurance': social_insurance_amount,
            'tax': tax_amount,
            'other_deductions': other_deductions,
            'total_deductions': total_deductions,
            'net_salary': net_salary
        }
    finally:
        db.close()


def process_monthly_salary(employee_id, month, year, overtime=0.0, bonus=0.0, 
                           other_deductions=0.0, payment_date=None, 
                           payment_method='bank_transfer', notes=None, created_by=None):
    db = SessionLocal()
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
        
        salary_record = SalaryRecord(
            employee_id=employee_id,
            month=month,
            year=year,
            basic_salary=salary_data['basic_salary'],
            allowances=salary_data['allowances'],
            overtime=overtime,
            bonus=bonus,
            gross_salary=salary_data['gross_salary'],
            social_insurance=salary_data['social_insurance'],
            tax=salary_data['tax'],
            other_deductions=other_deductions,
            total_deductions=salary_data['total_deductions'],
            net_salary=salary_data['net_salary'],
            payment_date=payment_date,
            payment_method=payment_method,
            status='paid',
            notes=notes,
            created_by=created_by
        )
        db.add(salary_record)
        
        journal_entry = models.JournalEntry(
            date=payment_date,
            description=f"راتب شهر {month}/{year} - {salary_data['employee'].full_name}",
            reference_type="salary",
            reference_id=employee_id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        salary_expense_account = db.query(models.Account).filter(
            models.Account.code == "5401"
        ).first()
        
        if salary_expense_account:
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=salary_expense_account.id,
                debit=salary_data['gross_salary'],
                credit=0.0
            ))
        
        if salary_data['social_insurance'] > 0:
            social_insurance_account = db.query(models.Account).filter(
                models.Account.code == "2401"
            ).first()
            
            if social_insurance_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=social_insurance_account.id,
                    debit=0.0,
                    credit=salary_data['social_insurance']
                ))
        
        if salary_data['tax'] > 0:
            tax_account = db.query(models.Account).filter(
                models.Account.code == "2402"
            ).first()
            
            if tax_account:
                db.add(models.JournalLine(
                    entry_id=journal_entry.id,
                    account_id=tax_account.id,
                    debit=0.0,
                    credit=salary_data['tax']
                ))
        
        db.add(models.JournalLine(
            entry_id=journal_entry.id,
            account_id=1,
            debit=0.0,
            credit=salary_data['net_salary']
        ))
        
        db.commit()
        return salary_record
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_employee_salary_history(employee_id):
    db = SessionLocal()
    try:
        records = db.query(SalaryRecord).filter(
            SalaryRecord.employee_id == employee_id
        ).order_by(SalaryRecord.year.desc(), SalaryRecord.month.desc()).all()
        
        history = []
        for record in records:
            history.append({
                'id': record.id,
                'month': record.month,
                'year': record.year,
                'gross_salary': record.gross_salary,
                'deductions': record.total_deductions,
                'net_salary': record.net_salary,
                'payment_date': record.payment_date,
                'status': record.status
            })
        
        return history
    finally:
        db.close()


def get_monthly_payroll(month=None, year=None):
    db = SessionLocal()
    try:
        if month is None:
            month = datetime.now().month
        if year is None:
            year = datetime.now().year
        
        records = db.query(SalaryRecord).filter(
            SalaryRecord.month == month,
            SalaryRecord.year == year
        ).all()
        
        payroll = []
        total_gross = 0
        total_deductions = 0
        total_net = 0
        
        for record in records:
            employee = db.query(Employee).filter(Employee.id == record.employee_id).first()
            
            payroll.append({
                'employee_code': employee.employee_code if employee else '-',
                'employee_name': employee.full_name if employee else '-',
                'job_title': employee.job_title if employee else '-',
                'department': employee.department if employee else '-',
                'basic_salary': record.basic_salary,
                'allowances': record.allowances,
                'overtime': record.overtime,
                'bonus': record.bonus,
                'gross_salary': record.gross_salary,
                'social_insurance': record.social_insurance,
                'tax': record.tax,
                'other_deductions': record.other_deductions,
                'total_deductions': record.total_deductions,
                'net_salary': record.net_salary,
                'status': record.status
            })
            
            total_gross += record.gross_salary
            total_deductions += record.total_deductions
            total_net += record.net_salary
        
        return {
            'payroll': payroll,
            'total_gross': total_gross,
            'total_deductions': total_deductions,
            'total_net': total_net,
            'month': month,
            'year': year
        }
    finally:
        db.close()


def get_employees_summary():
    db = SessionLocal()
    try:
        employees = db.query(Employee).all()
        
        active_count = len([e for e in employees if e.status == EmploymentStatus.ACTIVE])
        total_salaries = sum(e.basic_salary + e.allowances for e in employees if e.status == EmploymentStatus.ACTIVE)
        
        departments = {}
        for emp in employees:
            if emp.department:
                if emp.department not in departments:
                    departments[emp.department] = 0
                departments[emp.department] += 1
        
        return {
            'total_employees': len(employees),
            'active_employees': active_count,
            'total_monthly_salaries': total_salaries,
            'departments': departments
        }
    finally:
        db.close()


# ==========================================
# 22. دوال التقارير المالية المتقدمة
# ==========================================

def get_income_statement(start_date, end_date):
    """قائمة الدخل"""
    db = SessionLocal()
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
        
        gross_profit = revenue - cogs
        
        operating_expenses = db.query(func.sum(Expense.amount)).filter(
            Expense.date >= start_date,
            Expense.date <= end_date
        ).scalar() or 0.0
        
        salary_expenses = db.query(func.sum(SalaryRecord.gross_salary)).filter(
            SalaryRecord.payment_date >= start_date,
            SalaryRecord.payment_date <= end_date
        ).scalar() or 0.0
        
        depreciation_expenses = db.query(func.sum(DepreciationRecord.depreciation_amount)).filter(
            DepreciationRecord.date >= start_date,
            DepreciationRecord.date <= end_date
        ).scalar() or 0.0
        
        total_expenses = operating_expenses + salary_expenses + depreciation_expenses
        net_profit = gross_profit - total_expenses
        
        return {
            'revenue': revenue,
            'cogs': cogs,
            'gross_profit': gross_profit,
            'operating_expenses': operating_expenses,
            'salary_expenses': salary_expenses,
            'depreciation_expenses': depreciation_expenses,
            'total_expenses': total_expenses,
            'net_profit': net_profit,
            'gross_margin': (gross_profit / revenue * 100) if revenue > 0 else 0,
            'net_margin': (net_profit / revenue * 100) if revenue > 0 else 0
        }
    finally:
        db.close()


def get_balance_sheet(as_of_date):
    """الميزانية العمومية"""
    db = SessionLocal()
    try:
        assets = {}
        asset_accounts = db.query(models.Account).filter(models.Account.type == AccountType.ASSET).all()
        for acc in asset_accounts:
            if acc.parent_id is None:
                continue
            
            debit = db.query(func.sum(models.JournalLine.debit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            credit = db.query(func.sum(models.JournalLine.credit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            balance = debit - credit
            if balance > 0:
                assets[acc.name] = balance
        
        total_assets = sum(assets.values())
        
        liabilities = {}
        liability_accounts = db.query(models.Account).filter(models.Account.type == AccountType.LIABILITY).all()
        for acc in liability_accounts:
            if acc.parent_id is None:
                continue
            
            debit = db.query(func.sum(models.JournalLine.debit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            credit = db.query(func.sum(models.JournalLine.credit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            balance = credit - debit
            if balance > 0:
                liabilities[acc.name] = balance
        
        total_liabilities = sum(liabilities.values())
        
        equity = {}
        equity_accounts = db.query(models.Account).filter(models.Account.type == AccountType.EQUITY).all()
        for acc in equity_accounts:
            if acc.parent_id is None:
                continue
            
            debit = db.query(func.sum(models.JournalLine.debit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            credit = db.query(func.sum(models.JournalLine.credit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date <= as_of_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            balance = credit - debit
            if balance > 0:
                equity[acc.name] = balance
        
        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date <= as_of_date
        ).scalar() or 0.0
        
        expenses = db.query(func.sum(Expense.amount)).filter(
            Expense.date <= as_of_date
        ).scalar() or 0.0
        
        retained_earnings = revenue - expenses
        equity['الأرباح المحتجزة'] = retained_earnings
        
        total_equity = sum(equity.values())
        
        return {
            'assets': assets,
            'total_assets': total_assets,
            'liabilities': liabilities,
            'total_liabilities': total_liabilities,
            'equity': equity,
            'total_equity': total_equity
        }
    finally:
        db.close()


def get_trial_balance(start_date, end_date):
    """ميزان المراجعة"""
    db = SessionLocal()
    try:
        accounts = db.query(models.Account).all()
        
        trial_balance = []
        total_debit = 0
        total_credit = 0
        
        for acc in accounts:
            debit = db.query(func.sum(models.JournalLine.debit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date >= start_date,
                models.JournalEntry.date <= end_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            credit = db.query(func.sum(models.JournalLine.credit)).filter(
                models.JournalLine.account_id == acc.id,
                models.JournalEntry.date >= start_date,
                models.JournalEntry.date <= end_date
            ).join(models.JournalEntry).scalar() or 0.0
            
            if debit > 0 or credit > 0:
                balance = debit - credit
                trial_balance.append({
                    'account_code': acc.code,
                    'account_name': acc.name,
                    'debit': debit,
                    'credit': credit,
                    'balance': balance,
                    'type': acc.type.value
                })
                
                total_debit += debit
                total_credit += credit
        
        return {
            'accounts': trial_balance,
            'total_debit': total_debit,
            'total_credit': total_credit,
            'difference': total_debit - total_credit
        }
    finally:
        db.close()


def get_cash_flow_statement(start_date, end_date):
    """قائمة التدفقات النقدية"""
    db = SessionLocal()
    try:
        cash_from_operations = db.query(
            func.sum(models.Payment.amount)
        ).filter(
            models.Payment.payment_type == 'receipt',
            models.Payment.date >= start_date,
            models.Payment.date <= end_date
        ).scalar() or 0.0
        
        cash_to_operations = db.query(
            func.sum(models.Payment.amount)
        ).filter(
            models.Payment.payment_type == 'payment',
            models.Payment.date >= start_date,
            models.Payment.date <= end_date
        ).scalar() or 0.0
        
        cash_from_investing = db.query(
            func.sum(models.JournalLine.debit)
        ).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('15%'))
            ),
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).join(models.JournalEntry).scalar() or 0.0
        
        cash_from_financing = db.query(
            func.sum(models.JournalLine.credit)
        ).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('23%'))
            ),
            models.JournalEntry.date >= start_date,
            models.JournalEntry.date <= end_date
        ).join(models.JournalEntry).scalar() or 0.0
        
        net_cash_flow = cash_from_operations - cash_to_operations + cash_from_investing + cash_from_financing
        
        return {
            'cash_from_operations': cash_from_operations,
            'cash_to_operations': cash_to_operations,
            'cash_from_investing': cash_from_investing,
            'cash_from_financing': cash_from_financing,
            'net_cash_flow': net_cash_flow
        }
    finally:
        db.close()


def get_financial_ratios(start_date, end_date):
    """النسب المالية"""
    db = SessionLocal()
    try:
        current_assets = db.query(func.sum(models.JournalLine.debit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('11%'))
            )
        ).scalar() or 0.0
        
        current_liabilities = db.query(func.sum(models.JournalLine.credit)).filter(
            models.JournalLine.account_id.in_(
                db.query(models.Account.id).filter(models.Account.code.like('21%'))
            )
        ).scalar() or 0.0
        
        current_ratio = current_assets / current_liabilities if current_liabilities > 0 else 0
        
        revenue = db.query(func.sum(models.InvoiceLine.total)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0
        
        net_profit = db.query(func.sum(models.InvoiceLine.total - models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0
        
        profit_margin = (net_profit / revenue * 100) if revenue > 0 else 0
        
        inventory = db.query(func.sum(StockLevel.quantity * models.Item.cost_price)).join(
            models.Item
        ).scalar() or 0.0
        
        inventory_turnover = (db.query(func.sum(models.InvoiceLine.quantity * models.InvoiceLine.cost_price)).join(
            models.Invoice
        ).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= start_date,
            models.Invoice.date <= end_date
        ).scalar() or 0.0) / inventory if inventory > 0 else 0
        
        return {
            'current_ratio': current_ratio,
            'profit_margin': profit_margin,
            'inventory_turnover': inventory_turnover,
            'revenue': revenue,
            'net_profit': net_profit
        }
    finally:
        db.close()


def create_budget(account_id, month, year, budgeted_amount, notes=None, created_by=None):
    """إنشاء موازنة"""
    db = SessionLocal()
    try:
        existing = db.query(Budget).filter(
            Budget.account_id == account_id,
            Budget.month == month,
            Budget.year == year
        ).first()
        
        if existing:
            existing.budgeted_amount = budgeted_amount
            existing.notes = notes
            db.commit()
            return existing
        
        budget = Budget(
            account_id=account_id,
            month=month,
            year=year,
            budgeted_amount=budgeted_amount,
            notes=notes,
            created_by=created_by
        )
        db.add(budget)
        db.commit()
        return budget
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_budget_vs_actual(month, year):
    """مقارنة الموازنة بالفعلي — نسخة متوافقة مع PostgreSQL."""
    db = SessionLocal()
    try:
        budgets = db.query(Budget).filter(
            Budget.month == month,
            Budget.year == year,
        ).all()

        comparison = []
        month_expr = func.to_char(models.JournalEntry.date, literal_column("'MM'"))
        year_expr = func.to_char(models.JournalEntry.date, literal_column("'YYYY'"))

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
            variance_percentage = (variance / budget.budgeted_amount * 100) if budget.budgeted_amount > 0 else 0

            comparison.append({
                'account_code': account.code if account else '-',
                'account_name': account.name if account else '-',
                'budgeted': budget.budgeted_amount,
                'actual': actual,
                'variance': variance,
                'variance_percentage': variance_percentage,
            })
        return comparison
    finally:
        db.close()



def create_accounting_period(period_name, start_date, end_date, is_fiscal_year=False, created_by=None):
    """إنشاء فترة محاسبية جديدة"""
    db = SessionLocal()
    try:
        existing = db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= end_date,
            AccountingPeriod.end_date >= start_date
        ).first()
        
        if existing:
            raise ValueError(f"توجد فترة محاسبية متداخلة: '{existing.period_name}'")
        
        period = AccountingPeriod(
            period_name=period_name,
            start_date=start_date,
            end_date=end_date,
            is_fiscal_year=is_fiscal_year,
            status=PeriodStatus.OPEN,
            created_by=created_by
        )
        db.add(period)
        db.commit()
        return period
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_period_for_date(date):
    """الحصول على الفترة المحاسبية لتاريخ معين"""
    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(
            AccountingPeriod.start_date <= date,
            AccountingPeriod.end_date >= date
        ).first()
        return period
    finally:
        db.close()


def get_all_periods():
    """الحصول على جميع الفترات المحاسبية"""
    db = SessionLocal()
    try:
        periods = db.query(AccountingPeriod).order_by(AccountingPeriod.start_date.desc()).all()
        return periods
    finally:
        db.close()


def close_accounting_period(period_id, closing_notes=None, closed_by=None):
    """إغلاق فترة محاسبية"""
    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            raise ValueError("الفترة المحاسبية غير موجودة!")
        
        if period.status == PeriodStatus.CLOSED:
            raise ValueError("هذه الفترة مغلقة بالفعل!")
        
        # التحقق من أن جميع الفواتير في الفترة مسددة أو ملغاة
        pending_invoices = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date,
            models.Invoice.status == 'pending'
        ).count()
        
        if pending_invoices > 0:
            raise ValueError(f"يوجد {pending_invoices} فاتورة معلقة في هذه الفترة! يجب تسويتها أولاً.")
        
        # ترحيل الأرباح والخسائر إلى الأرباح المحتجزة
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
        
        net_profit = revenue - expenses
        
        # إنشاء قيد الإغلاق
        closing_entry = models.JournalEntry(
            date=period.end_date,
            description=f"إغلاق فترة '{period.period_name}' - ترحيل صافي الربح/الخسارة",
            reference_type="period_closing",
            reference_id=period_id,
            created_by=closed_by,
            period_id=period_id
        )
        db.add(closing_entry)
        db.flush()
        
        # حساب حساب الأرباح المحتجزة
        retained_earnings_account = db.query(models.Account).filter(
            models.Account.code == "3101"
        ).first()
        
        if not retained_earnings_account:
            raise ValueError("حساب الأرباح المحتجزة غير موجود!")
        
        if net_profit > 0:
            # ربح: مدين حساب الأرباح والخسائر، دائن الأرباح المحتجزة
            profit_loss_account = db.query(models.Account).filter(
                models.Account.code == "3201"
            ).first()
            
            if profit_loss_account:
                db.add(models.JournalLine(
                    entry_id=closing_entry.id,
                    account_id=profit_loss_account.id,
                    debit=net_profit,
                    credit=0.0
                ))
                db.add(models.JournalLine(
                    entry_id=closing_entry.id,
                    account_id=retained_earnings_account.id,
                    debit=0.0,
                    credit=net_profit
                ))
        else:
            # خسارة: مدين الأرباح المحتجزة، دائن حساب الأرباح والخسائر
            profit_loss_account = db.query(models.Account).filter(
                models.Account.code == "3201"
            ).first()
            
            if profit_loss_account:
                db.add(models.JournalLine(
                    entry_id=closing_entry.id,
                    account_id=retained_earnings_account.id,
                    debit=abs(net_profit),
                    credit=0.0
                ))
                db.add(models.JournalLine(
                    entry_id=closing_entry.id,
                    account_id=profit_loss_account.id,
                    debit=0.0,
                    credit=abs(net_profit)
                ))
        
        # تحديث حالة الفترة
        period.status = PeriodStatus.CLOSED
        period.closing_date = datetime.now()
        period.closing_notes = closing_notes
        period.closed_by = closed_by
        
        db.commit()
        
        return {
            'period': period,
            'net_profit': net_profit,
            'revenue': revenue,
            'expenses': expenses
        }
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def reopen_accounting_period(period_id, reopened_by=None):
    """إعادة فتح فترة محاسبية مغلقة"""
    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            raise ValueError("الفترة المحاسبية غير موجودة!")
        
        if period.status == PeriodStatus.OPEN:
            raise ValueError("هذه الفترة مفتوحة بالفعل!")
        
        # حذف قيد الإغلاق
        closing_entry = db.query(models.JournalEntry).filter(
            models.JournalEntry.reference_type == "period_closing",
            models.JournalEntry.reference_id == period_id
        ).first()
        
        if closing_entry:
            db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == closing_entry.id
            ).delete()
            db.delete(closing_entry)
        
        # تحديث حالة الفترة
        period.status = PeriodStatus.OPEN
        period.closing_date = None
        period.closing_notes = None
        period.closed_by = None
        
        db.commit()
        return period
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_period_summary(period_id):
    """ملخص الفترة المحاسبية"""
    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            return None
        
        # عدد الفواتير
        invoices_count = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).count()
        
        # إجمالي المبيعات
        total_sales = db.query(func.sum(models.Invoice.net_amount)).filter(
            models.Invoice.type == 'sale',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).scalar() or 0.0
        
        # إجمالي المشتريات
        total_purchases = db.query(func.sum(models.Invoice.net_amount)).filter(
            models.Invoice.type == 'purchase',
            models.Invoice.status != 'cancelled',
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date
        ).scalar() or 0.0
        
        # إجمالي المصروفات
        total_expenses = db.query(func.sum(Expense.amount)).filter(
            Expense.date >= period.start_date,
            Expense.date <= period.end_date
        ).scalar() or 0.0
        
        # عدد القيود المحاسبية
        journal_entries_count = db.query(models.JournalEntry).filter(
            models.JournalEntry.period_id == period_id
        ).count()
        
        return {
            'period': period,
            'invoices_count': invoices_count,
            'total_sales': total_sales,
            'total_purchases': total_purchases,
            'total_expenses': total_expenses,
            'journal_entries_count': journal_entries_count
        }
    finally:
        db.close()


def check_period_integrity(period_id):
    """التحقق من سلامة الفترة المحاسبية قبل الإغلاق"""
    db = SessionLocal()
    try:
        period = db.query(AccountingPeriod).filter(AccountingPeriod.id == period_id).first()
        if not period:
            return None
        
        issues = []
        
        # التحقق من الفواتير المعلقة
        pending_invoices = db.query(models.Invoice).filter(
            models.Invoice.date >= period.start_date,
            models.Invoice.date <= period.end_date,
            models.Invoice.status == 'pending'
        ).count()
        
        if pending_invoices > 0:
            issues.append(f"⚠️ يوجد {pending_invoices} فاتورة معلقة")
        
        # التحقق من القيود غير المتوازنة
        entries = db.query(models.JournalEntry).filter(
            models.JournalEntry.period_id == period_id
        ).all()
        
        unbalanced_entries = 0
        for entry in entries:
            lines = db.query(models.JournalLine).filter(
                models.JournalLine.entry_id == entry.id
            ).all()
            
            total_debit = sum(line.debit for line in lines)
            total_credit = sum(line.credit for line in lines)
            
            if abs(total_debit - total_credit) > 0.01:
                unbalanced_entries += 1
        
        if unbalanced_entries > 0:
            issues.append(f"⚠️ يوجد {unbalanced_entries} قيد غير متوازن")
        
        # التحقق من الأصناف تحت الحد الأدنى
        low_stock_items = db.query(models.Item).filter(
            models.Item.is_kit == False
        ).all()
        
        low_stock_count = 0
        for item in low_stock_items:
            movements = db.query(models.InventoryMovement).filter(
                models.InventoryMovement.item_id == item.id,
                models.InventoryMovement.date >= period.start_date,
                models.InventoryMovement.date <= period.end_date
            ).all()
            
            stock_in = sum(m.quantity for m in movements if m.type == 'in')
            stock_out = sum(m.quantity for m in movements if m.type == 'out')
            current_stock = stock_in - stock_out
            
            if current_stock <= item.min_stock:
                low_stock_count += 1
        
        if low_stock_count > 0:
            issues.append(f"️ يوجد {low_stock_count} صنف تحت الحد الأدنى للمخزون")
        
        return {
            'period': period,
            'issues': issues,
            'is_ready_to_close': len(issues) == 0
        }
    finally:
        db.close()
# ==========================================
# 24. دوال إدارة مراكز التكلفة (جديد)
# ==========================================

def create_cost_center(code, name, description=None, manager_name=None, 
                       parent_id=None, budget_limit=None, notes=None, created_by=None):
    """إنشاء مركز تكلفة جديد"""
    db = SessionLocal()
    try:
        existing = db.query(CostCenter).filter(CostCenter.code == code).first()
        if existing:
            raise ValueError(f"كود مركز التكلفة '{code}' موجود مسبقاً!")
        
        cost_center = CostCenter(
            code=code,
            name=name,
            description=description,
            manager_name=manager_name,
            parent_id=parent_id,
            budget_limit=budget_limit,
            notes=notes,
            is_active=True,
            created_by=created_by
        )
        db.add(cost_center)
        db.commit()
        return cost_center
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_cost_centers_tree():
    """الحصول على شجرة مراكز التكلفة"""
    db = SessionLocal()
    try:
        centers = db.query(CostCenter).filter(CostCenter.is_active == True).all()
        
        tree = []
        for center in centers:
            if center.parent_id is None:
                # مركز رئيسي
                children = [c for c in centers if c.parent_id == center.id]
                tree.append({
                    'id': center.id,
                    'code': center.code,
                    'name': center.name,
                    'manager': center.manager_name,
                    'budget_limit': center.budget_limit,
                    'children': [
                        {
                            'id': c.id,
                            'code': c.code,
                            'name': c.name,
                            'manager': c.manager_name,
                            'budget_limit': c.budget_limit
                        } for c in children
                    ]
                })
        
        return tree
    finally:
        db.close()


def allocate_cost_to_center(cost_center_id, account_id, amount, description, 
                            allocation_date=None, reference_type=None, reference_id=None, created_by=None):
    """تخصيص تكلفة لمركز تكلفة"""
    db = SessionLocal()
    try:
        cost_center = db.query(CostCenter).filter(CostCenter.id == cost_center_id).first()
        if not cost_center:
            raise ValueError("مركز التكلفة غير موجود!")
        
        if not cost_center.is_active:
            raise ValueError("مركز التكلفة غير نشط!")
        
        if allocation_date is None:
            allocation_date = datetime.now()
        
        # إنشاء معاملة مركز التكلفة
        transaction = CostCenterTransaction(
            cost_center_id=cost_center_id,
            date=allocation_date,
            description=description,
            amount=amount,
            transaction_type='expense',
            reference_type=reference_type,
            reference_id=reference_id,
            created_by=created_by
        )
        db.add(transaction)
        
        # إنشاء القيد المحاسبي
        journal_entry = models.JournalEntry(
            date=allocation_date,
            description=f"تخصيص تكلفة لـ {cost_center.name}: {description}",
            reference_type="cost_allocation",
            reference_id=cost_center_id,
            created_by=created_by
        )
        db.add(journal_entry)
        db.flush()
        
        # مدين: حساب المصروف
        db.add(models.JournalLine(
            entry_id=journal_entry.id,
            account_id=account_id,
            debit=amount,
            credit=0.0
        ))
        
        # دائن: الخزينة الرئيسية (افتراضي)
        main_cash_box = db.query(CashBox).filter(CashBox.is_active == True).first()
        if main_cash_box:
            db.add(models.JournalLine(
                entry_id=journal_entry.id,
                account_id=main_cash_box.account_id,
                debit=0.0,
                credit=amount
            ))
        
        db.commit()
        return transaction
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


def get_cost_center_report(cost_center_id, start_date=None, end_date=None):
    """تقرير مركز التكلفة"""
    db = SessionLocal()
    try:
        cost_center = db.query(CostCenter).filter(CostCenter.id == cost_center_id).first()
        if not cost_center:
            return None
        
        query = db.query(CostCenterTransaction).filter(
            CostCenterTransaction.cost_center_id == cost_center_id
        )
        
        if start_date:
            query = query.filter(CostCenterTransaction.date >= start_date)
        if end_date:
            query = query.filter(CostCenterTransaction.date <= end_date)
        
        transactions = query.order_by(CostCenterTransaction.date.desc()).all()
        
        total_expenses = sum(t.amount for t in transactions if t.transaction_type == 'expense')
        total_revenues = sum(t.amount for t in transactions if t.transaction_type == 'revenue')
        net_amount = total_revenues - total_expenses
        
        # الحصول على الأطفال
        children = db.query(CostCenter).filter(CostCenter.parent_id == cost_center_id).all()
        children_data = []
        for child in children:
            child_transactions = db.query(CostCenterTransaction).filter(
                CostCenterTransaction.cost_center_id == child.id
            ).all()
            child_total = sum(t.amount for t in child_transactions)
            children_data.append({
                'id': child.id,
                'name': child.name,
                'total': child_total
            })
        
        return {
            'cost_center': cost_center,
            'transactions': transactions,
            'total_expenses': total_expenses,
            'total_revenues': total_revenues,
            'net_amount': net_amount,
            'children': children_data
        }
    finally:
        db.close()


def get_all_cost_centers_summary(start_date=None, end_date=None):
    """ملخص جميع مراكز التكلفة"""
    db = SessionLocal()
    try:
        centers = db.query(CostCenter).filter(CostCenter.is_active == True).all()
        
        summary = []
        for center in centers:
            query = db.query(CostCenterTransaction).filter(
                CostCenterTransaction.cost_center_id == center.id
            )
            
            if start_date:
                query = query.filter(CostCenterTransaction.date >= start_date)
            if end_date:
                query = query.filter(CostCenterTransaction.date <= end_date)
            
            transactions = query.all()
            total = sum(t.amount for t in transactions)
            
            summary.append({
                'id': center.id,
                'code': center.code,
                'name': center.name,
                'manager': center.manager_name,
                'budget_limit': center.budget_limit,
                'total_spent': total,
                'remaining_budget': (center.budget_limit - total) if center.budget_limit else None,
                'usage_percentage': (total / center.budget_limit * 100) if center.budget_limit and center.budget_limit > 0 else 0
            })
        
        return summary
    finally:
        db.close()


def transfer_between_cost_centers(from_center_id, to_center_id, amount, description, 
                                   transfer_date=None, created_by=None):
    """تحويل مبلغ بين مركزي تكلفة"""
    db = SessionLocal()
    try:
        if from_center_id == to_center_id:
            raise ValueError("لا يمكن التحويل بين نفس مركز التكلفة!")
        
        from_center = db.query(CostCenter).filter(CostCenter.id == from_center_id).first()
        to_center = db.query(CostCenter).filter(CostCenter.id == to_center_id).first()
        
        if not from_center or not to_center:
            raise ValueError("أحد مراكز التكلفة غير موجود!")
        
        if transfer_date is None:
            transfer_date = datetime.now()
        
        # معاملة خصم من المركز الأول
        debit_transaction = CostCenterTransaction(
            cost_center_id=from_center_id,
            date=transfer_date,
            description=f"تحويل إلى {to_center.name}: {description}",
            amount=amount,
            transaction_type='transfer_out',
            created_by=created_by
        )
        db.add(debit_transaction)
        
        # معاملة إضافة للمركز الثاني
        credit_transaction = CostCenterTransaction(
            cost_center_id=to_center_id,
            date=transfer_date,
            description=f"تحويل من {from_center.name}: {description}",
            amount=amount,
            transaction_type='transfer_in',
            created_by=created_by
        )
        db.add(credit_transaction)
        
        db.commit()
        return {'from': from_center.name, 'to': to_center.name, 'amount': amount}
    except Exception as e:
        db.rollback()
        raise
    finally:
        db.close()


# ==========================================
# 25. دوال إدارة تصنيفات المصروفات (جديد)
# ==========================================
def get_expense_categories(include_inactive=False):
    """يرجع كل تصنيفات المصروفات مرتبة بالاسم."""
    db = SessionLocal()
    try:
        q = db.query(ExpenseCategory)
        if not include_inactive:
            q = q.filter(ExpenseCategory.is_active == True)
        return q.order_by(ExpenseCategory.name).all()
    finally:
        db.close()


def get_expense_category_by_id(category_id):
    """يرجع تصنيف واحد بالمعرف."""
    db = SessionLocal()
    try:
        return db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
    finally:
        db.close()


def update_expense_category(category_id, name=None, description=None,
                             account_id=None, is_active=None):
    """يعدل تصنيف مصروف موجود."""
    db = SessionLocal()
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
                ExpenseCategory.id != category_id,
            ).first()
            if existing:
                raise ValueError(f"يوجد تصنيف آخر باسم '{name}'!")
            cat.name = name

            # تحديث اسم الحساب المرتبط
            if cat.account_id:
                acc = db.query(models.Account).filter(
                    models.Account.id == cat.account_id
                ).first()
                if acc:
                    acc.name = name

        if description is not None:
            cat.description = description.strip() if description else None

        if account_id is not None:
            acc = db.query(models.Account).filter(
                models.Account.id == account_id
            ).first()
            if not acc:
                raise ValueError("الحساب غير موجود!")
            cat.account_id = account_id

        if is_active is not None:
            cat.is_active = bool(is_active)

        db.commit()
        return cat
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def delete_expense_category(category_id, force=False):
    """يحذف تصنيف مصروف.
    - لو فيه مصروفات مرتبطة يرفض إلا لو force=True.
    """
    db = SessionLocal()
    try:
        cat = db.query(ExpenseCategory).filter(
            ExpenseCategory.id == category_id
        ).first()
        if not cat:
            raise ValueError("تصنيف المصروف غير موجود!")

        # نحفظ account_id قبل الحذف
        account_id = cat.account_id

        expenses_count = db.query(Expense).filter(
            Expense.category_id == category_id
        ).count()

        if expenses_count > 0:
            if not force:
                raise ValueError(
                    f"لا يمكن الحذف: يوجد {expenses_count} مصروف مرتبط."
                )
            # حذف قسري: نحذف المصروفات المرتبطة + القيود المحاسبية
            expenses_list = db.query(Expense).filter(
                Expense.category_id == category_id
            ).all()
            for exp in expenses_list:
                # نحذف القيود المحاسبية المرتبطة
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

        # حذف الحساب المرتبط لو مفيش حساب آخر يستخدمه
        db.delete(cat)
        db.flush()

        if account_id:
            # نتأكد إن الحساب مش مستخدم في مكان آخر
            other_usage = db.query(ExpenseCategory).filter(
                ExpenseCategory.account_id == account_id
            ).first()
            if not other_usage:
                acc = db.query(models.Account).filter(
                    models.Account.id == account_id
                ).first()
                if acc:
                    db.delete(acc)

        db.commit()
        return True
    except Exception:
        db.rollback()
        raise
    finally:
        db.close()


def deactivate_expense_category(category_id):
    """يعطل تصنيف بدل حذفه."""
    return update_expense_category(category_id, is_active=False)


def activate_expense_category(category_id):
    """ينشط تصنيف معطل."""
    return update_expense_category(category_id, is_active=True)


def get_expense_category_usage(category_id):
    """إحصائيات استخدام تصنيف: عدد المصروفات + الإجمالي."""
    db = SessionLocal()
    try:
        count = db.query(Expense).filter(
            Expense.category_id == category_id
        ).count()
        total = db.query(func.sum(Expense.amount)).filter(
            Expense.category_id == category_id
        ).scalar() or 0.0
        return {"count": count, "total": float(total)}
    finally:
        db.close()

