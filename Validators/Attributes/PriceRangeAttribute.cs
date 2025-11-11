using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace AdvancedNetExercise.Validators.Attributes;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class PriceRangeAttribute : ValidationAttribute
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;
    
    
    public PriceRangeAttribute(double minPrice, double maxPrice)
    {
        _minPrice = (decimal)minPrice;
        _maxPrice = (decimal)maxPrice;

        
        var minFormatted = _minPrice.ToString("C2", CultureInfo.CurrentCulture);
        var maxFormatted = _maxPrice.ToString("C2", CultureInfo.CurrentCulture);

        ErrorMessage = $"Price must be between {minFormatted} and {maxFormatted}.";
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not decimal price)
        {
            return ValidationResult.Success;
        }

        if (price >= _minPrice && price <= _maxPrice)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}