using System;
using System.Text;

namespace LineByLine.App.Crypto;

public static class SealedPreviewGenerator
{
    public const int CurrentVersion = 1;

    private static readonly char[] PunctuationPool = ".,!?;:'\"()-_/@#*+=&%$~".ToCharArray();

    public static string Generate(string plaintext, string entryId, int previewVersion)
    {
        var rng = new Random(ComputeSeed(entryId, previewVersion));
        var sb = new StringBuilder(plaintext.Length);

        foreach (var c in plaintext)
        {
            if (c >= 'a' && c <= 'z')
                sb.Append((char)('a' + rng.Next(26)));
            else if (c >= 'A' && c <= 'Z')
                sb.Append((char)('A' + rng.Next(26)));
            else if (c >= '0' && c <= '9')
                sb.Append((char)('0' + rng.Next(10)));
            else if (char.IsPunctuation(c) || char.IsSymbol(c))
                sb.Append(PunctuationPool[rng.Next(PunctuationPool.Length)]);
            else
                sb.Append(c); // preserve spaces, newlines, tabs
        }

        return sb.ToString();
    }

    private static int ComputeSeed(string entryId, int previewVersion)
    {
        var hash = previewVersion * 1_000_003;
        foreach (var c in entryId)
            hash = hash * 31 + c;
        return hash;
    }
}
