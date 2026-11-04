# show_old_payments.py - عرض المدفوعات القديمة (للمعاينة فقط)
import psycopg2

CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"

conn = psycopg2.connect(CS)
cur = conn.cursor()

# اعرض كل أعمدة جدول payments
print("=" * 70)
print("COLUMNS IN payments TABLE:")
print("=" * 70)
cur.execute("""SELECT column_name, data_type FROM information_schema.columns 
    WHERE table_schema='public' AND lower(table_name)='payments' ORDER BY ordinal_position""")
columns = []
for r in cur.fetchall():
    columns.append(r[0])
    print(f"  {r[0]:25} | {r[1]}")

# اعرض كل الصفوف
print()
print("=" * 70)
print(f"ALL ROWS IN payments TABLE ({len(columns)} columns):")
print("=" * 70)

cur.execute("SELECT * FROM payments ORDER BY id")
rows = cur.fetchall()

if not rows:
    print("No payments found")
else:
    # اعرض العناوين
    print("-" * 70)
    header = " | ".join(c[:12].ljust(12) for c in columns)
    print(header)
    print("-" * 70)
    
    for row in rows:
        line = " | ".join(str(v)[:12].ljust(12) if v is not None else "NULL".ljust(12) for v in row)
        print(line)

print()
print("=" * 70)
print(f"TOTAL: {len(rows)} payments")
print("=" * 70)

cur.close()
conn.close()