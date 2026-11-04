# -*- coding: utf-8 -*-
import psycopg2
CS = "host=ep-bitter-night-za726w59-pooler.c-2.eu-west-2.aws.neon.tech port=5432 dbname=neondb user=neondb_owner password=npg_K9nHVkY5OjRC sslmode=require"
conn = psycopg2.connect(CS); conn.autocommit = True; cur = conn.cursor()
print("Applying financial migration...")
sqls = [
    "ALTER TABLE cash_transfers ADD COLUMN IF NOT EXISTS status VARCHAR DEFAULT 'completed'",
    "ALTER TABLE cash_transfers ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW()",
    "CREATE INDEX IF NOT EXISTS idx_payments_cash_box_date ON payments(cash_box_id, date)",
    "CREATE INDEX IF NOT EXISTS idx_expenses_cash_box_date ON expenses(cash_box_id, date)",
    "CREATE INDEX IF NOT EXISTS idx_cash_transfers_from ON cash_transfers(from_cash_box_id)",
    "CREATE INDEX IF NOT EXISTS idx_cash_transfers_to ON cash_transfers(to_cash_box_id)",
    """CREATE TABLE IF NOT EXISTS shift_closings (
        id SERIAL PRIMARY KEY,
        cash_box_id INTEGER NOT NULL,
        shift_date TIMESTAMP DEFAULT NOW(),
        opening_amount DOUBLE PRECISION NOT NULL,
        expected_closing DOUBLE PRECISION NOT NULL,
        actual_closing DOUBLE PRECISION NOT NULL,
        difference DOUBLE PRECISION NOT NULL,
        status VARCHAR DEFAULT 'closed',
        closed_by INTEGER,
        notes TEXT,
        created_at TIMESTAMP DEFAULT NOW()
    )"""
]
for s in sqls:
    try: cur.execute(s); print(f"  OK: {s[:60]}...")
    except Exception as e: print(f"  SKIP: {str(e)[:60]}")
cur.close(); conn.close()
print("Done!")
