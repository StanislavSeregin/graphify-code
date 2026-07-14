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
                Name = srv.Name,
                Endpoints = withEndpoints && srv.Endpoints?.EndpointList is { } endpoints
                    ? [.. endpoints.Select(e => new DetailedServiceEndpoint()
                        {
                            Name = e.Name,
                            Description = e.Description,
                            Type = e.Type,
                            LastAnalyzedAt = e.LastAnalyzedAt,
                            RelativeCodePath = e.RelativeCodePath
                        })]
                    : [],
                UseCases = withUseCases && srv.UseCases is { } useCases
                    ? [.. useCases.Select(u => new DetailedUseCaseOverview()
                        {
                            Name = u.Name,
                            Description = u.Description,
                            InitiatingEndpointName = u.InitiatingEndpointName,
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
    [MarkdownHeader]
    public required string Name { get; set; }

    [MarkdownSubHeader("Endpoints")]
    public required DetailedServiceEndpoint[] Endpoints { get; set; }

    [MarkdownSubHeader("Use cases")]
    public required DetailedUseCaseOverview[] UseCases { get; set; }
}

[MarkdownSerializable]
public partial class DetailedServiceEndpoint
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

[MarkdownSerializable]
public partial class DetailedUseCaseOverview
{
    [MarkdownHeader]
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string InitiatingEndpointName { get; set; }

    public DateTime LastAnalyzedAt { get; set; }
}
