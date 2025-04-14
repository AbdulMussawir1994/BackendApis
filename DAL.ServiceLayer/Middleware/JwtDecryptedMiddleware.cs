using Cryptography.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace DAL.ServiceLayer.Middleware;

public class JwtDecryptedMiddleware : IMiddleware
{
    private readonly ILogger<JwtDecryptedMiddleware> _logger;
    private readonly AesGcmEncryption _aesGcmEncryption;
    private readonly IConfiguration _configuration;

    public JwtDecryptedMiddleware(
        ILogger<JwtDecryptedMiddleware> logger,
        AesGcmEncryption aesGcmEncryption,
        IConfiguration configuration)
    {
        _logger = logger;
        _aesGcmEncryption = aesGcmEncryption;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            var token = DecryptToken(context);
            if (token != null)
            {
                var principal = ValidateTokenAndGetPrincipal(token);
                if (principal != null)
                {
                    context.User = principal;
                }
            }

            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }



    #region Private Methods

    //private void DecryptJwt(HttpContext context)
    //{
    //    if (context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
    //        authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    //    {
    //        var encryptedToken = authHeader.ToString().Substring("Bearer ".Length).Trim();
    //        try
    //        {
    //            var decryptedJwt = _aesGcmEncryption.Decrypt(encryptedToken);

    //            // Replace Authorization header with decrypted token
    //            context.Request.Headers["Authorization"] = $"Bearer {decryptedJwt}";
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogWarning(ex, "Failed to decrypt JWT token.");
    //            // Optionally: throw or ignore based on your requirement
    //            throw new UnauthorizedAccessException("Invalid encrypted token.");
    //        }
    //    }
    //}

    private string? DecryptToken(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) ||
            !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var encryptedToken = authHeader.ToString().Substring("Bearer ".Length).Trim();
        return _aesGcmEncryption.Decrypt(encryptedToken);
    }

    private ClaimsPrincipal? ValidateTokenAndGetPrincipal(string jwt)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["JWTKey:Secret"]!);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _configuration["JWTKey:ValidIssuer"],
            ValidAudience = _configuration["JWTKey:ValidAudience"],
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(jwt, validationParams, out _);
        return principal;
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCodeForException(exception);
        var errorId = Guid.NewGuid().ToString();
        var requestId = context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Title = GetTitleForStatusCode((int)statusCode),
        };

        problemDetails.Extensions["ErrorId"] = errorId;
        problemDetails.Extensions["RequestPath"] = context.Request.Path;
        problemDetails.Extensions["RequestId"] = requestId;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        _logger.LogError(exception, "Unhandled exception {ErrorId} at {Path} (Request ID: {RequestId})", errorId, context.Request.Path, requestId);

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static HttpStatusCode GetStatusCodeForException(Exception exception) => exception switch
    {
        ArgumentNullException => HttpStatusCode.BadRequest,
        UnauthorizedAccessException => HttpStatusCode.Unauthorized,
        ValidationException => HttpStatusCode.UnprocessableEntity,
        TimeoutException => HttpStatusCode.RequestTimeout,
        _ => HttpStatusCode.InternalServerError
    };

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status422UnprocessableEntity => "Validation Error",
        StatusCodes.Status408RequestTimeout => "Request Timeout",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => "Error"
    };

    #endregion
}
