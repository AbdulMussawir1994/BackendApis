namespace DAL.DatabaseLayer.Models;

public partial class UserNotification
{
    public long Id { get; set; }
    public long? NotificationId { get; set; }
    public string UserId { get; set; }
    public DateTime? NotificationTime { get; set; }
    public bool? IsRead { get; set; }
    public bool? IsActive { get; set; }
    public string DeviceId { get; set; }
    public string DeviceToken { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string DeletedBy { get; set; }
    public int? ReadCount { get; set; }

    public virtual MasterNotification Notification { get; set; }
}
