using GraphifyCode.MCP.Contracts;
using GraphifyCode.MCP.Features;
using Mediator;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(IMediator mediator)
{
    [McpServerTool(Name = "list_services"), Description("List all known services with compact metadata. Use this first to discover IDs.")]
    public Task<McpResult<ListServicesData>> ListServices(CancellationToken cancellationToken)
    {
        return mediator.Send(new ListServices.Query(), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "get_service"), Description("Get one service by ID. Optionally include endpoints and use cases.")]
    public Task<McpResult<GetServiceData>> GetService(
        [Description("Target service ID from list_services.")] Guid serviceId,
        [Description("When true, include endpoint list in response payload and markdown.")] bool includeEndpoints,
        [Description("When true, include use case list in response payload and markdown.")] bool includeUseCases,
        CancellationToken cancellationToken)
    {
        if (serviceId == Guid.Empty)
        {
            return Task.FromResult(McpResult<GetServiceData>.Failure("validation_error", "serviceId must not be empty."));
        }

        return mediator.Send(new GetService.Query(serviceId, includeEndpoints, includeUseCases), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "get_use_case"), Description("Get one use case with detailed steps and markdown snapshot.")]
    public Task<McpResult<GetUseCaseData>> GetUseCase(
        [Description("Target use case ID.")] Guid useCaseId,
        CancellationToken cancellationToken)
    {
        if (useCaseId == Guid.Empty)
        {
            return Task.FromResult(McpResult<GetUseCaseData>.Failure("validation_error", "useCaseId must not be empty."));
        }

        return mediator.Send(new GetUseCase.Query(useCaseId), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "search_graph"), Description("Search by name, description, or code path across services, endpoints, and use cases.")]
    public Task<McpResult<SearchGraphData>> SearchGraph(
        [Description("Case-insensitive search query (min 2 chars).")] string query,
        [Description("Maximum number of matches to return (1-100).")] int limit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Task.FromResult(McpResult<SearchGraphData>.Failure("validation_error", "query must contain at least 2 non-space characters."));
        }

        if (limit is < 1 or > 100)
        {
            return Task.FromResult(McpResult<SearchGraphData>.Failure("validation_error", "limit must be between 1 and 100."));
        }

        return mediator.Send(new SearchGraph.Query(query, limit), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_service"), Description("Create a new service or update existing one. Provide serviceId to update.")]
    public Task<McpResult<MutationResultData>> UpsertService(
        [Description("Service payload for create or update operation.")] UpsertServiceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure("validation_error", "name and description are required."));
        }

        return mediator.Send(new UpsertService.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_endpoint"), Description("Create or update an endpoint inside a service.")]
    public Task<McpResult<MutationResultData>> UpsertEndpoint(
        [Description("Endpoint payload; serviceId is required.")] UpsertEndpointRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ServiceId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description)
            || string.IsNullOrWhiteSpace(request.Type))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure("validation_error", "serviceId, name, description and type are required."));
        }

        return mediator.Send(new UpsertEndpoint.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_use_case"), Description("Create or update a use case under a service.")]
    public Task<McpResult<MutationResultData>> UpsertUseCase(
        [Description("Use case payload; serviceId and initiatingEndpointId are required.")] UpsertUseCaseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ServiceId == Guid.Empty
            || request.InitiatingEndpointId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure("validation_error", "serviceId, initiatingEndpointId, name and description are required."));
        }

        return mediator.Send(new UpsertUseCase.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "upsert_relation"), Description("Create or update one relation step inside an existing use case.")]
    public Task<McpResult<MutationResultData>> UpsertRelation(
        [Description("Relation payload; useCaseId, stepName and stepDescription are required.")] UpsertRelationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UseCaseId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.StepName)
            || string.IsNullOrWhiteSpace(request.StepDescription))
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure("validation_error", "useCaseId, stepName and stepDescription are required."));
        }

        return mediator.Send(new UpsertRelation.Command(request), cancellationToken).AsTask();
    }

    [McpServerTool(Name = "remove_entity"), Description("Remove service, endpoint, or use case by ID using a typed entity enum.")]
    public Task<McpResult<MutationResultData>> RemoveEntity(
        [Description("Target entity ID.")] Guid entityId,
        [Description("Entity type: Service, Endpoint, or UseCase.")] GraphEntityType entityType,
        CancellationToken cancellationToken)
    {
        if (entityId == Guid.Empty)
        {
            return Task.FromResult(McpResult<MutationResultData>.Failure("validation_error", "entityId must not be empty."));
        }

        return mediator.Send(new RemoveEntity.Command(entityId, entityType), cancellationToken).AsTask();
    }
}
