using FintechPayments.Api.Diagnostics;

namespace FintechPayments.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = CorrelationId.Normalize(
            context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault());

        context.TraceIdentifier = correlationId;
        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationId.ItemKey] = correlationId,
        }))
        {
            await next(context);
        }
    }
}
