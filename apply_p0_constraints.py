# -*- coding: utf-8 -*-
import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
conn = psycopg2.connect(CS); conn.autocommit = True; cur = conn.cursor()
print("Adding CHECK constraints...")
sqls = [
    "ALTER TABLE invoice_payments ADD CONSTRAINT IF NOT EXISTS chk_pay_amount_pos CHECK (amount >= 0)",
    "ALTER TABLE invoice_lines ADD CONSTRAINT IF NOT EXISTS chk_line_qty_pos CHECK (quantity > 0)",
    "ALTER TABLE invoice_lines ADD CONSTRAINT IF NOT EXISTS chk_line_price_pos CHECK (price >= 0)",
    "ALTER TABLE payments ADD CONSTRAINT IF NOT EXISTS chk_payment_amount_pos CHECK (amount >= 0)",
    "ALTER TABLE inventory_movements ADD CONSTRAINT IF NOT EXISTS chk_movement_qty_pos CHECK (quantity != 0)",
    "ALTER TABLE cash_transfers ADD CONSTRAINT IF NOT EXISTS chk_transfer_amount_pos CHECK (amount > 0)",
    "ALTER TABLE invoices ADD CONSTRAINT IF NOT EXISTS chk_invoice_total_pos CHECK (total >= 0)",
]
for s in sqls:
    try: cur.execute(s); print(f"  OK: {s[:70]}")
    except Exception as e:
        if "already exists" in str(e).lower(): print(f"  SKIP: exists")
        else: print(f"  WARN: {str(e)[:60]}")
cur.close(); conn.close()
print("Done!")
