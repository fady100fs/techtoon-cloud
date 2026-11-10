-- P5: نظام الاشتراكات (خدمات شهرية/سنوية) — إضافي فقط
-- داخل صفحة الفواتير: تبويب "اشتراكات"
-- كل اشتراك: عميل + خدمة + مبلغ + دورية (شهري/سنوي) + فترات مستحقة
-- عند استحقاق الفترة تُنشأ فاتورة/سند دفع؛ إن لم يُدفع تظهر إنذار في لوحة التحكم

CREATE TABLE IF NOT EXISTS subscriptions (
  id            SERIAL PRIMARY KEY,
  party_id      INTEGER NOT NULL REFERENCES parties(id) ON DELETE RESTRICT,
  service_name  VARCHAR(150) NOT NULL,
  description   TEXT,
  amount        NUMERIC(18,2) NOT NULL DEFAULT 0,
  frequency     VARCHAR(20) NOT NULL DEFAULT 'monthly',   -- monthly | yearly
  start_date    DATE NOT NULL,
  end_date      DATE,
  auto_invoice  BOOLEAN NOT NULL DEFAULT TRUE,
  status        VARCHAR(20) NOT NULL DEFAULT 'active',      -- active | paused | cancelled | completed
  notes         TEXT,
  created_by    INTEGER,
  created_at    TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_subscriptions_party   ON subscriptions(party_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status  ON subscriptions(status);

CREATE TABLE IF NOT EXISTS subscription_periods (
  id              SERIAL PRIMARY KEY,
  subscription_id INTEGER NOT NULL REFERENCES subscriptions(id) ON DELETE CASCADE,
  period_number   INTEGER NOT NULL,
  period_start    DATE NOT NULL,
  period_end      DATE NOT NULL,
  due_date        DATE NOT NULL,
  amount          NUMERIC(18,2) NOT NULL DEFAULT 0,
  paid_amount     NUMERIC(18,2) NOT NULL DEFAULT 0,
  status          VARCHAR(20) NOT NULL DEFAULT 'due',   -- due | paid | overdue | cancelled
  invoice_id      INTEGER REFERENCES invoices(id) ON DELETE SET NULL,
  payment_id      INTEGER REFERENCES payments(id) ON DELETE SET NULL,
  paid_at         TIMESTAMP,
  notes           TEXT,
  created_at      TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_sub_periods_sub  ON subscription_periods(subscription_id);
CREATE INDEX IF NOT EXISTS idx_sub_periods_due  ON subscription_periods(due_date, status);
