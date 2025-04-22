using DAL.RepositoryLayer.IRepositories;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace DAL.RepositoryLayer.Repositories
{
    public class JobService : IJobService
    {
        private readonly ILogger<JobService> _logger;

        public JobService(ILogger<JobService> logger)
        {
            _logger = logger;
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
    }
}
