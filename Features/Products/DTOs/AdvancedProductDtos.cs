using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Validators.Attributes;

namespace AdvancedNetExercise.Features.Products.DTOs;

// --- DTO for Response (Outbound) ---
public record ProductProfileDto(
    Guid Id, 
    string Name, 
    string Brand, 
    [ValidSKU] string SKU, 
    string CategoryDisplayName, 
    decimal Price, 
    string FormattedPrice, 
    DateTime ReleaseDate, 
    DateTime CreatedAt, 
    string? ImageUrl, 
    bool IsAvailable, 
    int StockQuantity, 
    string ProductAge, // Custom mapped: e.g., "1 year, 2 months old"
    string BrandInitials, // Custom mapped: e.g., "TC"
    string AvailabilityStatus // Custom mapped: e.g., "Low Stock"
);


public record CreateProductProfileRequest(
    string Name,
    string Brand,
    string SKU,
    [ProductCategory] ProductCategory Category,
    [PriceRange] decimal Price, 
    DateTime ReleaseDate,
    string? ImageUrl,
    int StockQuantity = 1
);