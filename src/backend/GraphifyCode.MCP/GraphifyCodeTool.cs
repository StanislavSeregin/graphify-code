using GraphifyCode.MCP.Contracts;
using GraphifyCode.MCP.Features;
using Mediator;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(IMediator mediator)
{
    [McpServerTool(Name = "list_services"), Description(
        "List all services. Each service is identified by its Name — use that Name in every other tool. " +
        "Returns compact metadata and a markdown snapshot.")]
    public Task<McpResult<ListServicesData>> ListServices(CancellationToken cancellationToken)
    {
        return mediator.Send(new ListServices.Query(), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "get_service"), Description(
        "Load one service by Name. Optionally include its endpoints and use cases (also named entities).")]
    public Task<McpResult<GetServiceData>> GetService(
        [Description("Service Name (same value as in list_services).")] string serviceName,
        [Description("Include endpoints in the payload and markdown.")] bool includeEndpoints,
        [Description("Include use cases in the payload and markdown.")] bool includeUseCases,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return Task.FromResult(McpResult<GetServiceData>.Failure(
                "validation_error",
                "serviceName must not be empty.",
                new ValidationErrorDetails { Field = nameof(serviceName), Reason = "Service name is required." }));
        }

        return mediator.Send(new GetService.Query(serviceName, includeEndpoints, includeUseCases), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "get_use_case"), Description(
        "Load one use case by Service Name + Use Case Name, including steps and markdown.")]
    public Task<McpResult<GetUseCaseData>> GetUseCase(
        [Description("Service Name that owns the use case.")] string serviceName,
        [Description("Use case Name within that service.")] string useCaseName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return Task.FromResult(McpResult<GetUseCaseData>.Failure(
                "validation_error",
                "serviceName must not be empty.",
                new ValidationErrorDetails { Field = nameof(serviceName), Reason = "Service name is required." }));
        }

        if (string.IsNullOrWhiteSpace(useCaseName))
        {
            return Task.FromResult(McpResult<GetUseCaseData>.Failure(
                "validation_error",
                "useCaseName must not be empty.",
                new ValidationErrorDetails { Field = nameof(useCaseName), Reason = "Use case name is required." }));
        }

        return mediator.Send(new GetUseCase.Query(serviceName, useCaseName), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "search_graph"), Description(
        "Search services, endpoints, and use cases by Name, description, or code path. " +
        "Matches return EntityName and ServiceName for follow-up tool calls.")]
    public Task<McpResult<SearchGraphData>> SearchGraph(
        [Description("Case-insensitive query (at least 2 non-space characters).")] string query,
        [Description("Maximum matches to return (1–100).")] int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Task.FromResult(McpResult<SearchGraphData>.Failure(
                "validation_error",
                "query must contain at least 2 non-space characters.",
                new ValidationErrorDetails { Field = nameof(query), Reason = "Minimum length is 2." }));
        }

        if (limit is < 1 or > 100)
        {
            return Task.FromResult(McpResult<SearchGraphData>.Failure(
                "validation_error",
                "limit must be between 1 and 100.",
                new ValidationErrorDetails { Field = nameof(limit), Reason = "Allowed range is 1..100." }));
        }

        return mediator.Send(new SearchGraph.Query(query, limit), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_service"), Description(
        "Create or update a service. Name is the service key: matching Name updates; otherwise creates.")]
    public Task<McpResult<MutationResultData>> UpsertService(
        [Description("Service fields. Name uniquely identifies the service.")] UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure(
                "validation_error",
                "name and description are required.",
                new ValidationErrorDetails { Field = nameof(request), Reason = "Name and Description are mandatory." }));
        }

        return mediator.Send(new UpsertService.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_endpoint"), Description(
        "Create or update an endpoint under a service. The endpoint Name is unique within that service.")]
    public Task<McpResult<MutationResultData>> UpsertEndpoint(
        [Description("Endpoint fields. serviceName + Name uniquely identify the endpoint.")] UpsertEndpointRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName)
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description)
            || string.IsNullOrWhiteSpace(request.Type))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure(
                "validation_error",
                "serviceName, name, description and type are required.",
                new ValidationErrorDetails { Field = nameof(request), Reason = "ServiceName/Name/Description/Type are mandatory." }));
        }

        return mediator.Send(new UpsertEndpoint.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_use_case"), Description(
        "Create or update a use case under a service. The use case Name is unique within that service. " +
        "initiatingEndpointName must be an existing endpoint Name in the same service.")]
    public Task<McpResult<MutationResultData>> UpsertUseCase(
        [Description("Use case fields. serviceName + Name uniquely identify the use case.")] UpsertUseCaseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName)
            || string.IsNullOrWhiteSpace(request.InitiatingEndpointName)
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure(
                "validation_error",
                "serviceName, initiatingEndpointName, name and description are required.",
                new ValidationErrorDetails { Field = nameof(request), Reason = "ServiceName/InitiatingEndpointName/Name/Description are mandatory." }));
        }

        return mediator.Send(new UpsertUseCase.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_relation"), Description(
        "Create or update a step inside a use case. Steps are keyed by stepName within the use case. " +
        "Optional relatedServiceName / endpointName link the step to another service or endpoint by Name.")]
    public Task<McpResult<MutationResultData>> UpsertRelation(
        [Description("Step fields. serviceName + useCaseName select the use case; stepName selects the step.")] UpsertRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceName)
            || string.IsNullOrWhiteSpace(request.UseCaseName)
            || string.IsNullOrWhiteSpace(request.StepName)
            || string.IsNullOrWhiteSpace(request.StepDescription))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure(
                "validation_error",
                "serviceName, useCaseName, stepName and stepDescription are required.",
                new ValidationErrorDetails { Field = nameof(request), Reason = "ServiceName/UseCaseName/StepName/StepDescription are mandatory." }));
        }

        return mediator.Send(new UpsertRelation.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "remove_entity"), Description(
        "Delete a service, endpoint, or use case by Name. " +
        "For Endpoint and UseCase, also pass serviceName (the owning service).")]
    public Task<McpResult<MutationResultData>> RemoveEntity(
        [Description("Entity Name to delete (service Name, endpoint Name, or use case Name).")] string entityName,
        [Description("Which kind of entity: Service, Endpoint, or UseCase.")] GraphEntityType entityType,
        [Description("Owning service Name. Required when entityType is Endpoint or UseCase.")] string? serviceName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure(
                "validation_error",
                "entityName must not be empty.",
                new ValidationErrorDetails { Field = nameof(entityName), Reason = "Entity name is required." }));
        }

        return mediator.Send(new RemoveEntity.Command(entityName, entityType, serviceName), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "list_endpoints"), Description(
        "List endpoints for a service by Service Name. Each endpoint is identified by its Name within that service.")]
    public Task<McpResult<ListEndpointsData>> ListEndpoints(
        [Description("Service Name.")] string serviceName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return Task.FromResult(McpResult<ListEndpointsData>.Failure(
                "validation_error",
                "serviceName must not be empty.",
                new ValidationErrorDetails { Field = nameof(serviceName), Reason = "Service name is required." }));
        }

        return mediator.Send(new ListEndpoints.Query(serviceName), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "list_use_cases"), Description(
        "List use cases for a service by Service Name. Each use case is identified by its Name within that service.")]
    public Task<McpResult<ListUseCasesData>> ListUseCases(
        [Description("Service Name.")] string serviceName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return Task.FromResult(McpResult<ListUseCasesData>.Failure(
                "validation_error",
                "serviceName must not be empty.",
                new ValidationErrorDetails { Field = nameof(serviceName), Reason = "Service name is required." }));
        }

        return mediator.Send(new ListUseCases.Query(serviceName), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "bulk_upsert_endpoint"), Description(
        "Batch create or update endpoints. Each item uses serviceName + endpoint Name as the key. Partial success is allowed.")]
    public Task<McpResult<BulkMutationData>> BulkUpsertEndpoint(
        [Description("Batch of endpoint upserts.")] BulkUpsertEndpointsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Length == 0)
        {
            return Task.FromResult(McpResult<BulkMutationData>.Failure(
                "validation_error",
                "items must not be empty.",
                new ValidationErrorDetails { Field = nameof(request.Items), Reason = "At least one item is required." }));
        }

        return mediator.Send(new BulkUpsertEndpoints.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "bulk_upsert_relation"), Description(
        "Batch create or update use-case steps. Each item selects the use case by serviceName + useCaseName and the step by stepName. Partial success is allowed.")]
    public Task<McpResult<BulkMutationData>> BulkUpsertRelation(
        [Description("Batch of relation/step upserts.")] BulkUpsertRelationsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Length == 0)
        {
            return Task.FromResult(McpResult<BulkMutationData>.Failure(
                "validation_error",
                "items must not be empty.",
                new ValidationErrorDetails { Field = nameof(request.Items), Reason = "At least one item is required." }));
        }

        return mediator.Send(new BulkUpsertRelations.Command(request), cancellationToken).AsTask();
    }
}
