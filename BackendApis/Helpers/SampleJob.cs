using Quartz;

namespace BackendApis.Helpers
{
    public class SampleJob : IJob
    {
        private readonly ILogger<SampleJob> _logger;

        public SampleJob(ILogger<SampleJob> logger)
        {
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("SampleJob executed at {Time}", DateTime.UtcNow);
            await Task.Delay(200); // Simulate some small work
        }
    }
}
