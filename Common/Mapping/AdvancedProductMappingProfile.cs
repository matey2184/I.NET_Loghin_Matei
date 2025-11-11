using System;
using AutoMapper;
using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Features.Products.DTOs;
namespace AdvancedNetExercise.Common.Mapping;


// Assuming custom resolvers are defined here or in an accessible folder.
// Since the environment is limited to a single output block per file, 
// I'll place the resolver logic directly here to ensure it's self-contained 
// and the profile compiles without missing dependencies

#region Custom AutoMapper Value Resolvers



public class CategoryDisplayResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        return source.Category switch
        {
            ProductCategory.Electronics => "Electronics & Technology",
            ProductCategory.Clothing    => "Clothing & Fashion",
            ProductCategory.Books       => "Books & Media",
            ProductCategory.Home        => "Home & Garden",
            _                           => "Uncategorized",
        };
    }
}


public class PriceFormatterResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        
        var mappedPrice = destination.Price == 0 ? source.Price : destination.Price;
        
        return mappedPrice.ToString("C2", System.Globalization.CultureInfo.CurrentCulture);
    }
}


public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var age = DateTime.UtcNow - source.ReleaseDate;
        var totalDays = (int)age.TotalDays;

        if (totalDays < 30) return "New Release";
        if (totalDays < 365) 
        {
            int months = (int)Math.Floor(totalDays / 30.0);
            return $"{months} month{(months == 1 ? "" : "s")} old";
        }
        if (totalDays < 1825) // Less than 5 years
        {
            int years = (int)Math.Floor(totalDays / 365.0);
            return $"{years} year{(years == 1 ? "" : "s")} old";
        }
        return "Classic";
    }
}

public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Brand)) return "?";

        var words = source.Brand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.Trim())
                                .ToArray();

        if (words.Length >= 2)
        {
            return $"{words.First()[0]}{words.Last()[0]}".ToUpperInvariant();
        }
        return words.First()[0].ToString().ToUpperInvariant();
    }
}

public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable || source.StockQuantity <= 0)
        {
            return "Out of Stock";
        }
        
        return source.StockQuantity switch
        {
            1 => "Last Item",
            <= 5 => "Limited Stock",
            _ => "In Stock"
        };
    }
}

#endregion


public class AdvancedProductMappingProfile : Profile
{
    public AdvancedProductMappingProfile()
    {
        
        CreateMap<CreateProductProfileRequest, Product>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0)); 


        CreateMap<Product, ProductProfileDto>()
            
            .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
            .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
            .ForMember(dest => dest.ProductAge, opt => opt.MapFrom<ProductAgeResolver>())
            .ForMember(dest => dest.BrandInitials, opt => opt.MapFrom<BrandInitialsResolver>())
            .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>())

            .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom((src, dest) =>
            {
                if (src.Category == ProductCategory.Home)
                    return null; 
                return src.ImageUrl; 
            }))
        
            .ForMember(dest => dest.Price, opt => opt.MapFrom((src, dest) =>
            {
                if (src.Category == ProductCategory.Home)
                    return src.Price * 0.9m; // Apply 10% discount
                return src.Price; 
            }));
    }
}