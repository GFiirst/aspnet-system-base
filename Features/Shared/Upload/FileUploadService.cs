using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

public class FileUploadService : IFileUploadService
{
    private readonly FileUploadOptions _options;

    public FileUploadService(IOptions<FileUploadOptions> options)
    {
        _options = options.Value;
    }

    public async Task<FileUploadResult> SaveAsync(IFormFile file, string subdirectory)
    {
        var uploadPath = GetSafePath(subdirectory);
        
        Directory.CreateDirectory(uploadPath);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        var fullPath = Path.Combine(uploadPath, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);

        await file.CopyToAsync(stream);

        return new FileUploadResult
        {
            OriginalName = file.FileName,
            FileName = fileName,
            RelativePath = Path.Combine(subdirectory, fileName),
            MimeType = file.ContentType,
            Size = file.Length
        };
    }

    public Task DeleteAsync(string relativePath)
    {
        var fullPath = GetSafePath(relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<(Stream Stream, string ContentType)> GetFileStreamAsync(string relativePath)
    {
        var fullPath = GetSafePath(relativePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Arquivo não encontrado.", fullPath);
        }

        var provider = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(fullPath, out var detectedContentType)
            ? detectedContentType
            : "application/octet-stream";
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Task.FromResult((stream, contentType));
    }

    private string GetSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("O caminho do arquivo deve ser relativo.", nameof(relativePath));
        }

        var uploadRoot = Path.GetFullPath(_options.UploadPath);
        var fullPath = Path.GetFullPath(Path.Combine(uploadRoot, relativePath));
        var rootWithSeparator = uploadRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("O caminho informado está fora da pasta de uploads.");
        }

        return fullPath;
    }
}
