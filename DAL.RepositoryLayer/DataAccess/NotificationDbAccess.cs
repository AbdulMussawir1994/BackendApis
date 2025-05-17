using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.RepositoryLayer.IDataAccess;
using DAL.ServiceLayer.Utilities;
using Microsoft.Extensions.Logging;

namespace DAL.RepositoryLayer.DataAccess;

public class NotificationDbAccess : INotificationDbAccess
{
    private readonly WebContextDb _context;
    private readonly ILogger<NotificationDbAccess> _logger;
    private readonly ConfigHandler _configHandler;

    public NotificationDbAccess(WebContextDb webContextDb, ConfigHandler configHandler)
    {
        _context = webContextDb;
        _configHandler = configHandler;
    }

    public async Task<bool> CreateNotificationAsync(CreateNotificationViewModel model)
    {
        var notification = new UserNotification
        {
            NotificationId = model.NotificationId,
            UserId = model.UserId,
            DeviceId = model.DeviceId,
            DeviceToken = model.DeviceToken,
            NotificationTime = model.NotificationTime,
            IsRead = model.IsRead,
            IsActive = model.IsActive,
            ReadCount = model.ReadCount,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "Hangfire",
            DeletedBy = "N/A",
            ModifiedBy = "1",
            ModifiedDate = DateTime.UtcNow,
        };

        _context.UserNotifications.Add(notification);
        return await _context.SaveChangesAsync() > 0;
    }

}
