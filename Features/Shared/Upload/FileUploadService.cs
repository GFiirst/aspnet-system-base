using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

public class FileUploadService : IFileUploadService
{
    private readonly FileUploadOptions _options;
    private readonly IFileEncryptionService _fileEncryptionService;

    public FileUploadService(
        IOptions<FileUploadOptions> options,
        IFileEncryptionService fileEncryptionService)
    {
        _options = options.Value;
        _fileEncryptionService = fileEncryptionService;
    }

    public async Task<FileUploadResult> SaveAsync(IFormFile file, string subdirectory)
    {
        var uploadPath = GetSafePath(subdirectory);
        
        Directory.CreateDirectory(uploadPath);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = Guid.NewGuid() + extension;

        var fullPath = Path.Combine(uploadPath, fileName);

        await using var plaintextStream = new MemoryStream();
        await file.CopyToAsync(plaintextStream);
        var plaintext = plaintextStream.ToArray();

        try
        {
            var encryptedFile = _fileEncryptionService.Encrypt(plaintext);
            await File.WriteAllBytesAsync(fullPath, encryptedFile);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }

        var provider = new FileExtensionContentTypeProvider();
        var mimeType = provider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";

        return new FileUploadResult
        {
            OriginalName = file.FileName,
            FileName = fileName,
            RelativePath = Path.Combine(subdirectory, fileName),
            MimeType = mimeType,
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

    public async Task<(Stream Stream, string ContentType)> GetFileStreamAsync(
        string relativePath)
    {
        var fullPath = GetSafePath(relativePath);

        if (!File.Exists(fullPath))
        {
            throw new NotFoundException("Arquivo não encontrado.");
        }

        var provider = new FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(fullPath, out var detectedContentType)
            ? detectedContentType
            : "application/octet-stream";
        var encryptedFile = await File.ReadAllBytesAsync(fullPath);
        var plaintext = _fileEncryptionService.Decrypt(encryptedFile);
        Stream stream = new MemoryStream(plaintext, writable: false);

        return (stream, contentType);
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
