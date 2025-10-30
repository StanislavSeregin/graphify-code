using GraphifyCode.Data.Experiment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Api;

public class TestService(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GraphifyContext>();
        await context.EnsureDataLoadedAsync(cancellationToken);
        var services = await context.Services.ToArrayAsync(cancellationToken);

        var usecase = services
            .Where(s => s.Id == Guid.Parse("b4562db7-4d58-430e-a288-42837cecae41"))
            .SelectMany(s => s.UseCases)
            .First();

        usecase.Description = usecase.Description + "!";
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
