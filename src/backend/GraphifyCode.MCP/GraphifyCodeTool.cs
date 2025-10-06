using GraphifyCode.Data;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(IDataService dataService)
{

    [McpServerTool, Description("""
        Get all services in the codebase.
        Returns Markdown formatted list with service metadata: ID, name, description, last analysis timestamp, relative code path, and flags indicating if service has endpoints or relations.
        Use this as the starting point to discover what services exist in the system and navigate the architecture.
        """)]
    public async Task<string> GetServices(CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await dataService.GetServices(cancellationToken);
            if (services.ServiceList.Length == 0)
            {
                return "No services found in the system.";
            }

            return services.ToMarkdown();
        }
        catch (Exception ex)
        {
            return $"Error reading services: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get all endpoints for a specific service.
        Returns Markdown formatted list with endpoint details: ID, name, description, type (http/queue/job), last analyzed timestamp, and relative code path.
        Use this to discover what entry points and interfaces a service exposes - APIs, queue consumers, or background jobs.
        """)]
    public async Task<string> GetEndpoints(
        [Description("Service ID as GUID string. Use GetServices to find service IDs.")] string serviceId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            var endpoints = await dataService.GetEndpoints(parsedServiceId, cancellationToken);
            if (endpoints.EndpointList.Length == 0)
            {
                return $"No endpoints found for service {serviceId}.";
            }

            return endpoints.ToMarkdown();
        }
        catch (Exception ex)
        {
            return $"Error reading endpoints: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get all outgoing relations for a specific service.
        Returns Markdown formatted list of target endpoint IDs that this service depends on (calls/consumes).
        Use this to understand service dependencies, integration points, and build the dependency graph.
        Each relation represents a connection from the source service to a target endpoint in another service.
        """)]
    public async Task<string> GetRelations(
        [Description("Source service ID as GUID string. Use GetServices to find service IDs.")] string serviceId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            var relations = await dataService.GetRelations(parsedServiceId, cancellationToken);
            if (relations.TargetEndpointIds.Length == 0)
            {
                return $"No relations found for service {serviceId}.";
            }

            return relations.ToMarkdown();
        }
        catch (Exception ex)
        {
            return $"Error reading relations: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Create or update a service in the system.
        Services represent distinct applications, microservices, or external systems in your architecture.
        Always provide relativeCodePath for internal services to enable proper classification and code navigation.
        If serviceId is provided, updates the existing service; otherwise creates a new service with a generated ID.
        Returns the service ID (new or existing) for use in subsequent operations like creating endpoints or relations.
        """)]
    public async Task<string> CreateOrUpdateService(
        [Description("Human-readable service name (e.g., 'User Service', 'Payment Gateway', 'External Email API')")] string name,
        [Description("Detailed service description explaining its purpose and responsibilities in the system")] string description,
        [Description("Service ID as GUID string (optional). Provide only when updating an existing service. Omit to create a new service.")] string? serviceId = null,
        [Description("Relative path to service root directory (e.g., 'src/Services/UserService', 'backend/PaymentService'). REQUIRED for internal services to identify source code location. Omit or set null ONLY for external/third-party services.")] string? relativeCodePath = null,
        CancellationToken cancellationToken = default)
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
            var id = await dataService.CreateOrUpdateService(name, description, parsedServiceId, relativeCodePath, cancellationToken);
            return $"Service {(parsedServiceId.HasValue ? "updated" : "created")} successfully. Service ID: {id}";
        }
        catch (Exception ex)
        {
            return $"Error creating/updating service: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Create or update an endpoint for a service.
        Endpoints are entry points to a service - HTTP APIs, message queue consumers, or background jobs.
        Always provide relativeCodePath for internal endpoints to enable code navigation and proper classification.
        Type must be exactly one of: 'http' (REST/GraphQL/gRPC), 'queue' (message consumer), or 'job' (scheduled task).
        If endpointId is provided, updates the existing endpoint; otherwise creates a new endpoint with a generated ID.
        Returns the endpoint ID (new or existing) for use in relations.
        """)]
    public async Task<string> CreateOrUpdateEndpoint(
        [Description("Service ID as GUID string (the service that owns this endpoint). Use GetServices to find service IDs.")] string serviceId,
        [Description("Endpoint name (e.g., 'GET /api/users', 'ProcessOrderQueue', 'DailyReportJob')")] string name,
        [Description("Detailed endpoint description explaining what it does, what it accepts, and what it returns")] string description,
        [Description("Endpoint type - must be exactly one of: 'http' (for REST/GraphQL/gRPC APIs), 'queue' (for message queue consumers), 'job' (for scheduled/background tasks)")] string type,
        [Description("Endpoint ID as GUID string (optional). Provide only when updating an existing endpoint. Omit to create a new endpoint.")] string? endpointId = null,
        [Description("Relative path to endpoint implementation with optional line number (e.g., 'src/Controllers/UserController.cs:42', 'src/Consumers/OrderConsumer.cs'). REQUIRED for internal endpoints to identify source code location. Omit or set null ONLY for external endpoints.")] string? relativeCodePath = null,
        CancellationToken cancellationToken = default)
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
            var id = await dataService.CreateOrUpdateEndpoint(parsedServiceId, name, description, type, parsedEndpointId, relativeCodePath, cancellationToken);
            return $"Endpoint {(parsedEndpointId.HasValue ? "updated" : "created")} successfully. Endpoint ID: {id}";
        }
        catch (Exception ex)
        {
            return $"Error creating/updating endpoint: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Add a relation between a source service and a target endpoint.
        Relations represent dependencies: when service A calls/consumes an endpoint in service B, create a relation from A to B's endpoint.
        This builds the service dependency graph for visualization and analysis.
        If the relation already exists, the operation is idempotent (no error, no duplicate).
        Use this after identifying service integration points in the code to map the actual dependencies.
        """)]
    public async Task<string> AddRelation(
        [Description("Source service ID as GUID string (the service that initiates the call/dependency). Use GetServices to find service IDs.")] string sourceServiceId,
        [Description("Target endpoint ID as GUID string (the endpoint being called/consumed). Use GetEndpoints to find endpoint IDs.")] string targetEndpointId,
        CancellationToken cancellationToken = default)
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
            await dataService.AddRelation(parsedSourceServiceId, parsedTargetEndpointId, cancellationToken);
            return $"Relation added successfully: Service {sourceServiceId} → Endpoint {targetEndpointId}";
        }
        catch (Exception ex)
        {
            return $"Error adding relation: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete a service and all its associated data permanently.
        WARNING: This is a destructive operation that cannot be undone.
        Cascading deletion: automatically removes all endpoints belonging to this service and all relations from other services that reference this service's endpoints.
        Use this when a service is completely removed from the architecture or was added by mistake.
        """)]
    public async Task<string> DeleteService(
        [Description("Service ID as GUID string. Use GetServices to find service IDs.")] string serviceId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            await dataService.DeleteService(parsedServiceId, cancellationToken);
            return $"Service {serviceId} and all its data deleted successfully (including all endpoints and related relations)";
        }
        catch (Exception ex)
        {
            return $"Error deleting service: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete an endpoint permanently.
        WARNING: This is a destructive operation that cannot be undone.
        Cascading deletion: automatically removes all relations from other services that reference this endpoint.
        Use this when an endpoint is removed from the codebase or was added by mistake.
        Note: This operation uses the endpoint's own ID, not the service ID.
        """)]
    public async Task<string> DeleteEndpoint(
        [Description("Endpoint ID as GUID string. Use GetEndpoints to find endpoint IDs.")] string endpointId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(endpointId, out var parsedEndpointId))
        {
            return $"Error: Invalid endpoint ID format. Expected GUID, got: {endpointId}";
        }

        try
        {
            await dataService.DeleteEndpoint(parsedEndpointId, cancellationToken);
            return $"Endpoint {endpointId} deleted successfully (including all relations that referenced it)";
        }
        catch (Exception ex)
        {
            return $"Error deleting endpoint: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete a specific relation between a source service and a target endpoint.
        Use this to remove a dependency when service A no longer calls/consumes an endpoint in service B.
        This only removes the relation link, not the service or endpoint themselves.
        Useful for maintaining accurate dependency graphs as the architecture evolves.
        """)]
    public async Task<string> DeleteRelation(
        [Description("Source service ID as GUID string (the service that has the dependency). Use GetServices to find service IDs.")] string sourceServiceId,
        [Description("Target endpoint ID as GUID string (the endpoint no longer being called). Use GetEndpoints to find endpoint IDs.")] string targetEndpointId,
        CancellationToken cancellationToken = default)
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
            await dataService.DeleteRelation(parsedSourceServiceId, parsedTargetEndpointId, cancellationToken);
            return $"Relation deleted successfully: Service {sourceServiceId} no longer depends on Endpoint {targetEndpointId}";
        }
        catch (Exception ex)
        {
            return $"Error deleting relation: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get all use cases for a specific service.
        Use cases represent complex business processes that start from a service's endpoint and flow through multiple services.
        Returns Markdown formatted list with use case metadata: ID, name, description, initiating endpoint ID, last analyzed timestamp, and step count.
        Use this to discover documented business processes and understand what complex workflows are handled by a service.
        Note: Not every endpoint has a use case - they are created only for multi-step processes that require analytical documentation.
        """)]
    public async Task<string> GetUseCases(
        [Description("Service ID as GUID string (the service whose use cases you want to retrieve). Use GetServices to find service IDs.")] string serviceId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        try
        {
            var useCases = await dataService.GetUseCases(parsedServiceId, cancellationToken);
            if (useCases.UseCaseList.Length == 0)
            {
                return $"No use cases found for service {serviceId}.";
            }

            return useCases.ToMarkdown();
        }
        catch (Exception ex)
        {
            return $"Error reading use cases: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Get detailed information about a specific use case including all its steps.
        Use cases describe complex business processes as a sequence of steps flowing through the system.
        Each step can represent internal logic within a service or an integration call to another service's endpoint.
        Returns Markdown formatted use case with complete step-by-step breakdown including service contexts and endpoint calls.
        Steps are ordered sequentially - the order in the response reflects the business process flow.
        Use this to understand the full business flow, trace logic through services, and navigate to specific code implementations.
        """)]
    public async Task<string> GetUseCaseDetails(
        [Description("Use case ID as GUID string. Use GetUseCases to find use case IDs.")] string useCaseId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(useCaseId, out var parsedUseCaseId))
        {
            return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
        }

        try
        {
            var useCase = await dataService.GetUseCaseDetails(parsedUseCaseId, cancellationToken);
            return useCase.ToMarkdown();
        }
        catch (Exception ex)
        {
            return $"Error reading use case details: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Create or update a use case for documenting complex business processes.
        Use cases capture multi-step workflows that span multiple services, providing analytical context on top of the technical architecture.
        Each use case must start from a specific endpoint (initiating endpoint). Steps are added separately using AddStep tool.
        The initiatingEndpointId is validated to ensure it exists and belongs to the specified service.
        If useCaseId is provided, updates the existing use case metadata (name, description, initiatingEndpointId); otherwise creates a new use case with a generated ID.
        Returns the use case ID (new or existing) for use in subsequent AddStep operations.
        Important: This operation only manages use case metadata. Use AddStep, UpdateStep, and DeleteAllSteps to manage the steps.
        """)]
    public async Task<string> CreateOrUpdateUseCase(
        [Description("Service ID as GUID string (the service that owns the initiating endpoint). Use GetServices to find service IDs.")] string serviceId,
        [Description("Use case name (e.g., 'Order Checkout with Payment', 'User Registration Flow', 'Daily Inventory Synchronization')")] string name,
        [Description("Detailed use case description explaining the business purpose, expected outcome, and overall process context")] string description,
        [Description("Initiating endpoint ID as GUID string (the endpoint where this business process starts). Must belong to the specified service. Use GetEndpoints to find endpoint IDs.")] string initiatingEndpointId,
        [Description("Use case ID as GUID string (optional). Provide only when updating an existing use case. Omit to create a new use case.")] string? useCaseId = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(serviceId, out var parsedServiceId))
        {
            return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
        }

        if (!Guid.TryParse(initiatingEndpointId, out var parsedInitiatingEndpointId))
        {
            return $"Error: Invalid initiating endpoint ID format. Expected GUID, got: {initiatingEndpointId}";
        }

        Guid? parsedUseCaseId = null;
        if (!string.IsNullOrWhiteSpace(useCaseId))
        {
            if (!Guid.TryParse(useCaseId, out var parsed))
            {
                return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
            }
            parsedUseCaseId = parsed;
        }

        try
        {
            var id = await dataService.CreateOrUpdateUseCase(parsedServiceId, name, description, parsedInitiatingEndpointId, parsedUseCaseId, cancellationToken);
            return $"Use case {(parsedUseCaseId.HasValue ? "updated" : "created")} successfully. Use case ID: {id}";
        }
        catch (Exception ex)
        {
            return $"Error creating/updating use case: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Add a new step to the end of a use case's step sequence.
        Steps represent sequential actions in a business process flow and are added in the order they should execute.
        Each step can represent: internal service logic (serviceId only), integration calls (serviceId + endpointId), or abstract descriptions (neither).
        When a step represents an integration call: serviceId indicates the calling service, endpointId indicates the called endpoint in another service.
        All provided serviceId and endpointId values are validated to ensure they exist in the system.
        Returns confirmation with the step index (position) in the sequence for reference.
        """)]
    public async Task<string> AddStep(
        [Description("Use case ID as GUID string (the use case to add the step to). Use GetUseCases to find use case IDs.")] string useCaseId,
        [Description("Step name describing the action (e.g., 'Validate user input', 'Reserve inventory', 'Send notification email')")] string name,
        [Description("Detailed step description explaining what happens, business rules, conditions, or expected outcomes")] string description,
        [Description("Service ID as GUID string (optional). The service where this step executes. Omit for abstract/conceptual steps. Use GetServices to find service IDs.")] string? serviceId = null,
        [Description("Endpoint ID as GUID string (optional). The endpoint being called if this step represents an integration call to another service. When specified, serviceId should indicate the calling service. Use GetEndpoints to find endpoint IDs.")] string? endpointId = null,
        [Description("Relative path to step implementation with optional line number (e.g., 'src/Handlers/OrderHandler.cs:123', 'src/Services/ValidationService.cs'). Helps navigate to the actual code for this step.")] string? relativeCodePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(useCaseId, out var parsedUseCaseId))
        {
            return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
        }

        Guid? parsedServiceId = null;
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            if (!Guid.TryParse(serviceId, out var parsed))
            {
                return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
            }
            parsedServiceId = parsed;
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
            var stepIndex = await dataService.AddStep(parsedUseCaseId, name, description, parsedServiceId, parsedEndpointId, relativeCodePath, cancellationToken);
            return $"Step added successfully at index {stepIndex}.";
        }
        catch (Exception ex)
        {
            return $"Error adding step: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Update an existing step in a use case by its index position.
        Steps are indexed starting from 0. Use GetUseCaseDetails to see all steps with their current positions.
        Only provided parameters are updated - omitted parameters keep their existing values.
        All provided serviceId and endpointId values are validated to ensure they exist in the system.
        Use this for targeted corrections to step details without recreating the entire use case.
        """)]
    public async Task<string> UpdateStep(
        [Description("Use case ID as GUID string (the use case containing the step). Use GetUseCases to find use case IDs.")] string useCaseId,
        [Description("Step index (0-based position in the sequence). Use GetUseCaseDetails to see step positions.")] int stepIndex,
        [Description("Step name (optional). Provide only if changing the name.")] string? name = null,
        [Description("Step description (optional). Provide only if changing the description.")] string? description = null,
        [Description("Service ID as GUID string (optional). Provide only if changing the service context. Set to empty string to clear. Use GetServices to find service IDs.")] string? serviceId = null,
        [Description("Endpoint ID as GUID string (optional). Provide only if changing the called endpoint. Set to empty string to clear. Use GetEndpoints to find endpoint IDs.")] string? endpointId = null,
        [Description("Relative code path (optional). Provide only if changing the code reference. Set to empty string to clear.")] string? relativeCodePath = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(useCaseId, out var parsedUseCaseId))
        {
            return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
        }

        Guid? parsedServiceId = null;
        if (!string.IsNullOrWhiteSpace(serviceId))
        {
            if (serviceId == string.Empty)
            {
                parsedServiceId = Guid.Empty; // Signal to clear
            }
            else if (!Guid.TryParse(serviceId, out var parsed))
            {
                return $"Error: Invalid service ID format. Expected GUID, got: {serviceId}";
            }
            else
            {
                parsedServiceId = parsed;
            }
        }

        Guid? parsedEndpointId = null;
        if (!string.IsNullOrWhiteSpace(endpointId))
        {
            if (endpointId == string.Empty)
            {
                parsedEndpointId = Guid.Empty; // Signal to clear
            }
            else if (!Guid.TryParse(endpointId, out var parsed))
            {
                return $"Error: Invalid endpoint ID format. Expected GUID, got: {endpointId}";
            }
            else
            {
                parsedEndpointId = parsed;
            }
        }

        try
        {
            await dataService.UpdateStep(parsedUseCaseId, stepIndex, name, description, parsedServiceId, parsedEndpointId, relativeCodePath, cancellationToken);
            return $"Step at index {stepIndex} updated successfully.";
        }
        catch (Exception ex)
        {
            return $"Error updating step: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete all steps from a use case while keeping the use case metadata intact.
        This is useful when you need to completely rebuild the step sequence due to significant process changes.
        After deletion, the use case will still exist but will have zero steps.
        Use AddStep to populate with new steps after clearing.
        WARNING: This operation cannot be undone. All step data will be permanently lost.
        """)]
    public async Task<string> DeleteAllSteps(
        [Description("Use case ID as GUID string (the use case whose steps should be deleted). Use GetUseCases to find use case IDs.")] string useCaseId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(useCaseId, out var parsedUseCaseId))
        {
            return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
        }

        try
        {
            await dataService.DeleteAllSteps(parsedUseCaseId, cancellationToken);
            return $"All steps deleted successfully from use case {useCaseId}.";
        }
        catch (Exception ex)
        {
            return $"Error deleting steps: {ex.Message}";
        }
    }

    [McpServerTool, Description("""
        Delete a use case permanently including all its steps and metadata.
        WARNING: This is a destructive operation that cannot be undone.
        Use this when a business process is no longer relevant, was documented incorrectly, or the architecture has changed significantly.
        This only removes the use case documentation - it does not affect services, endpoints, or relations.
        """)]
    public async Task<string> DeleteUseCase(
        [Description("Use case ID as GUID string. Use GetUseCases to find use case IDs.")] string useCaseId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(useCaseId, out var parsedUseCaseId))
        {
            return $"Error: Invalid use case ID format. Expected GUID, got: {useCaseId}";
        }

        try
        {
            await dataService.DeleteUseCase(parsedUseCaseId, cancellationToken);
            return $"Use case {useCaseId} deleted successfully.";
        }
        catch (Exception ex)
        {
            return $"Error deleting use case: {ex.Message}";
        }
    }
}
