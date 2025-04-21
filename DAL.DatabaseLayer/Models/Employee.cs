using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.DatabaseLayer.Models;


//[Table("Employee", Schema = "public")]
public class Employee
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EmployeeId { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(18, 65)]
    public int Age { get; set; }

    [Column(TypeName = "decimal(18,2)"), Range(0, 100_000_000)]
    public decimal Salary { get; set; }

    [MaxLength(255)]
    public string? CvUrl { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? ImageUrl { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;
    public long? CreatedBy { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ApplicationUserId))]
    public virtual AppUser ApplicationUser { get; set; } = null!;
}