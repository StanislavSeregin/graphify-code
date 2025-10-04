using GraphifyCode.Markdown;
using System;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable]
public partial class AnalysisMetadata
{
    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}
