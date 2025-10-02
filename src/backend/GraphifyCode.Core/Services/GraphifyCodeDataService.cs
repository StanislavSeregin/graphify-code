using GraphifyCode.Core.Models;
using GraphifyCode.Core.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Core.Services;

public class GraphifyCodeDataService(IOptions<GraphifyCodeSettings> options)
{
    private const string SERVICE_NODE_FILE_NAME = "index.json";

    private const string ENDPOINTS_FILE_NAME = "endpoints.json";

    private const string RELATIONS_FILE_NAME = "relations.json";

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly GraphifyCodeSettings _settings = options.Value;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Dictionary<Guid, ServiceNode>> GetServices()
    {
        await _semaphore.WaitAsync();
        try
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
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ServiceEndpoint[]> GetServiceEndpoints(Guid serviceId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var endpointsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), ENDPOINTS_FILE_NAME);
            return File.Exists(endpointsPath)
                    && File.ReadAllText(endpointsPath) is { } json
                    && !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.Deserialize<ServiceEndpoint[]>(json)!
                : [];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ServiceRelation[]> GetServiceRelations(Guid serviceId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var endpointsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), RELATIONS_FILE_NAME);
            return File.Exists(endpointsPath)
                    && File.ReadAllText(endpointsPath) is { } json
                    && !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.Deserialize<ServiceRelation[]>(json)!
                : [];
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Guid> CreateOrUpdateService(string name, string description, Guid? serviceId = null, string? codePath = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var id = serviceId ?? Guid.NewGuid();
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, id.ToString());

            Directory.CreateDirectory(serviceDir);

            var serviceNode = new ServiceNode
            {
                Id = id,
                Name = name,
                Description = description,
                Metadata = new AnalysisMetadata
                {
                    LastAnalyzedAt = DateTime.UtcNow,
                    RelativeCodePath = codePath
                }
            };

            var json = JsonSerializer.Serialize(serviceNode, _jsonSerializerOptions);
            var filePath = Path.Combine(serviceDir, SERVICE_NODE_FILE_NAME);
            await File.WriteAllTextAsync(filePath, json);

            return id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Guid> CreateOrUpdateServiceEndpoint(Guid serviceId, string name, string description, string type, Guid? endpointId = null, string? codePath = null)
    {
        await _semaphore.WaitAsync();
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            if (!EndpointTypes.IsValidEndpointType(type))
            {
                throw new ArgumentException($"Invalid endpoint type: {type}. Must be one of: {EndpointTypes.Http}, {EndpointTypes.Queue}, {EndpointTypes.Job}");
            }

            var id = endpointId ?? Guid.NewGuid();
            var endpointsPath = Path.Combine(serviceDir, ENDPOINTS_FILE_NAME);

            var endpoints = File.Exists(endpointsPath)
                    && File.ReadAllText(endpointsPath) is { } json
                    && !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.Deserialize<List<ServiceEndpoint>>(json)!
                : [];

            var existingEndpoint = endpoints.FirstOrDefault(e => e.ServiceId == id);
            if (existingEndpoint != null)
            {
                endpoints.Remove(existingEndpoint);
            }

            var newEndpoint = new ServiceEndpoint
            {
                ServiceId = id,
                Name = name,
                Description = description,
                Type = type,
                Metadata = new AnalysisMetadata
                {
                    LastAnalyzedAt = DateTime.UtcNow,
                    RelativeCodePath = codePath
                }
            };

            endpoints.Add(newEndpoint);

            var endpointsJson = JsonSerializer.Serialize(endpoints, _jsonSerializerOptions);
            await File.WriteAllTextAsync(endpointsPath, endpointsJson);

            return id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddServiceRelation(Guid sourceServiceId, Guid targetEndpointId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, sourceServiceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {sourceServiceId} does not exist");
            }

            var relationsPath = Path.Combine(serviceDir, RELATIONS_FILE_NAME);

            var relations = File.Exists(relationsPath)
                    && File.ReadAllText(relationsPath) is { } json
                    && !string.IsNullOrWhiteSpace(json)
                ? JsonSerializer.Deserialize<List<ServiceRelation>>(json)!
                : [];

            var newRelation = new ServiceRelation(sourceServiceId, targetEndpointId);

            if (!relations.Any(r => r.SourceServiceId == sourceServiceId && r.TargetEndpointId == targetEndpointId))
            {
                relations.Add(newRelation);
            }

            var relationsJson = JsonSerializer.Serialize(relations, _jsonSerializerOptions);
            await File.WriteAllTextAsync(relationsPath, relationsJson);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteService(Guid serviceId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            var endpoints = await GetServiceEndpoints(serviceId);
            var endpointIds = endpoints.Select(e => e.ServiceId).ToHashSet();

            var allServiceDirs = Directory.GetDirectories(_settings.GraphifyCodeDataPath);
            foreach (var dir in allServiceDirs)
            {
                var relationsPath = Path.Combine(dir, RELATIONS_FILE_NAME);
                if (File.Exists(relationsPath))
                {
                    var json = File.ReadAllText(relationsPath);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var relations = JsonSerializer.Deserialize<List<ServiceRelation>>(json)!;
                        var originalCount = relations.Count;

                        relations.RemoveAll(r => endpointIds.Contains(r.TargetEndpointId));

                        if (relations.Count != originalCount)
                        {
                            var relationsJson = JsonSerializer.Serialize(relations, _jsonSerializerOptions);
                            await File.WriteAllTextAsync(relationsPath, relationsJson);
                        }
                    }
                }
            }

            Directory.Delete(serviceDir, true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteServiceEndpoint(Guid serviceId, Guid endpointId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            var endpointsPath = Path.Combine(serviceDir, ENDPOINTS_FILE_NAME);
            if (File.Exists(endpointsPath))
            {
                var json = File.ReadAllText(endpointsPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var endpoints = JsonSerializer.Deserialize<List<ServiceEndpoint>>(json)!;
                    var removed = endpoints.RemoveAll(e => e.ServiceId == endpointId);

                    if (removed > 0)
                    {
                        var endpointsJson = JsonSerializer.Serialize(endpoints, _jsonSerializerOptions);
                        await File.WriteAllTextAsync(endpointsPath, endpointsJson);

                        var allServiceDirs = Directory.GetDirectories(_settings.GraphifyCodeDataPath);
                        foreach (var dir in allServiceDirs)
                        {
                            var relationsPath = Path.Combine(dir, RELATIONS_FILE_NAME);
                            if (File.Exists(relationsPath))
                            {
                                var relJson = File.ReadAllText(relationsPath);
                                if (!string.IsNullOrWhiteSpace(relJson))
                                {
                                    var relations = JsonSerializer.Deserialize<List<ServiceRelation>>(relJson)!;
                                    var originalCount = relations.Count;

                                    relations.RemoveAll(r => r.TargetEndpointId == endpointId);

                                    if (relations.Count != originalCount)
                                    {
                                        var relationsJson = JsonSerializer.Serialize(relations, _jsonSerializerOptions);
                                        await File.WriteAllTextAsync(relationsPath, relationsJson);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteServiceRelation(Guid sourceServiceId, Guid targetEndpointId)
    {
        await _semaphore.WaitAsync();
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, sourceServiceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {sourceServiceId} does not exist");
            }

            var relationsPath = Path.Combine(serviceDir, RELATIONS_FILE_NAME);
            if (File.Exists(relationsPath))
            {
                var json = File.ReadAllText(relationsPath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var relations = JsonSerializer.Deserialize<List<ServiceRelation>>(json)!;
                    relations.RemoveAll(r => r.SourceServiceId == sourceServiceId && r.TargetEndpointId == targetEndpointId);

                    var relationsJson = JsonSerializer.Serialize(relations, _jsonSerializerOptions);
                    await File.WriteAllTextAsync(relationsPath, relationsJson);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
