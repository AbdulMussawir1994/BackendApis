using DAL.RepositoryLayer.IRepositories;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace BackendApis.Controllers;

[Route("api/[controller]")]
[ApiController]
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

    [HttpPost("fire-and-forget")]
    public IActionResult FireAndForgetJob()
    {
        _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Fire-and-forget job executed"));
        _logger.LogInformation("Scheduled: Fire-and-forget");
        return Ok("Job scheduled.");
    }

    [HttpPost("delayed")]
    public IActionResult DelayedJob()
    {
        _backgroundJobClient.Schedule(() => _jobService.ProcessJobAsync("Delayed job executed"), TimeSpan.FromMinutes(1));
        _logger.LogInformation("Scheduled: Delayed job");
        return Ok("Delayed job scheduled.");
    }

    [HttpPost("recurring")]
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
    public IActionResult DeleteRecurringJob(string jobId)
    {
        _recurringJobManager.RemoveIfExists(jobId);
        _logger.LogInformation($"Removed: Recurring job {jobId}");
        return Ok($"Recurring job '{jobId}' removed.");
    }

    [HttpPost("continuation")]
    public IActionResult ContinuationJob()
    {
        var parentJobId = _backgroundJobClient.Enqueue(() => _jobService.ProcessJobAsync("Parent job executed"));
        _backgroundJobClient.ContinueWith(parentJobId, () => _jobService.ProcessJobAsync("Continuation job executed"));
        _logger.LogInformation("Scheduled: Continuation job");
        return Ok("Continuation job scheduled.");
    }

    // 🆕 Monitor failed jobs
    [HttpGet("stats")]
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
