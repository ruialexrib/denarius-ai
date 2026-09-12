using Microsoft.AspNetCore.Http.Features;

namespace DenariusAI.Web.Api;

/// <summary>Enforces transport, bounded requests and JSON failures only within the API boundary.</summary>
/// <param name="next">The remaining request pipeline.</param>
/// <param name="logger">The logger for non-sensitive failure diagnostics.</param>
public sealed class ApiBoundaryMiddleware(RequestDelegate next, ILogger<ApiBoundaryMiddleware> logger)
{
    /// <summary>The maximum size of an API request body in bytes.</summary>
    public const long MaximumRequestBodySize = 64 * 1024;

    /// <summary>Processes an API request without exposing MVC redirects or exception details.</summary>
    /// <param name="context">The current request context.</param>
    /// <returns>A task representing request completion.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!context.Request.IsHttps)
        {
            await ApiProblems.WriteAsync(context, StatusCodes.Status400BadRequest, "https_required");
            return;
        }

        var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (limit is { IsReadOnly: false })
            limit.MaxRequestBodySize = MaximumRequestBodySize;
        if (context.Request.ContentLength > MaximumRequestBodySize)
        {
            await ApiProblems.WriteAsync(context, StatusCodes.Status413PayloadTooLarge);
            return;
        }

        try
        {
            await next(context);
            if (context.Response.StatusCode >= 400 && !context.Response.HasStarted
                && context.Response.ContentType is null)
                await ApiProblems.WriteAsync(context, context.Response.StatusCode);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Abort();
        }
        catch (BadHttpRequestException exception) when (!context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.Headers.CacheControl = "no-store";
            await ApiProblems.WriteAsync(context, exception.StatusCode);
        }
        catch (Exception) when (!context.Response.HasStarted)
        {
            // Do not log exception messages: downstream services may include submitted data.
            logger.LogError("API request failed. Trace identifier: {TraceId}", context.TraceIdentifier);
            context.Response.Clear();
            context.Response.Headers.CacheControl = "no-store";
            await ApiProblems.WriteAsync(context, StatusCodes.Status500InternalServerError);
        }
    }
}
