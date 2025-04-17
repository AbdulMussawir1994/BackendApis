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

public class OldEnterpriseCustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private readonly IConfiguration _config;
    private readonly ILogger<OldEnterpriseCustomMiddleware> _logger;
    private readonly ConfigHandler _configHandler;
    private string _encryptedKey = string.Empty;

    public OldEnterpriseCustomMiddleware(RequestDelegate next, IConfiguration config, ILogger<OldEnterpriseCustomMiddleware> logger, ConfigHandler configHandler)
    {
        _next = next;
        _config = config;
        _logger = logger;
        _configHandler = configHandler;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            string? errorMessage;
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
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var response = new { message = errorMessage };
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
                    return;
                }
            }

            _configHandler.LogId = Guid.NewGuid().ToString();
            _configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            var claimsIdentity = context.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? claimsIdentity?.FindFirst("User")?.Value
                         ?? claimsIdentity?.FindFirst("sub")?.Value
                         ?? "0";
            _configHandler.UserId = userId;

            var isPostmanAllowed = _config.GetValue("EncryptionSettings:IsPostmanAllowed", true);
            var isRequestEncrypted = _config.GetValue("EncryptionSettings:Encryption", false);
            var isRefreshToken = context.Request.Headers.TryGetValue("IsRefreshToken", out var tokenBit) && bool.TryParse(tokenBit, out var result) ? result : true;

            var startTime = DateTime.UtcNow;
            var nonEncryptedPaths = _config["NonEncryptedRoute:List"]?.ToLower().Split(",") ?? [];

            var routeData = context.GetRouteData().Values;
            var controller = routeData["controller"]?.ToString()?.ToLower();
            var action = routeData["action"]?.ToString()?.ToLower();

            var key = context.Request.Headers["Key"].ToString();
            if (!string.IsNullOrEmpty(key))
            {
                key = new RsaEncryption(_config).Decrypt(key);
            }

            bool isDecrypted = true;
            if (isRequestEncrypted && !string.IsNullOrEmpty(key) && !nonEncryptedPaths.Contains(action) && context.Request.Method != HttpMethods.Get)
            {
                isDecrypted = await DecryptRequest(context.Request, key);
            }

            var userLogsHelper = new AppUserLogsHelper(_configHandler);
            var requestLog = await userLogsHelper.GetLogRequest(context);
            requestLog =/* isDecrypted &&*/ !string.IsNullOrEmpty(key)
                ? new LogsParamEncryption().CredentialsEncryptionRequest(requestLog)
                : (isPostmanAllowed || context.Request.Method == HttpMethods.Get
                    ? new LogsParamEncryption().CredentialsEncryptionRequest(requestLog)
                    : requestLog);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["Authorization"] = string.Empty;
                context.Response.Headers["EncryptedKey"] = _encryptedKey;
                return Task.CompletedTask;
            });

            var responseBody = await GetLogResponse(context);

            var reqModel = userLogsHelper.GetLogModel(new LogModel
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                StartTime = startTime,
                UserId = userId,
                Action = action,
                ReqBody = requestLog,
                ResBody = responseBody,
                Controller = controller,
                IsExceptionFromRequest = requestLog.Contains("Exception"),
                IsExceptionFromResponse = responseBody.Contains("Exception"),
                RequestHeaders = userLogsHelper.GetRequestHeaders(context.Request.Headers)
            });

            reqModel.ResponseBody = new LogsParamEncryption().CredentialsEncryptionResponse(reqModel.ResponseBody);
            _ = userLogsHelper.SaveAppUserLogs(reqModel);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var encryptedToken = authHeader.ToString().Substring("Bearer ".Length).Trim();
        return new AesGcmEncryption(_config).Decrypt(encryptedToken);
    }

    //private ClaimsPrincipal? ValidateTokenAndGetPrincipal(string jwt)
    //{
    //    var tokenHandler = new JwtSecurityTokenHandler();
    //    var key = Encoding.UTF8.GetBytes(_config["JWTKey:Secret"]!);

    //    var validationParams = new TokenValidationParameters
    //    {
    //        ValidateIssuerSigningKey = true,
    //        IssuerSigningKey = new SymmetricSecurityKey(key),
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidIssuer = _config["JWTKey:ValidIssuer"],
    //        ValidAudience = _config["JWTKey:ValidAudience"],
    //        RequireExpirationTime = true,
    //        ClockSkew = TimeSpan.Zero
    //    };

    //    return tokenHandler.ValidateToken(jwt, validationParams, out _);
    //}

    private ClaimsPrincipal? ValidateTokenAndGetPrincipal(string jwt, out string? errorMessage)
    {
        errorMessage = null;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["JWTKey:Secret"]!);

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
            return null;
        }
        catch (SecurityTokenException ex)
        {
            errorMessage = $"Invalid token: {ex.Message}";
            return null;
        }
        catch (Exception ex)
        {
            errorMessage = $"An unexpected error occurred: {ex.Message}";
            return null;
        }
    }

    private async Task<bool> DecryptRequest(HttpRequest request, string key)
    {
        if (!request.Path.Value.Contains("api")) return true;

        try
        {
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var encryptedData = Encoding.UTF8.GetString(memoryStream.ToArray());
            var decrypted = new AesGcmEncryption(_config).Decrypt(encryptedData, key);
            request.Body = new MemoryStream(Encoding.UTF8.GetBytes(decrypted));
            request.Body.Position = 0;
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
        string publicKey = _config["EncryptionSettings:ResponsePublicKey"];
        _encryptedKey = new RsaEncryption(_config).Encrypt(key, publicKey);

        var originalBodyStream = context.Response.Body;
        await using var responseBody = _memoryStreamManager.GetStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context); // Let the inner middleware write to responseBody
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Read the full response from the in-memory stream
            var response = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin); // Reset again before copying

            var finalOutput = encryptResponse
                ? new AesGcmEncryption(_config).Decrypt(response, key)
                : response;

            // Write the final output to the original body stream
            var outputBytes = Encoding.UTF8.GetBytes(finalOutput);
            context.Response.ContentLength = outputBytes.Length;
            context.Response.Body = originalBodyStream; // Important: reset stream before write
            await context.Response.Body.WriteAsync(outputBytes, 0, outputBytes.Length);

            return response;
        }
        catch (Exception ex)
        {
            var errorResponse = new MobileResponse<string>(_configHandler, "Application");
            errorResponse.SetError("ERR-1003", "Password is Invalid.");

            var errorBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(errorResponse));
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await originalBodyStream.WriteAsync(errorBytes);
            return $"Exception : {ex}";
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.UnprocessableEntity,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = statusCode switch
            {
                HttpStatusCode.BadRequest => "Bad Request",
                HttpStatusCode.Unauthorized => "Unauthorized",
                HttpStatusCode.UnprocessableEntity => "Validation Error",
                HttpStatusCode.RequestTimeout => "Request Timeout",
                HttpStatusCode.InternalServerError => "Internal Server Error",
                _ => "Error"
            },
        };

        problemDetails.Extensions["ErrorId"] = Guid.NewGuid().ToString();
        problemDetails.Extensions["RequestPath"] = context.Request.Path;
        problemDetails.Extensions["RequestId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        _logger.LogError(exception, "Unhandled exception {ErrorId} at {Path} (Request ID: {RequestId})", problemDetails.Extensions["ErrorId"], context.Request.Path, context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

public static class OldEnterpriseCustomMiddlewareExtensions
{
    public static IApplicationBuilder OldUseEnterpriseCustomMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OldEnterpriseCustomMiddleware>();
    }
}