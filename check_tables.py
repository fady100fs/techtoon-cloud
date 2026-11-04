# check_tables.py
from database import engine
from sqlalchemy import text

print("=" * 60)
print("فحص الجداول الجديدة في قاعدة البيانات")
print("=" * 60)

with engine.connect() as conn:
    result = conn.execute(text(
        "SELECT tablename FROM pg_tables "
        "WHERE schemaname='public' "
        "AND tablename LIKE 'recurring%' "
        "ORDER BY tablename"
    ))
    tables = [row[0] for row in result]

if tables:
    print("\n[OK] الجداول الجديدة موجودة:")
    for t in tables:
        print(f"  - {t}")
else:
    print("\n[FAIL] الجداول الجديدة غير موجودة!")

# فحص عام
with engine.connect() as conn:
    result = conn.execute(text(
        "SELECT COUNT(*) FROM pg_tables WHERE schemaname='public'"
    ))
    total = result.scalar()
    print(f"\n[INFO] إجمالي الجداول في قاعدة البيانات: {total}")

print("=" * 60)