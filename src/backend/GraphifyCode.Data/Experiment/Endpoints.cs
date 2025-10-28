using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Experiment;

[MarkdownSerializable]
public partial class Endpoints
{
    public required Endpoint[] EndpointList { get; set; }
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
