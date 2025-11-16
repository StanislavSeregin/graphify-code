using GraphifyCode.Data.Context;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class Remove
{
    public record Command(Guid Id, string Type) : ICommand<string>;

    public class Handler(GraphifyContext context) : ICommandHandler<Command, string>
    {
        public async ValueTask<string> Handle(Command command, CancellationToken cancellationToken)
        {
            return command.Type switch
            {
                "service" => await RemoveService(command.Id, cancellationToken),
                "endpoint" => await RemoveEndpoint(command.Id, cancellationToken),
                "usecase" => await RemoveUseCase(command.Id, cancellationToken),
                _ => "???"
            };
        }

        private async Task<string> RemoveService(Guid id, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            if (services.FirstOrDefault(s => s.Id == id) is { } service)
            {
                var relatedUseCaseIds = services
                    .Where(s => s.Id != id)
                    .SelectMany(s => s.UseCases)
                    .Where(u => u.Steps.Any(st => st.ServiceId == id))
                    .Select(u => u.Id)
                    .ToArray();

                if (relatedUseCaseIds.Length > 0)
                {
                    return $"Can not remove service, because used in usecases: [{string.Join(',', relatedUseCaseIds)}]";
                }
                else
                {
                    context.Remove(service);
                    await context.SaveChangesAsync(cancellationToken);
                    return "Service removed";
                }
            }
            else
            {
                return "Service not found";
            }
        }

        private async Task<string> RemoveEndpoint(Guid id, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            if (services.SelectMany(s => s.Endpoints?.EndpointList ?? []).FirstOrDefault(e => e.Id == id) is { } endpoint)
            {
                var relatedUseCaseIds = services
                    .SelectMany(s => s.UseCases)
                    .Where(u => u.InitiatingEndpointId == id
                        || u.Steps.Any(st => st.EndpointId == id))
                    .Select(u => u.Id)
                    .ToArray();

                if (relatedUseCaseIds.Length > 0)
                {
                    return $"Can not remove endpoint, because used in usecases: [{string.Join(',', relatedUseCaseIds)}]";
                }
                else
                {
                    context.Remove(endpoint);
                    await context.SaveChangesAsync(cancellationToken);
                    return "Endpoint removed";
                }
            }
            else
            {
                return "Endpoint not found";
            }
        }

        private async Task<string> RemoveUseCase(Guid id, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            if (services.SelectMany(s => s.UseCases).FirstOrDefault(u => u.Id == id) is { } useCase)
            {
                context.Remove(useCase);
                await context.SaveChangesAsync(cancellationToken);
                return "Usecase removed";
            }
            else
            {
                return "Usecase not found";
            }
        }
    }
}
