using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace SolmangoAPI.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private const string API_KEY_ID = "Api-Key";
    private readonly RequestDelegate next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        var logger = httpContext.RequestServices.GetRequiredService<ILogger<ApiKeyAuthenticationMiddleware>>();
        var endpoint = httpContext.GetEndpoint();
        var isAllowAnonymous = endpoint?.Metadata.Any(x => x.GetType() == typeof(AllowAnonymousAttribute));
        if (isAllowAnonymous == true)
        {
            await next(httpContext);
            return;
        }
        var conf = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var keys = conf.GetSection("Security:ApiKeys").Get<string[]>();
        if (httpContext.Request.Headers.TryGetValue(API_KEY_ID, out StringValues value))
        {
            if (keys.Contains((string)value))
            {
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await next(httpContext);
                return;
            }
        }
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        await httpContext.Response.WriteAsync("Not authorized");
        return;
    }
}