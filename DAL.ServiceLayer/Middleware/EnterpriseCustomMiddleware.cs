using Cryptography.Utilities;
using DAL.ServiceLayer.LogsHelper;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using ServiceStack.Text;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace DAL.ServiceLayer.Middleware;

public class EnterpriseCustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<EnterpriseCustomMiddleware> _logger;
    private readonly ConfigHandler _configHandler;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string _encryptedKey = string.Empty;

    public EnterpriseCustomMiddleware(RequestDelegate next, IConfiguration config, ILogger<EnterpriseCustomMiddleware> logger, ConfigHandler configHandler)
    {
        _next = next;
        _config = config;
        _logger = logger;
        _configHandler = configHandler;
    }

    public async Task Invoke(HttpContext context)
    {
        SetInitialContext(context);
        var userLogsHelper = new AppUserLogsHelper(_configHandler);
        string requestLog = string.Empty;

        try
        {
            if (context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() is null)
            {
                var token = DecryptToken(context);
                if (string.IsNullOrEmpty(token))
                {
                    await CustomAuthorizationMiddleware.WriteErrorAndLog(context, StatusCodes.Status401Unauthorized, "Authorization token is required.");
                    return;
                }

                var (principal, errorMessage) = ValidateTokenAndGetPrincipal(token);
                if (principal == null)
                {
                    await CustomAuthorizationMiddleware.WriteErrorAndLog(context, StatusCodes.Status401Unauthorized, errorMessage);
                    return;
                }

                context.User = principal;
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            _configHandler.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

            if (!await TryDecryptRequest(context))
            {
                await CustomAuthorizationMiddleware.WriteErrorAndLog(context, StatusCodes.Status401Unauthorized, "Failed to decrypt request body.");
                return;
            }

            requestLog = await userLogsHelper.GetLogRequest(context);
            requestLog = ApplyEncryptionToRequest(context, requestLog);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Authorization"] = string.Empty;
                context.Response.Headers["EncryptedKey"] = _encryptedKey;
                return Task.CompletedTask;
            });

            var responseBody = await GetLogResponse(context);
            await SaveRequestLog(context, requestLog, responseBody, userLogsHelper);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught in middleware");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task SaveRequestLog(HttpContext context, string requestLog, string responseBody, AppUserLogsHelper userLogsHelper)
    {
        var routeData = context.GetRouteData().Values;

        var reqModel = userLogsHelper.GetLogModel(new LogModel
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            StartTime = DateTime.UtcNow,
            UserId = _configHandler.UserId,
            Action = routeData["action"]?.ToString(),
            Controller = routeData["controller"]?.ToString(),
            ReqBody = requestLog,
            ResBody = responseBody,
            IsExceptionFromRequest = requestLog.Contains("Exception"),
            IsExceptionFromResponse = responseBody.Contains("Exception"),
            RequestHeaders = userLogsHelper.GetRequestHeaders(context.Request.Headers)
        });

        reqModel.ResponseBody = new LogsParamEncryption().CredentialsEncryptionResponse(reqModel.ResponseBody);
        _ = userLogsHelper.SaveAppUserLogs(reqModel);
    }

    private void SetInitialContext(HttpContext context)
    {
        _configHandler.LogId = Guid.NewGuid().ToString();
        _configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private async Task<bool> TryDecryptRequest(HttpContext context)
    {
        var isEncrypted = _config.GetValue("EncryptionSettings:Encryption", false);
        var nonEncryptedRoutes = _config["NonEncryptedRoute:List"]?.ToLower().Split(',') ?? [];
        var routeAction = context.GetRouteData().Values["action"]?.ToString()?.ToLower();

        var key = context.Request.Headers["Key"].ToString();
        if (!string.IsNullOrEmpty(key)) key = new RsaEncryption(_config).Decrypt(key);

        if (!isEncrypted || string.IsNullOrEmpty(key) ||
            nonEncryptedRoutes.Contains(routeAction) || context.Request.Method == HttpMethods.Get)
            return true;

        return await DecryptRequest(context.Request, key);
    }

    private string ApplyEncryptionToRequest(HttpContext context, string requestLog)
    {
        var isPostmanAllowed = _config.GetValue("EncryptionSettings:IsPostmanAllowed", true);
        return isPostmanAllowed || context.Request.Method == HttpMethods.Get
            ? new LogsParamEncryption().CredentialsEncryptionRequest(requestLog)
            : requestLog;
    }

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header) ||
            !header.ToString().StartsWith("Bearer ")) return null;

        var encryptedToken = header.ToString()["Bearer ".Length..].Trim();
        return new AesGcmEncryption(_config).Decrypt(encryptedToken);
    }

    private (ClaimsPrincipal? Principal, string? ErrorMessage) ValidateTokenAndGetPrincipal(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return (null, "JWT token is missing.");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["JWTKey:Secret"]);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _config["JWTKey:ValidIssuer"],
            ValidAudience = _config["JWTKey:ValidAudience"],
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(jwt, validationParams, out _);
            return (principal, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    private async Task<bool> DecryptRequest(HttpRequest request, string key)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);
            var decrypted = new AesGcmEncryption(_config).Decrypt(Encoding.UTF8.GetString(memoryStream.ToArray()), key);
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(decrypted)) { Position = 0 };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetLogResponse(HttpContext context, bool encryptResponse = false)
    {
        string key = new GeneralEncryption(_config).GenerateSymmetricKey();
        _encryptedKey = new RsaEncryption(_config).Encrypt(key, _config["EncryptionSettings:ResponsePublicKey"]);

        var originalBody = context.Response.Body;
        await using var responseBody = _memoryStreamManager.GetStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var finalOutput = encryptResponse ? new AesGcmEncryption(_config).Decrypt(response, key) : response;
            var outputBytes = Encoding.UTF8.GetBytes(finalOutput);
            context.Response.ContentLength = outputBytes.Length;
            context.Response.Body = originalBody;
            await context.Response.Body.WriteAsync(outputBytes);

            return response;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var errorResponse = new MobileResponse<string>(_configHandler, "Application");
            errorResponse.SetError("ERR-1003", "Internal Server Error.");
            var errorJson = JsonConvert.SerializeObject(errorResponse);

            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(errorJson));
            return $"Exception : {ex.Message}";
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted) return;

        var code = exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.UnprocessableEntity,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = (int)code,
            Type = $"https://httpstatuses.com/{(int)code}",
            Title = code.ToString(),
            Extensions =
         {
             ["ErrorId"] = Guid.NewGuid().ToString(),
             ["RequestPath"] = context.Request.Path,
             ["RequestId"] = context.TraceIdentifier
         }
        };

        _logger.LogError(exception, "Error {ErrorId} on {Path}", problem.Extensions["ErrorId"], context.Request.Path);
        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}

public class CustomAuthorizationMiddleware : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await WriteErrorAndLog(context, StatusCodes.Status403Forbidden, "Access Denied. You do not have the required permission.");
            return;
        }

        if (authorizeResult.Challenged)
        {
            await WriteErrorAndLog(context, StatusCodes.Status401Unauthorized, "Authentication required.");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    public static async Task WriteErrorAndLog(HttpContext context, int statusCode, string message)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<CustomAuthorizationMiddleware>>();
        var configHandler = context.RequestServices.GetRequiredService<ConfigHandler>();
        var logHelper = new AppUserLogsHelper(configHandler);

        var response = new { status = statusCode == StatusCodes.Status403Forbidden ? "Forbidden" : "Unauthorized", message };
        var json = JsonConvert.SerializeObject(response);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);

        var requestLog = await logHelper.GetLogRequest(context);
        var routeData = context.GetRouteData().Values;

        var logModel = logHelper.GetLogModel(new LogModel
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            StartTime = DateTime.UtcNow,
            UserId = "0",
            Action = routeData["action"]?.ToString(),
            Controller = routeData["controller"]?.ToString(),
            ReqBody = requestLog,
            ResBody = json,
            IsExceptionFromRequest = requestLog.Contains("Exception"),
            IsExceptionFromResponse = json.Contains("Exception"),
            RequestHeaders = logHelper.GetRequestHeaders(context.Request.Headers)
        });

        logModel.ResponseBody = new LogsParamEncryption().CredentialsEncryptionResponse(logModel.ResponseBody);
        _ = logHelper.SaveAppUserLogs(logModel);

        logger.LogWarning("Authorization failure logged with status code {StatusCode}", statusCode);
    }
}

public static class EnterpriseCustomMiddlewareExtensions
{
    public static IApplicationBuilder UseEnterpriseCustomMiddleware(this IApplicationBuilder builder) =>
        builder.UseMiddleware<EnterpriseCustomMiddleware>();
}