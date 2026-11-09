-- ============================================================================
-- P0-3 Part 2 (prod-only columns discovered 2026-11-09 after first prod apply)
-- ============================================================================
-- Test DB lacked these columns (prod schema is larger). Idempotent: only
-- alters columns still double precision. Run AFTER Part 1.
-- Money -> numeric(12,2), Quantity -> numeric(14,4), Rate -> numeric(12,6).
BEGIN;

ALTER TABLE cash_boxes
    ALTER COLUMN opening_balance TYPE numeric(12,2) USING (ROUND(opening_balance::numeric,2));

ALTER TABLE parties
    ALTER COLUMN opening_balance TYPE numeric(12,2) USING (ROUND(opening_balance::numeric,2));

ALTER TABLE items
    ALTER COLUMN opening_quantity TYPE numeric(14,4) USING (ROUND(opening_quantity::numeric,4));

ALTER TABLE inventory_movements
    ALTER COLUMN before_quantity TYPE numeric(14,4) USING (ROUND(before_quantity::numeric,4)),
    ALTER COLUMN after_quantity TYPE numeric(14,4) USING (ROUND(after_quantity::numeric,4)),
    ALTER COLUMN unit_cost TYPE numeric(12,2) USING (ROUND(unit_cost::numeric,2)),
    ALTER COLUMN total_value TYPE numeric(12,2) USING (ROUND(total_value::numeric,2));

ALTER TABLE damaged_items
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4)),
    ALTER COLUMN unit_cost TYPE numeric(12,2) USING (ROUND(unit_cost::numeric,2)),
    ALTER COLUMN total_value TYPE numeric(12,2) USING (ROUND(total_value::numeric,2));

ALTER TABLE loan_installments
    ALTER COLUMN fees_amount TYPE numeric(12,2) USING (ROUND(fees_amount::numeric,2)),
    ALTER COLUMN penalty_amount TYPE numeric(12,2) USING (ROUND(penalty_amount::numeric,2)),
    ALTER COLUMN remaining_balance TYPE numeric(12,2) USING (ROUND(remaining_balance::numeric,2));

ALTER TABLE loan_rescheduling_history
    ALTER COLUMN fees_amount TYPE numeric(12,2) USING (ROUND(fees_amount::numeric,2)),
    ALTER COLUMN old_interest_rate TYPE numeric(12,6) USING (ROUND(old_interest_rate::numeric,6)),
    ALTER COLUMN new_interest_rate TYPE numeric(12,6) USING (ROUND(new_interest_rate::numeric,6));

ALTER TABLE loans
    ALTER COLUMN admin_fees TYPE numeric(12,2) USING (ROUND(admin_fees::numeric,2)),
    ALTER COLUMN credit_limit TYPE numeric(12,2) USING (ROUND(credit_limit::numeric,2)),
    ALTER COLUMN insurance_amount TYPE numeric(12,2) USING (ROUND(insurance_amount::numeric,2));

ALTER TABLE overdraft_transactions
    ALTER COLUMN amount TYPE numeric(12,2) USING (ROUND(amount::numeric,2)),
    ALTER COLUMN balance_after TYPE numeric(12,2) USING (ROUND(balance_after::numeric,2)),
    ALTER COLUMN interest_amount TYPE numeric(12,2) USING (ROUND(interest_amount::numeric,2));

ALTER TABLE shift_closings
    ALTER COLUMN opening_amount TYPE numeric(12,2) USING (ROUND(opening_amount::numeric,2)),
    ALTER COLUMN expected_closing TYPE numeric(12,2) USING (ROUND(expected_closing::numeric,2)),
    ALTER COLUMN actual_closing TYPE numeric(12,2) USING (ROUND(actual_closing::numeric,2)),
    ALTER COLUMN difference TYPE numeric(12,2) USING (ROUND(difference::numeric,2));

ALTER TABLE stock_adjustments
    ALTER COLUMN old_quantity TYPE numeric(14,4) USING (ROUND(old_quantity::numeric,4)),
    ALTER COLUMN new_quantity TYPE numeric(14,4) USING (ROUND(new_quantity::numeric,4)),
    ALTER COLUMN difference TYPE numeric(14,4) USING (ROUND(difference::numeric,4)),
    ALTER COLUMN unit_cost TYPE numeric(12,2) USING (ROUND(unit_cost::numeric,2)),
    ALTER COLUMN value_before TYPE numeric(12,2) USING (ROUND(value_before::numeric,2)),
    ALTER COLUMN value_after TYPE numeric(12,2) USING (ROUND(value_after::numeric,2));

ALTER TABLE stock_counts
    ALTER COLUMN book_quantity TYPE numeric(14,4) USING (ROUND(book_quantity::numeric,4)),
    ALTER COLUMN recount_quantity TYPE numeric(14,4) USING (ROUND(recount_quantity::numeric,4)),
    ALTER COLUMN difference_value TYPE numeric(12,2) USING (ROUND(difference_value::numeric,2));

ALTER TABLE stock_levels
    ALTER COLUMN reserved_quantity TYPE numeric(14,4) USING (ROUND(reserved_quantity::numeric,4)),
    ALTER COLUMN damaged_quantity TYPE numeric(14,4) USING (ROUND(damaged_quantity::numeric,4)),
    ALTER COLUMN under_inspection_quantity TYPE numeric(14,4) USING (ROUND(under_inspection_quantity::numeric,4));

ALTER TABLE writeoff_requests
    ALTER COLUMN quantity TYPE numeric(14,4) USING (ROUND(quantity::numeric,4)),
    ALTER COLUMN unit_cost TYPE numeric(12,2) USING (ROUND(unit_cost::numeric,2)),
    ALTER COLUMN total_value TYPE numeric(12,2) USING (ROUND(total_value::numeric,2));

-- Confidence scores 0..1 -> rate (12,6); keep as numeric so nothing stays double
ALTER TABLE bank_statement_lines
    ALTER COLUMN match_confidence TYPE numeric(12,6) USING (ROUND(match_confidence::numeric,6));

ALTER TABLE reconciliation_matches
    ALTER COLUMN confidence_score TYPE numeric(12,6) USING (ROUND(confidence_score::numeric,6));

COMMIT;
