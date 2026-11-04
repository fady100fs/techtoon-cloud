import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
conn = psycopg2.connect(CS)
conn.autocommit = True
cur = conn.cursor()

# أضف كل الأعمدة المفقودة
sqls = [
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS voucher_no TEXT",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS date TIMESTAMP DEFAULT NOW()",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS party_id INTEGER",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS party_name TEXT",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS type TEXT",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS amount NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS method TEXT DEFAULT 'cash'",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS cash_box_id INTEGER",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS account_id INTEGER",
    "ALTER TABLE payments ADD COLUMN IF NOT EXISTS notes TEXT",
]
for s in sqls:
    try:
        cur.execute(s)
        print("OK:", s.split("ADD COLUMN IF NOT EXISTS")[1].strip())
    except Exception as e:
        print("SKIP:", str(e)[:80])

# اعرض الأعمدة النهائية
cur.execute("SELECT column_name FROM information_schema.columns WHERE table_schema='public' AND lower(table_name)='payments' ORDER BY ordinal_position")
print("\nFinal columns in payments:")
for r in cur.fetchall():
    print("  -", r[0])

cur.close()
conn.close()
print("\nDONE!")