using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AdvancedNetExercise.Validators.Attributes;
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
{
    
    private readonly Regex _skuRegex = new Regex(@"^[a-zA-Z0-9\-]{5,20}$", RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string sku)
        {
            return ValidationResult.Success;
        }

        sku = sku.Replace(" ", "");

        if (_skuRegex.IsMatch(sku))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage ?? "SKU must be alphanumeric with hyphens and 5-20 characters long.");
    }
    public void AddValidation(ClientModelValidationContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        context.Attributes.Add("data-val", "true");
        context.Attributes.Add("data-val-validsku", ErrorMessage ?? "SKU format is invalid.");
        context.Attributes.Add("data-val-validsku-pattern", _skuRegex.ToString());
    }
}