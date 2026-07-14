using GraphifyCode.Data;
using GraphifyCode.Data.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

public class Program
{
    private const string PATH_ENV_KEY = "DATA_PATH";

    static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole();
        builder.Services
            .Configure<MarkdownStorageSettings>(options =>
            {
                options.Path = GetDataPath();
                Console.WriteLine($"Using DATA_PATH '{options.Path}'");
            })
            .AddMediator()
            .AddGraphifyContext(builder.Configuration)
            .AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = true;
            })
            .WithToolsFromAssembly();

        var app = builder.Build();
        app.MapMcp();
        return app.RunAsync();
    }

    private static string GetDataPath()
    {
        return Environment.GetEnvironmentVariable(PATH_ENV_KEY)
            ?? Path.Combine(Directory.GetCurrentDirectory(), "graph-data");
    }
}
