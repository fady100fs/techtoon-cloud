-- =====================================================================
--  Techtoon Accounting - script to create the database schema
--  Auto-generated from the real schema (structure only - no data).
--  Usage:  psql -d <db> -f schema_full.sql
--  Idempotent: safe to run on an existing database.
-- =====================================================================

BEGIN;

-- ===== Sequences =====
CREATE SEQUENCE IF NOT EXISTS public.accounting_periods_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.accounts_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.app_audit_logs_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.app_settings_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.audit_log_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bad_debt_attachments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bad_debt_reversals_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bad_debt_writeoffs_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bank_reconciliations_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bank_statement_import_logs_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bank_statement_import_templates_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bank_statement_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.bank_statements_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.budgets_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cash_boxes_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cash_recognition_log_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cash_transfer_log_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cash_transfers_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.categories_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cost_allocations_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cost_center_transactions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cost_centers_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.cost_history_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.currencies_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.custody_increase_requests_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.damaged_item_inspection_log_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.damaged_items_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.depreciation_records_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.early_payment_discount_policies_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.employees_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.expense_categories_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.expenses_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.fixed_assets_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.inventory_movements_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.invoice_discount_applications_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.invoice_late_fees_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.invoice_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.invoice_payments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.invoices_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.items_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.journal_entries_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.journal_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.kit_component_history_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.kit_components_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.late_fee_policies_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.loan_installments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.loan_payments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.loan_rescheduling_history_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.loans_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.login_attempts_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.overdraft_transactions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.parties_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.party_discount_policies_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.party_late_fee_exceptions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.payments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.petty_cash_custodies_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.petty_cash_expenses_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.petty_cash_settlements_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.reconciliation_adjustments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.reconciliation_matches_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.recurring_invoice_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.recurring_invoice_templates_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.recurring_journal_executions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.recurring_journal_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.recurring_journal_templates_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.reporting_settings_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.roles_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.salary_records_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.shift_closings_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.stock_adjustments_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.stock_count_sessions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.stock_counts_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.stock_levels_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.stock_reasons_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.subscription_periods_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.subscriptions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 2147483647 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.user_devices_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.user_permissions_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.users_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.warehouse_transfer_lines_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.warehouse_transfer_log_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.warehouse_transfers_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.warehouses_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;
CREATE SEQUENCE IF NOT EXISTS public.writeoff_requests_id_seq START WITH 1 INCREMENT BY 1 MINVALUE 1 MAXVALUE 9223372036854775807 CACHE 1;

-- ===== accounting_periods =====
CREATE TABLE IF NOT EXISTS public.accounting_periods (
    "id" integer NOT NULL DEFAULT nextval('accounting_periods_id_seq'::regclass),
    "period_name" varchar(50) NOT NULL,
    "start_date" timestamp without time zone NOT NULL,
    "end_date" timestamp without time zone NOT NULL,
    "is_fiscal_year" boolean,
    "closing_date" timestamp without time zone,
    "closing_notes" text,
    "created_by" integer,
    "closed_by" integer,
    "created_at" timestamp without time zone,
    "status" text NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'accounting_periods_pkey') THEN ALTER TABLE public.accounting_periods ADD CONSTRAINT accounting_periods_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== accounts =====
CREATE TABLE IF NOT EXISTS public.accounts (
    "id" integer NOT NULL DEFAULT nextval('accounts_id_seq'::regclass),
    "code" varchar(20) NOT NULL,
    "name" varchar(100) NOT NULL,
    "parent_id" integer,
    "type" varchar(50) NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'accounts_pkey') THEN ALTER TABLE public.accounts ADD CONSTRAINT accounts_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== app_audit_logs =====
CREATE TABLE IF NOT EXISTS public.app_audit_logs (
    "id" integer NOT NULL DEFAULT nextval('app_audit_logs_id_seq'::regclass),
    "action" varchar(50) NOT NULL,
    "entity_type" varchar(100) NOT NULL,
    "entity_id" integer,
    "details" text,
    "user_name" varchar(100),
    "created_at" timestamp without time zone NOT NULL DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'app_audit_logs_pkey') THEN ALTER TABLE public.app_audit_logs ADD CONSTRAINT app_audit_logs_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== app_settings =====
CREATE TABLE IF NOT EXISTS public.app_settings (
    "id" integer NOT NULL DEFAULT nextval('app_settings_id_seq'::regclass),
    "key" varchar(100) NOT NULL,
    "value" text,
    "setting_type" varchar(20),
    "category" varchar(50),
    "description" text,
    "updated_at" timestamp without time zone,
    "updated_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'app_settings_pkey') THEN ALTER TABLE public.app_settings ADD CONSTRAINT app_settings_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== audit_log =====
CREATE TABLE IF NOT EXISTS public.audit_log (
    "id" integer NOT NULL DEFAULT nextval('audit_log_id_seq'::regclass),
    "user_id" integer,
    "username" character varying,
    "action" character varying NOT NULL,
    "entity_type" character varying,
    "entity_id" integer,
    "details" text,
    "ip_address" character varying,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'audit_log_pkey') THEN ALTER TABLE public.audit_log ADD CONSTRAINT audit_log_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bad_debt_attachments =====
CREATE TABLE IF NOT EXISTS public.bad_debt_attachments (
    "id" integer NOT NULL DEFAULT nextval('bad_debt_attachments_id_seq'::regclass),
    "writeoff_id" integer NOT NULL,
    "file_name" varchar(255) NOT NULL,
    "file_path" varchar(500) NOT NULL,
    "file_type" varchar(10),
    "file_size" bigint,
    "file_hash" varchar(64),
    "description" text,
    "uploaded_by" integer NOT NULL,
    "uploaded_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bad_debt_attachments_pkey') THEN ALTER TABLE public.bad_debt_attachments ADD CONSTRAINT bad_debt_attachments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bad_debt_reversals =====
CREATE TABLE IF NOT EXISTS public.bad_debt_reversals (
    "id" integer NOT NULL DEFAULT nextval('bad_debt_reversals_id_seq'::regclass),
    "reversal_number" varchar(50),
    "writeoff_id" integer NOT NULL,
    "reversal_date" date NOT NULL,
    "reversal_amount" numeric(15,2) NOT NULL,
    "reason" text NOT NULL,
    "journal_entry_id" integer,
    "reversed_by" integer NOT NULL,
    "reversed_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bad_debt_reversals_pkey') THEN ALTER TABLE public.bad_debt_reversals ADD CONSTRAINT bad_debt_reversals_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bad_debt_writeoffs =====
CREATE TABLE IF NOT EXISTS public.bad_debt_writeoffs (
    "id" integer NOT NULL DEFAULT nextval('bad_debt_writeoffs_id_seq'::regclass),
    "writeoff_number" varchar(50),
    "party_id" integer NOT NULL,
    "invoice_id" integer,
    "original_amount" numeric(15,2) NOT NULL,
    "remaining_amount" numeric(15,2) NOT NULL,
    "writeoff_amount" numeric(15,2) NOT NULL,
    "writeoff_type" varchar(20) NOT NULL,
    "reason" text NOT NULL,
    "evidence_file_path" varchar(500),
    "evidence_file_type" varchar(10),
    "evidence_file_hash" varchar(64),
    "writeoff_date" date NOT NULL,
    "bad_debt_account_id" integer NOT NULL,
    "ar_account_id" integer NOT NULL,
    "journal_entry_id" integer,
    "status" varchar(20) NOT NULL DEFAULT 'pending'::character varying,
    "requested_by" integer NOT NULL,
    "requested_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "posted_at" timestamp without time zone,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bad_debt_writeoffs_pkey') THEN ALTER TABLE public.bad_debt_writeoffs ADD CONSTRAINT bad_debt_writeoffs_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bank_reconciliations =====
CREATE TABLE IF NOT EXISTS public.bank_reconciliations (
    "id" integer NOT NULL DEFAULT nextval('bank_reconciliations_id_seq'::regclass),
    "reconciliation_number" varchar(50),
    "cash_box_id" integer NOT NULL,
    "period_start" date NOT NULL,
    "period_end" date NOT NULL,
    "bank_statement_id" integer,
    "opening_book_balance" numeric(15,2) NOT NULL,
    "opening_bank_balance" numeric(15,2) NOT NULL,
    "closing_book_balance" numeric(15,2) NOT NULL,
    "closing_bank_balance" numeric(15,2) NOT NULL,
    "difference" numeric(15,2) NOT NULL DEFAULT 0,
    "status" varchar(20) NOT NULL DEFAULT 'draft'::character varying,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "reviewed_by" integer,
    "reviewed_at" timestamp without time zone,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "locked_at" timestamp without time zone,
    "locked_by" integer,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_reconciliations_pkey') THEN ALTER TABLE public.bank_reconciliations ADD CONSTRAINT bank_reconciliations_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bank_statement_import_logs =====
CREATE TABLE IF NOT EXISTS public.bank_statement_import_logs (
    "id" integer NOT NULL DEFAULT nextval('bank_statement_import_logs_id_seq'::regclass),
    "statement_id" integer,
    "template_id" integer,
    "started_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "completed_at" timestamp without time zone,
    "status" varchar(20) NOT NULL,
    "total_rows" integer DEFAULT 0,
    "imported_rows" integer DEFAULT 0,
    "skipped_rows" integer DEFAULT 0,
    "error_rows" integer DEFAULT 0,
    "duplicate_rows" integer DEFAULT 0,
    "error_details" jsonb,
    "performed_by" integer NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_statement_import_logs_pkey') THEN ALTER TABLE public.bank_statement_import_logs ADD CONSTRAINT bank_statement_import_logs_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bank_statement_import_templates =====
CREATE TABLE IF NOT EXISTS public.bank_statement_import_templates (
    "id" integer NOT NULL DEFAULT nextval('bank_statement_import_templates_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "bank_name" varchar(200),
    "date_column" varchar(50) NOT NULL DEFAULT 'date'::character varying,
    "value_date_column" varchar(50),
    "description_column" varchar(50) DEFAULT 'description'::character varying,
    "reference_column" varchar(50) DEFAULT 'reference'::character varying,
    "debit_column" varchar(50) DEFAULT 'debit'::character varying,
    "credit_column" varchar(50) DEFAULT 'credit'::character varying,
    "amount_column" varchar(50),
    "balance_column" varchar(50) DEFAULT 'balance'::character varying,
    "date_format" varchar(50) DEFAULT 'YYYY-MM-DD'::character varying,
    "decimal_separator" varchar(5) DEFAULT '.'::character varying,
    "thousand_separator" varchar(5) DEFAULT ','::character varying,
    "encoding" varchar(20) DEFAULT 'utf-8'::character varying,
    "skip_rows" integer DEFAULT 0,
    "has_header" boolean DEFAULT true,
    "is_active" boolean DEFAULT true,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_statement_import_templates_pkey') THEN ALTER TABLE public.bank_statement_import_templates ADD CONSTRAINT bank_statement_import_templates_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bank_statement_lines =====
CREATE TABLE IF NOT EXISTS public.bank_statement_lines (
    "id" integer NOT NULL DEFAULT nextval('bank_statement_lines_id_seq'::regclass),
    "statement_id" integer NOT NULL,
    "line_number" integer NOT NULL,
    "transaction_date" date NOT NULL,
    "value_date" date,
    "description" text,
    "reference" varchar(200),
    "debit" numeric(15,2) DEFAULT 0,
    "credit" numeric(15,2) DEFAULT 0,
    "balance" numeric(15,2),
    "matched_payment_id" integer,
    "matched_journal_line_id" integer,
    "match_status" varchar(20) NOT NULL DEFAULT 'unmatched'::character varying,
    "matched_at" timestamp without time zone,
    "matched_by" integer,
    "match_confidence" numeric(12,6),
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_statement_lines_pkey') THEN ALTER TABLE public.bank_statement_lines ADD CONSTRAINT bank_statement_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== bank_statements =====
CREATE TABLE IF NOT EXISTS public.bank_statements (
    "id" integer NOT NULL DEFAULT nextval('bank_statements_id_seq'::regclass),
    "cash_box_id" integer NOT NULL,
    "statement_date" date NOT NULL,
    "period_start" date NOT NULL,
    "period_end" date NOT NULL,
    "opening_balance" numeric(15,2) NOT NULL,
    "closing_balance" numeric(15,2) NOT NULL,
    "currency" varchar(10) NOT NULL DEFAULT 'EGP'::character varying,
    "source_file_path" varchar(500),
    "source_file_format" varchar(10),
    "source_file_size" bigint,
    "file_hash" varchar(64),
    "imported_by" integer NOT NULL,
    "imported_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "status" varchar(20) NOT NULL DEFAULT 'imported'::character varying,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'bank_statements_pkey') THEN ALTER TABLE public.bank_statements ADD CONSTRAINT bank_statements_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== budgets =====
CREATE TABLE IF NOT EXISTS public.budgets (
    "id" integer NOT NULL DEFAULT nextval('budgets_id_seq'::regclass),
    "account_id" integer NOT NULL,
    "month" integer NOT NULL,
    "year" integer NOT NULL,
    "budgeted_amount" numeric(12,2) NOT NULL,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'budgets_pkey') THEN ALTER TABLE public.budgets ADD CONSTRAINT budgets_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cash_boxes =====
CREATE TABLE IF NOT EXISTS public.cash_boxes (
    "id" integer NOT NULL DEFAULT nextval('cash_boxes_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "code" varchar(20) NOT NULL,
    "account_id" integer NOT NULL,
    "responsible_person" varchar(100),
    "max_limit" numeric(12,2),
    "is_active" boolean,
    "notes" text,
    "created_at" timestamp without time zone,
    "type" varchar(50) NOT NULL,
    "opening_balance" numeric(12,2) DEFAULT 0
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cash_boxes_pkey') THEN ALTER TABLE public.cash_boxes ADD CONSTRAINT cash_boxes_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cash_recognition_log =====
CREATE TABLE IF NOT EXISTS public.cash_recognition_log (
    "id" integer NOT NULL DEFAULT nextval('cash_recognition_log_id_seq'::regclass),
    "journal_entry_id" integer NOT NULL,
    "recognition_date" date NOT NULL,
    "recognized_amount" numeric(15,2) NOT NULL,
    "payment_id" integer,
    "recognized_by" integer,
    "recognized_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cash_recognition_log_pkey') THEN ALTER TABLE public.cash_recognition_log ADD CONSTRAINT cash_recognition_log_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cash_transfer_log =====
CREATE TABLE IF NOT EXISTS public.cash_transfer_log (
    "id" integer NOT NULL DEFAULT nextval('cash_transfer_log_id_seq'::regclass),
    "transfer_id" integer NOT NULL,
    "action" varchar(50) NOT NULL,
    "performed_by" integer NOT NULL,
    "performed_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text,
    "old_status" varchar(20),
    "new_status" varchar(20),
    "metadata" jsonb
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cash_transfer_log_pkey') THEN ALTER TABLE public.cash_transfer_log ADD CONSTRAINT cash_transfer_log_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cash_transfers =====
CREATE TABLE IF NOT EXISTS public.cash_transfers (
    "id" integer NOT NULL DEFAULT nextval('cash_transfers_id_seq'::regclass),
    "date" timestamp without time zone,
    "from_cash_box_id" integer NOT NULL,
    "to_cash_box_id" integer NOT NULL,
    "amount" numeric(12,2) NOT NULL,
    "notes" text,
    "created_by" integer,
    "status" character varying DEFAULT 'completed'::character varying,
    "created_at" timestamp without time zone DEFAULT now(),
    "transfer_number" varchar(50),
    "transfer_fee" numeric(12,2) DEFAULT 0,
    "fee_account_id" integer,
    "in_transit_account_id" integer,
    "source_value_date" date,
    "destination_value_date" date,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "cancelled_at" timestamp without time zone,
    "cancelled_by" integer,
    "cancellation_reason" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cash_transfers_pkey') THEN ALTER TABLE public.cash_transfers ADD CONSTRAINT cash_transfers_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== categories =====
CREATE TABLE IF NOT EXISTS public.categories (
    "id" integer NOT NULL DEFAULT nextval('categories_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "description" text,
    "parent_id" integer,
    "is_active" boolean,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'categories_pkey') THEN ALTER TABLE public.categories ADD CONSTRAINT categories_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cost_allocations =====
CREATE TABLE IF NOT EXISTS public.cost_allocations (
    "id" integer NOT NULL DEFAULT nextval('cost_allocations_id_seq'::regclass),
    "cost_center_id" integer NOT NULL,
    "account_id" integer NOT NULL,
    "allocation_percentage" numeric(12,6) NOT NULL,
    "month" integer NOT NULL,
    "year" integer NOT NULL,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cost_allocations_pkey') THEN ALTER TABLE public.cost_allocations ADD CONSTRAINT cost_allocations_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cost_center_transactions =====
CREATE TABLE IF NOT EXISTS public.cost_center_transactions (
    "id" integer NOT NULL DEFAULT nextval('cost_center_transactions_id_seq'::regclass),
    "cost_center_id" integer NOT NULL,
    "date" timestamp without time zone NOT NULL,
    "description" varchar(255) NOT NULL,
    "amount" numeric(12,2) NOT NULL,
    "transaction_type" varchar(20) NOT NULL,
    "reference_type" varchar(50),
    "reference_id" integer,
    "created_by" integer,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cost_center_transactions_pkey') THEN ALTER TABLE public.cost_center_transactions ADD CONSTRAINT cost_center_transactions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cost_centers =====
CREATE TABLE IF NOT EXISTS public.cost_centers (
    "id" integer NOT NULL DEFAULT nextval('cost_centers_id_seq'::regclass),
    "code" varchar(20) NOT NULL,
    "name" varchar(100) NOT NULL,
    "description" text,
    "manager_name" varchar(100),
    "parent_id" integer,
    "is_active" boolean,
    "budget_limit" numeric(12,2),
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cost_centers_pkey') THEN ALTER TABLE public.cost_centers ADD CONSTRAINT cost_centers_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== cost_history =====
CREATE TABLE IF NOT EXISTS public.cost_history (
    "id" integer NOT NULL DEFAULT nextval('cost_history_id_seq'::regclass),
    "item_id" integer NOT NULL,
    "invoice_id" integer NOT NULL,
    "purchase_price" numeric(12,2) NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "total_cost" numeric(12,2) NOT NULL,
    "purchase_date" timestamp without time zone NOT NULL,
    "supplier_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'cost_history_pkey') THEN ALTER TABLE public.cost_history ADD CONSTRAINT cost_history_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== currencies =====
CREATE TABLE IF NOT EXISTS public.currencies (
    "id" integer NOT NULL DEFAULT nextval('currencies_id_seq'::regclass),
    "code" varchar(10) NOT NULL,
    "name" varchar(50) NOT NULL,
    "symbol" varchar(10) NOT NULL,
    "exchange_rate" numeric(12,6),
    "is_default" boolean
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'currencies_pkey') THEN ALTER TABLE public.currencies ADD CONSTRAINT currencies_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== custody_increase_requests =====
CREATE TABLE IF NOT EXISTS public.custody_increase_requests (
    "id" integer NOT NULL DEFAULT nextval('custody_increase_requests_id_seq'::regclass),
    "request_number" varchar(50),
    "custody_id" integer NOT NULL,
    "current_amount" numeric(15,2) NOT NULL,
    "requested_amount" numeric(15,2) NOT NULL,
    "new_total_amount" numeric(15,2) NOT NULL,
    "reason" text NOT NULL,
    "status" varchar(20) NOT NULL DEFAULT 'pending'::character varying,
    "requested_by" integer NOT NULL,
    "requested_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "reviewed_by" integer,
    "reviewed_at" timestamp without time zone,
    "review_notes" text,
    "journal_entry_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'custody_increase_requests_pkey') THEN ALTER TABLE public.custody_increase_requests ADD CONSTRAINT custody_increase_requests_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== damaged_item_inspection_log =====
CREATE TABLE IF NOT EXISTS public.damaged_item_inspection_log (
    "id" integer NOT NULL DEFAULT nextval('damaged_item_inspection_log_id_seq'::regclass),
    "damaged_item_id" integer,
    "action" varchar(50) NOT NULL,
    "notes" text,
    "performed_by" integer,
    "performed_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'damaged_item_inspection_log_pkey') THEN ALTER TABLE public.damaged_item_inspection_log ADD CONSTRAINT damaged_item_inspection_log_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== damaged_items =====
CREATE TABLE IF NOT EXISTS public.damaged_items (
    "id" integer NOT NULL DEFAULT nextval('damaged_items_id_seq'::regclass),
    "document_number" varchar(50) NOT NULL,
    "date" timestamp without time zone NOT NULL DEFAULT now(),
    "item_id" integer NOT NULL,
    "warehouse_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "unit_cost" numeric(12,2),
    "total_value" numeric(12,2),
    "damage_reason_id" integer,
    "notes" text,
    "inspection_status" varchar(20) DEFAULT 'pending'::character varying,
    "inspection_result" varchar(30),
    "inspection_notes" text,
    "inspected_by" integer,
    "inspected_at" timestamp without time zone,
    "status" varchar(20) DEFAULT 'draft'::character varying,
    "created_by" integer,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'damaged_items_pkey') THEN ALTER TABLE public.damaged_items ADD CONSTRAINT damaged_items_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== depreciation_records =====
CREATE TABLE IF NOT EXISTS public.depreciation_records (
    "id" integer NOT NULL DEFAULT nextval('depreciation_records_id_seq'::regclass),
    "asset_id" integer NOT NULL,
    "date" timestamp without time zone NOT NULL,
    "depreciation_amount" numeric(12,2) NOT NULL,
    "accumulated_depreciation" numeric(12,2) NOT NULL,
    "net_book_value" numeric(12,2) NOT NULL,
    "notes" text,
    "created_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'depreciation_records_pkey') THEN ALTER TABLE public.depreciation_records ADD CONSTRAINT depreciation_records_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== early_payment_discount_policies =====
CREATE TABLE IF NOT EXISTS public.early_payment_discount_policies (
    "id" integer NOT NULL DEFAULT nextval('early_payment_discount_policies_id_seq'::regclass),
    "name" varchar(200) NOT NULL,
    "description" text,
    "discount_type" varchar(10) NOT NULL,
    "discount_value" numeric(15,2) NOT NULL,
    "eligibility_days" integer NOT NULL,
    "offer_end_days" integer,
    "applies_to" varchar(20) NOT NULL DEFAULT 'all'::character varying,
    "discount_account_id" integer,
    "is_active" boolean NOT NULL DEFAULT true,
    "is_mandatory" boolean NOT NULL DEFAULT false,
    "effective_from" date NOT NULL,
    "effective_to" date,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" timestamp without time zone,
    "updated_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'early_payment_discount_policies_pkey') THEN ALTER TABLE public.early_payment_discount_policies ADD CONSTRAINT early_payment_discount_policies_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== employees =====
CREATE TABLE IF NOT EXISTS public.employees (
    "id" integer NOT NULL DEFAULT nextval('employees_id_seq'::regclass),
    "employee_code" varchar(20) NOT NULL,
    "full_name" varchar(100) NOT NULL,
    "national_id" varchar(20),
    "phone" varchar(20),
    "email" varchar(100),
    "address" text,
    "job_title" varchar(100) NOT NULL,
    "department" varchar(100),
    "hire_date" timestamp without time zone NOT NULL,
    "basic_salary" numeric(12,2) NOT NULL DEFAULT 0,
    "allowances" numeric(12,2) NOT NULL DEFAULT 0,
    "deductions" numeric(12,2) NOT NULL DEFAULT 0,
    "social_insurance" numeric(12,2) NOT NULL DEFAULT 0,
    "tax_rate" numeric(12,6) NOT NULL DEFAULT 0,
    "bank_account" varchar(50),
    "bank_name" varchar(100),
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone,
    "status" varchar(50) NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'employees_pkey') THEN ALTER TABLE public.employees ADD CONSTRAINT employees_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== expense_categories =====
CREATE TABLE IF NOT EXISTS public.expense_categories (
    "id" integer NOT NULL DEFAULT nextval('expense_categories_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "description" text,
    "account_id" integer NOT NULL,
    "is_active" boolean,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'expense_categories_pkey') THEN ALTER TABLE public.expense_categories ADD CONSTRAINT expense_categories_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== expenses =====
CREATE TABLE IF NOT EXISTS public.expenses (
    "id" integer NOT NULL DEFAULT nextval('expenses_id_seq'::regclass),
    "date" timestamp without time zone,
    "category_id" integer NOT NULL,
    "amount" numeric(12,2) NOT NULL,
    "description" text NOT NULL,
    "payment_method" varchar(100) NOT NULL,
    "cash_box_id" integer NOT NULL,
    "reference_number" varchar(100),
    "receipt_image" varchar(255),
    "created_by" integer,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'expenses_pkey') THEN ALTER TABLE public.expenses ADD CONSTRAINT expenses_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== fixed_assets =====
CREATE TABLE IF NOT EXISTS public.fixed_assets (
    "id" integer NOT NULL DEFAULT nextval('fixed_assets_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "code" varchar(20) NOT NULL,
    "category" varchar(50) NOT NULL,
    "purchase_date" timestamp without time zone NOT NULL,
    "purchase_cost" numeric(12,2) NOT NULL,
    "salvage_value" numeric(12,2),
    "useful_life_years" integer NOT NULL,
    "accumulated_depreciation" numeric(12,2),
    "net_book_value" numeric(12,2) NOT NULL,
    "location" varchar(200),
    "responsible_person" varchar(100),
    "status" varchar(20),
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone,
    "depreciation_method" text NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fixed_assets_pkey') THEN ALTER TABLE public.fixed_assets ADD CONSTRAINT fixed_assets_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== idempotency_keys =====
CREATE TABLE IF NOT EXISTS public.idempotency_keys (
    "key" varchar(100) NOT NULL,
    "user_id" integer,
    "endpoint" varchar(200),
    "request_hash" varchar(64),
    "response_status" integer,
    "response_body" text,
    "created_at" timestamp without time zone DEFAULT now(),
    "expires_at" timestamp without time zone NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'idempotency_keys_pkey') THEN ALTER TABLE public.idempotency_keys ADD CONSTRAINT idempotency_keys_pkey PRIMARY KEY ("key"); END IF; END $$;

-- ===== inventory_movements =====
CREATE TABLE IF NOT EXISTS public.inventory_movements (
    "id" integer NOT NULL DEFAULT nextval('inventory_movements_id_seq'::regclass),
    "item_id" integer NOT NULL,
    "date" timestamp without time zone,
    "type" varchar(20) NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "reference_id" integer,
    "warehouse_id" integer,
    "reference_type" character varying DEFAULT 'invoice'::character varying,
    "created_at" timestamp without time zone DEFAULT now(),
    "before_quantity" numeric(14,4),
    "after_quantity" numeric(14,4),
    "unit_cost" numeric(12,2),
    "total_value" numeric(12,2),
    "created_by" integer,
    "approved_by" integer,
    "reason" varchar(255),
    "status" varchar(20) DEFAULT 'approved'::character varying,
    "reversal_of_id" integer,
    "actual_date" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'inventory_movements_pkey') THEN ALTER TABLE public.inventory_movements ADD CONSTRAINT inventory_movements_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== invoice_discount_applications =====
CREATE TABLE IF NOT EXISTS public.invoice_discount_applications (
    "id" integer NOT NULL DEFAULT nextval('invoice_discount_applications_id_seq'::regclass),
    "invoice_id" integer NOT NULL,
    "payment_id" integer,
    "policy_id" integer,
    "party_policy_id" integer,
    "original_amount" numeric(15,2) NOT NULL,
    "days_from_invoice" integer NOT NULL,
    "discount_amount" numeric(15,2) NOT NULL,
    "paid_amount" numeric(15,2) NOT NULL,
    "discount_account_id" integer,
    "journal_entry_id" integer,
    "applied_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "applied_by" integer NOT NULL,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'invoice_discount_applications_pkey') THEN ALTER TABLE public.invoice_discount_applications ADD CONSTRAINT invoice_discount_applications_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== invoice_late_fees =====
CREATE TABLE IF NOT EXISTS public.invoice_late_fees (
    "id" integer NOT NULL DEFAULT nextval('invoice_late_fees_id_seq'::regclass),
    "invoice_id" integer NOT NULL,
    "policy_id" integer NOT NULL,
    "fee_amount" numeric(15,2) NOT NULL,
    "calculation_date" date NOT NULL,
    "calculation_details" text,
    "days_overdue" integer NOT NULL,
    "overdue_balance" numeric(15,2) NOT NULL,
    "journal_entry_id" integer,
    "demand_document_number" varchar(50),
    "demand_document_date" date,
    "status" varchar(20) NOT NULL DEFAULT 'calculated'::character varying,
    "exemption_reason" text,
    "exemption_amount" numeric(15,2) DEFAULT 0,
    "exempted_by" integer,
    "exempted_at" timestamp without time zone,
    "calculation_period_start" date NOT NULL,
    "calculation_period_end" date NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'invoice_late_fees_pkey') THEN ALTER TABLE public.invoice_late_fees ADD CONSTRAINT invoice_late_fees_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== invoice_lines =====
CREATE TABLE IF NOT EXISTS public.invoice_lines (
    "id" integer NOT NULL DEFAULT nextval('invoice_lines_id_seq'::regclass),
    "invoice_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "price" numeric(12,2) NOT NULL,
    "total" numeric(12,2) NOT NULL,
    "cost_price" numeric(12,2),
    "discount" numeric(14,2) DEFAULT 0
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'invoice_lines_pkey') THEN ALTER TABLE public.invoice_lines ADD CONSTRAINT invoice_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== invoice_payments =====
CREATE TABLE IF NOT EXISTS public.invoice_payments (
    "id" integer NOT NULL DEFAULT nextval('invoice_payments_id_seq'::regclass),
    "invoice_id" integer NOT NULL,
    "method" varchar(50) NOT NULL DEFAULT 'cash'::character varying,
    "amount" numeric(12,2) NOT NULL DEFAULT 0,
    "reference" varchar(100),
    "paid_at" timestamp without time zone NOT NULL DEFAULT now(),
    "created_at" timestamp without time zone NOT NULL DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'invoice_payments_pkey') THEN ALTER TABLE public.invoice_payments ADD CONSTRAINT invoice_payments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== invoices =====
CREATE TABLE IF NOT EXISTS public.invoices (
    "id" integer NOT NULL DEFAULT nextval('invoices_id_seq'::regclass),
    "invoice_number" varchar(50) NOT NULL,
    "date" timestamp without time zone,
    "due_date" timestamp without time zone,
    "party_id" integer NOT NULL,
    "type" varchar(20) NOT NULL,
    "total_amount" numeric(12,2),
    "status" varchar(20),
    "currency_id" integer NOT NULL,
    "discount_amount" numeric(12,2),
    "discount_percentage" numeric(12,6),
    "tax_rate" numeric(12,6),
    "tax_amount" numeric(12,2),
    "net_amount" numeric(12,2),
    "created_by" integer,
    "cash_box_id" integer,
    "warehouse_id" integer,
    "subtotal" numeric(14,2) DEFAULT 0,
    "total" numeric(14,2) DEFAULT 0,
    "paid_amount" numeric(12,2) DEFAULT 0,
    "notes" text,
    "created_at" timestamp without time zone DEFAULT now(),
    "updated_at" timestamp without time zone DEFAULT now(),
    "reference_invoice_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'invoices_pkey') THEN ALTER TABLE public.invoices ADD CONSTRAINT invoices_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== items =====
CREATE TABLE IF NOT EXISTS public.items (
    "id" integer NOT NULL DEFAULT nextval('items_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "barcode" varchar(100),
    "cost_price" numeric(12,2),
    "sell_price" numeric(12,2),
    "is_kit" boolean,
    "min_stock" numeric(14,4),
    "category_id" integer,
    "avg_cost_price" numeric(12,2),
    "total_purchased_qty" numeric(14,4),
    "inventory_account_id" integer,
    "cogs_account_id" integer,
    "revenue_account_id" integer,
    "opening_quantity" numeric(14,4) DEFAULT 0,
    "is_active" boolean DEFAULT true,
    "last_cost_recalculation" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'items_pkey') THEN ALTER TABLE public.items ADD CONSTRAINT items_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== journal_entries =====
CREATE TABLE IF NOT EXISTS public.journal_entries (
    "id" integer NOT NULL DEFAULT nextval('journal_entries_id_seq'::regclass),
    "date" timestamp without time zone,
    "description" varchar(255),
    "reference_type" varchar(50),
    "reference_id" integer,
    "created_by" integer,
    "period_id" integer,
    "cash_recognition_date" date,
    "cash_basis_posted" boolean DEFAULT false,
    "source_invoice_id" integer,
    "source_payment_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'journal_entries_pkey') THEN ALTER TABLE public.journal_entries ADD CONSTRAINT journal_entries_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== journal_lines =====
CREATE TABLE IF NOT EXISTS public.journal_lines (
    "id" integer NOT NULL DEFAULT nextval('journal_lines_id_seq'::regclass),
    "entry_id" integer NOT NULL,
    "account_id" integer NOT NULL,
    "debit" numeric(12,2),
    "credit" numeric(12,2)
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'journal_lines_pkey') THEN ALTER TABLE public.journal_lines ADD CONSTRAINT journal_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== kit_component_history =====
CREATE TABLE IF NOT EXISTS public.kit_component_history (
    "id" integer NOT NULL DEFAULT nextval('kit_component_history_id_seq'::regclass),
    "kit_item_id" integer NOT NULL,
    "component_item_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "warehouse_id" integer,
    "changed_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "changed_by" integer,
    "change_type" varchar(20) NOT NULL,
    "old_quantity" numeric(14,4),
    "new_quantity" numeric(14,4),
    "old_warehouse_id" integer,
    "new_warehouse_id" integer,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'kit_component_history_pkey') THEN ALTER TABLE public.kit_component_history ADD CONSTRAINT kit_component_history_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== kit_components =====
CREATE TABLE IF NOT EXISTS public.kit_components (
    "id" integer NOT NULL DEFAULT nextval('kit_components_id_seq'::regclass),
    "kit_item_id" integer NOT NULL,
    "component_item_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "warehouse_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'kit_components_pkey') THEN ALTER TABLE public.kit_components ADD CONSTRAINT kit_components_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== late_fee_policies =====
CREATE TABLE IF NOT EXISTS public.late_fee_policies (
    "id" integer NOT NULL DEFAULT nextval('late_fee_policies_id_seq'::regclass),
    "name" varchar(200) NOT NULL,
    "description" text,
    "fee_type" varchar(10) NOT NULL,
    "fee_value" numeric(15,2) NOT NULL,
    "grace_period_days" integer NOT NULL DEFAULT 0,
    "calculation_frequency" varchar(20) NOT NULL DEFAULT 'one_time'::character varying,
    "calculation_basis" varchar(20) NOT NULL DEFAULT 'overdue_balance'::character varying,
    "applies_to" varchar(20) NOT NULL DEFAULT 'all'::character varying,
    "is_active" boolean NOT NULL DEFAULT true,
    "is_mandatory" boolean NOT NULL DEFAULT false,
    "fee_income_account_id" integer,
    "effective_from" date NOT NULL,
    "effective_to" date,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" timestamp without time zone,
    "updated_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'late_fee_policies_pkey') THEN ALTER TABLE public.late_fee_policies ADD CONSTRAINT late_fee_policies_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== loan_installments =====
CREATE TABLE IF NOT EXISTS public.loan_installments (
    "id" integer NOT NULL DEFAULT nextval('loan_installments_id_seq'::regclass),
    "loan_id" integer NOT NULL,
    "installment_number" integer NOT NULL,
    "due_date" timestamp without time zone NOT NULL,
    "principal_amount" numeric(12,2) NOT NULL,
    "interest_amount" numeric(12,2),
    "total_amount" numeric(12,2) NOT NULL,
    "paid_amount" numeric(12,2),
    "paid_date" timestamp without time zone,
    "status" varchar(20),
    "notes" text,
    "fees_amount" numeric(12,2) DEFAULT 0,
    "remaining_balance" numeric(12,2) DEFAULT 0,
    "penalty_amount" numeric(12,2) DEFAULT 0
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'loan_installments_pkey') THEN ALTER TABLE public.loan_installments ADD CONSTRAINT loan_installments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== loan_payments =====
CREATE TABLE IF NOT EXISTS public.loan_payments (
    "id" integer NOT NULL DEFAULT nextval('loan_payments_id_seq'::regclass),
    "loan_id" integer NOT NULL,
    "installment_id" integer,
    "payment_date" date NOT NULL,
    "amount" numeric NOT NULL,
    "principal_part" numeric NOT NULL DEFAULT 0,
    "interest_part" numeric NOT NULL DEFAULT 0,
    "method" varchar(30) NOT NULL DEFAULT 'cash'::character varying,
    "cash_box_id" integer,
    "reference_number" varchar(60),
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone NOT NULL DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'loan_payments_pkey') THEN ALTER TABLE public.loan_payments ADD CONSTRAINT loan_payments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== loan_rescheduling_history =====
CREATE TABLE IF NOT EXISTS public.loan_rescheduling_history (
    "id" integer NOT NULL DEFAULT nextval('loan_rescheduling_history_id_seq'::regclass),
    "loan_id" integer,
    "reschedule_date" timestamp without time zone NOT NULL,
    "old_interest_rate" numeric(12,6),
    "new_interest_rate" numeric(12,6),
    "old_num_installments" integer,
    "new_num_installments" integer,
    "reason" text,
    "fees_amount" numeric(12,2) DEFAULT 0,
    "performed_by" integer,
    "performed_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'loan_rescheduling_history_pkey') THEN ALTER TABLE public.loan_rescheduling_history ADD CONSTRAINT loan_rescheduling_history_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== loans =====
CREATE TABLE IF NOT EXISTS public.loans (
    "id" integer NOT NULL DEFAULT nextval('loans_id_seq'::regclass),
    "loan_number" varchar(50),
    "borrower_name" varchar(100),
    "borrower_type" varchar(50),
    "principal_amount" numeric(12,2),
    "interest_rate" numeric(12,6),
    "loan_date" timestamp without time zone,
    "maturity_date" timestamp without time zone,
    "term_months" integer,
    "installment_amount" numeric(12,2),
    "total_interest" numeric(12,2),
    "total_amount" numeric(12,2),
    "paid_amount" numeric(12,2),
    "remaining_amount" numeric(12,2),
    "cash_box_id" integer,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone,
    "loan_type" varchar(50) NOT NULL,
    "status" varchar(50) NOT NULL,
    "bank_account_id" integer,
    "contract_number" varchar(100),
    "interest_type" varchar(20) DEFAULT 'fixed'::character varying,
    "interest_calc_method" varchar(20) DEFAULT 'declining'::character varying,
    "payment_frequency" varchar(20) DEFAULT 'monthly'::character varying,
    "num_installments" integer,
    "first_payment_date" date,
    "end_date" date,
    "grace_period" integer DEFAULT 0,
    "admin_fees" numeric(12,2) DEFAULT 0,
    "insurance_amount" numeric(12,2) DEFAULT 0,
    "credit_limit" numeric(12,2) DEFAULT 0,
    "day_count_basis" varchar(10) DEFAULT '365'::character varying,
    "liability_account_id" integer,
    "interest_expense_account_id" integer,
    "employee_id" integer,
    "custody_id" integer,
    "repayment_method" varchar(30) DEFAULT 'cash'::character varying
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'loans_pkey') THEN ALTER TABLE public.loans ADD CONSTRAINT loans_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== login_attempts =====
CREATE TABLE IF NOT EXISTS public.login_attempts (
    "id" integer NOT NULL DEFAULT nextval('login_attempts_id_seq'::regclass),
    "username" varchar(100),
    "ip_address" varchar(45),
    "success" boolean,
    "failure_reason" varchar(200),
    "attempted_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'login_attempts_pkey') THEN ALTER TABLE public.login_attempts ADD CONSTRAINT login_attempts_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== overdraft_transactions =====
CREATE TABLE IF NOT EXISTS public.overdraft_transactions (
    "id" integer NOT NULL DEFAULT nextval('overdraft_transactions_id_seq'::regclass),
    "loan_id" integer,
    "transaction_date" timestamp without time zone NOT NULL,
    "transaction_type" varchar(20) NOT NULL,
    "amount" numeric(12,2) NOT NULL,
    "balance_after" numeric(12,2),
    "interest_amount" numeric(12,2) DEFAULT 0,
    "notes" text,
    "reference_number" varchar(100),
    "created_by" integer,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'overdraft_transactions_pkey') THEN ALTER TABLE public.overdraft_transactions ADD CONSTRAINT overdraft_transactions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== parties =====
CREATE TABLE IF NOT EXISTS public.parties (
    "id" integer NOT NULL DEFAULT nextval('parties_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "type" varchar(50) NOT NULL,
    "phone" varchar(50),
    "address" text,
    "credit_limit" numeric(12,2),
    "account_id" integer NOT NULL,
    "opening_balance" numeric(12,2) DEFAULT 0,
    "is_active" boolean DEFAULT true
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'parties_pkey') THEN ALTER TABLE public.parties ADD CONSTRAINT parties_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== party_discount_policies =====
CREATE TABLE IF NOT EXISTS public.party_discount_policies (
    "id" integer NOT NULL DEFAULT nextval('party_discount_policies_id_seq'::regclass),
    "party_id" integer NOT NULL,
    "policy_id" integer NOT NULL,
    "custom_discount_value" numeric(15,2),
    "custom_eligibility_days" integer,
    "is_active" boolean NOT NULL DEFAULT true,
    "assigned_by" integer NOT NULL,
    "assigned_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'party_discount_policies_pkey') THEN ALTER TABLE public.party_discount_policies ADD CONSTRAINT party_discount_policies_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== party_late_fee_exceptions =====
CREATE TABLE IF NOT EXISTS public.party_late_fee_exceptions (
    "id" integer NOT NULL DEFAULT nextval('party_late_fee_exceptions_id_seq'::regclass),
    "party_id" integer NOT NULL,
    "policy_id" integer NOT NULL,
    "exception_type" varchar(20) NOT NULL,
    "custom_fee_value" numeric(15,2),
    "custom_grace_days" integer,
    "is_active" boolean NOT NULL DEFAULT true,
    "reason" text,
    "set_by" integer NOT NULL,
    "set_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "expires_at" date
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'party_late_fee_exceptions_pkey') THEN ALTER TABLE public.party_late_fee_exceptions ADD CONSTRAINT party_late_fee_exceptions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== payments =====
CREATE TABLE IF NOT EXISTS public.payments (
    "id" integer NOT NULL DEFAULT nextval('payments_id_seq'::regclass),
    "date" timestamp without time zone,
    "party_id" integer,
    "amount" numeric(12,2) NOT NULL,
    "payment_type" varchar(20) NOT NULL,
    "payment_method" varchar(100) NOT NULL,
    "reference_number" varchar(100),
    "notes" text,
    "currency_id" integer NOT NULL DEFAULT 1,
    "cash_box_id" integer NOT NULL,
    "account_id" integer,
    "invoice_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'payments_pkey') THEN ALTER TABLE public.payments ADD CONSTRAINT payments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== petty_cash_custodies =====
CREATE TABLE IF NOT EXISTS public.petty_cash_custodies (
    "id" integer NOT NULL DEFAULT nextval('petty_cash_custodies_id_seq'::regclass),
    "custody_number" varchar(50),
    "employee_id" integer NOT NULL,
    "amount" numeric(15,2) NOT NULL DEFAULT 0,
    "currency" varchar(10) NOT NULL DEFAULT 'EGP'::character varying,
    "cash_box_id" integer NOT NULL,
    "custody_account_id" integer NOT NULL,
    "issued_date" date NOT NULL,
    "expected_settlement_date" date,
    "status" varchar(20) NOT NULL DEFAULT 'open'::character varying,
    "description" text,
    "journal_entry_id" integer,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "settled_at" timestamp without time zone,
    "settled_by" integer,
    "cancelled_at" timestamp without time zone,
    "cancelled_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'petty_cash_custodies_pkey') THEN ALTER TABLE public.petty_cash_custodies ADD CONSTRAINT petty_cash_custodies_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== petty_cash_expenses =====
CREATE TABLE IF NOT EXISTS public.petty_cash_expenses (
    "id" integer NOT NULL DEFAULT nextval('petty_cash_expenses_id_seq'::regclass),
    "custody_id" integer NOT NULL,
    "expense_number" varchar(50),
    "expense_date" date NOT NULL,
    "amount" numeric(15,2) NOT NULL,
    "expense_category_id" integer,
    "expense_account_id" integer,
    "description" text NOT NULL,
    "receipt_file_path" varchar(500),
    "receipt_file_type" varchar(10),
    "receipt_file_size" bigint,
    "receipt_file_hash" varchar(64),
    "vendor_name" varchar(200),
    "journal_entry_id" integer,
    "status" varchar(20) NOT NULL DEFAULT 'pending'::character varying,
    "recorded_by" integer NOT NULL,
    "recorded_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "rejected_reason" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'petty_cash_expenses_pkey') THEN ALTER TABLE public.petty_cash_expenses ADD CONSTRAINT petty_cash_expenses_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== petty_cash_settlements =====
CREATE TABLE IF NOT EXISTS public.petty_cash_settlements (
    "id" integer NOT NULL DEFAULT nextval('petty_cash_settlements_id_seq'::regclass),
    "settlement_number" varchar(50),
    "custody_id" integer NOT NULL,
    "settlement_date" date NOT NULL,
    "total_expenses" numeric(15,2) NOT NULL,
    "remaining_amount" numeric(15,2) NOT NULL,
    "returned_amount" numeric(15,2) NOT NULL DEFAULT 0,
    "shortage_amount" numeric(15,2) NOT NULL DEFAULT 0,
    "shortage_account_id" integer,
    "cash_box_id" integer NOT NULL,
    "journal_entry_id" integer,
    "status" varchar(20) NOT NULL DEFAULT 'draft'::character varying,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "notes" text,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'petty_cash_settlements_pkey') THEN ALTER TABLE public.petty_cash_settlements ADD CONSTRAINT petty_cash_settlements_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== reconciliation_adjustments =====
CREATE TABLE IF NOT EXISTS public.reconciliation_adjustments (
    "id" integer NOT NULL DEFAULT nextval('reconciliation_adjustments_id_seq'::regclass),
    "reconciliation_id" integer NOT NULL,
    "adjustment_type" varchar(30) NOT NULL,
    "amount" numeric(15,2) NOT NULL,
    "description" text NOT NULL,
    "bank_statement_line_id" integer,
    "target_account_id" integer,
    "journal_entry_id" integer,
    "status" varchar(20) NOT NULL DEFAULT 'pending'::character varying,
    "posted_at" timestamp without time zone,
    "posted_by" integer,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'reconciliation_adjustments_pkey') THEN ALTER TABLE public.reconciliation_adjustments ADD CONSTRAINT reconciliation_adjustments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== reconciliation_matches =====
CREATE TABLE IF NOT EXISTS public.reconciliation_matches (
    "id" integer NOT NULL DEFAULT nextval('reconciliation_matches_id_seq'::regclass),
    "reconciliation_id" integer NOT NULL,
    "bank_statement_line_id" integer,
    "system_payment_id" integer,
    "system_journal_line_id" integer,
    "match_type" varchar(20) NOT NULL,
    "match_criteria" jsonb,
    "confidence_score" numeric(12,6),
    "matched_by" integer NOT NULL,
    "matched_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'reconciliation_matches_pkey') THEN ALTER TABLE public.reconciliation_matches ADD CONSTRAINT reconciliation_matches_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== recurring_invoice_lines =====
CREATE TABLE IF NOT EXISTS public.recurring_invoice_lines (
    "id" integer NOT NULL DEFAULT nextval('recurring_invoice_lines_id_seq'::regclass),
    "template_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "price" numeric(12,2) NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recurring_invoice_lines_pkey') THEN ALTER TABLE public.recurring_invoice_lines ADD CONSTRAINT recurring_invoice_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== recurring_invoice_templates =====
CREATE TABLE IF NOT EXISTS public.recurring_invoice_templates (
    "id" integer NOT NULL DEFAULT nextval('recurring_invoice_templates_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "party_id" integer NOT NULL,
    "invoice_type" varchar(20) NOT NULL,
    "start_date" timestamp without time zone NOT NULL,
    "end_date" timestamp without time zone,
    "next_run_date" timestamp without time zone NOT NULL,
    "last_run_date" timestamp without time zone,
    "discount_percentage" numeric(12,6),
    "tax_rate" numeric(12,6),
    "notes" text,
    "is_active" boolean,
    "auto_post" boolean,
    "runs_count" integer,
    "created_by" integer,
    "created_at" timestamp without time zone,
    "frequency" varchar(50) NOT NULL
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recurring_invoice_templates_pkey') THEN ALTER TABLE public.recurring_invoice_templates ADD CONSTRAINT recurring_invoice_templates_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== recurring_journal_executions =====
CREATE TABLE IF NOT EXISTS public.recurring_journal_executions (
    "id" integer NOT NULL DEFAULT nextval('recurring_journal_executions_id_seq'::regclass),
    "template_id" integer NOT NULL,
    "execution_date" date NOT NULL,
    "planned_run_date" date NOT NULL,
    "journal_entry_id" integer,
    "status" varchar(20) NOT NULL,
    "total_debit" numeric(15,2),
    "total_credit" numeric(15,2),
    "error_message" text,
    "executed_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "executed_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recurring_journal_executions_pkey') THEN ALTER TABLE public.recurring_journal_executions ADD CONSTRAINT recurring_journal_executions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== recurring_journal_lines =====
CREATE TABLE IF NOT EXISTS public.recurring_journal_lines (
    "id" integer NOT NULL DEFAULT nextval('recurring_journal_lines_id_seq'::regclass),
    "template_id" integer NOT NULL,
    "line_number" integer NOT NULL,
    "account_id" integer NOT NULL,
    "debit" numeric(15,2) DEFAULT 0,
    "credit" numeric(15,2) DEFAULT 0,
    "description" text,
    "cost_center_id" integer,
    "amount_rule" varchar(50) NOT NULL DEFAULT 'fixed'::character varying,
    "rule_parameters" jsonb
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recurring_journal_lines_pkey') THEN ALTER TABLE public.recurring_journal_lines ADD CONSTRAINT recurring_journal_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== recurring_journal_templates =====
CREATE TABLE IF NOT EXISTS public.recurring_journal_templates (
    "id" integer NOT NULL DEFAULT nextval('recurring_journal_templates_id_seq'::regclass),
    "template_number" varchar(50),
    "name" varchar(200) NOT NULL,
    "description" text,
    "frequency" varchar(20) NOT NULL,
    "custom_days" integer,
    "execution_day" integer,
    "start_date" date NOT NULL,
    "end_date" date,
    "is_active" boolean NOT NULL DEFAULT true,
    "auto_post" boolean NOT NULL DEFAULT false,
    "next_run_date" date NOT NULL,
    "last_run_date" timestamp without time zone,
    "last_run_status" varchar(20),
    "last_run_error" text,
    "description_template" text,
    "created_by" integer NOT NULL,
    "created_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" timestamp without time zone,
    "updated_by" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'recurring_journal_templates_pkey') THEN ALTER TABLE public.recurring_journal_templates ADD CONSTRAINT recurring_journal_templates_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== reporting_settings =====
CREATE TABLE IF NOT EXISTS public.reporting_settings (
    "id" integer NOT NULL DEFAULT nextval('reporting_settings_id_seq'::regclass),
    "default_basis" varchar(10) NOT NULL DEFAULT 'accrual'::character varying,
    "allow_comparison" boolean NOT NULL DEFAULT true,
    "cash_recognition_threshold_days" integer DEFAULT 0,
    "updated_by" integer,
    "updated_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'reporting_settings_pkey') THEN ALTER TABLE public.reporting_settings ADD CONSTRAINT reporting_settings_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== roles =====
CREATE TABLE IF NOT EXISTS public.roles (
    "id" integer NOT NULL DEFAULT nextval('roles_id_seq'::regclass),
    "name" varchar(50) NOT NULL,
    "display_name" varchar(200) NOT NULL,
    "description" text,
    "is_system_role" boolean DEFAULT false,
    "is_active" boolean DEFAULT true,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'roles_pkey') THEN ALTER TABLE public.roles ADD CONSTRAINT roles_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== salary_records =====
CREATE TABLE IF NOT EXISTS public.salary_records (
    "id" integer NOT NULL DEFAULT nextval('salary_records_id_seq'::regclass),
    "employee_id" integer NOT NULL,
    "month" integer NOT NULL,
    "year" integer NOT NULL,
    "basic_salary" numeric(12,2) NOT NULL,
    "allowances" numeric(12,2),
    "overtime" numeric(12,2),
    "bonus" numeric(12,2),
    "gross_salary" numeric(12,2) NOT NULL,
    "social_insurance" numeric(12,2),
    "tax" numeric(12,2),
    "other_deductions" numeric(12,2),
    "total_deductions" numeric(12,2),
    "net_salary" numeric(12,2) NOT NULL,
    "payment_date" timestamp without time zone,
    "payment_method" varchar(50),
    "status" varchar(20),
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'salary_records_pkey') THEN ALTER TABLE public.salary_records ADD CONSTRAINT salary_records_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== shift_closings =====
CREATE TABLE IF NOT EXISTS public.shift_closings (
    "id" integer NOT NULL DEFAULT nextval('shift_closings_id_seq'::regclass),
    "cash_box_id" integer NOT NULL,
    "shift_date" timestamp without time zone DEFAULT now(),
    "opening_amount" numeric(12,2) NOT NULL,
    "expected_closing" numeric(12,2) NOT NULL,
    "actual_closing" numeric(12,2) NOT NULL,
    "difference" numeric(12,2) NOT NULL,
    "status" character varying DEFAULT 'closed'::character varying,
    "closed_by" integer,
    "notes" text,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'shift_closings_pkey') THEN ALTER TABLE public.shift_closings ADD CONSTRAINT shift_closings_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== stock_adjustments =====
CREATE TABLE IF NOT EXISTS public.stock_adjustments (
    "id" integer NOT NULL DEFAULT nextval('stock_adjustments_id_seq'::regclass),
    "item_id" integer NOT NULL,
    "warehouse_id" integer,
    "adjustment_date" timestamp without time zone DEFAULT now(),
    "old_quantity" numeric(14,4) NOT NULL,
    "new_quantity" numeric(14,4) NOT NULL,
    "difference" numeric(14,4) NOT NULL,
    "reason" character varying NOT NULL,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone DEFAULT now(),
    "document_number" varchar(50),
    "status" varchar(20) DEFAULT 'draft'::character varying,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "executed_at" timestamp without time zone,
    "movement_id" integer,
    "unit_cost" numeric(12,2),
    "value_before" numeric(12,2),
    "value_after" numeric(12,2),
    "reference_document_type" varchar(50)
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'stock_adjustments_pkey') THEN ALTER TABLE public.stock_adjustments ADD CONSTRAINT stock_adjustments_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== stock_count_sessions =====
CREATE TABLE IF NOT EXISTS public.stock_count_sessions (
    "id" integer NOT NULL DEFAULT nextval('stock_count_sessions_id_seq'::regclass),
    "document_number" varchar(50) NOT NULL,
    "session_date" timestamp without time zone NOT NULL DEFAULT now(),
    "warehouse_id" integer NOT NULL,
    "count_type" varchar(20) NOT NULL DEFAULT 'full'::character varying,
    "status" varchar(20) NOT NULL DEFAULT 'draft'::character varying,
    "book_balance_locked" boolean DEFAULT false,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone DEFAULT now(),
    "started_at" timestamp without time zone,
    "submitted_at" timestamp without time zone,
    "reviewed_by" integer,
    "reviewed_at" timestamp without time zone,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "executed_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'stock_count_sessions_pkey') THEN ALTER TABLE public.stock_count_sessions ADD CONSTRAINT stock_count_sessions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== stock_counts =====
CREATE TABLE IF NOT EXISTS public.stock_counts (
    "id" integer NOT NULL DEFAULT nextval('stock_counts_id_seq'::regclass),
    "date" timestamp without time zone,
    "warehouse_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "system_quantity" numeric(14,4) NOT NULL,
    "counted_quantity" numeric(14,4),
    "difference" numeric(14,4) NOT NULL,
    "notes" text,
    "counted_by" integer,
    "session_id" integer,
    "book_quantity" numeric(14,4),
    "recount_quantity" numeric(14,4),
    "difference_value" numeric(12,2),
    "counted_at" timestamp without time zone,
    "status" varchar(20) DEFAULT 'pending'::character varying
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'stock_counts_pkey') THEN ALTER TABLE public.stock_counts ADD CONSTRAINT stock_counts_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== stock_levels =====
CREATE TABLE IF NOT EXISTS public.stock_levels (
    "id" integer NOT NULL DEFAULT nextval('stock_levels_id_seq'::regclass),
    "warehouse_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "quantity" numeric(14,4),
    "last_updated" timestamp without time zone,
    "reserved_quantity" numeric(14,4) DEFAULT 0,
    "damaged_quantity" numeric(14,4) DEFAULT 0,
    "under_inspection_quantity" numeric(14,4) DEFAULT 0
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'stock_levels_pkey') THEN ALTER TABLE public.stock_levels ADD CONSTRAINT stock_levels_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== stock_reasons =====
CREATE TABLE IF NOT EXISTS public.stock_reasons (
    "id" integer NOT NULL DEFAULT nextval('stock_reasons_id_seq'::regclass),
    "reason_type" varchar(30) NOT NULL,
    "code" varchar(50) NOT NULL,
    "name" varchar(200) NOT NULL,
    "is_active" boolean DEFAULT true,
    "affects_financial" boolean DEFAULT false,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'stock_reasons_pkey') THEN ALTER TABLE public.stock_reasons ADD CONSTRAINT stock_reasons_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== subscription_periods =====
CREATE TABLE IF NOT EXISTS public.subscription_periods (
    "id" integer NOT NULL DEFAULT nextval('subscription_periods_id_seq'::regclass),
    "subscription_id" integer NOT NULL,
    "period_number" integer NOT NULL,
    "period_start" date NOT NULL,
    "period_end" date NOT NULL,
    "due_date" date NOT NULL,
    "amount" numeric(18,2) NOT NULL DEFAULT 0,
    "paid_amount" numeric(18,2) NOT NULL DEFAULT 0,
    "status" varchar(20) NOT NULL DEFAULT 'due'::character varying,
    "invoice_id" integer,
    "payment_id" integer,
    "paid_at" timestamp without time zone,
    "notes" text,
    "created_at" timestamp without time zone NOT NULL DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscription_periods_pkey') THEN ALTER TABLE public.subscription_periods ADD CONSTRAINT subscription_periods_pkey PRIMARY KEY ("id"); END IF; END $$;
ALTER SEQUENCE public.subscription_periods_id_seq OWNED BY public.subscription_periods.id;
CREATE INDEX IF NOT EXISTS idx_sub_periods_due ON public.subscription_periods USING btree (due_date, status);
CREATE INDEX IF NOT EXISTS idx_sub_periods_sub ON public.subscription_periods USING btree (subscription_id);

-- ===== subscriptions =====
CREATE TABLE IF NOT EXISTS public.subscriptions (
    "id" integer NOT NULL DEFAULT nextval('subscriptions_id_seq'::regclass),
    "party_id" integer NOT NULL,
    "service_name" varchar(150) NOT NULL,
    "description" text,
    "amount" numeric(18,2) NOT NULL DEFAULT 0,
    "frequency" varchar(20) NOT NULL DEFAULT 'monthly'::character varying,
    "start_date" date NOT NULL,
    "end_date" date,
    "auto_invoice" boolean NOT NULL DEFAULT true,
    "status" varchar(20) NOT NULL DEFAULT 'active'::character varying,
    "notes" text,
    "created_by" integer,
    "created_at" timestamp without time zone NOT NULL DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscriptions_pkey') THEN ALTER TABLE public.subscriptions ADD CONSTRAINT subscriptions_pkey PRIMARY KEY ("id"); END IF; END $$;
ALTER SEQUENCE public.subscriptions_id_seq OWNED BY public.subscriptions.id;
CREATE INDEX IF NOT EXISTS idx_subscriptions_party ON public.subscriptions USING btree (party_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON public.subscriptions USING btree (status);

-- ===== user_devices =====
CREATE TABLE IF NOT EXISTS public.user_devices (
    "id" integer NOT NULL DEFAULT nextval('user_devices_id_seq'::regclass),
    "user_id" integer NOT NULL,
    "device_id" varchar(100) NOT NULL,
    "device_name" varchar(200),
    "refresh_token_hash" varchar(500) NOT NULL,
    "last_seen" timestamp without time zone DEFAULT now(),
    "is_revoked" boolean DEFAULT false,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_devices_pkey') THEN ALTER TABLE public.user_devices ADD CONSTRAINT user_devices_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== user_permissions =====
CREATE TABLE IF NOT EXISTS public.user_permissions (
    "id" integer NOT NULL DEFAULT nextval('user_permissions_id_seq'::regclass),
    "user_id" integer NOT NULL,
    "permission_key" character varying NOT NULL,
    "is_granted" boolean DEFAULT true,
    "created_at" timestamp without time zone DEFAULT now()
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_permissions_pkey') THEN ALTER TABLE public.user_permissions ADD CONSTRAINT user_permissions_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== users =====
CREATE TABLE IF NOT EXISTS public.users (
    "id" integer NOT NULL DEFAULT nextval('users_id_seq'::regclass),
    "username" varchar(50) NOT NULL,
    "password_hash" varchar(255) NOT NULL,
    "full_name" varchar(100) NOT NULL,
    "email" varchar(100),
    "is_active" boolean,
    "created_at" timestamp without time zone,
    "last_login" timestamp without time zone,
    "role" varchar(50) NOT NULL,
    "must_change_password" boolean DEFAULT false,
    "role_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'users_pkey') THEN ALTER TABLE public.users ADD CONSTRAINT users_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== warehouse_transfer_lines =====
CREATE TABLE IF NOT EXISTS public.warehouse_transfer_lines (
    "id" integer NOT NULL DEFAULT nextval('warehouse_transfer_lines_id_seq'::regclass),
    "transfer_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "requested_quantity" numeric(14,4) NOT NULL,
    "shipped_quantity" numeric(14,4) DEFAULT 0,
    "received_quantity" numeric(14,4) DEFAULT 0,
    "damaged_quantity" numeric(14,4) DEFAULT 0,
    "missing_quantity" numeric(14,4) DEFAULT 0,
    "difference_reason" text,
    "notes" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'warehouse_transfer_lines_pkey') THEN ALTER TABLE public.warehouse_transfer_lines ADD CONSTRAINT warehouse_transfer_lines_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== warehouse_transfer_log =====
CREATE TABLE IF NOT EXISTS public.warehouse_transfer_log (
    "id" integer NOT NULL DEFAULT nextval('warehouse_transfer_log_id_seq'::regclass),
    "transfer_id" integer NOT NULL,
    "action" varchar(50) NOT NULL,
    "performed_by" integer NOT NULL,
    "performed_at" timestamp without time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" text,
    "old_status" varchar(20),
    "new_status" varchar(20),
    "metadata" jsonb
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'warehouse_transfer_log_pkey') THEN ALTER TABLE public.warehouse_transfer_log ADD CONSTRAINT warehouse_transfer_log_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== warehouse_transfers =====
CREATE TABLE IF NOT EXISTS public.warehouse_transfers (
    "id" integer NOT NULL DEFAULT nextval('warehouse_transfers_id_seq'::regclass),
    "date" timestamp without time zone,
    "from_warehouse_id" integer NOT NULL,
    "to_warehouse_id" integer NOT NULL,
    "item_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "notes" text,
    "created_by" integer,
    "status" varchar(50),
    "transfer_number" varchar(50),
    "shipped_quantity" numeric(14,4) DEFAULT 0,
    "received_quantity" numeric(14,4) DEFAULT 0,
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "shipped_at" timestamp without time zone,
    "received_at" timestamp without time zone,
    "cancelled_at" timestamp without time zone,
    "cancelled_by" integer,
    "cancellation_reason" text
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'warehouse_transfers_pkey') THEN ALTER TABLE public.warehouse_transfers ADD CONSTRAINT warehouse_transfers_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== warehouses =====
CREATE TABLE IF NOT EXISTS public.warehouses (
    "id" integer NOT NULL DEFAULT nextval('warehouses_id_seq'::regclass),
    "name" varchar(100) NOT NULL,
    "code" varchar(20) NOT NULL,
    "location" varchar(200),
    "responsible_person" varchar(100),
    "is_active" boolean,
    "notes" text,
    "created_at" timestamp without time zone
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'warehouses_pkey') THEN ALTER TABLE public.warehouses ADD CONSTRAINT warehouses_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== writeoff_requests =====
CREATE TABLE IF NOT EXISTS public.writeoff_requests (
    "id" integer NOT NULL DEFAULT nextval('writeoff_requests_id_seq'::regclass),
    "document_number" varchar(50) NOT NULL,
    "date" timestamp without time zone NOT NULL DEFAULT now(),
    "damaged_item_id" integer,
    "item_id" integer NOT NULL,
    "warehouse_id" integer NOT NULL,
    "quantity" numeric(14,4) NOT NULL,
    "unit_cost" numeric(12,2),
    "total_value" numeric(12,2),
    "writeoff_reason_id" integer,
    "proposed_action" varchar(30) NOT NULL,
    "notes" text,
    "status" varchar(20) DEFAULT 'draft'::character varying,
    "requested_by" integer,
    "requested_at" timestamp without time zone DEFAULT now(),
    "approved_by" integer,
    "approved_at" timestamp without time zone,
    "rejected_by" integer,
    "rejected_at" timestamp without time zone,
    "rejection_reason" text,
    "executed_by" integer,
    "executed_at" timestamp without time zone,
    "movement_id" integer
);
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'writeoff_requests_pkey') THEN ALTER TABLE public.writeoff_requests ADD CONSTRAINT writeoff_requests_pkey PRIMARY KEY ("id"); END IF; END $$;

-- ===== Foreign Keys =====
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscription_periods_invoice_id_fkey') THEN ALTER TABLE subscription_periods ADD CONSTRAINT subscription_periods_invoice_id_fkey FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE SET NULL; END IF; END $$;
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscription_periods_payment_id_fkey') THEN ALTER TABLE subscription_periods ADD CONSTRAINT subscription_periods_payment_id_fkey FOREIGN KEY (payment_id) REFERENCES payments(id) ON DELETE SET NULL; END IF; END $$;
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscription_periods_subscription_id_fkey') THEN ALTER TABLE subscription_periods ADD CONSTRAINT subscription_periods_subscription_id_fkey FOREIGN KEY (subscription_id) REFERENCES subscriptions(id) ON DELETE CASCADE; END IF; END $$;
DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'subscriptions_party_id_fkey') THEN ALTER TABLE subscriptions ADD CONSTRAINT subscriptions_party_id_fkey FOREIGN KEY (party_id) REFERENCES parties(id) ON DELETE RESTRICT; END IF; END $$;

-- ===== Views =====
CREATE OR REPLACE VIEW v_monthly_purchases AS
 SELECT date_trunc('month'::text, date) AS month,
    count(*) AS invoice_count,
    sum(net_amount) AS total_net
   FROM invoices
  WHERE (((type)::text = 'purchase'::text) AND ((status)::text <> 'cancelled'::text))
  GROUP BY (date_trunc('month'::text, date))
  ORDER BY (date_trunc('month'::text, date)) DESC;
CREATE OR REPLACE VIEW v_monthly_sales AS
 SELECT date_trunc('month'::text, date) AS month,
    count(*) AS invoice_count,
    sum(net_amount) AS total_net,
    sum(discount_amount) AS total_discount,
    sum(tax_amount) AS total_tax
   FROM invoices
  WHERE (((type)::text = 'sale'::text) AND ((status)::text <> 'cancelled'::text))
  GROUP BY (date_trunc('month'::text, date))
  ORDER BY (date_trunc('month'::text, date)) DESC;
CREATE OR REPLACE VIEW v_party_ledger AS
 SELECT i.party_id,
    i.date,
    'invoice'::text AS source,
    i.invoice_number,
    i.type AS invoice_type,
    i.net_amount AS amount
   FROM invoices i
  WHERE ((i.status)::text <> 'cancelled'::text)
UNION ALL
 SELECT p.party_id,
    p.date,
    'payment'::text AS source,
    p.reference_number AS invoice_number,
    p.payment_type AS invoice_type,
        CASE
            WHEN ((p.payment_type)::text = 'receipt'::text) THEN p.amount
            ELSE (- p.amount)
        END AS amount
   FROM payments p
  ORDER BY 2 DESC;
CREATE OR REPLACE VIEW v_stock_balance AS
 SELECT item_id,
    sum(
        CASE
            WHEN ((type)::text = 'in'::text) THEN quantity
            ELSE (- quantity)
        END) AS balance,
    sum(
        CASE
            WHEN ((type)::text = 'in'::text) THEN quantity
            ELSE (0)::numeric
        END) AS total_in,
    sum(
        CASE
            WHEN ((type)::text = 'out'::text) THEN quantity
            ELSE (0)::numeric
        END) AS total_out
   FROM inventory_movements
  GROUP BY item_id;
CREATE OR REPLACE VIEW v_top_selling_items AS
 SELECT il.item_id,
    i.name AS item_name,
    sum(il.quantity) AS total_qty,
    sum(il.total) AS total_revenue,
    count(DISTINCT il.invoice_id) AS invoice_count
   FROM ((invoice_lines il
     JOIN invoices inv ON ((il.invoice_id = inv.id)))
     JOIN items i ON ((il.item_id = i.id)))
  WHERE (((inv.type)::text = 'sale'::text) AND ((inv.status)::text <> 'cancelled'::text))
  GROUP BY il.item_id, i.name
  ORDER BY (sum(il.quantity)) DESC;

COMMIT;

-- Sequences are synced automatically by the application on startup.
