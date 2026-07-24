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
        var uploadPath = Path.Combine(_options.UploadPath, subdirectory);
        
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
        var fullPath = Path.Combine(_options.UploadPath, relativePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}