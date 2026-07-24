using System.Diagnostics.CodeAnalysis;

public interface IFileValidator
{
    void Validate([NotNull] IFormFile? file);
}