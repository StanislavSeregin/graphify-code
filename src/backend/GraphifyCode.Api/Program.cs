using GraphifyCode.Api.Models;
using GraphifyCode.Data;
using GraphifyCode.Data.Context;
using GraphifyCode.Data.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole();
        builder.Services
            .AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
            .AddGraphifyContext(builder.Configuration);

        var app = builder.Build();
        app.UseCors();
        app.MapGet("/api/full-graph", async (GraphifyContext context, CancellationToken cancellationToken) =>
        {
            await context.EnsureDataLoadedAsync(cancellationToken);
            var services = await context.Services.ToArrayAsync(cancellationToken);
            var data = ServicesDetails.FromEntities(services, true, true).ToMarkdown();
            return Results.Text(data);
        });

        await app.RunAsync();
    }
}
