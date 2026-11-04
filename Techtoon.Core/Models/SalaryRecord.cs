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
    public double BasicSalary { get; set; }

    [Column("allowances")]
    public double Allowances { get; set; }

    [Column("overtime")]
    public double Overtime { get; set; }

    [Column("bonus")]
    public double Bonus { get; set; }

    [Column("gross_salary")]
    public double GrossSalary { get; set; }

    [Column("social_insurance")]
    public double SocialInsurance { get; set; }

    [Column("tax")]
    public double Tax { get; set; }

    [Column("other_deductions")]
    public double OtherDeductions { get; set; }

    [Column("total_deductions")]
    public double TotalDeductions { get; set; }

    [Column("net_salary")]
    public double NetSalary { get; set; }

    [Column("payment_date")]
    public DateTime? PaymentDate { get; set; }

    [Column("payment_method")]
    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "pending";
}
