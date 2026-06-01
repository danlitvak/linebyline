using System.Security.Cryptography;
using System.Text;

namespace LineByLine.App.Crypto;

public static class KeyDeriver
{
    public const string KdfName = "PBKDF2-SHA256";
    public const int Iterations = 300_000;
    public const int KeyLength = 32; // AES-256
    public const int SaltLength = 32;

    public static byte[] GenerateSalt()
    {
        var salt = new byte[SaltLength];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public static byte[] DeriveKey(string passphrase, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeyLength);
    }
}
