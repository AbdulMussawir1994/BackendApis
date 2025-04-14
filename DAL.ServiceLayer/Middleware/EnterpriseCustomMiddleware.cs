using Cryptography.Utilities;
using DAL.ServiceLayer.LogsHelper;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
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
        string? errorMessage = null;

        try
        {
            var decryptedToken = DecryptToken(context);
            if (!string.IsNullOrEmpty(decryptedToken))
            {
                var principal = ValidateTokenAndGetPrincipal(decryptedToken, out errorMessage);
                if (principal is not null)
                {
                    context.User = principal;
                    context.Request.Headers["Authorization"] = $"Bearer {decryptedToken}";
                }
                else
                {
                    await WriteUnauthorizedResponse(context, errorMessage);
                    return;
                }
            }

            SetInitialContext(context);

            var claimsIdentity = context.User.Identity as ClaimsIdentity;
            _configHandler.UserId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                    claimsIdentity?.FindFirst("User")?.Value ??
                                    claimsIdentity?.FindFirst("sub")?.Value ?? "0";

            if (!await TryDecryptRequest(context))
            {
                await WriteUnauthorizedResponse(context, "Failed to decrypt request body.");
                return;
            }

            var userLogsHelper = new AppUserLogsHelper(_configHandler);
            var requestLog = await userLogsHelper.GetLogRequest(context);
            requestLog = ApplyEncryptionToRequest(context, requestLog);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Authorization"] = string.Empty;
                context.Response.Headers["EncryptedKey"] = _encryptedKey;
                return Task.CompletedTask;
            });

            var responseBody = await GetLogResponse(context);
            var reqModel = userLogsHelper.GetLogModel(BuildLogModel(context, requestLog, responseBody));
            reqModel.ResponseBody = new LogsParamEncryption().CredentialsEncryption(reqModel.ResponseBody);

            _ = userLogsHelper.SaveAppUserLogs(reqModel);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private void SetInitialContext(HttpContext context)
    {
        _configHandler.LogId = Guid.NewGuid().ToString();
        _configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private async Task<bool> TryDecryptRequest(HttpContext context)
    {
        var isEncrypted = _config.GetValue("EncryptionSettings:Encryption", false);
        var nonEncryptedRoutes = _config["NonEncryptedRoute:List"]?.ToLower().Split(",") ?? [];
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
        if (isPostmanAllowed || context.Request.Method == HttpMethods.Get)
            return new LogsParamEncryption().CredentialsEncryption(requestLog);

        return requestLog;
    }

    private async Task WriteUnauthorizedResponse(HttpContext context, string? message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        var response = new { message = message ?? "Unauthorized access." };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private LogModel BuildLogModel(HttpContext context, string reqBody, string resBody)
    {
        var routeData = context.GetRouteData().Values;
        return new LogModel
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            StartTime = DateTime.UtcNow,
            UserId = _configHandler.UserId,
            Action = routeData["action"]?.ToString(),
            Controller = routeData["controller"]?.ToString(),
            ReqBody = reqBody,
            ResBody = resBody,
            IsExceptionFromRequest = reqBody.Contains("Exception"),
            IsExceptionFromResponse = resBody.Contains("Exception"),
            RequestHeaders = new AppUserLogsHelper(_configHandler).GetRequestHeaders(context.Request.Headers)
        };
    }

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header) ||
            !header.ToString().StartsWith("Bearer ")) return null;

        var encryptedToken = header.ToString()["Bearer ".Length..].Trim();
        return new AesGcmEncryption(_config).Decrypt(encryptedToken);
    }

    private ClaimsPrincipal? ValidateTokenAndGetPrincipal(string jwt, out string? errorMessage)
    {
        errorMessage = null;
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
            return tokenHandler.ValidateToken(jwt, validationParams, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            errorMessage = "Token has expired.";
        }
        catch (SecurityTokenException ex)
        {
            errorMessage = $"Invalid token: {ex.Message}";
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
        }
        return null;
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

            var finalOutput = encryptResponse
                ? new AesGcmEncryption(_config).Decrypt(response, key)
                : response;

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
            errorResponse.SetError("ERR-1003", "Password is Invalid.");
            var errorJson = JsonConvert.SerializeObject(errorResponse);

            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(errorJson));
            return $"Exception : {ex.Message}";
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var status = exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.UnprocessableEntity,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = (int)status,
            Type = $"https://httpstatuses.com/{(int)status}",
            Title = status.ToString(),
            Extensions =
            {
                ["ErrorId"] = Guid.NewGuid().ToString(),
                ["RequestPath"] = context.Request.Path,
                ["RequestId"] = context.TraceIdentifier
            }
        };

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        _logger.LogError(exception, "Unhandled exception {ErrorId} at {Path} (Request ID: {RequestId})",
            problem.Extensions["ErrorId"], context.Request.Path, context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class EnterpriseCustomMiddlewareExtensions
{
    public static IApplicationBuilder UseEnterpriseCustomMiddleware(this IApplicationBuilder builder) =>
        builder.UseMiddleware<EnterpriseCustomMiddleware>();
}
