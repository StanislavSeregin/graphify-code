using GraphifyCode.Data.Experiment;
using GraphifyCode.Data.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GraphifyCode.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraphifyContext(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "MarkdownStorage"
    )
    {
        var section = configuration?.GetSection(sectionName) ?? throw new InvalidOperationException("Configuration not found");
        services.Configure<MarkdownStorageSettings>(section);
        return services.AddScoped<GraphifyContext>();
    }
}
