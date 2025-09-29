using GraphifyCode.Core.Services;
using GraphifyCode.Core.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GraphifyCode.MCP;

public class Program
{
    static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole();
        builder.Services
            .Configure<GraphifyCodeSettings>(options =>
            {
                options.GraphifyCodeDataPath = System.Environment.GetEnvironmentVariable("GRAPHIFY_CODE_DATA_PATH")
                    ?? throw new System.InvalidOperationException("GRAPHIFY_CODE_DATA_PATH environment variable is required");
            })
            .AddSingleton<GraphifyCodeDataService>()
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        var app = builder.Build();
        return app.RunAsync();
    }
}
