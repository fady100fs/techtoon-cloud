# -*- coding: utf-8 -*-
"""apply_inventory_migration.py - تطبيق هجرة المخزون آمنة"""
import psycopg2

CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"

print("=" * 60)
print("  Applying Inventory Migration (Safe)")
print("=" * 60)

conn = psycopg2.connect(CS)
conn.autocommit = True
cur = conn.cursor()

sqls = [
    "ALTER TABLE inventory_movements ADD COLUMN IF NOT EXISTS warehouse_id INTEGER",
    "ALTER TABLE inventory_movements ADD COLUMN IF NOT EXISTS reference_type VARCHAR DEFAULT 'invoice'",
    "ALTER TABLE inventory_movements ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW()",
    """CREATE UNIQUE INDEX IF NOT EXISTS uq_inventory_movement_ref 
       ON inventory_movements(item_id, reference_id, reference_type)
       WHERE reference_id IS NOT NULL""",
    "CREATE INDEX IF NOT EXISTS idx_inventory_movements_item ON inventory_movements(item_id)",
    "CREATE INDEX IF NOT EXISTS idx_inventory_movements_type ON inventory_movements(type)",
    """CREATE TABLE IF NOT EXISTS stock_adjustments (
        id SERIAL PRIMARY KEY,
        item_id INTEGER NOT NULL,
        warehouse_id INTEGER,
        adjustment_date TIMESTAMP DEFAULT NOW(),
        old_quantity DOUBLE PRECISION NOT NULL,
        new_quantity DOUBLE PRECISION NOT NULL,
        difference DOUBLE PRECISION NOT NULL,
        reason VARCHAR NOT NULL,
        notes TEXT,
        created_by INTEGER,
        created_at TIMESTAMP DEFAULT NOW()
    )""",
    "CREATE INDEX IF NOT EXISTS idx_items_min_stock ON items(min_stock) WHERE min_stock IS NOT NULL",
]

for sql in sqls:
    try:
        cur.execute(sql)
        # اعرض أول 60 حرف من SQL للتأكيد
        short = sql.replace("\n", " ").strip()[:60]
        print(f"  OK: {short}...")
    except Exception as e:
        print(f"  SKIP: {str(e)[:80]}")

# تحقق من النتيجة
print("\n[Verification] inventory_movements columns:")
cur.execute("""
    SELECT column_name FROM information_schema.columns 
    WHERE table_schema='public' AND table_name='inventory_movements'
    ORDER BY ordinal_position
""")
for r in cur.fetchall():
    print(f"  - {r[0]}")

print("\n[Verification] stock_adjustments exists:")
cur.execute("SELECT to_regclass('stock_adjustments')")
print(f"  - {cur.fetchone()[0]}")

cur.close()
conn.close()
print("\n" + "=" * 60)
print("  MIGRATION COMPLETE")
print("=" * 60)
