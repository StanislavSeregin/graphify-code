using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Experiment;

[MarkdownSerializable]
public partial class UseCase
{
    [MarkdownIgnore]
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required UseCaseStep[] Steps { get; set; }

    [MarkdownIgnore]
    public required Service Parent { get; set; }
}

[MarkdownSerializable]
public partial class UseCaseStep
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? EndpointId { get; set; }

    public string? RelativeCodePath { get; set; }
}
