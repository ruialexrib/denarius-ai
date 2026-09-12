using System.Threading.RateLimiting;
using DenariusAI.Web.Api.Contracts;
using DenariusAI.Web.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DenariusAI.Web.Api;

/// <summary>Defines the versioned API boundary independently of MVC cookie authentication.</summary>
public static class ApiFoundation
{
    /// <summary>The replaceable authentication scheme used only by native API clients.</summary>
    public const string AuthenticationScheme = "DenariusApiBearer";

    /// <summary>The authorization policy required by every protected API route.</summary>
    public const string AuthorizationPolicy = "DenariusApi";

    private const string RateLimitPolicy = "DenariusApiRequests";

    /// <summary>Registers API authentication, authorization and abuse controls.</summary>
    /// <param name="services">The host service collection.</param>
    /// <returns>The collection for composition.</returns>
    public static IServiceCollection AddApiFoundation(this IServiceCollection services)
    {
        services.AddAuthentication().AddBearerToken(AuthenticationScheme, options =>
            options.BearerTokenExpiration = TimeSpan.FromMinutes(10));
        services.AddAuthorizationBuilder().AddPolicy(AuthorizationPolicy, policy =>
            policy.AddAuthenticationSchemes(AuthenticationScheme).RequireAuthenticatedUser());
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await ApiProblems.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests);
            };
        });
        return services;
    }

    /// <summary>Maps the protected API group and minimal anonymous availability probes.</summary>
    /// <param name="endpoints">The host endpoint builder.</param>
    /// <returns>The versioned group to which future API modules must attach their endpoints.</returns>
    public static RouteGroupBuilder MapDenariusApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api")
            .RequireAuthorization(AuthorizationPolicy)
            .RequireRateLimiting(RateLimitPolicy);
        var v1 = api.MapGroup("/v1");
        v1.MapGet("/info", (ApplicationInfo application) =>
            new ApiInfoResponse("1", application.Version));
        v1.MapGet("/health/live", () => new ApiHealthResponse("ok")).AllowAnonymous();
        v1.MapGet("/health/ready", ReadinessAsync).AllowAnonymous();
        // Reserve the entire namespace, including unsupported versions, from MVC routing.
        api.MapFallback("/{**path}", (HttpContext context) =>
            ApiProblems.WriteAsync(context, StatusCodes.Status404NotFound));
        return v1;
    }

    /// <summary>Reports readiness based on registered dependency checks without exposing their output.</summary>
    /// <param name="context">The current request context.</param>
    /// <param name="healthChecks">The host health check service.</param>
    /// <returns>A task representing the aggregate readiness response.</returns>
    private static async Task ReadinessAsync(HttpContext context, HealthCheckService healthChecks)
    {
        var report = await healthChecks.CheckHealthAsync(context.RequestAborted);
        if (report.Status != HealthStatus.Healthy)
        {
            await ApiProblems.WriteAsync(context, StatusCodes.Status503ServiceUnavailable);
            return;
        }
        await context.Response.WriteAsJsonAsync(new ApiHealthResponse("ready"), context.RequestAborted);
    }
}
