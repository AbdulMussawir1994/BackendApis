using DAL.DatabaseLayer.ViewModels.NotificationModel;
using DAL.RepositoryLayer.IRepositories;
using DAL.ServiceLayer.Helpers;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DAL.RepositoryLayer.Repositories
{
    public class JobService : IJobService
    {
        private readonly ILogger<JobService> _logger;
        private readonly INotificationService _notificationService;

        public JobService(ILogger<JobService> logger, INotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }

        [AutomaticRetry(Attempts = 2)] // Limit retries
        public async Task ProcessJobAsync(string message)
        {
            try
            {
                _logger.LogInformation($"{message} at {DateTime.UtcNow:O}");
                await Task.Delay(300); // Simulate real job
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing job: {message}");
                throw; // Important for Hangfire retry logic
            }
        }

        public async Task CreateNotificationsAutomatically()
        {
            _logger.LogInformation("Starting automatic notification creation...");

            // Create your notification model
            var model = new CreateNotificationViewModel
            {

                NotificationTime = DateTime.UtcNow,
                IsActive = true,
                IsRead = false,
                DeviceId = RandomStringGenerator.GenerateDeviceId(),
                DeviceToken = RandomStringGenerator.GenerateDeviceToken(),
                NotificationId = 1,
                ReadCount = 1,
                UserId = "2fa3cdc7-5625-47bd-9d76-4b934cc81c81",
            };

            // Call your notification service
            var result = await _notificationService.CreateNotification(model);

            if (!result.Status.IsSuccess)
            {
                _logger.LogError($"Failed to create notification: {result.Status.StatusMessage}");
            }
            else
            {
                _logger.LogInformation("Successfully created automatic notification");
            }
        }
    }
}
