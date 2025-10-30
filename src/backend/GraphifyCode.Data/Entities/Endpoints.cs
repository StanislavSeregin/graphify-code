using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;

namespace GraphifyCode.Data.Entities;

[MarkdownSerializable]
public partial class Endpoints
{
    public required List<Endpoint> EndpointList { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }
}

[MarkdownSerializable]
public partial class Endpoint
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}
