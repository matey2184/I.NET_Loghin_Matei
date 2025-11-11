using System;
using AdvancedNetExercise.Features.Products;
using Microsoft.Extensions.Logging;

namespace AdvancedNetExercise.Common.Logging;

public static class LoggingExtensions
{
    
    public static void LogProductCreationStarted(this ILogger logger, string name, string brand, string sku, ProductCategory category)
    {
        logger.LogInformation(
            LogEvents.ProductCreationStarted, 
            "Product creation operation started for Name: {ProductName}, Brand: {Brand}, Category: {Category}, SKU: {SKU}", 
            name, brand, category, sku);
    }
    
    public static void LogDatabaseOperationStarted(this ILogger logger, Guid productId)
    {
        logger.LogDebug(LogEvents.DatabaseOperationStarted, "Database operation started for ProductId: {ProductId}", productId);
    }
    
    public static void LogDatabaseOperationCompleted(this ILogger logger, Guid productId)
    {
        logger.LogDebug(LogEvents.DatabaseOperationCompleted, "Database operation completed for ProductId: {ProductId}", productId);
    }
    
    public static void LogCacheOperationPerformed(this ILogger logger, string cacheKey)
    {
        logger.LogDebug(LogEvents.CacheOperationPerformed, "Cache invalidation performed for key: {CacheKey}", cacheKey);
    }

        public static void LogProductCreationMetrics(this ILogger logger, ProductCreationMetrics metrics)
    {
        var logLevel = metrics.Success ? LogLevel.Information : LogLevel.Warning;
        var eventId = metrics.Success ? LogEvents.ProductCreationCompleted : LogEvents.ProductValidationFailed;
        
        var message = metrics.Success 
            ? "Product Creation Success. Metrics: Total {TotalDurationMs}ms, Validation {ValidationDurationMs}ms, DB {DatabaseSaveDurationMs}ms."
            : "Product Creation Failed. Reason: {ErrorReason}. Metrics: Total {TotalDurationMs}ms, Validation {ValidationDurationMs}ms, DB {DatabaseSaveDurationMs}ms.";

        
        logger.Log(
            logLevel,
            eventId,
            message,
            metrics.TotalDuration.TotalMilliseconds,
            metrics.ValidationDuration.TotalMilliseconds,
            metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.ErrorReason
        );
        
        
        logger.Log(LogLevel.Trace, "StructuredProductMetrics", new 
        {
            metrics.OperationId,
            metrics.ProductName,
            metrics.SKU,
            metrics.Category,
            TotalDurationMs = metrics.TotalDuration.TotalMilliseconds,
            ValidationDurationMs = metrics.ValidationDuration.TotalMilliseconds,
            DatabaseSaveDurationMs = metrics.DatabaseSaveDuration.TotalMilliseconds,
            metrics.Success,
            metrics.ErrorReason
        });
    }
}