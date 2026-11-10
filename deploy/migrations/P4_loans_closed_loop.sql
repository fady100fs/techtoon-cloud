-- P4: Closed-loop loans (سلف الموظفين + قروض المنشأة)
-- Additive only. Safe re-run. Date: 2026-11-10

-- 1) employees: zero-fill money columns (fixes EF InvalidCastException 'allowances' is null)
UPDATE employees SET
  basic_salary      = COALESCE(basic_salary, 0),
  allowances        = COALESCE(allowances, 0),
  deductions        = COALESCE(deductions, 0),
  social_insurance  = COALESCE(social_insurance, 0),
  tax_rate          = COALESCE(tax_rate, 0);
ALTER TABLE employees
  ALTER COLUMN basic_salary      SET DEFAULT 0,
  ALTER COLUMN allowances        SET DEFAULT 0,
  ALTER COLUMN deductions        SET DEFAULT 0,
  ALTER COLUMN social_insurance  SET DEFAULT 0,
  ALTER COLUMN tax_rate          SET DEFAULT 0;
ALTER TABLE employees
  ALTER COLUMN basic_salary      SET NOT NULL,
  ALTER COLUMN allowances        SET NOT NULL,
  ALTER COLUMN deductions        SET NOT NULL,
  ALTER COLUMN social_insurance  SET NOT NULL,
  ALTER COLUMN tax_rate          SET NOT NULL;

-- 2) loans: link advances to employee + their(open custody) — سلف الموظفين مع العهدة
ALTER TABLE loans ADD COLUMN IF NOT EXISTS employee_id INTEGER;
ALTER TABLE loans ADD COLUMN IF NOT EXISTS custody_id INTEGER;
ALTER TABLE loans ADD COLUMN IF NOT EXISTS repayment_method VARCHAR(30) DEFAULT 'cash';
ALTER TABLE loans ADD COLUMN IF NOT EXISTS interest_type VARCHAR(30);
CREATE INDEX IF NOT EXISTS idx_loans_employee   ON loans(employee_id);
CREATE INDEX IF NOT EXISTS idx_loans_borrower_t ON loans(borrower_type);

-- 3) installments defaults + integrity
UPDATE loan_installments SET paid_amount = COALESCE(paid_amount, 0), status = COALESCE(status, 'pending');
ALTER TABLE loan_installments ADD CONSTRAINT fk_li_loan
  FOREIGN KEY (loan_id) REFERENCES loans(id) ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS idx_loan_installments_loan ON loan_installments(loan_id);

-- 4) loan payments ledger (قيد التسديد الفعلي لكل قسط/تسوية)
CREATE TABLE IF NOT EXISTS loan_payments (
  id               SERIAL PRIMARY KEY,
  loan_id          INTEGER NOT NULL REFERENCES loans(id) ON DELETE CASCADE,
  installment_id   INTEGER REFERENCES loan_installments(id) ON DELETE SET NULL,
  payment_date     DATE NOT NULL,
  amount           NUMERIC NOT NULL,
  principal_part   NUMERIC NOT NULL DEFAULT 0,
  interest_part    NUMERIC NOT NULL DEFAULT 0,
  method           VARCHAR(30) NOT NULL DEFAULT 'cash',
  cash_box_id      INTEGER,
  reference_number VARCHAR(60),
  notes            TEXT,
  created_by       INTEGER,
  created_at       TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_loan_payments_loan ON loan_payments(loan_id);
CREATE INDEX IF NOT EXISTS idx_loan_payments_inst ON loan_payments(installment_id);
ALTER TABLE loan_payments ADD CONSTRAINT fk_lp_loan FOREIGN KEY (loan_id) REFERENCES loans(id) ON DELETE CASCADE;

-- 5) petty cash custodies: default amount
ALTER TABLE petty_cash_custodies ALTER COLUMN amount SET DEFAULT 0;
