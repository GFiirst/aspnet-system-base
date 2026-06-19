using System.ComponentModel.DataAnnotations;

public class CreateUserDto
{
    [Required(ErrorMessage = "O nome é obrigatorio")]
    [MaxLength(
        100,
        ErrorMessage = "{0} deve conter no maximo {1} caracteres."
    )]
    public string Name {get; set;} = "";

    [Required(ErrorMessage = "O email é obrigatorio")]
    [MaxLength(
        255,
        ErrorMessage = "{0} deve conter no maximo {1} caracteres."
    )]
    [EmailAddress(ErrorMessage = "Email invalido")]
    public string Email {get; set;} = "";

    [Required(ErrorMessage = "A senha é obrigatoria")]
    [MaxLength(
        255,
        ErrorMessage = "{0} deve conter no maximo {1} caracteres."
    )]
    public string Password {get; set;} = "";
}