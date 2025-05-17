using DAL.RepositoryLayer.IRepositories;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers;

[ApiController]
//[Authorize]
[AllowAnonymous]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
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
    }

    [HttpPost("schedule-notifications")]
    public IActionResult ScheduleNotificationJob()
    {
        // Schedule to run every minute
        _recurringJobManager.AddOrUpdate(
            "minutely-notification-job",
            () => _jobService.CreateNotificationsAutomatically(),
            Cron.Minutely);

        _logger.LogInformation("Scheduled notification job to run every minute");
        return Ok("Notification job scheduled to run every minute");
    }

    [HttpDelete("stop-notifications")]
    public IActionResult StopNotificationJob()
    {
        _recurringJobManager.RemoveIfExists("minutely-notification-job");
        _logger.LogInformation("Stopped minutely notification job");
        return Ok("Stopped minutely notification job");
    }

    [HttpPost("fire-and-forget")]
    //Schedules a background job to execute immediately.
    //To offload tasks that don't need to block the main thread, such as sending emails or logging.
    public IActionResult FireAndForgetJob()
    {
        _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Fire-and-forget job executed"));
        _logger.LogInformation("Scheduled: Fire-and-forget");
        return Ok("Job scheduled.");
    }

    [HttpPost("delayed")]
    //Schedules a job to run after a specified delay (1 minute in this case).
    //For tasks that should occur after a certain period, like sending follow-up emails.
    public IActionResult DelayedJob()
    {
        _backgroundJobClient.Schedule(() => _jobService.ProcessJobAsync("Delayed job executed"), TimeSpan.FromMinutes(1));
        _logger.LogInformation("Scheduled: Delayed job");
        return Ok("Delayed job scheduled.");
    }

    [HttpPost("recurring")]
    //Schedules a job to run daily at a specified time
    //For tasks that need to run on a regular schedule, such as daily reports
    public IActionResult RecurringJob()
    {
        _recurringJobManager.AddOrUpdate(
            nameof(RecurringJob),
            () => _jobService.ProcessJobAsync("Daily recurring job executed"),
            Cron.Daily);
        _logger.LogInformation("Scheduled: Daily recurring job");
        return Ok("Recurring job scheduled.");
    }

    [HttpDelete("recurring/{jobId}")]
    //Removes a scheduled recurring job by its identifier.
    //To stop a recurring task when it's no longer needed.
    public IActionResult DeleteRecurringJob(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
        _logger.LogInformation($"Removed: Recurring job {jobId}");
        return Ok($"Recurring job '{jobId}' removed.");
    }

    [HttpPost("continuation")]
    //Schedules a job to run after the completion of another job
    //To ensure tasks are executed in a specific sequence, where one depends on the completion of another.
    public IActionResult ContinuationJob()
    {
        var parentJobId = _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Parent job executed"));
        _backgroundJobClient.ContinueWith(parentJobId, () => _jobService.ProcessJobAsync("Continuation job executed"));
        _logger.LogInformation("Scheduled: Continuation job");
        return Ok("Continuation job scheduled.");
    }

    // 🆕 Monitor failed jobs
    [HttpGet("stats")]
    //Retrieves statistics about the background jobs, such as counts of enqueued, processing, succeeded, failed, and other job states.
    //To monitor the health and performance of background job processing.
    public IActionResult GetJobStats()
    {
        var stats = JobStorage.Current.GetMonitoringApi().GetStatistics();

        return Ok(new
        {
            stats.Enqueued,
            stats.Processing,
            stats.Succeeded,
            stats.Failed,
            stats.Deleted,
            stats.Scheduled,
            stats.Recurring,
            stats.Servers
        });
    }
}
