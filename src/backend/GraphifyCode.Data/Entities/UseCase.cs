using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;

namespace GraphifyCode.Data.Entities;

[MarkdownSerializable]
public partial class UseCase
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string InitiatingEndpointName { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required List<UseCaseStep> Steps { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }
}

[MarkdownSerializable]
public partial class UseCaseStep
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public string? ServiceName { get; set; }

    public string? EndpointName { get; set; }

    public string? RelativeCodePath { get; set; }
}
