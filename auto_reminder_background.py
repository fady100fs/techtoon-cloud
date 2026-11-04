# auto_reminder_background.py
"""
سكربت الخلفية للتذكيرات التلقائية
يمكن جدولة هذا الملف ليعمل يومياً عبر Windows Task Scheduler
"""

import schedule
import time
from datetime import datetime
from reminders import get_overdue_invoices_reminders, export_reminders_to_excel
import os

def job():
    print(f"[{datetime.now()}] بدء فحص التذكيرات التلقائية...")
    reminders = get_overdue_invoices_reminders()
    
    if reminders:
        filename = f"Auto_Reminder_{datetime.now().strftime('%Y%m%d_%H%M')}.xlsx"
        export_reminders_to_excel(reminders, filename)
        print(f"✅ تم العثور على {len(reminders)} تذكير. تم الحفظ في: {os.path.abspath(filename)}")
        # هنا يمكن إضافة كود لإرسال بريد إلكتروني للمدير إذا أردت
    else:
        print("✅ لا توجد تذكيرات متأخرة اليوم.")

# جدولة المهمة لتعمل كل يوم الساعة 9:00 صباحاً
schedule.every().day.at("09:00").do(job)

print("🤖 نظام التذكيرات التلقائية يعمل في الخلفية...")
print("اضغط Ctrl+C للإيقاف")

while True:
    schedule.run_pending()
    time.sleep(60) # فحص كل دقيقة