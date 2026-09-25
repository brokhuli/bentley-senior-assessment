using System.ComponentModel.DataAnnotations;

namespace FormsApi.Validation;

// Custom Validation attribute to ensure a DateTimeOffset is 
// in the future
public sealed class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        return value is null ||
               value is DateTime date && date > DateTime.UtcNow;
    }
}