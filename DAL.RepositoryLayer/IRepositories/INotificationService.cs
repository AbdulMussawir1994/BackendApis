using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.ServiceLayer.Models;

namespace DAL.RepositoryLayer.IRepositories
{
    public interface INotificationService
    {
        Task<MobileResponse<bool>> CreateNotification(CreateNotificationViewModel model);
    }
}
