using GraphifyCode.Data.Context;
using GraphifyCode.Data.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class GetServicesDetails
{
    public record Request(
        Guid[] ServiceIds,
        bool ShowEndpoints,
        bool ShowUseCases
    ) : IRequest<ServicesDetails>;

    public class Handler(GraphifyContext context) : IRequestHandler<Request, ServicesDetails>
    {
        public async ValueTask<ServicesDetails> Handle(Request request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var filteredServices = services
                .Where(s => request.ServiceIds.Contains(s.Id))
                .ToArray();

            return ServicesDetails.FromEntities(filteredServices, request.ShowEndpoints, request.ShowUseCases);
        }
    }
}
