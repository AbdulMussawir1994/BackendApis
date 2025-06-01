using BackendApis.Helpers;
using Cryptography.Utilities;
using DAL.RepositoryLayer.DataAccess;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.RepositoryLayer.Repositories;
using DAL.ServiceLayer.Helpers;
using DAL.ServiceLayer.Middleware;
using DAL.ServiceLayer.Utilities;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Mapster;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Quartz;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace BackendApis.Utilities;

public static class DependencyInjectionSetup
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {


        // 📦 Controllers + JSON
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = false;
            });

        //🔄 API Versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(2, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader(); // Important!
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Register FluentValidation with all validators in the current assembly
        services.AddValidatorsFromAssemblyContaining<ModelValidator>();
        services.AddFluentValidationAutoValidation();

        // 🧭 Swagger
        services.AddSwaggerGen(options =>
        {
            // JWT Bearer auth setup
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter your JWT token below with **Bearer** prefix.\r\nExample: Bearer eyJhbGciOi...",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http, // Use Http instead of ApiKey for better support
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
               {
            new OpenApiSecurityScheme
                  {
                      Reference = new OpenApiReference
                       {
                             Type = ReferenceType.SecurityScheme,
                              Id = "Bearer"
                       }
                   },
            Array.Empty<string>()
               }
            });

            // Optional: Add XML comment support for controller documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // 🔐 JWT Authentication
        services.AddAuthentication(option =>
        {
            option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var key = Encoding.ASCII.GetBytes(configuration["JWTKey:Secret"]);

            options.SaveToken = true;
            options.RequireHttpsMetadata = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = configuration["JWTKey:ValidIssuer"],
                ValidAudience = configuration["JWTKey:ValidAudience"],
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        // 🧩 Dependency Injection (DI)
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IDataBaseAccess, DataBaseAccess>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddScoped<IRoleUserService, RoleUserService>();

        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeDbAccess, EmployeeDbAccess>();
        services.AddScoped<IFilesServiceRepository, FilesServiceRepository>();
        services.AddScoped<IFileUtility, FileUtility>();
        services.AddScoped<INotificationDbAccess, NotificationDbAccess>();

        services.AddScoped<CustomUserManager>();

        // Register custom encryption utility as Singleton
        services.AddSingleton<AesGcmEncryption>();
        services.AddSingleton<ConfigHandler>();
        //   services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddleware>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, CoreAuthorizationMiddleware>();

        // Interceptors
        //  services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        // services.AddScoped<SqlCommandInterceptor>();
        //  services.AddScoped<TransactionInterceptor>();
        //   services.AddScoped<ConnectionInterceptor>();
        //  services.AddScoped<DataReaderInterceptor>();

        // 🔐 Decrypt Connection String
        var decryptedConnection = new AesGcmEncryption(configuration)
            .Decrypt(configuration.GetConnectionString("DefaultConnection"));

        // 📊 Hangfire
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseDefaultTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UseSqlServerStorage(decryptedConnection, new SqlServerStorageOptions
                  {
                      CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                      SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                      QueuePollInterval = TimeSpan.FromSeconds(15),
                      UseRecommendedIsolationLevel = true,
                      DisableGlobalLocks = true
                  });
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
        });

        services.AddHttpClient(); // Required for default HttpClient

        // 🔁 Polly Resilience & Circuit Breaker
        //services.AddHttpClient("ResilientClient", client =>
        //{
        //    client.BaseAddress = new Uri("https://example.com");
        //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //})
        //.AddPolicyHandler(PollyResilienceHelper.GetResiliencePipeline());


        //// Register HttpClient with Polly from the registry
        //services.AddHttpClient("MyPollyClient", client =>
        //{
        //    client.BaseAddress = new Uri("https://api.example.com/");
        //})
        //.AddPolicyHandler(PollyPolicyRegistry.GetRetryPolicy())
        //.AddPolicyHandler(PollyPolicyRegistry.GetCircuitBreakerPolicy());


        //services.AddResiliencePipeline("GlobalHttpPolicy", builder =>
        //{
        //    // Retry with exponential backoff
        //    builder.AddRetry(new RetryStrategyOptions
        //    {
        //        ShouldHandle = new PredicateBuilder()
        //            .Handle<HttpRequestException>()
        //            .HandleResult(response => !((HttpResponseMessage)response).IsSuccessStatusCode),
        //        BackoffType = DelayBackoffType.Exponential,
        //        MaxRetryAttempts = 3,
        //        Delay = TimeSpan.FromSeconds(2),
        //        UseJitter = true
        //    });

        //    // Circuit Breaker
        //    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        //    {
        //        ShouldHandle = new PredicateBuilder()
        //            .Handle<HttpRequestException>()
        //            .HandleResult(response => !((HttpResponseMessage)response).IsSuccessStatusCode),
        //        FailureRatio = 0.5,
        //        SamplingDuration = TimeSpan.FromSeconds(10),
        //        MinimumThroughput = 8,
        //        BreakDuration = TimeSpan.FromSeconds(30)
        //    });
        //});

        // 💥 Apply globally to all HttpClient instances
        //services.ConfigureAll<HttpClientFactoryOptions>(options =>
        //  {
        //      options.ResiliencePipelineName = "GlobalHttpPolicy"; // 👈 This now works
        //  });

        // services.Configure<RateLimitOptions>(configuration.GetSection("RateLimiting"));
        // services.AddSingleton<IRateLimiterService, RateLimiterService>();

        // 🚦 Rate Limiting
        services.AddRateLimiter(options =>
        {
            // ✅ Token-based Limiter for burst traffic control
            options.AddTokenBucketLimiter("TokenLimiter", limiterOptions =>
            {
                limiterOptions.TokenLimit = 20;
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 5;
                limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(20);
                limiterOptions.TokensPerPeriod = 10;
                limiterOptions.AutoReplenishment = true;
            });

            // ✅ Global Fixed Window Limiter - applies to all requests (anonymous/auth)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var isAuthenticated = httpContext.User.Identity?.IsAuthenticated ?? false;
                var identityKey = isAuthenticated
                    ? httpContext.User.Identity?.Name ?? $"user-{Guid.NewGuid()}"
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? $"anonymous-{Guid.NewGuid()}";

                return RateLimitPartition.GetFixedWindowLimiter(identityKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 25,
                    Window = TimeSpan.FromSeconds(30),
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "30";
                context.HttpContext.Response.Headers["X-RateLimit-Exceeded"] = "true";
                context.HttpContext.Response.ContentType = "application/json";
                var errorResponse = new
                {
                    status = new
                    {
                        Code = 429, // Status429TooManyRequests
                        IsSuccess = false,
                        StatusMessage = "Too many requests. Please try again after 30 seconds.",
                        RetryAfterSeconds = 30
                    }
                };

                await JsonSerializer.SerializeAsync(
                    context.HttpContext.Response.Body,
                    errorResponse,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
                    token).ConfigureAwait(false);

                await context.HttpContext.Response.CompleteAsync().ConfigureAwait(false);
            };
        });

        //services.AddRateLimiter(options =>
        //{
        //    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        //    {
        //        // Prefer User ID (if authenticated), fallback to IP
        //        var userId = context.User.Identity?.IsAuthenticated == true
        //                                            ? context.User.Identity.Name ?? $"user-{Guid.NewGuid()}"
        //                                            : context.Connection.RemoteIpAddress?.ToString() ?? $"anon-{Guid.NewGuid()}";

        //        return RateLimitPartition.GetFixedWindowLimiter(
        //            partitionKey: userId,
        //            factory: key => new FixedWindowRateLimiterOptions
        //            {
        //                PermitLimit = 20, // e.g., 20 requests
        //                Window = TimeSpan.FromMinutes(1),
        //                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        //                QueueLimit = 2
        //            });
        //    });

        //    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        //    options.OnRejected = async (context, token) =>
        //    {
        //        context.HttpContext.Response.Headers["Retry-After"] = "60";
        //        await context.HttpContext.Response.WriteAsync("Too many requests for this user. Try again in 60 seconds.", token);
        //    };
        //});

        ///[EnableRateLimiting("sliding")] --> use ActionMethod
        //services.AddRateLimiter(options =>
        //{
        //    options.AddSlidingWindowLimiter("sliding", sliding =>
        //    {
        //        sliding.PermitLimit = 5;
        //        sliding.Window = TimeSpan.FromSeconds(10);
        //        sliding.SegmentsPerWindow = 2;
        //        sliding.QueueLimit = 2;
        //        sliding.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        //    });
        //});

        //[Authorize]
        //[EnableRateLimiting("authenticated-policy")]
        //services.AddRateLimiter(options =>
        //{
        //    options.AddFixedWindowLimiter("authenticated-policy", opt =>
        //    {
        //        opt.PermitLimit = 20;
        //        opt.Window = TimeSpan.FromMinutes(1);
        //        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        //        opt.QueueLimit = 2;
        //    });
        //});

        // Register Rate Limiter globally
        //services.AddRateLimiter(options =>
        //{
        //    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        //    {
        //        // Use IP address or User Identity Name as a partition key
        //        var key = context.User.Identity?.IsAuthenticated == true
        //            ? context.User.Identity.Name ?? "authenticated"
        //            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        //        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        //        {
        //            PermitLimit = 10, // Max requests allowed in a window
        //            Window = TimeSpan.FromSeconds(30),
        //            SegmentsPerWindow = 3, // smoother distribution
        //            QueueLimit = 2,
        //            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        //        });
        //    });

        //    options.RejectionStatusCode = 429;
        //    options.OnRejected = async (context, token) =>
        //    {
        //        context.HttpContext.Response.Headers["Retry-After"] = "30";
        //        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        //        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
        //    };
        //});

        // 🌐 CORS
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });

        //📦 Mapster Mapping
        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
        services.AddSingleton(new MapsterProfile());

        // ⚙️ API Behavior (ModelState)
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // Load Quartz configuration from appsettings.json
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory(); // Inject scoped services in jobs

            // Bind the entire "Quartz" section to Quartz options
            configuration.GetSection("Quartz").Bind(q);

            // Optionally register jobs and triggers here or via a separate method
            var jobKey = new JobKey("SampleJob");
            q.AddJob<SampleJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("DatabaseJob-trigger")
                .WithCronSchedule("0 */1 * ? * *")); // Every 1 minute
        });

        // Runs Quartz scheduler as background service
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // 🧵 Misc Core Services
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        //  services.AddQuartzJobs();
        services.AddEndpointsApiExplorer();
        services.AddMemoryCache();
        services.AddResponseCaching();
        services.AddHttpContextAccessor();
        services.AddAuthorization();
        //services.AddAuthorization(options =>
        //{
        //    options.AddPolicy("AdminPolicy", policy =>
        //        policy.RequireClaim(ClaimTypes.Role, "Admin"));

        //    options.AddPolicy("ManagerPolicy", policy =>
        //        policy.RequireAssertion(context =>
        //            context.User.HasClaim(c => c.Type == ClaimTypes.Role &&
        //                (c.Value == "Manager" || c.Value == "Admin"))));

        //    options.AddPolicy("PermissionPolicy", policy =>
        //        policy.RequireClaim("permission", "Permission.Admin", "Permission.Manager"));
        //});

        // Register OpenAPI services
        services.AddOpenApi();

        return services;
    }
}
