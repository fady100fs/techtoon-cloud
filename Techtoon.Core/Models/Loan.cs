using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loans")]
public class Loan
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_number")]
    [StringLength(50)]
    public string LoanNumber { get; set; } = string.Empty;

    [Column("loan_type")]
    [StringLength(50)]
    public string LoanType { get; set; } = "RECEIVED";

    [Column("borrower_name")]
    [StringLength(100)]
    public string BorrowerName { get; set; } = string.Empty;

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_rate")]
    public double InterestRate { get; set; }

    [Column("loan_date")]
    public DateTime LoanDate { get; set; }

    [Column("maturity_date")]
    public DateTime MaturityDate { get; set; }

    [Column("term_months")]
    public int TermMonths { get; set; }

    [Column("installment_amount")]
    public double InstallmentAmount { get; set; }

    [Column("total_interest")]
    public double TotalInterest { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("remaining_amount")]
    public double RemainingAmount { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<LoanInstallment> Installments { get; set; } = new List<LoanInstallment>();
}
