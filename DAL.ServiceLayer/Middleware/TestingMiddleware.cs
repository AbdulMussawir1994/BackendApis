using Cryptography.Utilities;
using DAL.ServiceLayer.LogsHelper;
using DAL.ServiceLayer.Models;
using DAL.ServiceLayer.Models.LogsModels;
using DAL.ServiceLayer.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

public class TestEnterpriseCustomMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _config;
    private readonly ILogger<EnterpriseCustomMiddleware> _logger;
    private readonly ConfigHandler _configHandler;
    private readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private string _encryptedKey = string.Empty;

    public TestEnterpriseCustomMiddleware(RequestDelegate next, IConfiguration config, ILogger<EnterpriseCustomMiddleware> logger, ConfigHandler configHandler)
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

            var requestLog = await userLogsHelper.GetLogRequest(context);
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

    private async Task<string> GetLogResponse(HttpContext context)
    {
        string key = new GeneralEncryption(_config).GenerateSymmetricKey();
        _encryptedKey = new RsaEncryption(_config).Encrypt(key, _config["EncryptionSettings:ResponsePublicKey"]);

        var originalBody = context.Response.Body;
        await using var responseBody = _memoryStreamManager.GetStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            if (context.Response.ContentType?.StartsWith("image/") == true ||
                context.Response.ContentType == "application/pdf")
            {
                responseBody.Position = 0;
                await responseBody.CopyToAsync(originalBody);
                return "[BINARY_CONTENT_SKIPPED]";
            }

            responseBody.Position = 0;
            var response = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Position = 0;

            var final = _config.GetValue("EncryptionSettings:EncryptResponse", false)
                ? new AesGcmEncryption(_config).Encrypt(response, key)
                : response;

            var responseBytes = Encoding.UTF8.GetBytes(final);
            context.Response.ContentLength = responseBytes.Length;
            context.Response.Body = originalBody;
            await context.Response.Body.WriteAsync(responseBytes);

            return response;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
            return string.Empty;
        }
    }

    private void SetInitialContext(HttpContext context)
    {
        _configHandler.LogId = Guid.NewGuid().ToString();
        _configHandler.RequestedDateTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    private string ApplyEncryptionToRequest(HttpContext context, string body)
    {
        if (_config.GetValue("EncryptionSettings:IsPostmanAllowed", true) || context.Request.Method == HttpMethods.Get)
            return new LogsParamEncryption().CredentialsEncryptionRequest(body);

        return body;
    }

    private async Task SaveRequestLog(HttpContext context, string request, string response, AppUserLogsHelper helper)
    {
        var route = context.GetRouteData().Values;
        var model = helper.GetLogModel(new LogModel
        {
            Method = context.Request.Method,
            Path = context.Request.Path,
            QueryString = context.Request.QueryString.ToString(),
            StartTime = DateTime.UtcNow,
            UserId = _configHandler.UserId,
            Action = route["action"]?.ToString(),
            Controller = route["controller"]?.ToString(),
            ReqBody = request,
            ResBody = response,
            RequestHeaders = helper.GetRequestHeaders(context.Request.Headers),
            IsExceptionFromRequest = request.Contains("Exception"),
            IsExceptionFromResponse = response.Contains("Exception")
        });

        model.ResponseBody = new LogsParamEncryption().CredentialsEncryptionResponse(model.ResponseBody);
        _ = helper.SaveAppUserLogs(model);
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

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var header) || !header.ToString().StartsWith("Bearer "))
            return null;

        var encryptedToken = header.ToString()["Bearer ".Length..].Trim();
        return new AesGcmEncryption(_config).Decrypt(encryptedToken);
    }

    public (ClaimsPrincipal? Principal, string? ErrorMessage) ValidateTokenAndGetPrincipal(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt)) return (null, "JWT token is missing.");

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var baseKey = Encoding.UTF8.GetBytes(_config["JWTKey:Secret"]);
            var issuer = _config["JWTKey:ValidIssuer"];
            var audience = _config["JWTKey:ValidAudience"];

            var parameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKeyResolver = (token, _, _, _) =>
                {
                    var jwt = handler.ReadJwtToken(token);
                    var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                    var email = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                    var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).OrderBy(x => x);
                    if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(email) || !roles.Any())
                        throw new SecurityTokenException("Missing claims");
                    var derived = DeriveKmacKey(userId, roles, email, baseKey);
                    return new[] { new SymmetricSecurityKey(derived) };
                }
            };

            var principal = handler.ValidateToken(jwt, parameters, out _);
            return (principal, null);
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
        var input = Encoding.UTF8.GetBytes($"{userId}|{string.Join(",", roles)}|{email}");
        var kmac = new Org.BouncyCastle.Crypto.Macs.KMac(256, secret);
        kmac.Init(new Org.BouncyCastle.Crypto.Parameters.KeyParameter(secret));
        kmac.BlockUpdate(input, 0, input.Length);
        var output = new byte[kmac.GetMacSize()];
        kmac.DoFinal(output, 0);
        return output;
    }
}

//public static class TestEnterpriseCustomMiddlewareExtensions
//{
//    public static IApplicationBuilder UseEnterpriseCustomMiddleware(this IApplicationBuilder builder) =>
//        builder.UseMiddleware<TestEnterpriseCustomMiddleware>();
//}