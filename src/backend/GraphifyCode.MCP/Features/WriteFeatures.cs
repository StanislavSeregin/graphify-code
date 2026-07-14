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

            if (!PathIdentity.IsValidSegment(request.Name))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "Service name is not a valid path identity segment.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(request.Name),
                        Reason = "Name must be a non-empty filesystem-safe segment."
                    });
            }

            var entity = await context.Services
                .FirstOrDefaultAsync(s => s.Name == request.Name, cancellationToken);

            var action = entity is null ? "created" : "updated";
            entity ??= new Service
            {
                Name = request.Name,
                Description = request.Description,
                RelativeCodePath = request.RelativeCodePath,
                LastAnalyzedAt = DateTime.UtcNow,
                Endpoints = null,
                UseCases = []
            };

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
                EntityName = entity.Name,
                ServiceName = entity.Name,
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
            var service = await context.Services
                .FirstOrDefaultAsync(s => s.Name == request.ServiceName, cancellationToken);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = request.ServiceName
                    });
            }

            service.Endpoints ??= new Endpoints { Parent = service, EndpointList = [] };
            service.Endpoints.Parent = service;

            var endpoint = service.Endpoints.EndpointList
                .FirstOrDefault(e => PathIdentity.NamesEqual(e.Name, request.Name));

            var action = endpoint is null ? "created" : "updated";
            endpoint ??= new Endpoint
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                RelativeCodePath = request.RelativeCodePath,
                LastAnalyzedAt = DateTime.UtcNow
            };

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
                EntityName = endpoint.Name,
                ServiceName = service.Name,
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

            if (!PathIdentity.IsValidSegment(request.Name))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "Use case name is not a valid path identity segment.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(request.Name),
                        Reason = "Name must be a non-empty filesystem-safe segment."
                    });
            }

            var service = await context.Services
                .FirstOrDefaultAsync(s => s.Name == request.ServiceName, cancellationToken);
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Service '{request.ServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = request.ServiceName
                    });
            }

            if (!HasEndpoint(service, request.InitiatingEndpointName))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "Initiating endpoint does not belong to the target service.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(request.InitiatingEndpointName),
                        Reason = "Endpoint must exist inside the target service."
                    });
            }

            var useCase = service.UseCases
                .FirstOrDefault(u => PathIdentity.NamesEqual(u.Name, request.Name));

            var action = useCase is null ? "created" : "updated";
            if (useCase is null)
            {
                useCase = new UseCase
                {
                    Parent = service,
                    Name = request.Name,
                    Description = request.Description,
                    InitiatingEndpointName = request.InitiatingEndpointName,
                    LastAnalyzedAt = DateTime.UtcNow,
                    Steps = []
                };
                context.Add(useCase);
            }

            useCase.Parent = service;
            useCase.Description = request.Description;
            useCase.InitiatingEndpointName = request.InitiatingEndpointName;
            useCase.LastAnalyzedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.UseCase,
                EntityName = useCase.Name,
                ServiceName = service.Name,
                Action = action
            });
        }

        private static bool HasEndpoint(Service service, string endpointName)
        {
            return service.Endpoints?.EndpointList.Any(e => PathIdentity.NamesEqual(e.Name, endpointName)) is true;
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
            var useCase = await context.Services
                .Where(s => s.Name == request.ServiceName)
                .SelectMany(s => s.UseCases)
                .FirstOrDefaultAsync(uc => uc.Name == request.UseCaseName, cancellationToken);
            if (useCase is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Use case '{request.UseCaseName}' not found in service '{request.ServiceName}'.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.UseCase,
                        EntityName = request.UseCaseName,
                        ServiceName = request.ServiceName
                    });
            }

            if (request.EndpointName is { } endpointName)
            {
                var endpointServiceName = request.RelatedServiceName ?? request.ServiceName;
                var endpointExists = context.Services.Any(s =>
                    PathIdentity.NamesEqual(s.Name, endpointServiceName)
                    && s.Endpoints != null
                    && s.Endpoints.EndpointList.Any(e => PathIdentity.NamesEqual(e.Name, endpointName)));
                if (!endpointExists)
                {
                    return McpResult<MutationResultData>.Failure(
                        "not_found",
                        $"Endpoint '{endpointName}' not found in service '{endpointServiceName}'.",
                        new NotFoundErrorDetails
                        {
                            EntityType = GraphEntityType.Endpoint,
                            EntityName = endpointName,
                            ServiceName = endpointServiceName
                        });
                }
            }

            if (request.RelatedServiceName is { } relatedServiceName
                && !context.Services.Any(s => PathIdentity.NamesEqual(s.Name, relatedServiceName)))
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Service '{relatedServiceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = relatedServiceName
                    });
            }

            var step = useCase.Steps.FirstOrDefault(s => s.Name.Equals(request.StepName, StringComparison.OrdinalIgnoreCase));
            var action = step is null ? "created" : "updated";
            step ??= new UseCaseStep
            {
                Name = request.StepName,
                Description = request.StepDescription,
                ServiceName = request.RelatedServiceName,
                EndpointName = request.EndpointName,
                RelativeCodePath = request.RelativeCodePath
            };

            step.Name = request.StepName;
            step.Description = request.StepDescription;
            step.ServiceName = request.RelatedServiceName;
            step.EndpointName = request.EndpointName;
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
                EntityName = useCase.Name,
                ServiceName = request.ServiceName,
                Action = action,
                Message = $"Relation step '{step.Name}' {action}."
            });
        }
    }
}

public static class RemoveEntity
{
    public sealed record Command(string EntityName, GraphEntityType EntityType, string? ServiceName) : ICommand<McpResult<MutationResultData>>;

    public sealed class Handler(GraphifyContext context) : ICommandHandler<Command, McpResult<MutationResultData>>
    {
        public async ValueTask<McpResult<MutationResultData>> Handle(Command command, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            return command.EntityType switch
            {
                GraphEntityType.Service => await RemoveService(command.EntityName, cancellationToken),
                GraphEntityType.Endpoint => await RemoveEndpoint(command.EntityName, command.ServiceName, cancellationToken),
                GraphEntityType.UseCase => await RemoveUseCase(command.EntityName, command.ServiceName, cancellationToken),
                _ => McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "Unsupported entity type.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(command.EntityType),
                        Reason = "Value must be Service, Endpoint, or UseCase."
                    })
            };
        }

        private async Task<McpResult<MutationResultData>> RemoveService(string serviceName, CancellationToken cancellationToken)
        {
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var service = services.FirstOrDefault(s => PathIdentity.NamesEqual(s.Name, serviceName));
            if (service is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Service '{serviceName}' not found.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = serviceName
                    });
            }

            var blocking = services
                .Where(s => !PathIdentity.NamesEqual(s.Name, serviceName))
                .SelectMany(s => s.UseCases.Select(uc => (Service: s, UseCase: uc)))
                .Where(x => x.UseCase.Steps.Any(step => PathIdentity.NamesEqual(step.ServiceName, serviceName)))
                .Select(x => new UseCaseRef { ServiceName = x.Service.Name, UseCaseName = x.UseCase.Name })
                .DistinctBy(x => (x.ServiceName, x.UseCaseName))
                .ToArray();

            if (blocking.Length > 0)
            {
                return McpResult<MutationResultData>.Failure(
                    "conflict",
                    "Service is used by existing use cases.",
                    new ConflictErrorDetails
                    {
                        EntityType = GraphEntityType.Service,
                        EntityName = serviceName,
                        BlockingUseCases = blocking
                    });
            }

            context.Remove(service);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Service,
                EntityName = serviceName,
                ServiceName = serviceName,
                Action = "removed"
            });
        }

        private async Task<McpResult<MutationResultData>> RemoveEndpoint(string endpointName, string? serviceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "serviceName is required when removing an endpoint.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(serviceName),
                        Reason = "Endpoint identity is scoped by service."
                    });
            }

            var services = await context.Services.ToArrayAsync(cancellationToken);
            var service = services.FirstOrDefault(s => PathIdentity.NamesEqual(s.Name, serviceName));
            var endpoint = service?.Endpoints?.EndpointList
                .FirstOrDefault(e => PathIdentity.NamesEqual(e.Name, endpointName));
            if (service is null || endpoint is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Endpoint '{endpointName}' not found in service '{serviceName}'.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.Endpoint,
                        EntityName = endpointName,
                        ServiceName = serviceName
                    });
            }

            var blocking = services
                .SelectMany(s => s.UseCases.Select(uc => (Service: s, UseCase: uc)))
                .Where(x =>
                    (PathIdentity.NamesEqual(x.Service.Name, serviceName)
                     && PathIdentity.NamesEqual(x.UseCase.InitiatingEndpointName, endpointName))
                    || x.UseCase.Steps.Any(step =>
                        PathIdentity.NamesEqual(step.EndpointName, endpointName)
                        && (step.ServiceName is null
                            ? PathIdentity.NamesEqual(x.Service.Name, serviceName)
                            : PathIdentity.NamesEqual(step.ServiceName, serviceName))))
                .Select(x => new UseCaseRef { ServiceName = x.Service.Name, UseCaseName = x.UseCase.Name })
                .DistinctBy(x => (x.ServiceName, x.UseCaseName))
                .ToArray();

            if (blocking.Length > 0)
            {
                return McpResult<MutationResultData>.Failure(
                    "conflict",
                    "Endpoint is used by existing use cases.",
                    new ConflictErrorDetails
                    {
                        EntityType = GraphEntityType.Endpoint,
                        EntityName = endpointName,
                        ServiceName = serviceName,
                        BlockingUseCases = blocking
                    });
            }

            context.Remove(endpoint);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.Endpoint,
                EntityName = endpointName,
                ServiceName = serviceName,
                Action = "removed"
            });
        }

        private async Task<McpResult<MutationResultData>> RemoveUseCase(string useCaseName, string? serviceName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return McpResult<MutationResultData>.Failure(
                    "validation_error",
                    "serviceName is required when removing a use case.",
                    new ValidationErrorDetails
                    {
                        Field = nameof(serviceName),
                        Reason = "Use case identity is scoped by service."
                    });
            }

            var services = await context.Services.ToArrayAsync(cancellationToken);
            var useCase = services
                .Where(s => PathIdentity.NamesEqual(s.Name, serviceName))
                .SelectMany(s => s.UseCases)
                .FirstOrDefault(uc => PathIdentity.NamesEqual(uc.Name, useCaseName));
            if (useCase is null)
            {
                return McpResult<MutationResultData>.Failure(
                    "not_found",
                    $"Use case '{useCaseName}' not found in service '{serviceName}'.",
                    new NotFoundErrorDetails
                    {
                        EntityType = GraphEntityType.UseCase,
                        EntityName = useCaseName,
                        ServiceName = serviceName
                    });
            }

            context.Remove(useCase);
            await context.SaveChangesAsync(cancellationToken);
            return McpResult<MutationResultData>.Success(new MutationResultData
            {
                EntityType = GraphEntityType.UseCase,
                EntityName = useCaseName,
                ServiceName = serviceName,
                Action = "removed"
            });
        }
    }
}

public static class BulkUpsertEndpoints
{
    public sealed record Command(BulkUpsertEndpointsRequest Request) : ICommand<McpResult<BulkMutationData>>;

    public sealed class Handler(IMediator mediator) : ICommandHandler<Command, McpResult<BulkMutationData>>
    {
        public async ValueTask<McpResult<BulkMutationData>> Handle(Command command, CancellationToken cancellationToken)
        {
            var succeeded = new System.Collections.Generic.List<BulkMutationSuccessItem>();
            var failed = new System.Collections.Generic.List<BulkMutationFailedItem>();

            for (var index = 0; index < command.Request.Items.Length; index++)
            {
                var result = await mediator.Send(new UpsertEndpoint.Command(command.Request.Items[index]), cancellationToken);
                if (result.Ok && result.Data is { } mutation)
                {
                    succeeded.Add(new BulkMutationSuccessItem { Index = index, Result = mutation });
                }
                else
                {
                    failed.Add(new BulkMutationFailedItem
                    {
                        Index = index,
                        Code = result.Error?.Code ?? "internal_error",
                        Message = result.Error?.Message ?? "Unknown error",
                        Details = result.Error?.Details
                    });
                }
            }

            var batchData = new BulkMutationData
            {
                Succeeded = [.. succeeded],
                Failed = [.. failed]
            };

            if (succeeded.Count > 0)
            {
                string[] warnings = failed.Count > 0
                    ? [$"{failed.Count} items failed in batch."]
                    : Array.Empty<string>();
                return McpResult<BulkMutationData>.Success(
                    batchData,
                    warnings);
            }

            return McpResult<BulkMutationData>.Failure(
                "batch_failed",
                "No endpoints were upserted.",
                new BatchErrorDetails
                {
                    FailedItems = failed.Count,
                    TotalItems = command.Request.Items.Length
                });
        }
    }
}

public static class BulkUpsertRelations
{
    public sealed record Command(BulkUpsertRelationsRequest Request) : ICommand<McpResult<BulkMutationData>>;

    public sealed class Handler(IMediator mediator) : ICommandHandler<Command, McpResult<BulkMutationData>>
    {
        public async ValueTask<McpResult<BulkMutationData>> Handle(Command command, CancellationToken cancellationToken)
        {
            var succeeded = new System.Collections.Generic.List<BulkMutationSuccessItem>();
            var failed = new System.Collections.Generic.List<BulkMutationFailedItem>();

            for (var index = 0; index < command.Request.Items.Length; index++)
            {
                var result = await mediator.Send(new UpsertRelation.Command(command.Request.Items[index]), cancellationToken);
                if (result.Ok && result.Data is { } mutation)
                {
                    succeeded.Add(new BulkMutationSuccessItem { Index = index, Result = mutation });
                }
                else
                {
                    failed.Add(new BulkMutationFailedItem
                    {
                        Index = index,
                        Code = result.Error?.Code ?? "internal_error",
                        Message = result.Error?.Message ?? "Unknown error",
                        Details = result.Error?.Details
                    });
                }
            }

            var batchData = new BulkMutationData
            {
                Succeeded = [.. succeeded],
                Failed = [.. failed]
            };

            if (succeeded.Count > 0)
            {
                string[] warnings = failed.Count > 0
                    ? [$"{failed.Count} items failed in batch."]
                    : Array.Empty<string>();
                return McpResult<BulkMutationData>.Success(
                    batchData,
                    warnings);
            }

            return McpResult<BulkMutationData>.Failure(
                "batch_failed",
                "No relations were upserted.",
                new BatchErrorDetails
                {
                    FailedItems = failed.Count,
                    TotalItems = command.Request.Items.Length
                });
        }
    }
}
