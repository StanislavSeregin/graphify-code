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
    [McpServerTool, Description("???")]
    public async Task<string> GetServicesOverview(CancellationToken cancellationToken)
    {
        var servicesOverview = await mediator.Send(new GetServicesOverview.Request(), cancellationToken);
        return servicesOverview.ToMarkdown();
    }

    [McpServerTool, Description("???")]
    public async Task<string> GetServicesDetails(
        [Description("???")] Guid[] serviceIds,
        [Description("???")] bool showEndpoints,
        [Description("???")] bool showUseCases,
        CancellationToken cancellationToken)
    {
        var request = new GetServicesDetails.Request(serviceIds, showEndpoints, showUseCases);
        var servicesDetails = await mediator.Send(request, cancellationToken);
        return servicesDetails.ToMarkdown();
    }

    [McpServerTool, Description("???")]
    public async Task<string> GetUseCasesDetails(
        [Description("???")] Guid[] useCaseIds,
        CancellationToken cancellationToken)
    {
        var request = new GetUseCasesDetails.Request(useCaseIds);
        var useCasesDetails = await mediator.Send(request, cancellationToken);
        return useCasesDetails.ToMarkdown();
    }

    [McpServerTool, Description("???")]
    public async Task<string> Remove(
        [Description("???")] Guid id,
        [Description("???")] string type,
        CancellationToken cancellationToken)
    {

        var command = new Remove.Command(id, type);
        return await mediator.Send(command, cancellationToken);
    }
}
