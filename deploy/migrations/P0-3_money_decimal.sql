-- ============================================================================
-- TECHTOON P0-3: Migrate monetary + quantity double -> numeric
-- Status: PREPARATION ONLY — rehearsed on local test DB, NOT yet applied to prod.
-- ============================================================================
-- Rules:
--  * Money  -> numeric(12,2)
--  * Quantity / Unit -> numeric(14,4)
--  * Rate / Percent / Ratio -> numeric(12,6)
--  * Keeps exact binary->decimal conversion via USING (round to target scale)
--
-- File: deploy/migrations/P0-3_money_decimal.sql (idempotent guard: only alters
--       columns currently double precision)
-- ============================================================================

BEGIN;

-- --- Money (12,2) ---
ALTER TABLE invoices
    ALTER COLUMN total_amount TYPE numeric(12,2) USING (ROUND(total_amount::numeric,2)),
    ALTER COLUMN discount_amount TYPE numeric(12,2) USING (ROUND(discount_amount::numeric,2)),
    ALTER COLUMN tax_amount TYPE numeric(12,2) USING (ROUND(tax_amount::numeric,2)),
    ALTER COLUMN net_amount TYPE numeric(12,2) USING (ROUND(net_amount::numeric,2)),
    ALTER COLUMN paid_amount TYPE numeric(12,2) USING (ROUND(paid_amount::numeric,2));
-- discount_percentage / tax_rate -> rate (handled below)

ALTER TABLE invoice_lines
    ALTER COLUMN price TYPE numeric(12,2) USING (ROUND(price::numeric,2)),
    ALTER COLUMN total TYPE numeric(12,2) USING (ROUND(total::numeric,2)),
    ALTER COLUMN cost_price TYPE numeric(12,2) USING (ROUND(cost_price::numeric,2));

ALTER TABLE payments
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2));

ALTER TABLE invoice_payments
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2));

ALTER TABLE expenses
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2));

ALTER TABLE cash_boxes
    ALTER COLUMN max_limit TYPE numeric(12,2) USING (ROUND(max_limit::numeric,2));

ALTER TABLE cash_transfers
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2)),
    ALTER COLUMN transfer_fee TYPE numeric(12,2) USING (ROUND(transfer_fee::numeric,2));

ALTER TABLE journal_lines
    ALTER COLUMN debit TYPE numeric(12,2) USING (ROUND(debit::numeric,2)),
    ALTER COLUMN credit TYPE numeric(12,2) USING (ROUND(credit::numeric,2));

ALTER TABLE items
    ALTER COLUMN cost_price TYPE numeric(12,2) USING (ROUND(cost_price::numeric,2)),
    ALTER COLUMN sell_price TYPE numeric(12,2) USING (ROUND(sell_price::numeric,2)),
    ALTER COLUMN avg_cost_price TYPE numeric(12,2) USING (ROUND(avg_cost_price::numeric,2));

ALTER TABLE parties
    ALTER COLUMN credit_limit TYPE numeric(12,2) USING (ROUND(credit_limit::numeric,2));

ALTER TABLE loans
    ALTER COLUMN principal_amount TYPE numeric(12,2) USING (ROUND(principal_amount::numeric,2)),
    ALTER COLUMN installment_amount TYPE numeric(12,2) USING (ROUND(installment_amount::numeric,2)),
    ALTER COLUMN total_interest TYPE numeric(12,2) USING (ROUND(total_interest::numeric,2)),
    ALTER COLUMN total_amount TYPE numeric(12,2) USING (ROUND(total_amount::numeric,2)),
    ALTER COLUMN paid_amount TYPE numeric(12,2) USING (ROUND(paid_amount::numeric,2)),
    ALTER COLUMN remaining_amount TYPE numeric(12,2) USING (ROUND(remaining_amount::numeric,2));

ALTER TABLE loan_installments
    ALTER COLUMN principal_amount TYPE numeric(12,2) USING (ROUND(principal_amount::numeric,2)),
    ALTER COLUMN interest_amount TYPE numeric(12,2) USING (ROUND(interest_amount::numeric,2)),
    ALTER COLUMN total_amount TYPE numeric(12,2) USING (ROUND(total_amount::numeric,2)),
    ALTER COLUMN paid_amount TYPE numeric(12,2) USING (ROUND(paid_amount::numeric,2));

ALTER TABLE employees
    ALTER COLUMN basic_salary TYPE numeric(12,2) USING (ROUND(basic_salary::numeric,2)),
    ALTER COLUMN allowances TYPE numeric(12,2) USING (ROUND(allowances::numeric,2)),
    ALTER COLUMN deductions TYPE numeric(12,2) USING (ROUND(deductions::numeric,2)),
    ALTER COLUMN social_insurance TYPE numeric(12,2) USING (ROUND(social_insurance::numeric,2));

ALTER TABLE salary_records
    ALTER COLUMN basic_salary TYPE numeric(12,2) USING (ROUND(basic_salary::numeric,2)),
    ALTER COLUMN allowances TYPE numeric(12,2) USING (ROUND(allowances::numeric,2)),
    ALTER COLUMN overtime TYPE numeric(12,2) USING (ROUND(overtime::numeric,2)),
    ALTER COLUMN bonus TYPE numeric(12,2) USING (ROUND(bonus::numeric,2)),
    ALTER COLUMN gross_salary TYPE numeric(12,2) USING (ROUND(gross_salary::numeric,2)),
    ALTER COLUMN social_insurance TYPE numeric(12,2) USING (ROUND(social_insurance::numeric,2)),
    ALTER COLUMN tax TYPE numeric(12,2) USING (ROUND(tax::numeric,2)),
    ALTER COLUMN other_deductions TYPE numeric(12,2) USING (ROUND(other_deductions::numeric,2)),
    ALTER COLUMN total_deductions TYPE numeric(12,2) USING (ROUND(total_deductions::numeric,2)),
    ALTER COLUMN net_salary TYPE numeric(12,2) USING (ROUND(net_salary::numeric,2));

ALTER TABLE budgets
    ALTER COLUMN budgeted_amount TYPE numeric(12,2) USING (ROUND(budgeted_amount::numeric,2));

ALTER TABLE cost_centers
    ALTER COLUMN budget_limit TYPE numeric(12,2) USING (ROUND(budget_limit::numeric,2));

ALTER TABLE cost_history
    ALTER COLUMN purchase_price TYPE numeric(12,2) USING (ROUND(purchase_price::numeric,2)),
    ALTER COLUMN total_cost TYPE numeric(12,2) USING (ROUND(total_cost::numeric,2));

ALTER TABLE depreciation_records
    ALTER COLUMN depreciation_amount TYPE numeric(12,2) USING (ROUND(depreciation_amount::numeric,2));

ALTER TABLE fixed_assets
    ALTER COLUMN purchase_cost TYPE numeric(12,2) USING (ROUND(purchase_cost::numeric,2)),
    ALTER COLUMN salvage_value TYPE numeric(12,2) USING (ROUND(salvage_value::numeric,2)),
    ALTER COLUMN accumulated_depreciation TYPE numeric(12,2) USING (ROUND(accumulated_depreciation::numeric,2)),
    ALTER COLUMN net_book_value TYPE numeric(12,2) USING (ROUND(net_book_value::numeric,2));

ALTER TABLE depreciation_records
    ALTER COLUMN depreciation_amount TYPE numeric(12,2) USING (ROUND(depreciation_amount::numeric,2)),
    ALTER COLUMN accumulated_depreciation TYPE numeric(12,2) USING (ROUND(accumulated_depreciation::numeric,2)),
    ALTER COLUMN net_book_value TYPE numeric(12,2) USING (ROUND(net_book_value::numeric,2));

ALTER TABLE cost_center_transactions
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2));

-- --- Rates / Percents (12,6) ---
ALTER TABLE invoices
    ALTER COLUMN discount_percentage TYPE numeric(12,6) USING (ROUND(discount_percentage::numeric,6)),
    ALTER COLUMN tax_rate TYPE numeric(12,6) USING (ROUND(tax_rate::numeric,6));

ALTER TABLE currencies
    ALTER COLUMN exchange_rate TYPE numeric(12,6) USING (ROUND(exchange_rate::numeric,6));

ALTER TABLE employees
    ALTER COLUMN tax_rate TYPE numeric(12,6) USING (ROUND(tax_rate::numeric,6));

ALTER TABLE loans
    ALTER COLUMN interest_rate TYPE numeric(12,6) USING (ROUND(interest_rate::numeric,6));

ALTER TABLE cost_allocations
    ALTER COLUMN allocation_percentage TYPE numeric(12,6) USING (ROUND(allocation_percentage::numeric,6));

ALTER TABLE recurring_invoice_templates
    ALTER COLUMN discount_percentage TYPE numeric(12,6) USING (ROUND(discount_percentage::numeric,6)),
    ALTER COLUMN tax_rate TYPE numeric(12,6) USING (ROUND(tax_rate::numeric,6));

ALTER TABLE warehouse_transfer_lines
    ALTER COLUMN requested_quantity TYPE numeric(14,4) USING (ROUND(requested_quantity::numeric,4)),
    ALTER COLUMN shipped_quantity TYPE numeric(14,4) USING (ROUND(shipped_quantity::numeric,4)),
    ALTER COLUMN received_quantity TYPE numeric(14,4) USING (ROUND(received_quantity::numeric,4)),
    ALTER COLUMN damaged_quantity TYPE numeric(14,4) USING (ROUND(damaged_quantity::numeric,4)),
    ALTER COLUMN missing_quantity TYPE numeric(14,4) USING (ROUND(missing_quantity::numeric,4));

ALTER TABLE kit_component_history
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4)),
    ALTER COLUMN old_quantity TYPE numeric(14,4) USING (ROUND(old_quantity::numeric,4)),
    ALTER COLUMN new_quantity TYPE numeric(14,4) USING (ROUND(new_quantity::numeric,4));

-- --- Quantities (14,4) ---
ALTER TABLE invoice_lines
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4));

ALTER TABLE inventory_movements
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4));

ALTER TABLE stock_levels
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4));

ALTER TABLE items
    ALTER COLUMN min_stock TYPE numeric(14,4) USING (ROUND(min_stock::numeric,4)),
    ALTER COLUMN total_purchased_qty TYPE numeric(14,4) USING (ROUND(total_purchased_qty::numeric,4));

ALTER TABLE kit_components
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4));

ALTER TABLE warehouse_transfers
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4)),
    ALTER COLUMN shipped_quantity TYPE numeric(14,4) USING (ROUND(shipped_quantity::numeric,4)),
    ALTER COLUMN received_quantity TYPE numeric(14,4) USING (ROUND(received_quantity::numeric,4));

ALTER TABLE stock_counts
    ALTER COLUMN system_quantity TYPE numeric(14,4) USING (ROUND(system_quantity::numeric,4)),
    ALTER COLUMN counted_quantity TYPE numeric(14,4) USING (ROUND(counted_quantity::numeric,4)),
    ALTER COLUMN difference TYPE numeric(14,4) USING (ROUND(difference::numeric,4));

ALTER TABLE cost_history
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4));

ALTER TABLE recurring_invoice_lines
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4)),
    ALTER COLUMN price TYPE numeric(12,2) USING (ROUND(price::numeric,2));

COMMIT;

-- Post-check queries (run separately to verify):
-- SELECT table_name, column_name, data_type, numeric_precision, numeric_scale
-- FROM information_schema.columns
-- WHERE table_schema='public' AND data_type='double precision'
-- ORDER BY table_name, column_name;
-- Expected: only non-financial double columns remain (e.g. match_confidence, confidence_score).
