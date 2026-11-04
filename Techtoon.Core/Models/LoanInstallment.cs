using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("loan_installments")]
public class LoanInstallment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("loan_id")]
    public int LoanId { get; set; }
    [ForeignKey("LoanId")]
    public Loan? Loan { get; set; }

    [Column("installment_number")]
    public int InstallmentNumber { get; set; }

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("principal_amount")]
    public double PrincipalAmount { get; set; }

    [Column("interest_amount")]
    public double InterestAmount { get; set; }

    [Column("total_amount")]
    public double TotalAmount { get; set; }

    [Column("paid_amount")]
    public double PaidAmount { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
