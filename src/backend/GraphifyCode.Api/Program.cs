using GraphifyCode.Data;
using GraphifyCode.Data.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddGraphifyContext(builder.Configuration);
        builder.Services.AddHostedService<TestService>();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        builder.Services.AddSingleton<IDataService, DataService>();

        var app = builder.Build();

        app.UseCors();

        app.MapGet("/api/full-graph", async (IDataService dataService, CancellationToken cancellationToken) =>
        {
            var fullGraph = await dataService.GetFullGraph(cancellationToken);
            return Results.Ok(fullGraph);
        });

        await app.RunAsync();
    }
}
