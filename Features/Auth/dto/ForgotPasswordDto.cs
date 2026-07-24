using System.ComponentModel.DataAnnotations;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "O email é obrigatorio")]
    [MaxLength(
        255,
        ErrorMessage = "{0} deve conter no maximo {1} caracteres."
    )]
    [EmailAddress(ErrorMessage = "Email invalido")]
    public string Email { get; set; } = "";
}
