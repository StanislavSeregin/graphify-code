using GraphifyCode.Data.Entities;
using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable("Services details")]
public partial class ServicesDetails
{
    public required DetailedService[] Services { get; set; }

    public static ServicesDetails FromEntities(IEnumerable<Service> services, bool withEndpoints, bool withUseCases)
    {
        return new ServicesDetails()
        {
            Services = [.. services.Select(srv => new DetailedService()
            {
                Id = srv.Id,
                Name = srv.Name,
                Endpoints = withEndpoints && srv.Endpoints?.EndpointList is { } endpoints
                    ? [.. endpoints.Select(e => new DetailedServiceEndpoint()
                        {
                            Id = e.Id,
                            Name = e.Name,
                            Description = e.Description,
                            Type = e.Type,
                            LastAnalyzedAt = e.LastAnalyzedAt,
                            RelativeCodePath = e.RelativeCodePath
                        })]
                    : [],
                UseCases = withUseCases && srv.UseCases is { } useCases
                    ? [.. useCases.Select(u => new DetailedServiceUseCase()
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Description = u.Description,
                            InitiatingEndpointId = u.InitiatingEndpointId,
                            LastAnalyzedAt = u.LastAnalyzedAt
                        })]
                    : []
            })]
        };
    }
}

[MarkdownSerializable]
public partial class DetailedService
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    [MarkdownSubHeader("Endpoints")]
    public required DetailedServiceEndpoint[] Endpoints { get; set; }

    [MarkdownSubHeader("Use cases")]
    public required DetailedServiceUseCase[] UseCases { get; set; }
}

[MarkdownSerializable]
public partial class DetailedServiceEndpoint
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
public partial class DetailedServiceUseCase
{
    public Guid Id { get; set; }

    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzedAt { get; set; }
}
