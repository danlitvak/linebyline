using System;
using System.Security.Cryptography;
using System.Text;

namespace LineByLine.App.Crypto;

public static class AesGcmCrypto
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    // Returns nonce (12 bytes) and ciphertext+tag as separate values
    public static (byte[] nonce, byte[] ciphertextWithTag) Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var combined = new byte[ciphertext.Length + TagSize];
        ciphertext.CopyTo(combined, 0);
        tag.CopyTo(combined, ciphertext.Length);

        return (nonce, combined);
    }

    // Returns decrypted plaintext, or throws AuthenticationTagMismatchException on wrong key
    public static byte[] Decrypt(byte[] nonce, byte[] ciphertextWithTag, byte[] key)
    {
        var ciphertext = ciphertextWithTag[..^TagSize];
        var tag = ciphertextWithTag[^TagSize..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public static (byte[] nonce, byte[] ciphertextWithTag) EncryptString(string plaintext, byte[] key)
        => Encrypt(Encoding.UTF8.GetBytes(plaintext), key);

    public static string DecryptString(byte[] nonce, byte[] ciphertextWithTag, byte[] key)
        => Encoding.UTF8.GetString(Decrypt(nonce, ciphertextWithTag, key));
}
