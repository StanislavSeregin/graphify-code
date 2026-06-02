using GraphifyCode.Data.Context;
using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Models;
using GraphifyCode.MCP.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class ListServices
{
    public sealed record Query() : IRequest<McpResult<ListServicesData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<ListServicesData>>
    {
        public async ValueTask<McpResult<ListServicesData>> Handle(Query _, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .ToArrayAsync(cancellationToken);

            var data = ServicesOverview.FromEntities(services);
            return McpResult<ListServicesData>.Success(new ListServicesData
            {
                Services = data,
                Markdown = data.ToMarkdown()
            });
        }
    }
}

public static class GetService
{
    public sealed record Query(Guid ServiceId, bool IncludeEndpoints, bool IncludeUseCases) : IRequest<McpResult<GetServiceData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<GetServiceData>>
    {
        public async ValueTask<McpResult<GetServiceData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var service = await context.Services
                .AsNoTracking()
                .Where(s => s.Id == request.ServiceId)
                .ToArrayAsync(cancellationToken);

            if (service.Length == 0)
            {
                return McpResult<GetServiceData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceId}' not found.");
            }

            var details = ServicesDetails.FromEntities(service, request.IncludeEndpoints, request.IncludeUseCases);
            return McpResult<GetServiceData>.Success(new GetServiceData
            {
                Service = details,
                Markdown = details.ToMarkdown()
            });
        }
    }
}

public static class GetUseCase
{
    public sealed record Query(Guid UseCaseId) : IRequest<McpResult<GetUseCaseData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<GetUseCaseData>>
    {
        public async ValueTask<McpResult<GetUseCaseData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var useCase = await context.Services
                .AsNoTracking()
                .SelectMany(s => s.UseCases)
                .Where(uc => uc.Id == request.UseCaseId)
                .ToArrayAsync(cancellationToken);

            if (useCase.Length == 0)
            {
                return McpResult<GetUseCaseData>.Failure(
                    "not_found",
                    $"Use case '{request.UseCaseId}' not found.");
            }

            var details = UseCasesDetails.FromEntities(useCase);
            return McpResult<GetUseCaseData>.Success(new GetUseCaseData
            {
                UseCase = details,
                Markdown = details.ToMarkdown()
            });
        }
    }
}

public static class SearchGraph
{
    public sealed record Query(string QueryText, int Limit) : IRequest<McpResult<SearchGraphData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<SearchGraphData>>
    {
        public async ValueTask<McpResult<SearchGraphData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var normalized = request.QueryText.Trim();
            var services = await context.Services.AsNoTracking().ToArrayAsync(cancellationToken);

            bool Contains(string? value) => value?.Contains(normalized, StringComparison.OrdinalIgnoreCase) is true;
            var matches = services
                .SelectMany(service => SearchInService(service, Contains))
                .Take(request.Limit)
                .ToArray();

            return McpResult<SearchGraphData>.Success(new SearchGraphData
            {
                Matches = matches
            });
        }

        private static SearchMatch[] SearchInService(Service service, Func<string?, bool> contains)
        {
            SearchMatch[] serviceMatch = contains(service.Name) || contains(service.Description) || contains(service.RelativeCodePath)
                ? [new SearchMatch
                    {
                        EntityType = GraphEntityType.Service,
                        EntityId = service.Id,
                        ServiceId = service.Id,
                        Name = service.Name,
                        RelativeCodePath = service.RelativeCodePath
                    }]
                : [];

            var endpointMatches = (service.Endpoints?.EndpointList ?? [])
                .Where(endpoint => contains(endpoint.Name) || contains(endpoint.Description) || contains(endpoint.RelativeCodePath))
                .Select(endpoint => new SearchMatch
                {
                    EntityType = GraphEntityType.Endpoint,
                    EntityId = endpoint.Id,
                    ServiceId = service.Id,
                    Name = endpoint.Name,
                    RelativeCodePath = endpoint.RelativeCodePath
                });

            var useCaseMatches = (service.UseCases ?? [])
                .Where(useCase => contains(useCase.Name) || contains(useCase.Description))
                .Select(useCase => new SearchMatch
                {
                    EntityType = GraphEntityType.UseCase,
                    EntityId = useCase.Id,
                    ServiceId = service.Id,
                    Name = useCase.Name,
                    RelativeCodePath = null
                });

            return [.. serviceMatch, .. endpointMatches, .. useCaseMatches];
        }
    }
}
