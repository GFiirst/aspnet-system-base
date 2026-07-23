using System.ComponentModel.DataAnnotations;

public class CpfValidationAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
        {
            return ValidationResult.Success;
        }

        var cpf = value.ToString();
        
        if (string.IsNullOrEmpty(cpf))
        {
            return ValidationResult.Success;
        }

        if (!CpfValidator.IsValid(cpf))
        {
            return new ValidationResult("CPF inválido.");
        }

        return ValidationResult.Success;
    }
}
