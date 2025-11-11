using System;
using System.Linq;
using AutoMapper;
using AdvancedNetExercise.Features.Products;
using AdvancedNetExercise.Features.Products.DTOs;

namespace AdvancedNetExercise.Common.Mapping.Resolvers;

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
        
        
        return mappedPrice.ToString("C2");
    }
}

public class ProductAgeResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        var age = DateTime.UtcNow - source.ReleaseDate;
        var totalDays = (int)age.TotalDays;

        if (totalDays < 30) {
            return "New Release";
        }
        else if (totalDays < 365) 
        {
            int months = (int)Math.Floor(totalDays / 30.0);
            return $"{months} month{(months == 1 ? "" : "s")} old";
        }
        else if (totalDays < 1825) 
        {
            int years = (int)Math.Floor(totalDays / 365.0);
            return $"{years} year{(years == 1 ? "" : "s")} old";
        }
        else 
        {
            return "Classic";
        }
    }
}


public class BrandInitialsResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(source.Brand))
        {
            return "?";
        }

        var words = source.Brand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(w => w.Trim())
                                .ToArray();

        if (words.Length >= 2)
        {
            
            return $"{words.First()[0]}{words.Last()[0]}".ToUpperInvariant();
        }
        else
        {
            
            return words.First()[0].ToString().ToUpperInvariant();
        }
    }
}


public class AvailabilityStatusResolver : IValueResolver<Product, ProductProfileDto, string>
{
    public string Resolve(Product source, ProductProfileDto destination, string destMember, ResolutionContext context)
    {
        if (!source.IsAvailable)
        {
            return "Out of Stock"; 
        }
        
        
        return source.StockQuantity switch
        {
            0 => "Unavailable", 
            1 => "Last Item",
            <= 5 => "Limited Stock",
            _ => "In Stock" 
        };
    }
}