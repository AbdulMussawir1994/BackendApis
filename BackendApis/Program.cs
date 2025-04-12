using BackendApis.Utilities;
using Cryptography.Utilities;
using DAL.DatabaseLayer.DataContext;
using DAL.ServiceLayer.Helpers;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using Serilog;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("Initializing Application...");

try
{
    Log.Information("Application starting...");

    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // 🔹 Configure Serilog
    builder.Host.SerilogConfiguration();


    //Code for Migration--->

    //   var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //   var LogsString = builder.Configuration.GetConnectionString("LogsConnection");

    //   builder.Services.AddDbContext<WebContext>(options =>
    //       options.UseSqlServer(connectionString, sqlOptions =>
    //       {
    //           sqlOptions.EnableRetryOnFailure(
    //               maxRetryCount: 5,
    //               maxRetryDelay: TimeSpan.FromSeconds(10),
    //               errorNumbersToAdd: null);
    //       }));

    //   builder.Services.AddDbContext<LogsContext>(options =>
    //options.UseSqlServer(LogsString, sqlOptions =>
    //{
    //    sqlOptions.EnableRetryOnFailure(
    //        maxRetryCount: 5,
    //        maxRetryDelay: TimeSpan.FromSeconds(10),
    //        errorNumbersToAdd: null);
    //}));

    builder.Services.AddDbContextPool<WebContextDb>(options =>
    options
        .UseSqlServer(
            new AesGcmEncryption(builder.Configuration).Decrypt(
                builder.Configuration.GetConnectionString("DefaultConnection")
            ),
            sqlOptions => sqlOptions.CommandTimeout((int)TimeSpan.FromMinutes(1).TotalSeconds)
        )
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    );

    // 🔹 Redis Caching
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration["RedisConnection:LocalHost"];
        options.InstanceName = configuration["RedisConnection:InstanceName"];
    });

    // 🔹 Register App Services
    builder.Services.RegisterServices(configuration);
    builder.Services.AddEnterpriseIdentity(builder.Configuration);
    builder.Services.AddTransient<DecryptedJwtMiddleware>();

    var app = builder.Build();

    ////Configure Swagger
    app.ConfigureSwagger();

    app.MapOpenApi();

    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors("CorsPolicy");

    app.UseRateLimiter(); // 👈 Apply rate limiter globally

    app.UseMiddleware<DecryptedJwtMiddleware>();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseResponseCaching();
    app.UseHangfireDashboard("/hangfire");

    app.MapControllers();
    app.MapHangfireDashboard();


    app.Run();

}
catch (Exception ex)
{
    logger.Error(ex, "Application startup failed.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
