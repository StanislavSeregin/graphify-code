using GraphifyCode.Data.Context;
using GraphifyCode.Data.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class GetServicesOverview
{
    public record Request() : IRequest<ServicesOverview>;

    public class Handler(GraphifyContext context) : IRequestHandler<Request, ServicesOverview>
    {
        public async ValueTask<ServicesOverview> Handle(Request _, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            return ServicesOverview.FromEntities(services);
        }
    }
}
