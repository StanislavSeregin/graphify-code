using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class UseCases
{
    public required UseCaseSummary[] UseCaseList { get; set; }
}

[MarkdownSerializable]
public partial class UseCaseSummary
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzed { get; set; }

    public int StepCount { get; set; }
}
