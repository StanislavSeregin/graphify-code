using System;

namespace GraphifyCode.Core.Models;

public class ServiceEndpoint
{
    public Guid ServiceId { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public required AnalysisMetadata Metadata { get; set; }
}
