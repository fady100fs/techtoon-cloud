<<<<<<< HEAD
# fix_db.py - إصلاح خطأ column i.cash_box_id does not exist
import psycopg2

CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"

SQL = [
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS cash_box_id INTEGER",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS warehouse_id INTEGER",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS party_id INTEGER",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS subtotal NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS discount_amount NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS tax_amount NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS paid_amount NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS status TEXT DEFAULT 'draft'",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS notes TEXT",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW()",
    "ALTER TABLE invoices ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT NOW()",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS invoice_id INTEGER",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS item_id INTEGER",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS quantity NUMERIC(14,3) DEFAULT 0",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS price NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS discount NUMERIC(14,2) DEFAULT 0",
    "ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS total NUMERIC(14,2) DEFAULT 0",
    """CREATE TABLE IF NOT EXISTS invoice_payments (
        id SERIAL PRIMARY KEY,
        invoice_id INTEGER NOT NULL,
        method TEXT NOT NULL DEFAULT 'cash',
        amount NUMERIC(14,2) NOT NULL DEFAULT 0,
        reference TEXT,
        cash_box_id INTEGER,
        bank_account_id INTEGER,
        payment_date TIMESTAMP DEFAULT NOW(),
        notes TEXT,
        created_at TIMESTAMP DEFAULT NOW()
    )""",
    "CREATE INDEX IF NOT EXISTS idx_invoices_cash_box ON invoices(cash_box_id)",
    "CREATE INDEX IF NOT EXISTS idx_invoice_payments_invoice ON invoice_payments(invoice_id)",
]

conn = psycopg2.connect(CS)
conn.autocommit = True
cur = conn.cursor()
ok = 0
for stmt in SQL:
    try:
        cur.execute(stmt)
        ok += 1
    except Exception as e:
        if "already exists" in str(e).lower():
            ok += 1
        else:
            print(f"WARN: {e}")
cur.close()
conn.close()
print(f"Done! {ok}/{len(SQL)} statements executed successfully.")
=======
import sys
sys.path.insert(0, ".")
from database import SessionLocal
from sqlalchemy import text

db = SessionLocal()

# 1) رحّل فواتير INV- المتبقية
print("1) Migrating INV- invoices...")
rows = db.execute(text("SELECT id, invoice_number, type FROM invoices WHERE invoice_number LIKE 'INV-%' ORDER BY id")).fetchall()
print(f"   Found: {len(rows)}")

for r in rows:
    if r[2] == "sale":
        cnt = db.execute(text("SELECT COUNT(*) FROM invoices WHERE type='sale' AND invoice_number LIKE 'SAL-%'")).scalar()
        new = f"SAL-{cnt + 1:06d}"
    else:
        cnt = db.execute(text("SELECT COUNT(*) FROM invoices WHERE type='purchase' AND invoice_number LIKE 'PUR-%'")).scalar()
        new = f"PUR-{cnt + 1:06d}"
    
    db.execute(text("UPDATE invoices SET invoice_number = :n WHERE id = :i"), {"n": new, "i": r[0]})
    print(f"   {r[1]} -> {new}")

db.commit()

# 2) أصلح الأصناف بدون تصنيف
print("\n2) Fixing items without category...")
result = db.execute(text("UPDATE items SET category_id = 1 WHERE category_id IS NULL"))
updated = result.rowcount
db.commit()
print(f"   Updated: {updated} items")

# 3) تحقق
print("\n3) Verification:")
rows = db.execute(text("SELECT type, COUNT(*), MIN(invoice_number), MAX(invoice_number) FROM invoices GROUP BY type")).fetchall()
for r in rows:
    print(f"   {r[0]}: {r[1]} ({r[2]} .. {r[3]})")

inv_count = db.execute(text("SELECT COUNT(*) FROM invoices WHERE invoice_number LIKE 'INV-%'")).scalar()
print(f"\n   Remaining INV-: {inv_count}")

db.close()
print("\nDone!")
>>>>>>> 8325fba6af09ee66ab808da0e5526f6efad8e4c5
