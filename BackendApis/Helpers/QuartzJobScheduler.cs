using Quartz;

namespace BackendApis.Helpers;

public static class QuartzJobScheduler
{
    public static void AddQuartzJobs(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            var jobKey = new JobKey("SampleJob");

            q.AddJob<SampleJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("SampleJob-trigger")
                .WithCronSchedule("0 * * ? * *")); // Every minute at 0th second
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}
