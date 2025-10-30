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
    TODO
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
            return "TODO";
        }
    }

    [McpServerTool, Description("""
    TODO
    """)]
    public async Task<string> GetServicesDetails(
        [Description("TODO")] Guid[] serviceIds,
        [Description("TODO")] bool showEndpoints,
        [Description("TODO")] bool showUseCases,
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
            return "TODO";
        }
    }
}
