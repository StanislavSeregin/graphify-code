using System;

namespace GraphifyCode.Core.Models;

public class AnalysisMetadata
{
    public DateTime LastAnalyzedAt { get; set; }

    public string? SourceCodeHash { get; set; }

    public string? RelativeCodePath { get; set; }
}
