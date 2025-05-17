using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Utilities;

namespace DAL.RepositoryLayer.Repositories
{
    public class NotificationService : INotificationService
    {
        private readonly ConfigHandler _configHandler;
        private readonly INotificationDbAccess _notificationDbAccess;

        public NotificationService(ConfigHandler configHandler, INotificationDbAccess notificationDbAccess)
        {
            _configHandler = configHandler;
            _notificationDbAccess = notificationDbAccess;
        }

        public async Task<MobileResponse<bool>> CreateNotification(CreateNotificationViewModel model)
        {
            var response = new MobileResponse<bool>(_configHandler, "notification");

            // Basic manual validation
            if (model == null)
            {
                return response.SetError("ERR-400", "Request model cannot be null", false);
            }

            if (string.IsNullOrWhiteSpace(model.UserId))
            {
                return response.SetError("ERR-400", "Either UserId must be provided", false);
            }

            var result = await _notificationDbAccess.CreateNotificationAsync(model);

            return result
                ? response.SetSuccess("SUCCESS-200", "Notification created successfully.", true)
                : response.SetError("ERR-500", "Failed to create notification.", false);
        }
    }
}
