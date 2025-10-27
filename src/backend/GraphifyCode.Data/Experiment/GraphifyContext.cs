using GraphifyCode.Markdown;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Experiment;

[MarkdownSerializable]
public partial class Service
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }

    [MarkdownIgnore]
    public Endpoints? Endpoints { get; set; }

    [MarkdownIgnore]
    public required List<UseCase> UseCases { get; set; }
}

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

[MarkdownSerializable]
public partial class UseCase
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required UseCaseStep[] Steps { get; set; }
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

public class GraphifyContext : DbContext
{
    private static async Task<Service[]> LoadServices(string pathContext, CancellationToken cancellationToken)
    {
        return await EntityLoader.Load<Service>(pathContext, cancellationToken).ToArrayAsync(cancellationToken);
    }
}
