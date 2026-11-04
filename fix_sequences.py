# fix_sequences.py
# إصلاح sequences جدول PostgreSQL بعد نقل البيانات من SQLite
# المشكلة: SQLite بيستخدم auto-increment، بينما PostgreSQL محتاج sequences منفصلة.
# بعد نقل البيانات، الـ sequence بيتفضل صفر، فبيحصل تضارب لما نضيف صف جديد.
# الحل: نضبط كل sequence يبدأ من MAX(id) + 1

from database import engine
from sqlalchemy import text

# كل جداول البرنامج (بالترتيب)
TABLES = [
    "currencies", "accounts", "users", "accounting_periods",
    "parties", "cash_boxes", "categories", "items",
    "invoices", "invoice_lines", "cost_history", "kit_components",
    "inventory_movements", "cost_centers", "cost_allocations",
    "cost_center_transactions", "journal_entries", "journal_lines",
    "payments", "cash_transfers", "expense_categories", "expenses",
    "warehouses", "stock_levels", "warehouse_transfers", "stock_counts",
    "fixed_assets", "depreciation_records", "loans", "loan_installments",
    "employees", "salary_records", "budgets",
]

print("=" * 60)
print("🔧 إصلاح sequences الجداول")
print("=" * 60)

fixed_count = 0
skipped_count = 0
failed_count = 0

with engine.connect() as conn:
    for table in TABLES:
        try:
            # 1) نتأكد إن الجدول موجود
            exists = conn.execute(text(
                f"SELECT EXISTS (SELECT FROM information_schema.tables "
                f"WHERE table_schema='public' AND table_name='{table}')"
            )).scalar()

            if not exists:
                print(f"⏭️  {table}: غير موجود")
                skipped_count += 1
                continue

            # 2) نجيب اسم الـ sequence المرتبط بعمود id
            seq_name = conn.execute(text(
                f"SELECT pg_get_serial_sequence('{table}', 'id')"
            )).scalar()

            if not seq_name:
                print(f"⏭️  {table}: لا يوجد sequence لـ id")
                skipped_count += 1
                continue

            # 3) نجيب أعلى id موجود في الجدول
            max_id = conn.execute(text(
                f"SELECT COALESCE(MAX(id), 0) FROM {table}"
            )).scalar()

            # 4) نضبط الـ sequence على max_id + 1
            conn.execute(text(
                f"SELECT setval('{seq_name}', :next_val, false)"
            ), {"next_val": int(max_id) + 1})

            print(f"✅ {table}: sequence = {max_id + 1}")
            fixed_count += 1

        except Exception as e:
            print(f"❌ {table}: {str(e)[:100]}")
            failed_count += 1

print()
print("=" * 60)
print(f"🎉 تم إصلاح {fixed_count} جدول")
if skipped_count:
    print(f"⏭️  تم تخطي {skipped_count} جدول")
if failed_count:
    print(f"❌ فشل {failed_count} جدول")
print("=" * 60)
print()
print("الآن جرّب تضيف فاتورة جديدة من البرنامج.")
print("لو ظهر نفس الخطأ في جدول تاني، شغّل السكربت تاني.")