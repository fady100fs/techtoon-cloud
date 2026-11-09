using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Techtoon.Core.Models;

[Table("employees")]
public class Employee
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("employee_code")]
    [StringLength(20)]
    public string EmployeeCode { get; set; } = string.Empty;

    [Column("full_name")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("job_title")]
    [StringLength(100)]
    public string JobTitle { get; set; } = string.Empty;

    [Column("hire_date")]
    public DateTime HireDate { get; set; }

    [Column("basic_salary")]
    public decimal BasicSalary { get; set; }

    [Column("allowances")]
    public decimal Allowances { get; set; }

    [Column("deductions")]
    public decimal Deductions { get; set; }

    [Column("social_insurance")]
    public decimal SocialInsurance { get; set; }

    [Column("tax_rate")]
    public decimal TaxRate { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "ACTIVE";

    public ICollection<SalaryRecord> SalaryRecords { get; set; } = new List<SalaryRecord>();
}
