-- =====================================================================
-- Techtoon Accounting - Database Schema Fix
-- يحل خطأ: column i.cash_box_id does not exist
-- يضيف الأعمدة المفقودة في جداول الفواتير والدفعات
-- =====================================================================

-- 1) جدول الفواتير: إضافة cash_box_id والمفاتيح الأجنبية المفقودة
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS cash_box_id INTEGER;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS warehouse_id INTEGER;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS party_id INTEGER;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS subtotal NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS discount_amount NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS discount_percent NUMERIC(5,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS tax_amount NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS paid_amount NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS status TEXT DEFAULT 'draft';
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS notes TEXT;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS created_at TIMESTAMP DEFAULT NOW();
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP DEFAULT NOW();

-- 2) جدول بنود الفاتورة (invoice_lines)
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS invoice_id INTEGER;
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS item_id INTEGER;
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS quantity NUMERIC(14,3) DEFAULT 0;
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS price NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS discount NUMERIC(14,2) DEFAULT 0;
ALTER TABLE invoice_lines ADD COLUMN IF NOT EXISTS total NUMERIC(14,2) DEFAULT 0;

-- 3) جدول دفعات الفاتورة (invoice_payments) - لطرق الدفع المتعددة
CREATE TABLE IF NOT EXISTS invoice_payments (
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
);

-- 4) جدول الأصناف (items) - التأكد من وجود الأعمدة
ALTER TABLE items ADD COLUMN IF NOT EXISTS barcode TEXT;
ALTER TABLE items ADD COLUMN IF NOT EXISTS sku TEXT;
ALTER TABLE items ADD COLUMN IF NOT EXISTS name TEXT;
ALTER TABLE items ADD COLUMN IF NOT EXISTS unit TEXT;
ALTER TABLE items ADD COLUMN IF NOT EXISTS sale_price NUMERIC(14,2) DEFAULT 0;
ALTER TABLE items ADD COLUMN IF NOT EXISTS purchase_price NUMERIC(14,2) DEFAULT 0;
ALTER TABLE items ADD COLUMN IF NOT EXISTS tax_rate NUMERIC(5,2) DEFAULT 0;
ALTER TABLE items ADD COLUMN IF NOT EXISTS is_active BOOLEAN DEFAULT TRUE;

-- 5) فهرسة الباركود لتسريع البحث
CREATE INDEX IF NOT EXISTS idx_items_barcode ON items(barcode);
CREATE INDEX IF NOT EXISTS idx_items_name ON items(name);
CREATE INDEX IF NOT EXISTS idx_items_sku ON items(sku);
CREATE INDEX IF NOT EXISTS idx_invoices_cash_box ON invoices(cash_box_id);
CREATE INDEX IF NOT EXISTS idx_invoice_payments_invoice ON invoice_payments(invoice_id);

-- =====================================================================
-- انتهى السكربت
-- =====================================================================
