using GraphifyCode.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Api.Models;

public class FullGraph
{
    public ServiceData[] Services { get; set; }

    public FullGraph(IEnumerable<Service> services)
    {
        Services = [.. services.Select(srv => new ServiceData()
        {
            Service = new ServiceModel()
            {
                Id = srv.Id,
                Name = srv.Name,
                Description = srv.Description,
                LastAnalyzedAt = srv.LastAnalyzedAt,
                RelativeCodePath = srv.RelativeCodePath
            },
            Endpoint = [.. srv.Endpoints?.EndpointList.Select(e => new EndpointModel()
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Type = e.Type,
                LastAnalyzedAt = e.LastAnalyzedAt,
                RelativeCodePath = e.RelativeCodePath
            }) ?? []],
            UseCases = [.. srv.UseCases.Select(u => new UseCaseModel()
            {
                Id = u.Id,
                Name = u.Name,
                Description = u.Description,
                InitiatingEndpointId = u.InitiatingEndpointId,
                LastAnalyzedAt = u.LastAnalyzedAt,
                Steps = [.. u.Steps.Select(s => new UseCaseStepModel()
                {
                    Name = s.Name,
                    Description = s.Description,
                    ServiceId = s.ServiceId,
                    EndpointId = s.EndpointId,
                    RelativeCodePath = s.RelativeCodePath
                })]
            })],
            Relations = new RelationsModel()
            {
                TargetEndpointIds = []
            }
        })];
    }
}

public class ServiceData
{
    public required ServiceModel Service { get; set; }

    public required EndpointModel[] Endpoint { get; set; }

    public required UseCaseModel[] UseCases { get; set; }

    public required RelationsModel Relations { get; set; }
}

public class ServiceModel
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class EndpointModel
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class UseCaseModel
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid InitiatingEndpointId { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required UseCaseStepModel[] Steps { get; set; }
}

public class UseCaseStepModel
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? EndpointId { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class RelationsModel
{
    public required Guid[] TargetEndpointIds { get; set; }
}
