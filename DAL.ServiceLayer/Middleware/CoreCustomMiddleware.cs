﻿using Cryptography.Utilities;
using DAL.ServiceLayer.LogsHelper;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

public class CoreCustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<CoreCustomMiddleware> _logger;
    private readonly ConfigHandler _configHandler;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string _encryptedKey = string.Empty;

    public CoreCustomMiddleware(RequestDelegate next, IConfiguration config, ILogger<CoreCustomMiddleware> logger, ConfigHandler configHandler)
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
            if (!IsAnonymousAllowed(context))
            {
                var token = DecryptToken(context);
                if (string.IsNullOrEmpty(token))
                {
                    await CoreAuthorizationMiddleware.WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", "Authorization token is required.");
                    return;
                }

                var (principal, errorMessage) = ValidateTokenAndGetPrincipal(token);
                if (principal is null)
                {
                    await CoreAuthorizationMiddleware.WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", errorMessage);
                    return;
                }

                context.User = principal;
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            _configHandler.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

            requestLog = await userLogsHelper.GetLogRequest(context);
            requestLog = ApplyEncryptionToRequest(context, requestLog);

            context.Response.OnStarting(() =>
            {
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

    private static bool IsAnonymousAllowed(HttpContext context)
    {
        return context.GetEndpoint()?.Metadata?.GetMetadata<IAllowAnonymous>() != null;
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
        //  _configHandler.LogId = Guid.NewGuid().ToString();
        _configHandler.LogId = Guid.CreateVersion7().ToString();
        //   _configHandler.LogId = Ulid.NewUlid().ToString();
        _configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private string ApplyEncryptionToRequest(HttpContext context, string body)
    {
        if (_config.GetValue("EncryptionSettings:IsPostmanAllowed", true) || context.Request.Method == HttpMethods.Get)
            return new LogsParamEncryption().CredentialsEncryptionRequest(body);

        return body;
    }

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header) || !header.ToString().StartsWith("Bearer "))
            return null;

        var encryptedToken = header.ToString()["Bearer ".Length..].Trim();
        return new AesGcmEncryption(_config).Decrypt(encryptedToken);
    }

    //for KMAC
    public (ClaimsPrincipal? Principal, string? ErrorMessage) ValidateTokenAndGetPrincipal(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return (null, "JWT token is missing.");

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secret = _config["JWTKey:Secret"] ?? throw new InvalidOperationException("JWT secret is missing.");
            var issuer = _config["JWTKey:ValidIssuer"];
            var audience = _config["JWTKey:ValidAudience"];
            var baseKey = Encoding.UTF8.GetBytes(secret);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);

                    var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value
                                 ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                    var email = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

                    var roles = jwtToken.Claims
                        .Where(c => c.Type == ClaimTypes.Role || c.Type.Equals("role", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Value)
                        .OrderBy(r => r)
                        .ToList();

                    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || roles.Count == 0)
                    {
                        throw new SecurityTokenException("Token is missing required claims.");
                    }

                    var derivedKey = DeriveKmacKey(userId, roles, email, baseKey);
                    return new[] { new SymmetricSecurityKey(derivedKey) };
                }
            };

            var principal = tokenHandler.ValidateToken(jwt, validationParameters, out _);
            return (principal, null);
        }
        catch (SecurityTokenExpiredException)
        {
            return (null, "JWT token has expired.");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return (null, "JWT token signature is invalid.");
        }
        catch (SecurityTokenException ex)
        {
            return (null, $"JWT token validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, $"Unexpected error: {ex.Message}");
        }
    }

    private static byte[] DeriveKmacKey(string userId, IEnumerable<string> roles, string email, byte[] secret)
    {
        var rolesString = string.Join(",", roles.OrderBy(r => r)); // Ensure order is consistent
        var inputData = Encoding.UTF8.GetBytes($"{userId}|{rolesString}|{email}");

        var kmac = new Org.BouncyCastle.Crypto.Macs.KMac(256, secret);
        kmac.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(secret));
        kmac.BlockUpdate(inputData, 0, inputData.Length);
        var output = new byte[kmac.GetMacSize()];
        kmac.DoFinal(output, 0);
        return output;
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

            // Skip encryption for binary content (e.g., images, PDFs)
            var contentType = context.Response.ContentType;
            if (!string.IsNullOrEmpty(contentType) &&
                (contentType.StartsWith("image/") || contentType == "application/pdf" || contentType.StartsWith("video/") || contentType.StartsWith("audio/")))
            {
                context.Response.Body = originalBody;
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBody);
                return "[BINARY_CONTENT_SKIPPED]"; // Placeholder for logging (optional)
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            var response = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);

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
            errorResponse.SetError("ERR-1003", ex.InnerException?.Message ?? ex.Message);

            var filteredResponse = new
            {
                errorResponse.LogId,
                errorResponse.Content,
                errorResponse.RequestDateTime,
                errorResponse.Status
            };

            var errorJson = JsonConvert.SerializeObject(filteredResponse);
            await originalBody.WriteAsync(Encoding.UTF8.GetBytes(errorJson));
            return errorJson;
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var code = ex switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            ValidationException => HttpStatusCode.UnprocessableEntity,
            TimeoutException => HttpStatusCode.RequestTimeout,
            _ => HttpStatusCode.InternalServerError
        };

        var response = new MobileResponse<string>(_configHandler, "Application")
            .SetError("ERR-500", ex.InnerException?.Message ?? ex.Message);

        var filtered = new
        {
            response.LogId,
            response.Content,
            response.RequestDateTime,
            response.Status
        };

        context.Response.Clear();
        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonConvert.SerializeObject(filtered), Encoding.UTF8);
    }
}

public class CoreAuthorizationMiddleware : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await WriteCustomResponse(context, StatusCodes.Status403Forbidden, "ERR-403", "Access Denied. You do not have the required permission.");
            return;
        }

        if (authorizeResult.Challenged)
        {
            await WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", "Authentication required.");
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    public static async Task WriteCustomResponse(HttpContext context, int statusCode, string errorCode, string message)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<CoreAuthorizationMiddleware>>();
        var configHandler = context.RequestServices.GetRequiredService<ConfigHandler>();
        var logHelper = new AppUserLogsHelper(configHandler);

        // Build response using MobileResponse pattern
        var response = new MobileResponse<object>(configHandler, "Application")
            .SetError(errorCode, message);

        // Create anonymous object with only required fields
        var filteredResponse = new
        {
            response.LogId,
            response.Content,
            response.RequestDateTime,
            response.Status
        };

        // Serialize only filtered structure
        var json = JsonConvert.SerializeObject(filteredResponse);

        // Write response
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);

        try
        {
            var requestLog = await logHelper.GetLogRequest(context);
            var routeData = context.GetRouteData().Values;

            var logModel = logHelper.GetLogModel(new LogModel
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                QueryString = context.Request.QueryString.ToString(),
                StartTime = DateTime.UtcNow,
                UserId = configHandler.UserId ?? "0",
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
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write log in WriteCustomResponse");
        }
    }
}

public static class CoreCustomMiddlewareExtensions
{
    public static IApplicationBuilder UseCoreCustomMiddleware(this IApplicationBuilder builder) =>
        builder.UseMiddleware<CoreCustomMiddleware>();
}
