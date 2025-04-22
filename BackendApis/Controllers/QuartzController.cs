using BackendApis.Helpers;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace BackendApis.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
