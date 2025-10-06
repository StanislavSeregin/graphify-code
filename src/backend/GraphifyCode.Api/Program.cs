using GraphifyCode.Core.Settings;
using GraphifyCode.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Configure settings from appsettings.json and environment variables
        builder.Services.Configure<GraphifyCodeSettings>(builder.Configuration.GetSection("GraphifyCode"));

        // Add services to the container
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

        // Register data service
        builder.Services.AddSingleton<IDataService, DataService>();

        var app = builder.Build();

        app.UseCors();

        // GET /api/services - Get all services
        app.MapGet("/api/services", async (IDataService dataService, CancellationToken cancellationToken) =>
        {
            var services = await dataService.GetServices(cancellationToken);
            return Results.Ok(services);
        });

        // GET /api/endpoints/{serviceId} - Get endpoints for a service
        app.MapGet("/api/endpoints/{serviceId:guid}", async (Guid serviceId, IDataService dataService, CancellationToken cancellationToken) =>
        {
            var endpoints = await dataService.GetEndpoints(serviceId, cancellationToken);
            return Results.Ok(endpoints);
        });

        // GET /api/relations/{serviceId} - Get relations for a service
        app.MapGet("/api/relations/{serviceId:guid}", async (Guid serviceId, IDataService dataService, CancellationToken cancellationToken) =>
        {
            var relations = await dataService.GetRelations(serviceId, cancellationToken);
            return Results.Ok(relations);
        });

        // GET /api/use-cases/{serviceId} - Get use cases for a service
        app.MapGet("/api/use-cases/{serviceId:guid}", async (Guid serviceId, IDataService dataService, CancellationToken cancellationToken) =>
        {
            var useCases = await dataService.GetUseCases(serviceId, cancellationToken);
            return Results.Ok(useCases);
        });

        // GET /api/use-case/{useCaseId} - Get use case details
        app.MapGet("/api/use-case/{useCaseId:guid}", async (Guid useCaseId, IDataService dataService, CancellationToken cancellationToken) =>
        {
            var useCase = await dataService.GetUseCaseDetails(useCaseId, cancellationToken);
            return Results.Ok(useCase);
        });

        await app.RunAsync();
    }
}
