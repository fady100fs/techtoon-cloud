-- Techtoon - Financial System Migration (Safe)

-- 1) إضافة status لـ cash_transfers (pending/completed/cancelled)
ALTER TABLE cash_transfers 
    ADD COLUMN IF NOT EXISTS status VARCHAR DEFAULT 'completed';

-- 2) إضافة created_at لـ cash_transfers
ALTER TABLE cash_transfers 
    ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW();

-- 3) فهرس لتسريع تقارير الصندوق
CREATE INDEX IF NOT EXISTS idx_payments_cash_box_date 
    ON payments(cash_box_id, date);
CREATE INDEX IF NOT EXISTS idx_expenses_cash_box_date 
    ON expenses(cash_box_id, date);
CREATE INDEX IF NOT EXISTS idx_cash_transfers_from 
    ON cash_transfers(from_cash_box_id);
CREATE INDEX IF NOT EXISTS idx_cash_transfers_to 
    ON cash_transfers(to_cash_box_id);

-- 4) جدول إغلاق الوردية (Shift Closing)
CREATE TABLE IF NOT EXISTS shift_closings (
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
);
