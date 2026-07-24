public class FileUploadResult
{
    public string OriginalName { get; set; } = default!;

    public string FileName { get; set; } = default!;

    public string RelativePath { get; set; } = default!;

    public string MimeType { get; set; } = default!;

    public long Size { get; set; }
}