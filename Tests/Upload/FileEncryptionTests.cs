using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace AspnetSystemBase.Tests.Upload;

public sealed class FileEncryptionTests
{
    private static readonly string TestKek = Convert.ToBase64String(
        Enumerable.Range(1, 32).Select(value => (byte)value).ToArray());

    [Fact]
    public void Encrypt_deve_produzir_arquivo_autenticado_e_recuperar_conteudo()
    {
        using var service = CreateEncryptionService();
        var plaintext = Encoding.UTF8.GetBytes("conteúdo confidencial");

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        Assert.False(plaintext.SequenceEqual(encrypted));
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_deve_usar_material_aleatorio_em_cada_arquivo()
    {
        using var service = CreateEncryptionService();
        var plaintext = Encoding.UTF8.GetBytes("mesmo conteúdo");

        var first = service.Encrypt(plaintext);
        var second = service.Encrypt(plaintext);

        Assert.False(first.SequenceEqual(second));
        Assert.Equal(plaintext, service.Decrypt(first));
        Assert.Equal(plaintext, service.Decrypt(second));
    }

    [Fact]
    public void Decrypt_deve_rejeitar_arquivo_adulterado()
    {
        using var service = CreateEncryptionService();
        var encrypted = service.Encrypt(Encoding.UTF8.GetBytes("conteúdo"));
        encrypted[^1] ^= 0x01;

        Assert.Throws<InvalidDataException>(() => service.Decrypt(encrypted));
    }

    [Theory]
    [InlineData("nao-e-base64")]
    [InlineData("AQID")]
    public void Construtor_deve_rejeitar_kek_invalida(string kek)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new AesGcmFileEncryptionService(
                Options.Create(new FileEncryptionOptions { Kek = kek })));
    }

    [Fact]
    public async Task Upload_deve_persistir_criptografado_e_ler_descriptografado()
    {
        var uploadPath = Path.Combine(
            Path.GetTempPath(),
            $"system-base-file-encryption-tests-{Guid.NewGuid():N}");
        var plaintext = Encoding.UTF8.GetBytes("arquivo protegido");

        try
        {
            using var encryption = CreateEncryptionService();
            var upload = new FileUploadService(
                Options.Create(new FileUploadOptions { UploadPath = uploadPath }),
                encryption);
            await using var source = new MemoryStream(plaintext);
            var file = new FormFile(source, 0, source.Length, "file", "documento.txt")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/plain"
            };

            var result = await upload.SaveAsync(file, "documents");
            var persisted = await File.ReadAllBytesAsync(
                Path.Combine(uploadPath, result.RelativePath));
            var (stream, contentType) = await upload.GetFileStreamAsync(
                result.RelativePath);
            await using (stream)
            await using (var recovered = new MemoryStream())
            {
                await stream.CopyToAsync(recovered);

                Assert.False(plaintext.SequenceEqual(persisted));
                Assert.Equal(plaintext, recovered.ToArray());
                Assert.Equal("text/plain", contentType);
                Assert.Equal("text/plain", result.MimeType);
                Assert.Equal(plaintext.Length, result.Size);
            }
        }
        finally
        {
            if (Directory.Exists(uploadPath))
                Directory.Delete(uploadPath, recursive: true);

            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static AesGcmFileEncryptionService CreateEncryptionService() =>
        new(Options.Create(new FileEncryptionOptions { Kek = TestKek }));
}
