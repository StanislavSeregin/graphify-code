using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class Services
{
    public required Service[] ServiceList { get; set; }
}

[MarkdownSerializable]
public partial class Service
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public bool HasEndpoints { get; set; }

    public bool HasRelations { get; set; }

    public DateTime LastAnalyzed { get; set; }

    public string? CodePath { get; set; }
}
