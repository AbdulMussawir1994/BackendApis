using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {

        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        //private readonly IEmployeeLayer _employeeLayer;
        private readonly IJobService _jobService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            IJobService jobService,
            ILogger<JobsController> logger)
        {
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
            _jobService = jobService;
            _logger = logger;
            //     _employeeLayer = employeeLayer;
        }

        /// <summary>
        /// Schedule automatic employee addition every 1 minute
        /// </summary>
        //[HttpPost("schedule-employee-job")]
        //public IActionResult ScheduleEmployeeJob()
        //{
        //    _recurringJobManager.AddOrUpdate("auto-add-employee", () => _employeeLayer.AddEmployeeAsync(),
        //        "* * * * *" // Runs every 1 minute
        //    );

        //    _logger.LogInformation("Recurring job scheduled to add an employee every 1 minute.");
        //    return Ok("Employee addition job scheduled.");
        //}

        /// <summary>
        /// Schedules a fire-and-forget job.
        /// </summary>
        [HttpPost("fire-and-forget")]
        public IActionResult FireAndForgetJob()
        {
            _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Fire-and-forget job executed"));
            _logger.LogInformation("Fire-and-forget job scheduled.");
            return Ok("Fire-and-forget job scheduled.");
        }

        /// <summary>
        /// Schedules a delayed job to execute after 1 minute.
        /// </summary>
        [HttpPost("delayed")]
        public IActionResult DelayedJob()
        {
            _backgroundJobClient.Schedule(() => _jobService.ProcessJobAsync("Delayed job executed"), TimeSpan.FromMinutes(1));
            _logger.LogInformation("Delayed job scheduled.");
            return Ok("Delayed job scheduled.");
        }

        /// <summary>
        /// Schedules a recurring job that runs daily.
        /// </summary>
        [HttpPost("recurring")]
        public IActionResult RecurringJob()
        {
            _recurringJobManager.AddOrUpdate(
                "daily-recurring-job",
                () => _jobService.ProcessJobAsync("Recurring job executed"),
                Cron.Daily); // Runs every day at midnight
            _logger.LogInformation("Recurring job scheduled.");
            return Ok("Recurring job scheduled.");
        }

        /// <summary>
        /// Removes a scheduled recurring job.
        /// </summary>
        [HttpDelete("recurring/{jobId}")]
        public IActionResult DeleteRecurringJob(string jobId)
        {
            _recurringJobManager.RemoveIfExists(jobId);
            _logger.LogInformation($"Recurring job '{jobId}' removed.");
            return Ok($"Recurring job '{jobId}' removed.");
        }

        /// <summary>
        /// Schedules a continuation job that runs after the parent job.
        /// </summary>
        [HttpPost("continuation")]
        public IActionResult ContinuationJob()
        {
            var parentJobId = _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Parent job executed"));
            _backgroundJobClient.ContinueWith(parentJobId, () => _jobService.ProcessJobAsync("Continuation job executed"));
            _logger.LogInformation("Continuation job scheduled.");
            return Ok("Continuation job scheduled.");
        }
    }

    /// <summary>
    /// Interface for job processing service.
    /// </summary>
    public interface IJobService
    {
        Task ProcessJobAsync(string message);
    }

    /// <summary>
    /// Implementation of job processing service.
    /// </summary>
    public class JobService : IJobService
    {
        private readonly ILogger<JobService> _logger;

        public JobService(ILogger<JobService> logger)
        {
            _logger = logger;
        }

        public async Task ProcessJobAsync(string message)
        {
            _logger.LogInformation($"{message} at {DateTime.Now}");
            await Task.Delay(500); // Simulate processing time
        }
    }
}
