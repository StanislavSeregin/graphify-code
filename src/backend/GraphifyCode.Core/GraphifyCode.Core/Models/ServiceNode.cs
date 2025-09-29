using System;

namespace GraphifyCode.Core.Models;

public class ServiceNode
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required ServiceRelation[] Relations { get; set; }

    public required AnalysisMetadata Metadata { get; set; }
}
