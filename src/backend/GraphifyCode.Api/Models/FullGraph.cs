using GraphifyCode.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphifyCode.Api.Models;

public class FullGraph
{
    public required ServiceData[] Services { get; set; }

    public static FullGraph FromEntities(IEnumerable<Service> services)
    {
        return new FullGraph()
        {
            Services = [.. services.Select(srv => new ServiceData()
            {
                Service = new ServiceModel()
                {
                    Name = srv.Name,
                    Description = srv.Description,
                    LastAnalyzedAt = srv.LastAnalyzedAt,
                    RelativeCodePath = srv.RelativeCodePath
                },
                Endpoint = [.. srv.Endpoints?.EndpointList.Select(e => new EndpointModel()
                {
                    Name = e.Name,
                    Description = e.Description,
                    Type = e.Type,
                    LastAnalyzedAt = e.LastAnalyzedAt,
                    RelativeCodePath = e.RelativeCodePath
                }) ?? []],
                UseCases = [.. srv.UseCases.Select(u => new UseCaseModel()
                {
                    Name = u.Name,
                    Description = u.Description,
                    InitiatingEndpointName = u.InitiatingEndpointName,
                    LastAnalyzedAt = u.LastAnalyzedAt,
                    Steps = [.. u.Steps.Select(s => new UseCaseStepModel()
                    {
                        Name = s.Name,
                        Description = s.Description,
                        ServiceName = s.ServiceName,
                        EndpointName = s.EndpointName,
                        RelativeCodePath = s.RelativeCodePath
                    })]
                })],
                Relations = new RelationsModel()
                {
                    TargetEndpointNames = []
                }
            })]
        };
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
    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class EndpointModel
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Type { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class UseCaseModel
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string InitiatingEndpointName { get; set; }

    public DateTime LastAnalyzedAt { get; set; }

    public required UseCaseStepModel[] Steps { get; set; }
}

public class UseCaseStepModel
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public string? ServiceName { get; set; }

    public string? EndpointName { get; set; }

    public string? RelativeCodePath { get; set; }
}

public class RelationsModel
{
    public required string[] TargetEndpointNames { get; set; }
}
