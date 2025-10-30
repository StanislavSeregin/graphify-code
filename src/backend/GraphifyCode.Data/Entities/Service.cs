using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;

namespace GraphifyCode.Data.Entities;

[MarkdownSerializable]
public partial class Service
{
    [MarkdownIgnore]
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }

    [MarkdownIgnore]
    public Endpoints? Endpoints { get; set; }

    [MarkdownIgnore]
    public required List<UseCase> UseCases { get; set; }
}
