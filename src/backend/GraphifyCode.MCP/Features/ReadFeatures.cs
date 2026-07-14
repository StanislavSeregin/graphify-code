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
    public sealed record Query(string ServiceName, bool IncludeEndpoints, bool IncludeUseCases) : IRequest<McpResult<GetServiceData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<GetServiceData>>
    {
        public async ValueTask<McpResult<GetServiceData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var service = await context.Services
                .AsNoTracking()
                .Where(s => s.Name == request.ServiceName)
                .ToArrayAsync(cancellationToken);

            if (service.Length == 0)
            {
                return McpResult<GetServiceData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = request.ServiceName
                    });
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
    public sealed record Query(string ServiceName, string UseCaseName) : IRequest<McpResult<GetUseCaseData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<GetUseCaseData>>
    {
        public async ValueTask<McpResult<GetUseCaseData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var useCase = await context.Services
                .AsNoTracking()
                .Where(s => s.Name == request.ServiceName)
                .SelectMany(s => s.UseCases)
                .Where(uc => uc.Name == request.UseCaseName)
                .ToArrayAsync(cancellationToken);

            if (useCase.Length == 0)
            {
                return McpResult<GetUseCaseData>.Failure(
                    "not_found",
                    $"Use case '{request.UseCaseName}' not found in service '{request.ServiceName}'.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.UseCase,
                        EntityName = request.UseCaseName,
                        ServiceName = request.ServiceName
                    });
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

public static class ListEndpoints
{
    public sealed record Query(string ServiceName) : IRequest<McpResult<ListEndpointsData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<ListEndpointsData>>
    {
        public async ValueTask<McpResult<ListEndpointsData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var service = await context.Services
                .FirstOrDefaultAsync(s => s.Name == request.ServiceName, cancellationToken);

            if (service is null)
            {
                return McpResult<ListEndpointsData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = request.ServiceName
                    });
            }

            var data = new ListEndpointsData
            {
                ServiceName = service.Name,
                Endpoints = [.. (service.Endpoints?.EndpointList ?? []).Select(endpoint => new EndpointSummary
                {
                    Name = endpoint.Name,
                    Description = endpoint.Description,
                    Type = endpoint.Type,
                    RelativeCodePath = endpoint.RelativeCodePath
                })]
            };

            return McpResult<ListEndpointsData>.Success(data);
        }
    }
}

public static class ListUseCases
{
    public sealed record Query(string ServiceName) : IRequest<McpResult<ListUseCasesData>>;

    public sealed class Handler(GraphifyContext context) : IRequestHandler<Query, McpResult<ListUseCasesData>>
    {
        public async ValueTask<McpResult<ListUseCasesData>> Handle(Query request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var service = await context.Services
                .FirstOrDefaultAsync(s => s.Name == request.ServiceName, cancellationToken);

            if (service is null)
            {
                return McpResult<ListUseCasesData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = request.ServiceName
                    });
            }

            var data = new ListUseCasesData
            {
                ServiceName = service.Name,
                UseCases = [.. (service.UseCases ?? []).Select(useCase => new UseCaseSummary
                {
                    Name = useCase.Name,
                    Description = useCase.Description,
                    InitiatingEndpointName = useCase.InitiatingEndpointName
                })]
            };

            return McpResult<ListUseCasesData>.Success(data);
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
                        EntityName = service.Name,
                        ServiceName = service.Name,
                        RelativeCodePath = service.RelativeCodePath
                    }]
                : [];

            var endpointMatches = (service.Endpoints?.EndpointList ?? [])
                .Where(endpoint => contains(endpoint.Name) || contains(endpoint.Description) || contains(endpoint.RelativeCodePath))
                .Select(endpoint => new SearchMatch
                {
                    EntityType = GraphEntityType.Endpoint,
                    EntityName = endpoint.Name,
                    ServiceName = service.Name,
                    RelativeCodePath = endpoint.RelativeCodePath
                });

            var useCaseMatches = (service.UseCases ?? [])
                .Where(useCase => contains(useCase.Name) || contains(useCase.Description))
                .Select(useCase => new SearchMatch
                {
                    EntityType = GraphEntityType.UseCase,
                    EntityName = useCase.Name,
                    ServiceName = service.Name,
                    RelativeCodePath = null
                });

            return [.. serviceMatch, .. endpointMatches, .. useCaseMatches];
        }
    }
}
