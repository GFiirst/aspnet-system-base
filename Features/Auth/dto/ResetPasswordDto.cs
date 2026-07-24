using System.ComponentModel.DataAnnotations;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "O token é obrigatorio")]
    public string Token { get; set; } = "";

    [Required(ErrorMessage = "A nova senha é obrigatoria")]
    [MaxLength(
        255,
        ErrorMessage = "{0} deve conter no maximo {1} caracteres."
    )]
    [MinLength(5, ErrorMessage = "A senha deve ter no minimo 5 caracteres.")]
    public string NewPassword { get; set; } = "";
}
