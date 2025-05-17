using DAL.DatabaseLayer.ViewModels.NotificationModel;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface INotificationDbAccess
    {
        public Task<bool> CreateNotificationAsync(CreateNotificationViewModel model);
    }
}
