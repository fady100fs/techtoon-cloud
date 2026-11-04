# find_old_payments.py - البحث عن المدفوعات القديمة المفقودة
import psycopg2

CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"

conn = psycopg2.connect(CS)
cur = conn.cursor()

print("=" * 70)
print("  البحث عن المدفوعات القديمة المفقودة")
print("=" * 70)

# 1) اعرض كل الجداول في قاعدة البيانات
print("\n[1] كل الجداول الموجودة:")
cur.execute("""SELECT table_name FROM information_schema.tables 
    WHERE table_schema='public' AND table_type='BASE TABLE' ORDER BY table_name""")
all_tables = [r[0] for r in cur.fetchall()]
for t in all_tables:
    print(f"  - {t}")

# 2) ابحث في كل جدول عن بيانات مدفوعات/مصاريف
print("\n[2] البحث في الجداول المحتملة:")
candidate_tables = [t for t in all_tables if any(k in t.lower() for k in ['payment', 'expense', 'receipt', 'voucher', 'cash', 'transaction'])]

for table in candidate_tables:
    print(f"\n  === {table} ===")
    try:
        cur.execute(f'SELECT COUNT(*) FROM "{table}"')
        count = cur.fetchone()[0]
        print(f"    Rows: {count}")
        
        if count > 0:
            # اعرض الأعمدة
            cur.execute("""SELECT column_name FROM information_schema.columns 
                WHERE table_schema='public' AND lower(table_name)=%s ORDER BY ordinal_position""", (table.lower(),))
            cols = [r[0] for r in cur.fetchall()]
            print(f"    Columns: {', '.join(cols)}")
            
            # اعرض أول 5 صفوف
            cur.execute(f'SELECT * FROM "{table}" ORDER BY 1 LIMIT 5')
            rows = cur.fetchall()
            print(f"    Sample data (first 5 rows):")
            for row in rows:
                print(f"      {row}")
    except Exception as e:
        print(f"    Error: {e}")

# 3) ابحث في جدول invoice_payments (دفعات الفواتير)
print("\n[3] دفعات الفواتير (invoice_payments):")
try:
    cur.execute("SELECT COUNT(*) FROM invoice_payments")
    count = cur.fetchone()[0]
    print(f"  Rows: {count}")
    if count > 0:
        cur.execute("""SELECT ip.id, ip.invoice_id, ip.method, ip.amount, ip.reference, ip.paid_at,
            i.invoice_number, i.type
            FROM invoice_payments ip 
            LEFT JOIN invoices i ON ip.invoice_id = i.id
            ORDER BY ip.id LIMIT 20""")
        rows = cur.fetchall()
        print(f"  Sample data:")
        for row in rows:
            print(f"    ID:{row[0]} | Inv:{row[1]} | Method:{row[2]} | Amount:{row[3]} | Ref:{row[4]} | Date:{row[5]} | InvNo:{row[6]} | Type:{row[7]}")
except Exception as e:
    print(f"  Error: {e}")

# 4) ابحث في جدول expenses (المصاريف)
print("\n[4] المصاريف (expenses):")
try:
    cur.execute("SELECT COUNT(*) FROM expenses")
    count = cur.fetchone()[0]
    print(f"  Rows: {count}")
    if count > 0:
        cur.execute("SELECT * FROM expenses ORDER BY 1 LIMIT 10")
        rows = cur.fetchall()
        # اعرض الأعمدة
        cur.execute("""SELECT column_name FROM information_schema.columns 
            WHERE table_schema='public' AND lower(table_name)='expenses' ORDER BY ordinal_position""")
        cols = [r[0] for r in cur.fetchall()]
        print(f"  Columns: {', '.join(cols)}")
        print(f"  Sample data:")
        for row in rows:
            print(f"    {row}")
except Exception as e:
    print(f"  Error: {e}")

# 5) ابحث في جدول journal_entries (قيود اليومية - قد تحتوي مصاريف)
print("\n[5] قيود اليومية (journal_entries) - آخر 20:")
try:
    cur.execute("SELECT COUNT(*) FROM journal_entries")
    count = cur.fetchone()[0]
    print(f"  Total rows: {count}")
    if count > 0:
        cur.execute("""SELECT column_name FROM information_schema.columns 
            WHERE table_schema='public' AND lower(table_name)='journal_entries' ORDER BY ordinal_position""")
        cols = [r[0] for r in cur.fetchall()]
        print(f"  Columns: {', '.join(cols)}")
        
        cur.execute("SELECT * FROM journal_entries ORDER BY 1 DESC LIMIT 10")
        rows = cur.fetchall()
        print(f"  Last 10 entries:")
        for row in rows:
            print(f"    {row}")
except Exception as e:
    print(f"  Error: {e}")

# 6) تحقق من وجود نسخ احتياطية في schemas أخرى
print("\n[6] البحث عن schemas أخرى:")
cur.execute("""SELECT schema_name FROM information_schema.schemata 
    WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'public')""")
schemas = cur.fetchall()
if schemas:
    for s in schemas:
        print(f"  Found schema: {s[0]}")
        cur.execute("""SELECT table_name FROM information_schema.tables 
            WHERE table_schema=%s""", (s[0],))
        for t in cur.fetchall():
            print(f"    - {t[0]}")
else:
    print("  No other schemas found")

# 7) تحقق من جدول payments الحالي
print("\n[7] جدول payments الحالي:")
try:
    cur.execute("SELECT COUNT(*) FROM payments")
    count = cur.fetchone()[0]
    print(f"  Rows: {count}")
    if count > 0:
        cur.execute("SELECT * FROM payments ORDER BY id LIMIT 5")
        rows = cur.fetchall()
        print(f"  Sample:")
        for row in rows:
            print(f"    {row}")
except Exception as e:
    print(f"  Error: {e}")

print("\n" + "=" * 70)
print("  انتهى البحث")
print("=" * 70)

cur.close()
conn.close()