using BackendApis.Utilities;
using Cryptography.Utilities;
using DAL.DatabaseLayer.DataContext;
using DAL.ServiceLayer.Helpers;
using DAL.ServiceLayer.Middleware;
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

    //    builder.Services.AddDbContextPool<InterceptorDbContext>(options =>
    //options
    //    .UseSqlServer(
    //        new AesGcmEncryption(builder.Configuration).Decrypt(
    //            builder.Configuration.GetConnectionString("InterceptorConnection")
    //        ),
    //        sqlOptions => sqlOptions.CommandTimeout((int)TimeSpan.FromMinutes(1).TotalSeconds)
    //    )
    //    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
    //);

    // 🔹 Redis Caching
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration["RedisConnection:LocalHost"];
        options.InstanceName = configuration["RedisConnection:InstanceName"];
    });

    // Load configuration from appsettings.json
    builder.Services.AddReverseProxy()
        .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

    // 🔹 Register App Services
    builder.Services.RegisterServices(configuration);
    builder.Services.AddEnterpriseIdentity(builder.Configuration);
    //builder.Services.AddTransient<JwtDecryptedMiddleware>();

    var app = builder.Build();

    //// 1. Swagger Configuration (should come first in dev/test)
    app.ConfigureSwagger();       // Sets up Swagger services
    app.MapOpenApi();             // Maps OpenAPI documents

    //// 2. Logging — Capture full pipeline including auth and middleware
    app.UseSerilogRequestLogging();

    //// 3. HTTPS Redirect & Static files (before routing)
    app.UseHttpsRedirection();
    //app.UseStaticFiles();

    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            ctx.Context.Response.Headers.Append("Cache-Control", $"public, max-age={60 * 60 * 24}"); // 1 day
        }
    });

    //// 4. Routing
    app.UseRouting();

    //// 5. CORS Policy (before auth, especially if using tokens)
    app.UseCors("CorsPolicy");

    //// 6. Rate Limiting — must be before authentication if scoped per user
    app.UseRateLimiter();

    //// 7. Global Exception Handling / Custom middleware (e.g., exception handling, request context)
    app.UseEnterpriseCustomMiddleware();
    //app.UseMiddleware<RateLimitingMiddleware>();

    //// 8. Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    //// 9. Response Caching
    app.UseResponseCaching();

    //// 10. Hangfire Dashboard (only if needed)
    app.UseHangfireDashboard("/hangfire");

    //// 11. Endpoint Mapping
    app.MapControllers();
    app.MapHangfireDashboard();

    //// 12. Run the app
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
