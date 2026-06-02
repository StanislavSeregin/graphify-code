using GraphifyCode.Data.Context;
using GraphifyCode.Data.Entities;
using GraphifyCode.MCP.Contracts;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class UpsertService
{
    public sealed record Command(UpsertServiceRequest Request) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var request = command.Request;

            var entity = request.ServiceId is { } serviceId
                ? await context.Services.FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken)
                : null;

            var action = entity is null ? "created" : "updated";
            entity ??= new Service
            {
                Id = request.ServiceId ?? Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                RelativeCodePath = request.RelativeCodePath,
                LastAnalyzedAt = DateTime.UtcNow,
                Endpoints = null,
                UseCases = []
            };

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.RelativeCodePath = request.RelativeCodePath;
            entity.LastAnalyzedAt = DateTime.UtcNow;

            if (action == "created")
            {
                context.Services.Add(entity);
            }

            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Service,
                EntityId = entity.Id,
                Action = action
            });
        }
    }
}

public static class UpsertEndpoint
{
    public sealed record Command(UpsertEndpointRequest Request) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var request = command.Request;
            var service = await context.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Service '{request.ServiceId}' not found.");
            }

            service.Endpoints ??= new Endpoints { Parent = service, EndpointList = [] };
            service.Endpoints.Parent = service;

            var endpoint = request.EndpointId is { } endpointId
                ? service.Endpoints.EndpointList.FirstOrDefault(e => e.Id == endpointId)
                : null;

            var action = endpoint is null ? "created" : "updated";
            endpoint ??= new Endpoint
            {
                Id = request.EndpointId ?? Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                RelativeCodePath = request.RelativeCodePath,
                LastAnalyzedAt = DateTime.UtcNow
            };

            endpoint.Name = request.Name;
            endpoint.Description = request.Description;
            endpoint.Type = request.Type;
            endpoint.RelativeCodePath = request.RelativeCodePath;
            endpoint.LastAnalyzedAt = DateTime.UtcNow;

            if (action == "created")
            {
                service.Endpoints.EndpointList.Add(endpoint);
            }

            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Endpoint,
                EntityId = endpoint.Id,
                Action = action
            });
        }
    }
}

public static class UpsertUseCase
{
    public sealed record Command(UpsertUseCaseRequest Request) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var request = command.Request;
            var service = await context.Services.FirstOrDefaultAsync(s => s.Id == request.ServiceId, cancellationToken);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Service '{request.ServiceId}' not found.");
            }

            if (!HasEndpoint(service, request.InitiatingEndpointId))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "Initiating endpoint does not belong to the target service.");
            }

            var useCase = request.UseCaseId is { } useCaseId
                ? service.UseCases.FirstOrDefault(u => u.Id == useCaseId)
                : null;

            var action = useCase is null ? "created" : "updated";
            if (useCase is null)
            {
                useCase = new UseCase
                {
                    Id = request.UseCaseId ?? Guid.NewGuid(),
                    Parent = service,
                    Name = request.Name,
                    Description = request.Description,
                    InitiatingEndpointId = request.InitiatingEndpointId,
                    LastAnalyzedAt = DateTime.UtcNow,
                    Steps = []
                };
                context.Add(useCase);
            }

            useCase.Parent = service;
            useCase.Name = request.Name;
            useCase.Description = request.Description;
            useCase.InitiatingEndpointId = request.InitiatingEndpointId;
            useCase.LastAnalyzedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.UseCase,
                EntityId = useCase.Id,
                Action = action
            });
        }

        private static bool HasEndpoint(Service service, Guid endpointId)
        {
            return service.Endpoints?.EndpointList.Any(e => e.Id == endpointId) is true;
        }
    }
}

public static class UpsertRelation
{
    public sealed record Command(UpsertRelationRequest Request) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var request = command.Request;
            var service = await context.Services
                .FirstOrDefaultAsync(s => s.UseCases.Any(uc => uc.Id == request.UseCaseId), cancellationToken);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Use case '{request.UseCaseId}' not found.");
            }

            var useCase = service.UseCases.First(uc => uc.Id == request.UseCaseId);
            if (request.EndpointId is { } endpointId && !context.Services.Any(s => s.Endpoints != null
                && s.Endpoints.EndpointList.Any(e => e.Id == endpointId)))
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Endpoint '{endpointId}' not found.");
            }

            if (request.ServiceId is { } relatedServiceId && !context.Services.Any(s => s.Id == relatedServiceId))
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Service '{relatedServiceId}' not found.");
            }

            var step = useCase.Steps.FirstOrDefault(s => s.Name.Equals(request.StepName, StringComparison.OrdinalIgnoreCase));
            var action = step is null ? "created" : "updated";
            step ??= new UseCaseStep
            {
                Name = request.StepName,
                Description = request.StepDescription,
                ServiceId = request.ServiceId,
                EndpointId = request.EndpointId,
                RelativeCodePath = request.RelativeCodePath
            };

            step.Name = request.StepName;
            step.Description = request.StepDescription;
            step.ServiceId = request.ServiceId;
            step.EndpointId = request.EndpointId;
            step.RelativeCodePath = request.RelativeCodePath;

            if (action == "created")
            {
                useCase.Steps.Add(step);
            }

            useCase.LastAnalyzedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.UseCase,
                EntityId = useCase.Id,
                Action = action,
                Message = $"Relation step '{step.Name}' {action}."
            });
        }
    }
}

public static class RemoveEntity
{
    public sealed record Command(Guid EntityId, GraphEntityType EntityType) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            return command.EntityType switch
            {
                GraphEntityType.Service => await RemoveService(command.EntityId, cancellationToken),
                GraphEntityType.Endpoint => await RemoveEndpoint(command.EntityId, cancellationToken),
                GraphEntityType.UseCase => await RemoveUseCase(command.EntityId, cancellationToken),
                _ => McpResult<MutationResultData>.Failure("validation_error", "Unsupported entity type.")
            };
        }

        private async Task<McpResult<MutationResultData>> RemoveService(Guid serviceId, CancellationToken cancellationToken)
        {
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var service = services.FirstOrDefault(s => s.Id == serviceId);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Service '{serviceId}' not found.");
            }

            var relatedUseCaseIds = services
                .Where(s => s.Id != serviceId)
                .SelectMany(s => s.UseCases)
                .Where(uc => uc.Steps.Any(step => step.ServiceId == serviceId))
                .Select(uc => uc.Id)
                .Distinct()
                .ToArray();

            if (relatedUseCaseIds.Length > 0)
            {
                return McpResult<MutationResultData>.Failure(
                    "conflict",
                    "Service is used by existing use cases.",
                    new { UseCaseIds = relatedUseCaseIds });
            }

            context.Remove(service);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Service,
                EntityId = serviceId,
                Action = "removed"
            });
        }

        private async Task<McpResult<MutationResultData>> RemoveEndpoint(Guid endpointId, CancellationToken cancellationToken)
        {
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var endpoint = services
                .SelectMany(s => s.Endpoints?.EndpointList ?? [])
                .FirstOrDefault(e => e.Id == endpointId);
            if (endpoint is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Endpoint '{endpointId}' not found.");
            }

            var relatedUseCaseIds = services
                .SelectMany(s => s.UseCases)
                .Where(uc => uc.InitiatingEndpointId == endpointId || uc.Steps.Any(step => step.EndpointId == endpointId))
                .Select(uc => uc.Id)
                .Distinct()
                .ToArray();
            if (relatedUseCaseIds.Length > 0)
            {
                return McpResult<MutationResultData>.Failure(
                    "conflict",
                    "Endpoint is used by existing use cases.",
                    new { UseCaseIds = relatedUseCaseIds });
            }

            context.Remove(endpoint);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Endpoint,
                EntityId = endpointId,
                Action = "removed"
            });
        }

        private async Task<McpResult<MutationResultData>> RemoveUseCase(Guid useCaseId, CancellationToken cancellationToken)
        {
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var useCase = services.SelectMany(s => s.UseCases).FirstOrDefault(uc => uc.Id == useCaseId);
            if (useCase is null)
            {
                return McpResult<MutationResultData>.Failure("not_found", $"Use case '{useCaseId}' not found.");
            }

            context.Remove(useCase);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.UseCase,
                EntityId = useCaseId,
                Action = "removed"
            });
        }
    }
}
