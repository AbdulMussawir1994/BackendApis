using DAL.DatabaseLayer.DataContext;
using DAL.DatabaseLayer.Models;
using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.RepositoryLayer.IDataAccess;
using DAL.ServiceLayer.Models;
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

    public async Task<MobileResponse<bool>> CreateNotificationAsync(CreateNotificationViewModel model)
    {
        var response = new MobileResponse<bool>(_configHandler, "EmployeeDbAccess");

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

        // High-performance async add
        await _context.UserNotifications.AddAsync(notification);

        // Optimized save (only if needed)
        var affectedRows = await _context.SaveChangesAsync();

        return affectedRows > 0
            ? response.SetSuccess("SUCCESS-200", "Notification created successfully", true)
            : response.SetError("ERR-500", "No records were created", false);
    }

}
