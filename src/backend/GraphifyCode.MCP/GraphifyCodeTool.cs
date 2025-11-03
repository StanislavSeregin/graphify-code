using GraphifyCode.Data.Context;
using GraphifyCode.Data.Models;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

[McpServerToolType]
public class GraphifyCodeTool(GraphifyContext context)
{
    [McpServerTool, Description("""
    ???
    """)]
    public async Task<string> GetServicesOverview(CancellationToken cancellationToken)
    {
        try
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            return ServicesOverview
                .FromEntities(services)
                .ToMarkdown();
        }
        catch (Exception ex)
        {
            return "???";
        }
    }

    [McpServerTool, Description("""
    ???
    """)]
    public async Task<string> GetServicesDetails(
        [Description("???")] Guid[] serviceIds,
        [Description("???")] bool showEndpoints,
        [Description("???")] bool showUseCases,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var filteredServices = services
                .Where(s => serviceIds.Contains(s.Id))
                .ToArray();

            return ServicesDetails
                .FromEntities(filteredServices, showEndpoints, showUseCases)
                .ToMarkdown();
        }
        catch (Exception ex)
        {
            return "???";
        }
    }

    [McpServerTool, Description("""
    ???
    """)]
    public async Task<string> GetUseCasesDetails(
        [Description("???")] Guid[] useCaseIds,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var useCases = services
                .SelectMany(s => s.UseCases)
                .Where(u => useCaseIds.Contains(u.Id))
                .ToArray();

            return UseCasesDetails
                .FromEntities(useCases)
                .ToMarkdown();
        }
        catch (Exception ex)
        {
            return "???";
        }
    }

    [McpServerTool, Description("""
    ???
    """)]
    public async Task<string> Remove(
        [Description("???")] Guid id,
        [Description("???")] string type,
        CancellationToken cancellationToken)
    {
        try
        {
            return type switch
            {
                "service" => await RemoveService(id, cancellationToken),
                "endpoint" => await RemoveEndpoint(id, cancellationToken),
                "usecase" => await RemoveUseCase(id, cancellationToken),
                _ => "???"
            };
        }
        catch (Exception ex)
        {
            return "???";
        }
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
