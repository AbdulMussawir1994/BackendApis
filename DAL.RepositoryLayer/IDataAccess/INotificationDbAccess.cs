using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IDataAccess
{
    public interface INotificationDbAccess
    {
        public Task<MobileResponse<bool>> CreateNotificationAsync(CreateNotificationViewModel model);
    }
}
