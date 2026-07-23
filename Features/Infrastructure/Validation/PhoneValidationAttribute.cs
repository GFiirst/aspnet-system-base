using System.ComponentModel.DataAnnotations;

public class PhoneValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var phone = value.ToString();
        
        if (string.IsNullOrEmpty(phone))
        {
            return ValidationResult.Success;
        }

        if (!PhoneValidator.IsValid(phone))
        {
            return new ValidationResult("Telefone inválido.");
        }

        return ValidationResult.Success;
    }
}
