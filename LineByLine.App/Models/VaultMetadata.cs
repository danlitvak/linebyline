using System;

namespace LineByLine.App.Models;

public sealed class VaultMetadata
{
    public required string KdfName { get; init; }
    public required byte[] KdfSalt { get; init; }
    public required int KdfIterations { get; init; }
    public required byte[] PasswordCheckNonce { get; init; }
    public required byte[] PasswordCheckCiphertext { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required int SchemaVersion { get; init; }
}
