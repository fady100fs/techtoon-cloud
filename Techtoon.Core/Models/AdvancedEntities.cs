using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

// === Migration 001: Inventory Kits ===
[Table("kit_component_history")]
public class KitComponentHistory
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("kit_item_id")] public int KitItemId { get; set; }
    [Column("component_item_id")] public int ComponentItemId { get; set; }
    [Column("quantity")] public decimal Quantity { get; set; }
    [Column("warehouse_id")] public int? WarehouseId { get; set; }
    [Column("changed_at")] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    [Column("changed_by")] public int? ChangedBy { get; set; }
    [Column("change_type")] public string ChangeType { get; set; } = "";
    [Column("old_quantity")] public decimal? OldQuantity { get; set; }
    [Column("new_quantity")] public decimal? NewQuantity { get; set; }
    [Column("old_warehouse_id")] public int? OldWarehouseId { get; set; }
    [Column("new_warehouse_id")] public int? NewWarehouseId { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("KitItemId")] public Item? KitItem { get; set; }
    [ForeignKey("ComponentItemId")] public Item? ComponentItem { get; set; }
}

// === Migration 002: Warehouse Transfers ===
[Table("warehouse_transfer_lines")]
public class WarehouseTransferLine
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("transfer_id")] public int TransferId { get; set; }
    [Column("item_id")] public int ItemId { get; set; }
    [Column("requested_quantity")] public decimal RequestedQuantity { get; set; }
    [Column("shipped_quantity")] public decimal ShippedQuantity { get; set; }
    [Column("received_quantity")] public decimal ReceivedQuantity { get; set; }
    [Column("damaged_quantity")] public decimal DamagedQuantity { get; set; }
    [Column("missing_quantity")] public decimal MissingQuantity { get; set; }
    [Column("difference_reason")] public string? DifferenceReason { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("TransferId")] public WarehouseTransfer? Transfer { get; set; }
    [ForeignKey("ItemId")] public Item? Item { get; set; }
}

[Table("warehouse_transfer_log")]
public class WarehouseTransferLog
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("transfer_id")] public int TransferId { get; set; }
    [Column("action")] public string Action { get; set; } = "";
    [Column("performed_by")] public int PerformedBy { get; set; }
    [Column("performed_at")] public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }
    [Column("old_status")] public string? OldStatus { get; set; }
    [Column("new_status")] public string? NewStatus { get; set; }
    [Column("metadata")] public string? Metadata { get; set; }
}

// === Migration 003: Cash Transfer Log ===
[Table("cash_transfer_log")]
public class CashTransferLog
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("transfer_id")] public int TransferId { get; set; }
    [Column("action")] public string Action { get; set; } = "";
    [Column("performed_by")] public int PerformedBy { get; set; }
    [Column("performed_at")] public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }
    [Column("old_status")] public string? OldStatus { get; set; }
    [Column("new_status")] public string? NewStatus { get; set; }
    [Column("metadata")] public string? Metadata { get; set; }
}

// === Migration 004: Bank Statement Import ===
[Table("bank_statements")]
public class BankStatement
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("cash_box_id")] public int CashBoxId { get; set; }
    [Column("statement_date")] public DateTime StatementDate { get; set; }
    [Column("period_start")] public DateTime PeriodStart { get; set; }
    [Column("period_end")] public DateTime PeriodEnd { get; set; }
    [Column("opening_balance")] public decimal OpeningBalance { get; set; }
    [Column("closing_balance")] public decimal ClosingBalance { get; set; }
    [Column("currency")] public string Currency { get; set; } = "EGP";
    [Column("source_file_path")] public string? SourceFilePath { get; set; }
    [Column("source_file_format")] public string? SourceFileFormat { get; set; }
    [Column("source_file_size")] public long? SourceFileSize { get; set; }
    [Column("file_hash")] public string? FileHash { get; set; }
    [Column("imported_by")] public int ImportedBy { get; set; }
    [Column("imported_at")] public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    [Column("status")] public string Status { get; set; } = "imported";
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("CashBoxId")] public CashBox? CashBox { get; set; }
}

[Table("bank_statement_lines")]
public class BankStatementLine
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("statement_id")] public int StatementId { get; set; }
    [Column("line_number")] public int LineNumber { get; set; }
    [Column("transaction_date")] public DateTime TransactionDate { get; set; }
    [Column("value_date")] public DateTime? ValueDate { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("reference")] public string? Reference { get; set; }
    [Column("debit")] public decimal Debit { get; set; }
    [Column("credit")] public decimal Credit { get; set; }
    [Column("balance")] public decimal? Balance { get; set; }
    [Column("matched_payment_id")] public int? MatchedPaymentId { get; set; }
    [Column("matched_journal_line_id")] public int? MatchedJournalLineId { get; set; }
    [Column("match_status")] public string MatchStatus { get; set; } = "unmatched";
    [Column("matched_at")] public DateTime? MatchedAt { get; set; }
    [Column("matched_by")] public int? MatchedBy { get; set; }
    [Column("match_confidence")] public decimal? MatchConfidence { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("StatementId")] public BankStatement? Statement { get; set; }
}

[Table("bank_statement_import_templates")]
public class BankStatementImportTemplate
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("bank_name")] public string? BankName { get; set; }
    [Column("date_column")] public string DateColumn { get; set; } = "date";
    [Column("value_date_column")] public string? ValueDateColumn { get; set; }
    [Column("description_column")] public string DescriptionColumn { get; set; } = "description";
    [Column("reference_column")] public string ReferenceColumn { get; set; } = "reference";
    [Column("debit_column")] public string DebitColumn { get; set; } = "debit";
    [Column("credit_column")] public string CreditColumn { get; set; } = "credit";
    [Column("amount_column")] public string? AmountColumn { get; set; }
    [Column("balance_column")] public string BalanceColumn { get; set; } = "balance";
    [Column("date_format")] public string DateFormat { get; set; } = "yyyy-MM-dd";
    [Column("decimal_separator")] public string DecimalSeparator { get; set; } = ".";
    [Column("thousand_separator")] public string ThousandSeparator { get; set; } = ",";
    [Column("encoding")] public string Encoding { get; set; } = "utf-8";
    [Column("skip_rows")] public int SkipRows { get; set; }
    [Column("has_header")] public bool HasHeader { get; set; } = true;
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
}

[Table("bank_statement_import_logs")]
public class BankStatementImportLog
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("statement_id")] public int? StatementId { get; set; }
    [Column("template_id")] public int? TemplateId { get; set; }
    [Column("started_at")] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    [Column("completed_at")] public DateTime? CompletedAt { get; set; }
    [Column("status")] public string Status { get; set; } = "";
    [Column("total_rows")] public int TotalRows { get; set; }
    [Column("imported_rows")] public int ImportedRows { get; set; }
    [Column("skipped_rows")] public int SkippedRows { get; set; }
    [Column("error_rows")] public int ErrorRows { get; set; }
    [Column("duplicate_rows")] public int DuplicateRows { get; set; }
    [Column("error_details")] public string? ErrorDetails { get; set; }
    [Column("performed_by")] public int PerformedBy { get; set; }
}

// === Migration 005: Bank Reconciliation ===
[Table("bank_reconciliations")]
public class BankReconciliation
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("reconciliation_number")] public string? ReconciliationNumber { get; set; }
    [Column("cash_box_id")] public int CashBoxId { get; set; }
    [Column("period_start")] public DateTime PeriodStart { get; set; }
    [Column("period_end")] public DateTime PeriodEnd { get; set; }
    [Column("bank_statement_id")] public int? BankStatementId { get; set; }
    [Column("opening_book_balance")] public decimal OpeningBookBalance { get; set; }
    [Column("opening_bank_balance")] public decimal OpeningBankBalance { get; set; }
    [Column("closing_book_balance")] public decimal ClosingBookBalance { get; set; }
    [Column("closing_bank_balance")] public decimal ClosingBankBalance { get; set; }
    [Column("difference")] public decimal Difference { get; set; }
    [Column("status")] public string Status { get; set; } = "draft";
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("reviewed_by")] public int? ReviewedBy { get; set; }
    [Column("reviewed_at")] public DateTime? ReviewedAt { get; set; }
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("locked_at")] public DateTime? LockedAt { get; set; }
    [Column("locked_by")] public int? LockedBy { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("CashBoxId")] public CashBox? CashBox { get; set; }
}

[Table("reconciliation_matches")]
public class ReconciliationMatch
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("reconciliation_id")] public int ReconciliationId { get; set; }
    [Column("bank_statement_line_id")] public int? BankStatementLineId { get; set; }
    [Column("system_payment_id")] public int? SystemPaymentId { get; set; }
    [Column("system_journal_line_id")] public int? SystemJournalLineId { get; set; }
    [Column("match_type")] public string MatchType { get; set; } = "";
    [Column("match_criteria")] public string? MatchCriteria { get; set; }
    [Column("confidence_score")] public decimal? ConfidenceScore { get; set; }
    [Column("matched_by")] public int MatchedBy { get; set; }
    [Column("matched_at")] public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }
}

[Table("reconciliation_adjustments")]
public class ReconciliationAdjustment
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("reconciliation_id")] public int ReconciliationId { get; set; }
    [Column("adjustment_type")] public string AdjustmentType { get; set; } = "";
    [Column("amount")] public decimal Amount { get; set; }
    [Column("description")] public string Description { get; set; } = "";
    [Column("bank_statement_line_id")] public int? BankStatementLineId { get; set; }
    [Column("target_account_id")] public int? TargetAccountId { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("posted_at")] public DateTime? PostedAt { get; set; }
    [Column("posted_by")] public int? PostedBy { get; set; }
    [Column("notes")] public string? Notes { get; set; }
}

// === Migration 006: Recurring Journal Entries ===
[Table("recurring_journal_templates")]
public class RecurringJournalTemplate
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("template_number")] public string? TemplateNumber { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("description")] public string? Description { get; set; }
    [Column("frequency")] public string Frequency { get; set; } = "monthly";
    [Column("custom_days")] public int? CustomDays { get; set; }
    [Column("execution_day")] public int? ExecutionDay { get; set; }
    [Column("start_date")] public DateTime StartDate { get; set; }
    [Column("end_date")] public DateTime? EndDate { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("auto_post")] public bool AutoPost { get; set; }
    [Column("next_run_date")] public DateTime NextRunDate { get; set; }
    [Column("last_run_date")] public DateTime? LastRunDate { get; set; }
    [Column("last_run_status")] public string? LastRunStatus { get; set; }
    [Column("last_run_error")] public string? LastRunError { get; set; }
    [Column("description_template")] public string? DescriptionTemplate { get; set; }
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }

    public ICollection<RecurringJournalLine> Lines { get; set; } = new List<RecurringJournalLine>();
}

[Table("recurring_journal_lines")]
public class RecurringJournalLine
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("template_id")] public int TemplateId { get; set; }
    [Column("line_number")] public int LineNumber { get; set; }
    [Column("account_id")] public int AccountId { get; set; }
    [Column("debit")] public decimal Debit { get; set; }
    [Column("credit")] public decimal Credit { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("cost_center_id")] public int? CostCenterId { get; set; }
    [Column("amount_rule")] public string AmountRule { get; set; } = "fixed";
    [Column("rule_parameters")] public string? RuleParameters { get; set; }

    [ForeignKey("TemplateId")] public RecurringJournalTemplate? Template { get; set; }
    [ForeignKey("AccountId")] public Account? Account { get; set; }
}

[Table("recurring_journal_executions")]
public class RecurringJournalExecution
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("template_id")] public int TemplateId { get; set; }
    [Column("execution_date")] public DateTime ExecutionDate { get; set; }
    [Column("planned_run_date")] public DateTime PlannedRunDate { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("status")] public string Status { get; set; } = "";
    [Column("total_debit")] public decimal? TotalDebit { get; set; }
    [Column("total_credit")] public decimal? TotalCredit { get; set; }
    [Column("error_message")] public string? ErrorMessage { get; set; }
    [Column("executed_at")] public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    [Column("executed_by")] public int? ExecutedBy { get; set; }
}

// === Migration 007: Petty Cash & Custody ===
[Table("petty_cash_custodies")]
public class PettyCashCustody
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("custody_number")] public string? CustodyNumber { get; set; }
    [Column("employee_id")] public int EmployeeId { get; set; }
    [Column("amount")] public decimal Amount { get; set; }
    [Column("currency")] public string Currency { get; set; } = "EGP";
    [Column("cash_box_id")] public int CashBoxId { get; set; }
    [Column("custody_account_id")] public int CustodyAccountId { get; set; }
    [Column("issued_date")] public DateTime IssuedDate { get; set; }
    [Column("expected_settlement_date")] public DateTime? ExpectedSettlementDate { get; set; }
    [Column("status")] public string Status { get; set; } = "open";
    [Column("description")] public string? Description { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("settled_at")] public DateTime? SettledAt { get; set; }
    [Column("settled_by")] public int? SettledBy { get; set; }
    [Column("cancelled_at")] public DateTime? CancelledAt { get; set; }
    [Column("cancelled_by")] public int? CancelledBy { get; set; }

    [ForeignKey("EmployeeId")] public Employee? Employee { get; set; }
    [ForeignKey("CashBoxId")] public CashBox? CashBox { get; set; }
    public ICollection<PettyCashExpense> Expenses { get; set; } = new List<PettyCashExpense>();
}

[Table("petty_cash_expenses")]
public class PettyCashExpense
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("custody_id")] public int CustodyId { get; set; }
    [Column("expense_number")] public string? ExpenseNumber { get; set; }
    [Column("expense_date")] public DateTime ExpenseDate { get; set; }
    [Column("amount")] public decimal Amount { get; set; }
    [Column("expense_category_id")] public int? ExpenseCategoryId { get; set; }
    [Column("expense_account_id")] public int? ExpenseAccountId { get; set; }
    [Column("description")] public string Description { get; set; } = "";
    [Column("receipt_file_path")] public string? ReceiptFilePath { get; set; }
    [Column("receipt_file_type")] public string? ReceiptFileType { get; set; }
    [Column("receipt_file_size")] public long? ReceiptFileSize { get; set; }
    [Column("receipt_file_hash")] public string? ReceiptFileHash { get; set; }
    [Column("vendor_name")] public string? VendorName { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("recorded_by")] public int RecordedBy { get; set; }
    [Column("recorded_at")] public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("rejected_reason")] public string? RejectedReason { get; set; }

    [ForeignKey("CustodyId")] public PettyCashCustody? Custody { get; set; }
}

[Table("petty_cash_settlements")]
public class PettyCashSettlement
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("settlement_number")] public string? SettlementNumber { get; set; }
    [Column("custody_id")] public int CustodyId { get; set; }
    [Column("settlement_date")] public DateTime SettlementDate { get; set; }
    [Column("total_expenses")] public decimal TotalExpenses { get; set; }
    [Column("remaining_amount")] public decimal RemainingAmount { get; set; }
    [Column("returned_amount")] public decimal ReturnedAmount { get; set; }
    [Column("shortage_amount")] public decimal ShortageAmount { get; set; }
    [Column("shortage_account_id")] public int? ShortageAccountId { get; set; }
    [Column("cash_box_id")] public int CashBoxId { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("status")] public string Status { get; set; } = "draft";
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("notes")] public string? Notes { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("custody_increase_requests")]
public class CustodyIncreaseRequest
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("request_number")] public string? RequestNumber { get; set; }
    [Column("custody_id")] public int CustodyId { get; set; }
    [Column("current_amount")] public decimal CurrentAmount { get; set; }
    [Column("requested_amount")] public decimal RequestedAmount { get; set; }
    [Column("new_total_amount")] public decimal NewTotalAmount { get; set; }
    [Column("reason")] public string Reason { get; set; } = "";
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("requested_by")] public int RequestedBy { get; set; }
    [Column("requested_at")] public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    [Column("reviewed_by")] public int? ReviewedBy { get; set; }
    [Column("reviewed_at")] public DateTime? ReviewedAt { get; set; }
    [Column("review_notes")] public string? ReviewNotes { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
}

// === Migration 008: Early Payment Discount ===
[Table("early_payment_discount_policies")]
public class EarlyPaymentDiscountPolicy
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("description")] public string? Description { get; set; }
    [Column("discount_type")] public string DiscountType { get; set; } = "";
    [Column("discount_value")] public decimal DiscountValue { get; set; }
    [Column("eligibility_days")] public int EligibilityDays { get; set; }
    [Column("offer_end_days")] public int? OfferEndDays { get; set; }
    [Column("applies_to")] public string AppliesTo { get; set; } = "all";
    [Column("discount_account_id")] public int? DiscountAccountId { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("is_mandatory")] public bool IsMandatory { get; set; }
    [Column("effective_from")] public DateTime EffectiveFrom { get; set; }
    [Column("effective_to")] public DateTime? EffectiveTo { get; set; }
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }
}

[Table("party_discount_policies")]
public class PartyDiscountPolicy
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("party_id")] public int PartyId { get; set; }
    [Column("policy_id")] public int PolicyId { get; set; }
    [Column("custom_discount_value")] public decimal? CustomDiscountValue { get; set; }
    [Column("custom_eligibility_days")] public int? CustomEligibilityDays { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("assigned_by")] public int AssignedBy { get; set; }
    [Column("assigned_at")] public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }
}

[Table("invoice_discount_applications")]
public class InvoiceDiscountApplication
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("invoice_id")] public int InvoiceId { get; set; }
    [Column("payment_id")] public int? PaymentId { get; set; }
    [Column("policy_id")] public int? PolicyId { get; set; }
    [Column("party_policy_id")] public int? PartyPolicyId { get; set; }
    [Column("original_amount")] public decimal OriginalAmount { get; set; }
    [Column("days_from_invoice")] public int DaysFromInvoice { get; set; }
    [Column("discount_amount")] public decimal DiscountAmount { get; set; }
    [Column("paid_amount")] public decimal PaidAmount { get; set; }
    [Column("discount_account_id")] public int? DiscountAccountId { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("applied_at")] public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    [Column("applied_by")] public int AppliedBy { get; set; }
    [Column("notes")] public string? Notes { get; set; }
}

// === Migration 009: Late Payment Fees ===
[Table("late_fee_policies")]
public class LateFeePolicy
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("description")] public string? Description { get; set; }
    [Column("fee_type")] public string FeeType { get; set; } = "";
    [Column("fee_value")] public decimal FeeValue { get; set; }
    [Column("grace_period_days")] public int GracePeriodDays { get; set; }
    [Column("calculation_frequency")] public string CalculationFrequency { get; set; } = "one_time";
    [Column("calculation_basis")] public string CalculationBasis { get; set; } = "overdue_balance";
    [Column("applies_to")] public string AppliesTo { get; set; } = "all";
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("is_mandatory")] public bool IsMandatory { get; set; }
    [Column("fee_income_account_id")] public int? FeeIncomeAccountId { get; set; }
    [Column("effective_from")] public DateTime EffectiveFrom { get; set; }
    [Column("effective_to")] public DateTime? EffectiveTo { get; set; }
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }
}

[Table("party_late_fee_exceptions")]
public class PartyLateFeeException
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("party_id")] public int PartyId { get; set; }
    [Column("policy_id")] public int PolicyId { get; set; }
    [Column("exception_type")] public string ExceptionType { get; set; } = "";
    [Column("custom_fee_value")] public decimal? CustomFeeValue { get; set; }
    [Column("custom_grace_days")] public int? CustomGraceDays { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("reason")] public string? Reason { get; set; }
    [Column("set_by")] public int SetBy { get; set; }
    [Column("set_at")] public DateTime SetAt { get; set; } = DateTime.UtcNow;
    [Column("expires_at")] public DateTime? ExpiresAt { get; set; }
}

[Table("invoice_late_fees")]
public class InvoiceLateFee
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("invoice_id")] public int InvoiceId { get; set; }
    [Column("policy_id")] public int PolicyId { get; set; }
    [Column("fee_amount")] public decimal FeeAmount { get; set; }
    [Column("calculation_date")] public DateTime CalculationDate { get; set; }
    [Column("calculation_details")] public string? CalculationDetails { get; set; }
    [Column("days_overdue")] public int DaysOverdue { get; set; }
    [Column("overdue_balance")] public decimal OverdueBalance { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("demand_document_number")] public string? DemandDocumentNumber { get; set; }
    [Column("demand_document_date")] public DateTime? DemandDocumentDate { get; set; }
    [Column("status")] public string Status { get; set; } = "calculated";
    [Column("exemption_reason")] public string? ExemptionReason { get; set; }
    [Column("exemption_amount")] public decimal ExemptionAmount { get; set; }
    [Column("exempted_by")] public int? ExemptedBy { get; set; }
    [Column("exempted_at")] public DateTime? ExemptedAt { get; set; }
    [Column("calculation_period_start")] public DateTime CalculationPeriodStart { get; set; }
    [Column("calculation_period_end")] public DateTime CalculationPeriodEnd { get; set; }
}

// === Migration 010: Bad Debt Write-off ===
[Table("bad_debt_writeoffs")]
public class BadDebtWriteoff
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("writeoff_number")] public string? WriteoffNumber { get; set; }
    [Column("party_id")] public int PartyId { get; set; }
    [Column("invoice_id")] public int? InvoiceId { get; set; }
    [Column("original_amount")] public decimal OriginalAmount { get; set; }
    [Column("remaining_amount")] public decimal RemainingAmount { get; set; }
    [Column("writeoff_amount")] public decimal WriteoffAmount { get; set; }
    [Column("writeoff_type")] public string WriteoffType { get; set; } = "";
    [Column("reason")] public string Reason { get; set; } = "";
    [Column("evidence_file_path")] public string? EvidenceFilePath { get; set; }
    [Column("evidence_file_type")] public string? EvidenceFileType { get; set; }
    [Column("evidence_file_hash")] public string? EvidenceFileHash { get; set; }
    [Column("writeoff_date")] public DateTime WriteoffDate { get; set; }
    [Column("bad_debt_account_id")] public int BadDebtAccountId { get; set; }
    [Column("ar_account_id")] public int ArAccountId { get; set; }
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("status")] public string Status { get; set; } = "pending";
    [Column("requested_by")] public int RequestedBy { get; set; }
    [Column("requested_at")] public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    [Column("approved_by")] public int? ApprovedBy { get; set; }
    [Column("approved_at")] public DateTime? ApprovedAt { get; set; }
    [Column("posted_at")] public DateTime? PostedAt { get; set; }
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("PartyId")] public Party? Party { get; set; }
    [ForeignKey("InvoiceId")] public Invoice? Invoice { get; set; }
    public ICollection<BadDebtAttachment> Attachments { get; set; } = new List<BadDebtAttachment>();
    public ICollection<BadDebtReversal> Reversals { get; set; } = new List<BadDebtReversal>();
}

[Table("bad_debt_reversals")]
public class BadDebtReversal
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("reversal_number")] public string? ReversalNumber { get; set; }
    [Column("writeoff_id")] public int WriteoffId { get; set; }
    [Column("reversal_date")] public DateTime ReversalDate { get; set; }
    [Column("reversal_amount")] public decimal ReversalAmount { get; set; }
    [Column("reason")] public string Reason { get; set; } = "";
    [Column("journal_entry_id")] public int? JournalEntryId { get; set; }
    [Column("reversed_by")] public int ReversedBy { get; set; }
    [Column("reversed_at")] public DateTime ReversedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }

    [ForeignKey("WriteoffId")] public BadDebtWriteoff? Writeoff { get; set; }
}

[Table("bad_debt_attachments")]
public class BadDebtAttachment
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("writeoff_id")] public int WriteoffId { get; set; }
    [Column("file_name")] public string FileName { get; set; } = "";
    [Column("file_path")] public string FilePath { get; set; } = "";
    [Column("file_type")] public string? FileType { get; set; }
    [Column("file_size")] public long? FileSize { get; set; }
    [Column("file_hash")] public string? FileHash { get; set; }
    [Column("description")] public string? Description { get; set; }
    [Column("uploaded_by")] public int UploadedBy { get; set; }
    [Column("uploaded_at")] public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("WriteoffId")] public BadDebtWriteoff? Writeoff { get; set; }
}

// === Migration 011: Reporting Settings ===
[Table("reporting_settings")]
public class ReportingSetting
{
    [Key][Column("id")] public int Id { get; set; } = 1;
    [Column("default_basis")] public string DefaultBasis { get; set; } = "accrual";
    [Column("allow_comparison")] public bool AllowComparison { get; set; } = true;
    [Column("cash_recognition_threshold_days")] public int CashRecognitionThresholdDays { get; set; }
    [Column("updated_by")] public int? UpdatedBy { get; set; }
    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

[Table("cash_recognition_log")]
public class CashRecognitionLog
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("journal_entry_id")] public int JournalEntryId { get; set; }
    [Column("recognition_date")] public DateTime RecognitionDate { get; set; }
    [Column("recognized_amount")] public decimal RecognizedAmount { get; set; }
    [Column("payment_id")] public int? PaymentId { get; set; }
    [Column("recognized_by")] public int? RecognizedBy { get; set; }
    [Column("recognized_at")] public DateTime RecognizedAt { get; set; } = DateTime.UtcNow;
    [Column("notes")] public string? Notes { get; set; }
}