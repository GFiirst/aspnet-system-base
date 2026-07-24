public class FileUploadOptions
{
    public string[] AllowedExtensions { get; set; } = [];

    public string[] AllowedMimeTypes { get; set; } = [];

    public long MaxSize { get; set; }

    public string UploadPath { get; set; } = "uploads";
}