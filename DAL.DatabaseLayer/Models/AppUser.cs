using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.DatabaseLayer.Models;

[Index(nameof(CNIC), IsUnique = true)]
public class AppUser : IdentityUser
{
    public string CNIC { get; set; }
    public long? CreatedBy { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public string? CurrentGuid { get; set; }
    public string? RefreshGuid { get; set; }
    public string? DeviceId { get; set; }
    public bool? IsDeviceChanged { get; set; }
    public DateTime? LastLoginDate { get; set; }
}
