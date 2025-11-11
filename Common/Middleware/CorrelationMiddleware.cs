using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AdvancedNetExercise.Common.Middleware;

public class CorrelationMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) || string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
            _logger.LogDebug("Generated new Correlation ID: {CorrelationId}", correlationId);
        }
        
        
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        
        using (_logger.BeginScope(new 
        { 
            CorrelationId = correlationId.ToString() 
        }))
        {
            // 4. Process the rest of the pipeline
            await _next(context);
        }
    }
}

public static class CorrelationMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationMiddleware>();
    }
}