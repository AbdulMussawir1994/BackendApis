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
                    await CustomAuthorizationMiddleware.WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", "Authorization token is required.");
                    return;
                }

                var (principal, errorMessage) = ValidateTokenAndGetPrincipal(token);
                if (principal is null)
                {
                    await CustomAuthorizationMiddleware.WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", errorMessage);
                    return;
                }

                context.User = principal;
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            _configHandler.UserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";

            if (!await TryDecryptRequest(context))
            {
                await CustomAuthorizationMiddleware.WriteCustomResponse(context, StatusCodes.Status401Unauthorized, "ERR-401", "Failed to decrypt request body.");
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

        ////  _configHandler.LogId = Guid.NewGuid().ToString();
        //_configHandler.LogId = Guid.CreateVersion7().ToString();
        ////   _configHandler.LogId = Ulid.NewUlid().ToString();
        //_configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private async Task<bool> TryDecryptRequest(HttpContext context)
    {
        var isEncrypted = _config.GetValue("EncryptionSettings:Encryption", false);
        var route = context.GetRouteData().Values["action"]?.ToString()?.ToLower();
        var skipDecrypt = _config["NonEncryptedRoute:List"]?.ToLower().Split(',')?.Contains(route) == true;

        var key = context.Request.Headers["Key"].ToString();
        if (!string.IsNullOrEmpty(key))
            key = new RsaEncryption(_config).Decrypt(key);

        if (!isEncrypted || string.IsNullOrEmpty(key) || skipDecrypt || context.Request.Method == HttpMethods.Get)
            return true;

        try
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var encrypted = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            var decrypted = new AesGcmEncryption(_config).Decrypt(encrypted, key);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(decrypted)) { Position = 0 };
            return true;
        }
        catch
        {
            return false;
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

    //private (ClaimsPrincipal? Principal, string? ErrorMessage) ValidateTokenAndGetPrincipal(string jwt)
    //{
    //    if (string.IsNullOrWhiteSpace(jwt))
    //        return (null, "JWT token is missing.");

    //    var tokenHandler = new JwtSecurityTokenHandler();
    //    var key = Encoding.UTF8.GetBytes(_config["JWTKey:Secret"]);

    //    var validationParams = new TokenValidationParameters
    //    {
    //        ValidateIssuerSigningKey = true,
    //        IssuerSigningKey = new SymmetricSecurityKey(key),
    //        ValidateIssuer = true,
    //        ValidateAudience = true,
    //        ValidIssuer = _config["JWTKey:ValidIssuer"],
    //        ValidAudience = _config["JWTKey:ValidAudience"],
    //        RequireExpirationTime = true,
    //        ClockSkew = TimeSpan.Zero // No leeway, strict expiration check
    //    };

    //    try
    //    {
    //        var principal = tokenHandler.ValidateToken(jwt, validationParams, out var validatedToken);

    //        if (validatedToken is JwtSecurityToken jwtToken)
    //        {
    //            // Extra expiration validation (optional, for explicit logic)
    //            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
    //            if (expClaim != null && long.TryParse(expClaim.Value, out var expUnix))
    //            {
    //                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
    //                if (expirationTime < DateTime.UtcNow)
    //                    return (null, "Token has been expired.");
    //            }
    //        }

    //        return (principal, null);
    //    }
    //    catch (SecurityTokenExpiredException)
    //    {
    //        return (null, "JWT token has expired.");
    //    }
    //    catch (SecurityTokenInvalidSignatureException)
    //    {
    //        return (null, "JWT token signature is invalid.");
    //    }
    //    catch (SecurityTokenException ex)
    //    {
    //        return (null, $"JWT token validation failed: {ex.Message}");
    //    }
    //    catch (Exception ex)
    //    {
    //        return (null, $"Unexpected error: {ex.Message}");
    //    }
    //}

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

    //This method is used for Server to Server side communication.
    private async Task EncryptRequest(HttpContext context, string key)
    {
        try
        {
            // Read request body into string
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset request body stream position
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
                return;

            // Encrypt the request content using AES-GCM (or any algorithm you use)
            var encrypted = new AesGcmEncryption(_config).Encrypt(body, key);

            // Replace request body with encrypted content
            var encryptedBytes = Encoding.UTF8.GetBytes(encrypted);
            context.Request.Body = new MemoryStream(encryptedBytes) { Position = 0 };

            // Optional: Log or trace encrypted data for debugging
            // _logger.LogInformation("Request encrypted successfully.");
        }
        catch (Exception ex)
        {
            // Optional: Log error
            //_logger.LogError(ex, "Failed to encrypt request.");
            throw new InvalidOperationException("Failed to encrypt request.", ex);
        }
    }

    //This method is used for Server to Server side communication.
    private async Task<bool> DecryptRequest1(HttpRequest request, string key)
    {
        try
        {
            // Ensure the body can be read multiple times
            request.EnableBuffering();

            // Read stream directly into memory
            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var encryptedPayload = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(encryptedPayload))
                return false;

            // Reset stream for replacement
            request.Body.Position = 0;

            // Decrypt
            var decrypted = new AesGcmEncryption(_config).Decrypt(encryptedPayload, key);

            // Replace request body with decrypted data
            var decryptedBytes = Encoding.UTF8.GetBytes(decrypted);
            request.Body = new MemoryStream(decryptedBytes) { Position = 0 };

            return true;
        }
        catch (Exception ex)
        {
            // Optional: Log decryption error
            //_logger.LogError(ex, "Failed to decrypt request body.");
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

public class CustomAuthorizationMiddleware : IAuthorizationMiddlewareResultHandler
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
        var logger = context.RequestServices.GetRequiredService<ILogger<CustomAuthorizationMiddleware>>();
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

public static class EnterpriseCustomMiddlewareExtensions
{
    public static IApplicationBuilder UseEnterpriseCustomMiddleware(this IApplicationBuilder builder) =>
        builder.UseMiddleware<EnterpriseCustomMiddleware>();
}
