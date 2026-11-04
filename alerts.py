# alerts.py
"""
نظام التنبيهات والإشعارات
"""

from database import SessionLocal
import models
from sqlalchemy import func
from datetime import datetime, timedelta

def get_low_stock_items():
    """الحصول على الأصناف التي وصلت للحد الأدنى"""
    db = SessionLocal()
    try:
        items = db.query(models.Item).filter(models.Item.is_kit == False).all()
        low_stock_items = []
        
        for item in items:
            movements = db.query(models.InventoryMovement).filter(
                models.InventoryMovement.item_id == item.id
            ).all()
            
            stock_in = sum(m.quantity for m in movements if m.type == 'in')
            stock_out = sum(m.quantity for m in movements if m.type == 'out')
            current_stock = stock_in - stock_out
            
            if current_stock <= item.min_stock:
                low_stock_items.append({
                    'item': item,
                    'current_stock': current_stock,
                    'min_stock': item.min_stock,
                    'shortage': item.min_stock - current_stock
                })
        
        return low_stock_items
    finally:
        db.close()

def get_overdue_invoices():
    """الحصول على الفواتير المتأخرة"""
    db = SessionLocal()
    try:
        today = datetime.now()
        overdue_invoices = []
        
        invoices = db.query(models.Invoice).filter(
            models.Invoice.status == 'pending',
            models.Invoice.due_date != None,
            models.Invoice.due_date < today
        ).all()
        
        for inv in invoices:
            party = db.query(models.Party).filter(models.Party.id == inv.party_id).first()
            days_overdue = (today - inv.due_date).days
            
            overdue_invoices.append({
                'invoice': inv,
                'party': party,
                'days_overdue': days_overdue
            })
        
        return overdue_invoices
    finally:
        db.close()

def get_credit_limit_warnings():
    """تحذيرات تجاوز حد الائتمان"""
    db = SessionLocal()
    try:
        customers = db.query(models.Party).filter(
            models.Party.type == 'customer',
            models.Party.credit_limit > 0
        ).all()
        
        warnings = []
        
        for customer in customers:
            # حساب الرصيد المستحق
            invoices = db.query(models.Invoice).filter(
                models.Invoice.party_id == customer.id,
                models.Invoice.type == 'sale'
            ).all()
            
            total_invoiced = sum(inv.net_amount for inv in invoices)
            
            payments = db.query(models.Payment).filter(
                models.Payment.party_id == customer.id,
                models.Payment.payment_type == 'receipt'
            ).all()
            
            total_paid = sum(p.amount for p in payments)
            
            balance = total_invoiced - total_paid
            
            if balance > customer.credit_limit:
                warnings.append({
                    'customer': customer,
                    'balance': balance,
                    'credit_limit': customer.credit_limit,
                    'excess': balance - customer.credit_limit
                })
        
        return warnings
    finally:
        db.close()

def get_all_alerts():
    """الحصول على جميع التنبيهات"""
    return {
        'low_stock': get_low_stock_items(),
        'overdue_invoices': get_overdue_invoices(),
        'credit_warnings': get_credit_limit_warnings()
    }