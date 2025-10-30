using GraphifyCode.Data.Entities;
using GraphifyCode.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Data.Models;

[MarkdownSerializable("Services details")]
public partial class ServicesDetails
{
    public required ServiceItem[] ServiceList { get; set; }

    [MarkdownSerializable]
    public partial class ServiceItem
    {
        public Guid Id { get; set; }

        [MarkdownHeader]
        public required string Name { get; set; }

        public Endpoints? Endpoints { get; set; }

        public UseCases? UseCases { get; set; }
    }

    [MarkdownSerializable("Endpoints")]
    public partial class Endpoints
    {
        public required EndpointItem[] EndpointList { get; set; }
    }

    [MarkdownSerializable]
    public partial class EndpointItem
    {
        public Guid Id { get; set; }

        [MarkdownHeader]
        public required string Name { get; set; }

        public required string Description { get; set; }

        public required string Type { get; set; }

        public DateTime LastAnalyzedAt { get; set; }

        public string? RelativeCodePath { get; set; }
    }

    [MarkdownSerializable("Use cases")]
    public partial class UseCases
    {
        public required UseCaseItem[] UseCaseList { get; set; }
    }

    [MarkdownSerializable]
    public partial class UseCaseItem
    {
        [MarkdownIgnore]
        public Guid Id { get; set; }

        [MarkdownHeader]
        public required string Name { get; set; }

        public required string Description { get; set; }

        public Guid InitiatingEndpointId { get; set; }

        public DateTime LastAnalyzedAt { get; set; }
    }

    public static ServicesDetails FromEntities(IEnumerable<Service> services, bool withEndpoints, bool withUseCases)
    {
        return new ServicesDetails()
        {
            ServiceList = [.. services.Select(srv => new ServiceItem()
            {
                Id = srv.Id,
                Name = srv.Name,
                Endpoints = withEndpoints && srv.Endpoints?.EndpointList is { } endpoints
                    ? new Endpoints()
                    {
                        EndpointList = [.. endpoints.Select(e => new EndpointItem()
                        {
                            Id = e.Id,
                            Name = e.Name,
                            Description = e.Description,
                            Type = e.Type,
                            LastAnalyzedAt = e.LastAnalyzedAt,
                            RelativeCodePath = e.RelativeCodePath
                        })]
                    }
                    : null,
                UseCases = withUseCases && srv.UseCases is { } useCases
                    ? new UseCases()
                    {
                        UseCaseList = [.. useCases.Select(u => new UseCaseItem()
                        {
                            Id = u.Id,
                            Name = u.Name,
                            Description = u.Description,
                            InitiatingEndpointId = u.InitiatingEndpointId,
                            LastAnalyzedAt = u.LastAnalyzedAt
                        })]
                    }
                    : null
            })]
        };
    }
}
