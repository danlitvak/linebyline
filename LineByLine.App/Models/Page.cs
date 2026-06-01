using System;

namespace LineByLine.App.Models;

public sealed class Page
{
    public required string Id { get; init; }
    public required string NotebookId { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
