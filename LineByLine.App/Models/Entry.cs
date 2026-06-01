using System;

namespace LineByLine.App.Models;

public sealed class Entry
{
    public required string Id { get; init; }
    public required string NotebookId { get; init; }
    public string? PageId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UnlockAt { get; init; }
    public required byte[] EncryptedText { get; init; }
    public required byte[] Nonce { get; init; }
    public required string SealedPreview { get; init; }
    public required int PreviewVersion { get; init; }
    public required bool IsDeleted { get; init; }
}
