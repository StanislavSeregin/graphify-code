using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Services;
using GraphifyCode.Data.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        builder.Services.Configure<MarkdownStorageSettings>(builder.Configuration.GetSection("MarkdownStorage"));

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

        // GET /api/full-graph - Get all data
        app.MapGet("/api/full-graph", async (IDataService dataService, CancellationToken cancellationToken) =>
        {
            var fullGraph = await dataService.GetFullGraph(cancellationToken);
            return Results.Ok(fullGraph);
        });

        await app.RunAsync();
    }
}
