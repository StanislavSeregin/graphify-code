using GraphifyCode.Core.Models;
using GraphifyCode.Core.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GraphifyCode.Core.Services;

public class GraphifyCodeDataService(IOptions<GraphifyCodeSettings> options)
{
    private const string SERVICE_NODE_FILE_NAME = "index.json";

    private const string ENDPOINTS_FILE_NAME = "endpoints.json";

    private const string RELATIONS_FILE_NAME = "relations.json";

    private readonly GraphifyCodeSettings _settings = options.Value;

    public Dictionary<Guid, ServiceNode> GetServices()
    {
        return Directory
            .GetDirectories(_settings.GraphifyCodeDataPath)
            .Select(dirPath => Path.Combine(dirPath, SERVICE_NODE_FILE_NAME))
            .Where(File.Exists)
            .Select(File.ReadAllText)
            .Where(json => !string.IsNullOrWhiteSpace(json))
            .Select(json => JsonSerializer.Deserialize<ServiceNode>(json))
            .ToDictionary(node => node!.Id, node => node!);
    }

    public ServiceEndpoint[] GetServiceEndpoints(Guid serviceId)
    {
        var endpointsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), ENDPOINTS_FILE_NAME);
        return File.Exists(endpointsPath)
                && File.ReadAllText(endpointsPath) is { } json
                && !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<ServiceEndpoint[]>(json)!
            : [];
    }

    public ServiceRelation[] GetServiceRelations(Guid serviceId)
    {
        var endpointsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), RELATIONS_FILE_NAME);
        return File.Exists(endpointsPath)
                && File.ReadAllText(endpointsPath) is { } json
                && !string.IsNullOrWhiteSpace(json)
            ? JsonSerializer.Deserialize<ServiceRelation[]>(json)!
            : [];
    }
}
