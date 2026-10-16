public interface IFileEncryptionService
{
    byte[] Encrypt(ReadOnlySpan<byte> plaintext);

    byte[] Decrypt(ReadOnlySpan<byte> encryptedFile);
}
