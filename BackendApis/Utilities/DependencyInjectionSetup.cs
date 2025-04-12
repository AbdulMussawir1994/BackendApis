using BackendApis.Helpers;
using Cryptography.Utilities;
using DAL.RepositoryLayer.DataAccess;
using DAL.RepositoryLayer.IDataAccess;
using DAL.RepositoryLayer.IRepositories;
using DAL.RepositoryLayer.Repositories;
using DAL.ServiceLayer.Helpers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
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
            options.DefaultApiVersion = new ApiVersion(1, 0);
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
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                    ClockSkew = TimeSpan.Zero
                };
            });

        // 🧩 Dependency Injection (DI)
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IDataBaseAccess, DataBaseAccess>();
        services.AddScoped<CustomUserManager>();

        // Register custom encryption utility as Singleton
        services.AddSingleton<AesGcmEncryption>();

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
        services.AddResiliencePipeline("GlobalHttpPolicy", builder =>
        {
            // Retry with exponential backoff
            builder.AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !((HttpResponseMessage)response).IsSuccessStatusCode),
                BackoffType = DelayBackoffType.Exponential,
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                UseJitter = true
            });

            // Circuit Breaker
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .HandleResult(response => !((HttpResponseMessage)response).IsSuccessStatusCode),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 8,
                BreakDuration = TimeSpan.FromSeconds(30)
            });
        });

        // 💥 Apply globally to all HttpClient instances
        //services.ConfigureAll<HttpClientFactoryOptions>(options =>
        //  {
        //      options.ResiliencePipelineName = "GlobalHttpPolicy"; // 👈 This now works
        //  });

        // 🚦 Rate Limiting
        services.AddRateLimiter(options =>
        {
            // Global Partitioned Rate Limiter for all requests (authenticated + anonymous)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Use authenticated user or fallback to IP address
                var partitionKey = context.User?.Identity?.IsAuthenticated == true
                    ? $"user:{context.User.Identity.Name}"
                    : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5, // Total requests allowed per window
                    Window = TimeSpan.FromSeconds(60), // Duration of the sliding window
                    SegmentsPerWindow = 1, // Number of segments (better smoothing)
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5 // Allow small queue
                });
            });

            // Optional: Use Token Bucket (can be swapped with SlidingWindow above)
            // options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            // {
            //     var key = context.User?.Identity?.IsAuthenticated == true
            //         ? $"user:{context.User.Identity.Name}"
            //         : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

            //     return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
            //     {
            //         TokenLimit = 100,
            //         TokensPerPeriod = 20,
            //         ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            //         AutoReplenishment = true,
            //         QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            //         QueueLimit = 5
            //     });
            // });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.Headers["Retry-After"] = "60";
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync("{\"message\": \"Too many requests. Please wait and try again.\"}", token);
            };
        });

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

        // 📦 Mapster Mapping
        //  TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());

        // ⚙️ API Behavior (ModelState)
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        // 🧵 Misc Core Services
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        services.AddEndpointsApiExplorer();
        services.AddResponseCaching();
        services.AddHttpContextAccessor();
        services.AddAuthorization();

        // Register OpenAPI services
        services.AddOpenApi();

        return services;
    }
}
