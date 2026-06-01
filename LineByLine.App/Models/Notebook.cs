using System;

namespace LineByLine.App.Models;

public sealed class Notebook
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsDefault { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
