using GraphifyCode.Data.Context;
using GraphifyCode.Data.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.MCP.Features;

public static class GetUseCasesDetails
{
    public record Request(Guid[] UseCaseIds) : IRequest<UseCasesDetails>;

    public class Handler(GraphifyContext context) : IRequestHandler<Request, UseCasesDetails>
    {
        public async ValueTask<UseCasesDetails> Handle(Request request, CancellationToken cancellationToken)
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var useCases = services
                .SelectMany(s => s.UseCases)
                .Where(u => request.UseCaseIds.Contains(u.Id))
                .ToArray();

            return UseCasesDetails.FromEntities(useCases);
        }
    }
}
