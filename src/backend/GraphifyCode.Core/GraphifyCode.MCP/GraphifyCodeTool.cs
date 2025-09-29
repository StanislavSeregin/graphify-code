using GraphifyCode.Core.Models;
using GraphifyCode.Core.Services;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(GraphifyCodeDataService graphifyCodeDataService)
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    [McpServerTool, Description("""
        Get overview of all services in the codebase.
        Returns basic information about each service including name, description, and last analysis timestamp.
        """)]
    public string GetServicesOverview()
    {
        var servicesDict = graphifyCodeDataService.GetServices();
        if (servicesDict.Count == 0)
        {
            return "No services found.";
        }

        try
        {
            var services = servicesDict.Values
                .Select(GetServiceOverviewInfo)
                .Where(service => service != null)
                .ToArray();

            return JsonSerializer.Serialize(services, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error reading services data: {ex.Message}";
        }
    }

    private ServiceOverviewInfo GetServiceOverviewInfo(ServiceNode service)
    {
        var endpoints = graphifyCodeDataService.GetServiceEndpoints(service.Id);
        var relations = graphifyCodeDataService.GetServiceRelations(service.Id);
        return new ServiceOverviewInfo
        {
            Id = service.Id,
            Name = service.Name,
            Description = service.Description,
            LastAnalyzed = service.Metadata.LastAnalyzedAt,
            CodePath = service.Metadata.RelativeCodePath,
            HasEndpoints = endpoints.Length > 0,
            HasRelations = relations.Length > 0
        };
    }
}

public class ServiceOverviewInfo
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public DateTime LastAnalyzed { get; set; }

    public string? CodePath { get; set; }

    public bool HasEndpoints { get; set; }

    public bool HasRelations { get; set; }
}
