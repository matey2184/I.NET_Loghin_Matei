using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdvancedNetExercise.Common.Logging;
using AdvancedNetExercise.Features.Products.DTOs;
using AdvancedNetExercise.Features.Products;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FluentValidation;
namespace AdvancedNetExercise.Features.Products;

public interface ICacheService
{
    void Invalidate(string key);
}

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);
}

public class MockCacheService : ICacheService
{
    public void Invalidate(string key) { /* Simulation */ }
}

public class MockProductRepository : IProductRepository
{
    public Task AddAsync(Product product, CancellationToken cancellationToken) => Task.CompletedTask;
}





public class CreateProductHandler
{
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductHandler> _logger;
    private readonly IProductRepository _repository;
    private readonly IValidator<CreateProductProfileRequest> _validator;
    private readonly ICacheService _cache;
    private const string AllProductsCacheKey = "all_products";

    public CreateProductHandler(
        IMapper mapper, 
        ILogger<CreateProductHandler> logger, 
        IProductRepository repository, 
        IValidator<CreateProductProfileRequest> validator,
        ICacheService cache)
    {
        _mapper = mapper;
        _logger = logger;
        _repository = repository;
        _validator = validator;
        _cache = cache;
    }

    
    public async Task<ProductProfileDto> Handle(CreateProductProfileRequest request, CancellationToken cancellationToken)
    {
        
        var operationId = Guid.NewGuid().ToString().Substring(0, 8).ToUpperInvariant();
        var totalTimer = Stopwatch.StartNew();
        var validationTimer = new Stopwatch();
        var dbTimer = new Stopwatch();

        
        using (_logger.BeginScope(new Dictionary<string, object> { { "OperationId", operationId } }))
        {
            _logger.LogProductCreationStarted(request.Name, request.Brand, request.SKU, request.Category); 

            Product? product;
            try
            {
                
                validationTimer.Start();
                
                
                var validationResult = await _validator.ValidateAsync(request, cancellationToken);
                
                if (!validationResult.IsValid)
                {
                    
                    throw new ValidationException(validationResult.Errors);
                }

                validationTimer.Stop();
                

                
                product = _mapper.Map<Product>(request); 

                dbTimer.Start();
                
                _logger.LogDatabaseOperationStarted(product.Id); 
                await _repository.AddAsync(product, cancellationToken);
                _logger.LogDatabaseOperationCompleted(product.Id); 
                
                dbTimer.Stop();
                

                
                _cache.Invalidate(AllProductsCacheKey); 
                _logger.LogCacheOperationPerformed(AllProductsCacheKey); 

                
                totalTimer.Stop();
                var dto = _mapper.Map<ProductProfileDto>(product); 
                
                
                _logger.LogProductCreationMetrics(new ProductCreationMetrics(
                    OperationId: operationId,
                    ProductName: request.Name,
                    SKU: request.SKU,
                    Category: request.Category,
                    ValidationDuration: validationTimer.Elapsed,
                    DatabaseSaveDuration: dbTimer.Elapsed,
                    TotalDuration: totalTimer.Elapsed,
                    Success: true
                ));

                return dto; 
            }
            catch (ValidationException)
            {
                
                totalTimer.Stop();
                _logger.LogProductCreationMetrics(new ProductCreationMetrics(
                    OperationId: operationId,
                    ProductName: request.Name,
                    SKU: request.SKU,
                    Category: request.Category,
                    ValidationDuration: validationTimer.Elapsed,
                    DatabaseSaveDuration: dbTimer.Elapsed,
                    TotalDuration: totalTimer.Elapsed,
                    Success: false,
                    ErrorReason: "Validation Failure"
                ));
                throw; 
            }
            catch (Exception ex)
            {
                
                totalTimer.Stop();
                _logger.LogProductCreationMetrics(new ProductCreationMetrics(
                    OperationId: operationId,
                    ProductName: request.Name,
                    SKU: request.SKU,
                    Category: request.Category,
                    ValidationDuration: validationTimer.Elapsed,
                    DatabaseSaveDuration: dbTimer.Elapsed,
                    TotalDuration: totalTimer.Elapsed,
                    Success: false,
                    ErrorReason: ex.Message
                ));
                _logger.LogError(ex, "Product creation failed unexpectedly for SKU {SKU}.", request.SKU);
                throw; 
            }
        }
    }
}