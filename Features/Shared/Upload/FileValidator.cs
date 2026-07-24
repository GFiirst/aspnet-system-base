using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

public class FileValidator : IFileValidator
{
    private readonly FileUploadOptions _options;

    public FileValidator(IOptions<FileUploadOptions> options)
    {
        _options = options.Value;
    }

    public void Validate([NotNull] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            throw new BadRequestException("Arquivo obrigatório.");

        if (file.Length > _options.MaxSize)
            throw new BadRequestException("Arquivo muito grande.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_options.AllowedExtensions.Contains(extension))
            throw new BadRequestException("Extensão inválida.");

        if (!_options.AllowedMimeTypes.Contains(file.ContentType))
            throw new BadRequestException("Tipo inválido.");
    }
}