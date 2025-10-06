using GraphifyCode.Data.Services;
using GraphifyCode.Data.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

public class Program
{
    private const string PATH_ENV_KEY = "GRAPHIFY_CODE_DATA_PATH";

    static Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Logging.AddConsole();
        builder.Services
            .Configure<MarkdownStorageSettings>(options =>
            {
                options.Path = GetDataPath();
                Console.WriteLine($"Selected path is {options.Path}");
            })
            .AddSingleton<IDataService, DataService>()
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        var app = builder.Build();
        return app.RunAsync();
    }

    private static string GetDataPath()
    {
        return Environment.GetEnvironmentVariable(PATH_ENV_KEY)
            ?? Environment.ProcessPath
            ?? throw new InvalidOperationException($"Data path not specified, set {PATH_ENV_KEY}");
    }
}
