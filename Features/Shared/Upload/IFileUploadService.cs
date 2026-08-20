public interface IFileUploadService
{
    Task<FileUploadResult> SaveAsync(IFormFile file, string subdirectory);

    Task DeleteAsync(string relativePath);

    Task<(Stream Stream, string ContentType)> GetFileStreamAsync(string relativePath);
}
