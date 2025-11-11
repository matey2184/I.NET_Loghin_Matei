using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AdvancedNetExercise.Features.Products;

namespace AdvancedNetExercise.Validators.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ProductCategoryAttribute : ValidationAttribute
{
    private readonly ProductCategory[] _allowedCategories;
    

    public ProductCategoryAttribute(params ProductCategory[] allowedCategories)
    {
        _allowedCategories = allowedCategories ?? Array.Empty<ProductCategory>();
        
        
        ErrorMessage = $"Product category must be one of the following: {string.Join(", ", _allowedCategories.Select(c => c.ToString()))}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not ProductCategory category)
        {
            
            return ValidationResult.Success;
        }

        if (_allowedCategories.Contains(category))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}