using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("salary_records")]
public class SalaryRecord
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_id")]
    public int EmployeeId { get; set; }
    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("basic_salary")]
    public decimal BasicSalary { get; set; }

    [Column("allowances")]
    public decimal Allowances { get; set; }

    [Column("overtime")]
    public decimal Overtime { get; set; }

    [Column("bonus")]
    public decimal Bonus { get; set; }

    [Column("gross_salary")]
    public decimal GrossSalary { get; set; }

    [Column("social_insurance")]
    public decimal SocialInsurance { get; set; }

    [Column("tax")]
    public decimal Tax { get; set; }

    [Column("other_deductions")]
    public decimal OtherDeductions { get; set; }

    [Column("total_deductions")]
    public decimal TotalDeductions { get; set; }

    [Column("net_salary")]
    public decimal NetSalary { get; set; }

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
