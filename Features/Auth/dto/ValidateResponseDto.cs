public class ValidateResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public List<string> Roles { get; set; } = [];
}
