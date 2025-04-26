using BackendApis.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace BackendApis.Controllers
{
    [ApiController]
    //[Authorize]
    [AllowAnonymous]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class QuartzController : ControllerBase
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public QuartzController(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerSampleJob()
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var job = JobBuilder.Create<SampleJob>()
                .WithIdentity("SampleJob-Manual")
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity("SampleJob-Manual-Trigger")
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);

            return Ok("SampleJob triggered manually.");
        }
    }
}
