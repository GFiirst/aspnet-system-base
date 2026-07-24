public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    string ComputeHash(string plainText);
}
