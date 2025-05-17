namespace DAL.DatabaseLayer.ViewModels.NotificationModel;

public class CreateNotificationViewModel
{
    public long? NotificationId { get; set; }
    public string UserId { get; set; }
    public string DeviceId { get; set; }
    public string DeviceToken { get; set; }
    public DateTime NotificationTime { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public int ReadCount { get; set; } = 0;
}

