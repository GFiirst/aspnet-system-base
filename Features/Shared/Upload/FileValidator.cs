using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Options;

public class FileValidator : IFileValidator
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] OleSignature =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
    private static readonly UTF8Encoding StrictUtf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private static readonly IReadOnlyDictionary<string, string[]> MimeTypesByExtension =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".gif"] = ["image/gif"],
            [".webp"] = ["image/webp"],
            [".doc"] = ["application/msword"],
            [".docx"] =
                ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] =
                ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".txt"] = ["text/plain"]
        };

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

        if (!_options.AllowedExtensions.Contains(
                extension,
                StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Extensão inválida.");

        var contentType = file.ContentType.Split(';', 2)[0].Trim();

        if (!_options.AllowedMimeTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
            throw new BadRequestException("Tipo inválido.");

        if (!MimeTypesByExtension.TryGetValue(extension, out var expectedMimeTypes)
            || !expectedMimeTypes.Contains(
                contentType,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException(
                "A extensão não corresponde ao tipo do arquivo.");
        }

        using var stream = file.OpenReadStream();

        if (!HasValidContent(extension, stream))
        {
            throw new BadRequestException(
                "O conteúdo não corresponde ao tipo de arquivo informado.");
        }
    }

    private static bool HasValidContent(string extension, Stream stream) =>
        extension switch
        {
            ".pdf" => StartsWith(stream, PdfSignature),
            ".jpg" or ".jpeg" => StartsWith(stream, JpegSignature),
            ".png" => StartsWith(stream, PngSignature),
            ".gif" => HasGifSignature(stream),
            ".webp" => HasWebpSignature(stream),
            ".doc" or ".xls" => StartsWith(stream, OleSignature),
            ".docx" => HasOpenXmlStructure(stream, "word/document.xml"),
            ".xlsx" => HasOpenXmlStructure(stream, "xl/workbook.xml"),
            ".txt" => HasValidTextContent(stream),
            _ => false
        };

    private static bool StartsWith(Stream stream, ReadOnlySpan<byte> signature)
    {
        Span<byte> header = stackalloc byte[signature.Length];
        var bytesRead = 0;

        while (bytesRead < header.Length)
        {
            var read = stream.Read(header[bytesRead..]);

            if (read == 0)
                break;

            bytesRead += read;
        }

        return bytesRead == header.Length && header.SequenceEqual(signature);
    }

    private static bool HasGifSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[6];
        return ReadHeader(stream, header)
            && (header.SequenceEqual("GIF87a"u8)
                || header.SequenceEqual("GIF89a"u8));
    }

    private static bool HasWebpSignature(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        return ReadHeader(stream, header)
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..].SequenceEqual("WEBP"u8);
    }

    private static bool ReadHeader(Stream stream, Span<byte> header)
    {
        var bytesRead = 0;

        while (bytesRead < header.Length)
        {
            var read = stream.Read(header[bytesRead..]);

            if (read == 0)
                return false;

            bytesRead += read;
        }

        return true;
    }

    private static bool HasOpenXmlStructure(Stream stream, string mainEntry)
    {
        try
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;

            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") != null
                && archive.GetEntry("_rels/.rels") != null
                && archive.GetEntry(mainEntry) != null;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool HasValidTextContent(Stream stream)
    {
        try
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();
            return !bytes.Contains((byte)0) && StrictUtf8.GetString(bytes).Length > 0;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
