using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdvancedNetExercise.Common.Logging;
using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Features.Products.DTOs;
using AdvancedNetExercise.Common.Mapping;
using AdvancedNetExercise.Validators;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Xunit;
namespace AdvancedNetExercise.Tests;

public class MockLogger<T> : ILogger<T>
{
    
    public List<(LogLevel, int, string, Exception?)> LoggedMessages { get; } = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        
        LoggedMessages.Add((logLevel, eventId.Id, formatter(state, exception), exception));
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}



/// <summary>
/// Integration tests for the CreateProductHandler, verifying mapping, validation, and logging.
/// Implements IDisposable for proper in-memory context cleanup (Task 4.2).
/// </summary>
public class CreateProductHandlerIntegrationTests : IDisposable
{
    private readonly IMapper _mapper;
    private readonly ApplicationContext _dbContext;
    private readonly ICacheService _cache;
    private readonly IProductRepository _repository;
    private readonly MockLogger<CreateProductHandler> _handlerLogger;
    private readonly IValidator<CreateProductProfileRequest> _validator;

    public CreateProductHandlerIntegrationTests()
    {
        
    
        _dbContext = new ApplicationContext(); 
        
        _cache = new MockCacheService();
        _repository = new MockProductRepository();
        
        
        var mappingConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new AdvancedProductMappingProfile());
        });
        _mapper = mappingConfig.CreateMapper();

        
        _handlerLogger = new MockLogger<CreateProductHandler>();
        
        
        var validatorLogger = new MockLogger<CreateProductProfileValidator>();
        _validator = new CreateProductProfileValidator(_dbContext, validatorLogger);
    }

    public void Dispose()
    {
        
        _dbContext.Products.Clear();
        GC.SuppressFinalize(this);
    }

    
    [Fact]
    public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
    {
        
        var request = new CreateProductProfileRequest
        {
            Name = "Quantum Super Display",
            Brand = "Nova Tech", 
            SKU = "ELEC-QSD-001",
            Category = ProductCategory.Electronics,
            Price = 999.99m,
            ReleaseDate = DateTime.Today.AddDays(-65), // ~2 months old
            ImageUrl = "https://example.com/image.jpg",
            StockQuantity = 15
        };

        var handler = new CreateProductHandler(_mapper, _handlerLogger, _repository, _validator, _cache);

        
        var result = await handler.Handle(request, CancellationToken.None);

        
        Assert.Equal("Electronics & Technology", result.CategoryDisplayName);
        Assert.Equal("NT", result.BrandInitials); 
        Assert.Equal("2 months old", result.ProductAge);
        Assert.Equal("In Stock", result.AvailabilityStatus); 
        Assert.Contains("999.99", result.FormattedPrice); 

        
        Assert.Contains(_handlerLogger.LoggedMessages, l => l.Item2 == LogEvents.ProductCreationStarted);
        Assert.Contains(_handlerLogger.LoggedMessages, l => l.Item2 == LogEvents.ProductCreationCompleted); // Success metrics log
    }

    
    // Test 2: Verifies async SKU validation and failure logging
    [Fact]
    public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
    {
        
        const string duplicateSku = "DUP-SKU-123";
        _dbContext.Products.Add(new Product { Name = "Existing Product", Brand = "Old Brand", SKU = duplicateSku, CreatedAt = DateTime.UtcNow });
        
        
        var request = new CreateProductProfileRequest
        {
            Name = "New Item", Brand = "New Brand", SKU = duplicateSku, Category = ProductCategory.Books, Price = 10.00m, ReleaseDate = DateTime.Today, StockQuantity = 1
        };

        var handler = new CreateProductHandler(_mapper, _handlerLogger, _repository, _validator, _cache);
        
        
        var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(request, CancellationToken.None));
        
        
        Assert.Contains("already in use", exception.Message);

        
        Assert.Contains(_handlerLogger.LoggedMessages, l => l.Item2 == LogEvents.ProductValidationFailed);
    }

    
    // Test 3: Verifies conditional mapping logic for a Home product (discount and null ImageUrl)
    [Fact]
    public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
    {
        
        var originalPrice = 100.00m;
        var expectedPrice = originalPrice * 0.9m; 
        var request = new CreateProductProfileRequest
        {
            Name = "Luxury Scented Candle",
            Brand = "HomeGoods",
            SKU = "HOME-CAN-001",
            Category = ProductCategory.Home,
            Price = originalPrice,
            ReleaseDate = DateTime.Today,
            ImageUrl = "https://example.com/home-image.jpg",
            StockQuantity = 8
        };

        var handler = new CreateProductHandler(_mapper, _handlerLogger, _repository, _validator, _cache);

        
        var result = await handler.Handle(request, CancellationToken.None);

        
        Assert.Equal("Home & Garden", result.CategoryDisplayName);

        Assert.Null(result.ImageUrl);

    
        Assert.Equal(expectedPrice, result.Price);
        Assert.Contains("90.00", result.FormattedPrice);
        Assert.Equal("In Stock", result.AvailabilityStatus);
    }
}