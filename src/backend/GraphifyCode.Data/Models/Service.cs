using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class Service
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required AnalysisMetadata Metadata { get; set; }
}
