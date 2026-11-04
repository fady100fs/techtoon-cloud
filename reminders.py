# reminders.py
"""
محرك التذكيرات والتنبيهات
يحتوي على دوال لتوليد رسائل التذكير وتصديرها
"""

from database import SessionLocal
import models
from datetime import datetime, timedelta
import pandas as pd

def get_overdue_invoices_reminders():
    """الحصول على قائمة الفواتير المتأخرة مع رسائل واتساب جاهزة"""
    db = SessionLocal()
    try:
        today = datetime.now()
        reminders = []
        
        # جلب الفواتير المعلقة والتي تجاوزت تاريخ الاستحقاق
        overdue_invoices = db.query(models.Invoice).filter(
            models.Invoice.status == 'pending',
            models.Invoice.due_date != None,
            models.Invoice.due_date < today
        ).all()
        
        for inv in overdue_invoices:
            party = db.query(models.Party).filter(models.Party.id == inv.party_id).first()
            days_overdue = (today - inv.due_date).days
            
            if party and party.phone:
                # تنظيف رقم الهاتف (إزالة المسافات والأصفار الزائدة وإضافة كود الدولة 20 لمصر)
                clean_phone = party.phone.replace(" ", "").replace("-", "")
                if clean_phone.startswith("0"):
                    clean_phone = "2" + clean_phone # لمصر: 20
                elif not clean_phone.startswith("20"):
                    clean_phone = "20" + clean_phone
                
                message = (
                    f"مرحباً {party.name}،\n"
                    f"نود تذكيركم بوجود فاتورة متأخرة السداد:\n"
                    f"📄 رقم الفاتورة: {inv.invoice_number}\n"
                    f"📅 تاريخ الاستحقاق: {inv.due_date.strftime('%Y-%m-%d')}\n"
                    f"⏳ عدد أيام التأخير: {days_overdue} يوم\n"
                    f"💰 المبلغ المستحق: {inv.net_amount:,.2f} ج.م\n"
                    f"نشكركم على سرعة السداد. لتسديد المبلغ، يرجى التواصل معنا."
                )
                
                # ترميز الرسالة للرابط
                import urllib.parse
                encoded_msg = urllib.parse.quote(message)
                whatsapp_link = f"https://wa.me/{clean_phone}?text={encoded_msg}"
                
                reminders.append({
                    'رقم الفاتورة': inv.invoice_number,
                    'العميل': party.name,
                    'الهاتف': party.phone,
                    'المبلغ': inv.net_amount,
                    'أيام التأخير': days_overdue,
                    'رابط واتساب': whatsapp_link
                })
        
        return reminders
    finally:
        db.close()

def export_reminders_to_excel(reminders_list: list, filename: str = "reminders.xlsx"):
    """تصدير قائمة التذكيرات إلى Excel"""
    if not reminders_list:
        return None
    
    df = pd.DataFrame(reminders_list)
    # إزالة عمود الرابط من ملف الإكسل لأنه طويل، أو الاحتفاظ به
    df.to_excel(filename, index=False, engine='xlsxwriter')
    return filename