using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace DAL.ServiceLayer.Middleware;

public class AnonymousDetectionMiddleware
{
    private readonly RequestDelegate _next;

    public AnonymousDetectionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            context.Items["AllowAnonymous"] = true;
        }

        await _next(context);
    }
}
