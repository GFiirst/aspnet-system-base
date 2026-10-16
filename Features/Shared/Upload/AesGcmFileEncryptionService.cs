using System.Security.Cryptography;
using Microsoft.Extensions.Options;

public sealed class AesGcmFileEncryptionService : IFileEncryptionService, IDisposable
{
    private static readonly byte[] Magic = "SYSBENC1"u8.ToArray();
    private static readonly byte[] KekAssociatedData =
        "SYSTEM-BASE-FILE-DEK-V1"u8.ToArray();

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int DekSize = 32;
    private const int HeaderSize =
        8 + NonceSize + DekSize + TagSize + NonceSize + TagSize;

    private readonly byte[] _kek;

    public AesGcmFileEncryptionService(IOptions<FileEncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _kek = Convert.FromBase64String(options.Value.Kek);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "FileEncryption:Kek deve ser uma chave de 32 bytes codificada em Base64.",
                exception);
        }

        if (_kek.Length != DekSize)
        {
            CryptographicOperations.ZeroMemory(_kek);
            throw new InvalidOperationException(
                "FileEncryption:Kek deve ser uma chave de 32 bytes codificada em Base64.");
        }
    }

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext)
    {
        var dek = RandomNumberGenerator.GetBytes(DekSize);

        try
        {
            var kekNonce = RandomNumberGenerator.GetBytes(NonceSize);
            var encryptedDek = new byte[DekSize];
            var kekTag = new byte[TagSize];

            using (var kekCipher = new AesGcm(_kek, TagSize))
            {
                kekCipher.Encrypt(
                    kekNonce,
                    dek,
                    encryptedDek,
                    kekTag,
                    KekAssociatedData);
            }

            var contentNonce = RandomNumberGenerator.GetBytes(NonceSize);
            var contentTag = new byte[TagSize];
            var ciphertext = new byte[plaintext.Length];
            var contentAssociatedData = BuildContentAssociatedData(
                kekNonce,
                encryptedDek,
                kekTag);

            using (var contentCipher = new AesGcm(dek, TagSize))
            {
                contentCipher.Encrypt(
                    contentNonce,
                    plaintext,
                    ciphertext,
                    contentTag,
                    contentAssociatedData);
            }

            var result = new byte[HeaderSize + ciphertext.Length];
            var offset = 0;
            CopyToResult(Magic, result, ref offset);
            CopyToResult(kekNonce, result, ref offset);
            CopyToResult(encryptedDek, result, ref offset);
            CopyToResult(kekTag, result, ref offset);
            CopyToResult(contentNonce, result, ref offset);
            CopyToResult(contentTag, result, ref offset);
            ciphertext.CopyTo(result, offset);

            return result;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public byte[] Decrypt(ReadOnlySpan<byte> encryptedFile)
    {
        if (encryptedFile.Length < HeaderSize
            || !encryptedFile[..Magic.Length].SequenceEqual(Magic))
        {
            throw new InvalidDataException(
                "O arquivo não possui um formato criptografado válido.");
        }

        var offset = Magic.Length;
        var kekNonce = encryptedFile.Slice(offset, NonceSize);
        offset += NonceSize;
        var encryptedDek = encryptedFile.Slice(offset, DekSize);
        offset += DekSize;
        var kekTag = encryptedFile.Slice(offset, TagSize);
        offset += TagSize;
        var contentNonce = encryptedFile.Slice(offset, NonceSize);
        offset += NonceSize;
        var contentTag = encryptedFile.Slice(offset, TagSize);
        offset += TagSize;
        var ciphertext = encryptedFile[offset..];

        var dek = new byte[DekSize];
        byte[]? plaintext = null;

        try
        {
            using (var kekCipher = new AesGcm(_kek, TagSize))
            {
                kekCipher.Decrypt(
                    kekNonce,
                    encryptedDek,
                    kekTag,
                    dek,
                    KekAssociatedData);
            }

            plaintext = new byte[ciphertext.Length];
            var contentAssociatedData = BuildContentAssociatedData(
                kekNonce,
                encryptedDek,
                kekTag);

            using (var contentCipher = new AesGcm(dek, TagSize))
            {
                contentCipher.Decrypt(
                    contentNonce,
                    ciphertext,
                    contentTag,
                    plaintext,
                    contentAssociatedData);
            }

            return plaintext;
        }
        catch (AuthenticationTagMismatchException exception)
        {
            if (plaintext is not null)
                CryptographicOperations.ZeroMemory(plaintext);

            throw new InvalidDataException(
                "Não foi possível autenticar o arquivo criptografado.",
                exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dek);
        }
    }

    public void Dispose()
    {
        CryptographicOperations.ZeroMemory(_kek);
    }

    private static byte[] BuildContentAssociatedData(
        ReadOnlySpan<byte> kekNonce,
        ReadOnlySpan<byte> encryptedDek,
        ReadOnlySpan<byte> kekTag)
    {
        var associatedData = new byte[
            Magic.Length + NonceSize + DekSize + TagSize];
        var offset = 0;
        CopyToResult(Magic, associatedData, ref offset);
        CopyToResult(kekNonce, associatedData, ref offset);
        CopyToResult(encryptedDek, associatedData, ref offset);
        CopyToResult(kekTag, associatedData, ref offset);
        return associatedData;
    }

    private static void CopyToResult(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        ref int offset)
    {
        source.CopyTo(destination[offset..]);
        offset += source.Length;
    }
}
