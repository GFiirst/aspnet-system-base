using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspnetSystemBase.Tests.Unit.Upload;

public sealed class FileSecurityTests
{
    [Fact]
    public void Validador_deve_aceitar_pdf_com_extensao_mime_e_assinatura_compativeis()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "documento.PDF",
            "application/pdf; charset=binary",
            "%PDF-1.4\n%%EOF"u8.ToArray());

        var exception = Record.Exception(() => validator.Validate(file));

        Assert.Null(exception);
    }

    [Fact]
    public void Validador_deve_rejeitar_extensao_e_mime_incompativeis()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "documento.pdf",
            "image/png",
            "%PDF-1.4"u8.ToArray());

        var exception = Assert.Throws<BadRequestException>(() =>
            validator.Validate(file));

        Assert.Equal(
            "A extensão não corresponde ao tipo do arquivo.",
            exception.Message);
    }

    [Fact]
    public void Validador_deve_rejeitar_pdf_sem_assinatura_real()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "documento.pdf",
            "application/pdf",
            "arquivo falso"u8.ToArray());

        Assert.Throws<BadRequestException>(() => validator.Validate(file));
    }

    [Fact]
    public void Validador_deve_aceitar_docx_com_estrutura_minima_do_word()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "documento.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            CreateOpenXml("word/document.xml"));

        var exception = Record.Exception(() => validator.Validate(file));

        Assert.Null(exception);
    }

    [Fact]
    public void Validador_deve_rejeitar_docx_sem_estrutura_do_word()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "documento.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            CreateOpenXml("arquivo.txt"));

        Assert.Throws<BadRequestException>(() => validator.Validate(file));
    }

    [Fact]
    public void Validador_deve_rejeitar_texto_com_conteudo_binario()
    {
        var validator = CreateValidator();
        var file = FormFile(
            "anotacoes.txt",
            "text/plain",
            [0x41, 0x00, 0x42]);

        Assert.Throws<BadRequestException>(() => validator.Validate(file));
    }

    [Fact]
    public async Task Armazenamento_deve_rejeitar_caminho_fora_da_raiz_de_upload()
    {
        var uploadRoot = Path.Combine(
            Path.GetTempPath(),
            $"system-base-path-test-{Guid.NewGuid():N}");
        using var encryption = CreateEncryption();
        var service = new FileUploadService(
            Options.Create(new FileUploadOptions { UploadPath = uploadRoot }),
            encryption);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DeleteAsync("../fora-da-raiz.pdf"));
    }

    private static FileValidator CreateValidator() => new(Options.Create(
        new FileUploadOptions
        {
            AllowedExtensions = [".pdf", ".png", ".docx", ".txt"],
            AllowedMimeTypes =
            [
                "application/pdf",
                "image/png",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "text/plain"
            ],
            MaxSize = 10 * 1024 * 1024
        }));

    private static AesGcmFileEncryptionService CreateEncryption() => new(
        Options.Create(new FileEncryptionOptions
        {
            Kek = Convert.ToBase64String(
                Enumerable.Range(1, 32).Select(value => (byte)value).ToArray())
        }));

    private static FormFile FormFile(
        string name,
        string contentType,
        byte[] content) => new(
            new MemoryStream(content),
            0,
            content.Length,
            "file",
            name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private static byte[] CreateOpenXml(string mainEntry)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(
            stream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", "<Types />");
            WriteEntry(archive, "_rels/.rels", "<Relationships />");
            WriteEntry(archive, mainEntry, "<document />");
        }

        return stream.ToArray();
    }

    private static void WriteEntry(
        ZipArchive archive,
        string name,
        string content)
    {
        using var writer = new StreamWriter(
            archive.CreateEntry(name).Open(),
            Encoding.UTF8);
        writer.Write(content);
    }
}
