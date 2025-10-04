using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Entities;

[MarkdownSerializable]
public partial class Service
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}
