using System.Security.Cryptography;
using System.Text;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string encryptionKey)
    {
        if (string.IsNullOrEmpty(encryptionKey))
        {
            throw new ArgumentException("Encryption key cannot be null or empty.");
        }

        _key = Encoding.UTF8.GetBytes(encryptionKey);

        if (_key.Length != 32)
        {
            throw new ArgumentException("Encryption key must be exactly 32 bytes (256 bits) for AES-256.");
        }
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
        {
            using var swEncrypt = new StreamWriter(csEncrypt);
            swEncrypt.Write(plainText);
        }

        var iv = aes.IV;
        var encrypted = msEncrypt.ToArray();

        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        using var decryptor = aes.CreateDecryptor(_key, iv);
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }

    public string ComputeHash(string plainText)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}
