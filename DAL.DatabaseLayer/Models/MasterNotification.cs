namespace DAL.DatabaseLayer.Models;

public partial class MasterNotification
{
    public MasterNotification()
    {
        UserNotifications = new HashSet<UserNotification>();
    }

    public long Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string Link { get; set; }
    public string ImageBase64 { get; set; }
    public string NotificationType { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string ModifiedBy { get; set; }
    public string AppNavigation { get; set; }
    public string ButtonText { get; set; }
    public string PlatformType { get; set; }
    public int? DisplayCount { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }


    public string TitleAr { get; set; }
    public string ButtonTextAr { get; set; }
    public string BodyAr { get; set; }
    public virtual ICollection<UserNotification> UserNotifications { get; set; }
}