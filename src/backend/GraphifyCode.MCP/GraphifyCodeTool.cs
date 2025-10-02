using GraphifyCode.Core.Models;
using GraphifyCode.Core.Services;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(GraphifyCodeDataService graphifyCodeDataService)
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    [McpServerTool, Description("""
        Get overview of all services in the codebase.
        Returns JSON array with service metadata: ID, name, description, last analysis timestamp, code path, and availability of endpoints/relations.
        """)]
    public async Task<string> GetServicesOverview()
    {
        var servicesDict = await graphifyCodeDataService.GetServices();
        if (servicesDict.Count == 0)
        {
            return "No services found.";
        }

        try
        {
            var endpointsDict = new Dictionary<Guid, ServiceEndpoint[]>();
            var relationsDict = new Dictionary<Guid, ServiceRelation[]>();
            foreach (var serviceId in servicesDict.Keys)
            {
                endpointsDict[serviceId] = await graphifyCodeDataService.GetServiceEndpoints(serviceId);
                relationsDict[serviceId] = await graphifyCodeDataService.GetServiceRelations(serviceId);
            }

            var services = servicesDict.Values
                .Select(service => new ServiceOverviewInfo
                {
                    Id = service.Id,
                    Name = service.Name,
                    Description = service.Description,
                    LastAnalyzed = service.Metadata.LastAnalyzedAt,
                    CodePath = service.Metadata.RelativeCodePath,
                    HasEndpoints = endpointsDict[service.Id].Length > 0,
                    HasRelations = relationsDict[service.Id].Length > 0
                })
                .ToArray();

            return JsonSerializer.Serialize(services, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error reading services data: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get all endpoints for a specific service.
        Returns JSON array with endpoint details: ID, name, description, type (http/queue/job), code path, and metadata.
        """)]
    public async Task<string> GetServiceEndpoints(
        [Description("Service ID as GUID string")] string serviceId)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            var endpoints = await graphifyCodeDataService.GetServiceEndpoints(parsedServiceId);
            return JsonSerializer.Serialize(endpoints, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error reading service endpoints: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get all outgoing relations for a specific service.
        Returns JSON array showing which external endpoints this service calls (dependencies).
        """)]
    public async Task<string> GetServiceRelations(
        [Description("Service ID as GUID string")] string serviceId)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            var relations = await graphifyCodeDataService.GetServiceRelations(parsedServiceId);
            return JsonSerializer.Serialize(relations, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error reading service relations: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Create or update a service.
        Always provide codePath for internal services to enable proper classification.
        If serviceId provided, updates existing; otherwise creates new with generated ID.
        Returns the service ID as JSON.
        """)]
    public async Task<string> CreateOrUpdateService(
        [Description("Service name")] string name,
        [Description("Service description")] string description,
        [Description("Service ID as GUID string (optional, for updates)")] string? serviceId = null,
        [Description("Relative path to service code (e.g., 'src/Services/UserService'). REQUIRED for internal services (identifies source code location). Omit or set null for external services only.")] string? codePath = null)
    {
        Guid? parsedServiceId = null;
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            if (!Guid.TryParse(serviceId, out var parsed))
            {
                return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
            }
            parsedServiceId = parsed;
        }

        try
        {
            var id = await graphifyCodeDataService.CreateOrUpdateService(name, description, parsedServiceId, codePath);
            return JsonSerializer.Serialize(new { ServiceId = id }, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error creating/updating service: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Create or update a service endpoint.
        Always provide codePath for internal endpoints to enable proper classification.
        Type must be: http, queue, or job (error if invalid).
        If endpointId provided, updates existing; otherwise creates new with generated ID.
        Returns the endpoint ID as JSON.
        """)]
    public async Task<string> CreateOrUpdateServiceEndpoint(
        [Description("Service ID as GUID string")] string serviceId,
        [Description("Endpoint name")] string name,
        [Description("Endpoint description")] string description,
        [Description("Endpoint type (http, queue, or job)")] string type,
        [Description("Endpoint ID as GUID string (optional, for updates)")] string? endpointId = null,
        [Description("Relative path to endpoint implementation (e.g., 'src/Services/UserService/Controllers/UserController.cs:42'). REQUIRED for internal endpoints (identifies source code location). Omit or set null for external endpoints only.")] string? codePath = null)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        Guid? parsedEndpointId = null;
        if (!string.IsNullOrWhiteSpace(endpointId))
        {
            if (!Guid.TryParse(endpointId, out var parsed))
            {
                return $"Error: Invalid endpoint ID format. Expected GUID, got: {endpointId}";
            }
            parsedEndpointId = parsed;
        }

        try
        {
            var id = await graphifyCodeDataService.CreateOrUpdateServiceEndpoint(parsedServiceId, name, description, type, parsedEndpointId, codePath);
            return JsonSerializer.Serialize(new { EndpointId = id }, _jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            return $"Error creating/updating service endpoint: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Add a relation between a source service and a target endpoint.
        If the relation already exists, no changes are made.
        """)]
    public async Task<string> AddServiceRelation(
        [Description("Source service ID as GUID string")] string sourceServiceId,
        [Description("Target endpoint ID as GUID string")] string targetEndpointId)
    {
        if (!Guid.TryParse(sourceServiceId, out var parsedSourceServiceId))
        {
            return $"Error: Invalid source service ID format. Expected GUID, got: {sourceServiceId}";
        }

        if (!Guid.TryParse(targetEndpointId, out var parsedTargetEndpointId))
        {
            return $"Error: Invalid target endpoint ID format. Expected GUID, got: {targetEndpointId}";
        }

        try
        {
            await graphifyCodeDataService.AddServiceRelation(parsedSourceServiceId, parsedTargetEndpointId);
            return "Relation added successfully";
        }
        catch (Exception ex)
        {
            return $"Error adding service relation: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete a service and all its data.
        Cascading deletion: also removes all relations from other services that reference this service's endpoints.
        """)]
    public async Task<string> DeleteService(
        [Description("Service ID as GUID string")] string serviceId)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            await graphifyCodeDataService.DeleteService(parsedServiceId);
            return "Service deleted successfully";
        }
        catch (Exception ex)
        {
            return $"Error deleting service: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete an endpoint from a service.
        Cascading deletion: also removes all relations that reference this endpoint.
        """)]
    public async Task<string> DeleteServiceEndpoint(
        [Description("Service ID as GUID string")] string serviceId,
        [Description("Endpoint ID as GUID string")] string endpointId)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        if (!Guid.TryParse(endpointId, out var parsedEndpointId))
        {
            return $"Error: Invalid endpoint ID format. Expected GUID, got: {endpointId}";
        }

        try
        {
            await graphifyCodeDataService.DeleteServiceEndpoint(parsedServiceId, parsedEndpointId);
            return "Endpoint deleted successfully";
        }
        catch (Exception ex)
        {
            return $"Error deleting service endpoint: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete a specific relation between a source service and a target endpoint.
        """)]
    public async Task<string> DeleteServiceRelation(
        [Description("Source service ID as GUID string")] string sourceServiceId,
        [Description("Target endpoint ID as GUID string")] string targetEndpointId)
    {
        if (!Guid.TryParse(sourceServiceId, out var parsedSourceServiceId))
        {
            return $"Error: Invalid source service ID format. Expected GUID, got: {sourceServiceId}";
        }

        if (!Guid.TryParse(targetEndpointId, out var parsedTargetEndpointId))
        {
            return $"Error: Invalid target endpoint ID format. Expected GUID, got: {targetEndpointId}";
        }

        try
        {
            await graphifyCodeDataService.DeleteServiceRelation(parsedSourceServiceId, parsedTargetEndpointId);
            return "Relation deleted successfully";
        }
        catch (Exception ex)
        {
            return $"Error deleting service relation: {ex.Message}";
        }
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
