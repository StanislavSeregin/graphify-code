using GraphifyCode.Core.Settings;
using GraphifyCode.Data.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data;

public class DataService(IOptions<GraphifyCodeSettings> options) : IDataService
{
    private const string SERVICE_FILE_NAME = "service.md";

    private const string ENDPOINTS_FILE_NAME = "endpoints.md";

    private const string RELATIONS_FILE_NAME = "relations.md";

    private readonly GraphifyCodeSettings _settings = options.Value;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Models.Services> GetServices(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var serviceDirs = Directory.GetDirectories(_settings.GraphifyCodeDataPath);
            var services = new List<Models.Service>();

            foreach (var serviceDir in serviceDirs)
            {
                var servicePath = Path.Combine(serviceDir, SERVICE_FILE_NAME);
                if (File.Exists(servicePath))
                {
                    var markdown = await File.ReadAllTextAsync(servicePath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(markdown))
                    {
                        var originalService = Service.FromMarkdown(markdown);
                        var service = new Models.Service()
                        {
                            Id = originalService.Id,
                            Name = originalService.Name,
                            Description = originalService.Description,
                            CodePath = originalService.RelativeCodePath,
                            LastAnalyzed = originalService.LastAnalyzedAt,
                            HasEndpoints = File.Exists(Path.Combine(serviceDir, ENDPOINTS_FILE_NAME)),
                            HasRelations = File.Exists(Path.Combine(serviceDir, RELATIONS_FILE_NAME))
                        };

                        services.Add(service);
                    }
                }
            }

            return new Models.Services
            {
                ServiceList = [.. services]
            };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Endpoints> GetEndpoints(Guid serviceId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var endpointsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), ENDPOINTS_FILE_NAME);
            if (File.Exists(endpointsPath))
            {
                var markdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    return Endpoints.FromMarkdown(markdown);
                }
            }
            return new Endpoints { EndpointList = [] };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Relations> GetRelations(Guid serviceId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var relationsPath = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString(), RELATIONS_FILE_NAME);
            if (File.Exists(relationsPath))
            {
                var markdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    return Relations.FromMarkdown(markdown);
                }
            }
            return new Relations { TargetEndpointIds = [] };
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Guid> CreateOrUpdateService(string name, string description, Guid? serviceId, string? codePath, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var id = serviceId ?? Guid.NewGuid();
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, id.ToString());

            Directory.CreateDirectory(serviceDir);

            var service = new Models.Service
            {
                Id = id,
                Name = name,
                Description = description,
                HasEndpoints = File.Exists(Path.Combine(serviceDir, ENDPOINTS_FILE_NAME)),
                HasRelations = File.Exists(Path.Combine(serviceDir, RELATIONS_FILE_NAME)),
                LastAnalyzed = DateTime.UtcNow,
                CodePath = codePath
            };

            var serviceMarkdown = service.ToMarkdown();
            var filePath = Path.Combine(serviceDir, SERVICE_FILE_NAME);
            await File.WriteAllTextAsync(filePath, serviceMarkdown, cancellationToken);

            return id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteService(Guid serviceId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            var endpoints = await GetEndpoints(serviceId, cancellationToken);
            var endpointIds = endpoints.EndpointList.Select(e => e.Id).ToHashSet();

            var allServiceDirs = Directory.GetDirectories(_settings.GraphifyCodeDataPath);
            foreach (var dir in allServiceDirs)
            {
                var relationsPath = Path.Combine(dir, RELATIONS_FILE_NAME);
                if (File.Exists(relationsPath))
                {
                    var markdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(markdown))
                    {
                        var relations = Relations.FromMarkdown(markdown);
                        var originalCount = relations.TargetEndpointIds.Length;

                        relations.TargetEndpointIds = Array.FindAll(relations.TargetEndpointIds, id => !endpointIds.Contains(id));

                        if (relations.TargetEndpointIds.Length != originalCount)
                        {
                            var relationsMarkdown = relations.ToMarkdown();
                            await File.WriteAllTextAsync(relationsPath, relationsMarkdown, cancellationToken);
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

    public async Task<Guid> CreateOrUpdateEndpoint(Guid serviceId, string name, string description, string type, Guid? endpointId, string? codePath, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
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

            Endpoints endpoints;
            if (File.Exists(endpointsPath))
            {
                var markdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    endpoints = Endpoints.FromMarkdown(markdown);
                }
                else
                {
                    endpoints = new Endpoints { EndpointList = [] };
                }
            }
            else
            {
                endpoints = new Endpoints { EndpointList = [] };
            }

            var existingEndpoint = Array.Find(endpoints.EndpointList, e => e.Id == id);
            if (existingEndpoint != null)
            {
                endpoints.EndpointList = Array.FindAll(endpoints.EndpointList, e => e.Id != id);
            }

            var newEndpoint = new Endpoint
            {
                Id = id,
                Name = name,
                Description = description,
                Type = type,
                LastAnalyzedAt = DateTime.UtcNow,
                RelativeCodePath = codePath
            };

            var newEndpointList = new Endpoint[endpoints.EndpointList.Length + 1];
            Array.Copy(endpoints.EndpointList, newEndpointList, endpoints.EndpointList.Length);
            newEndpointList[^1] = newEndpoint;
            endpoints.EndpointList = newEndpointList;

            var endpointsMarkdown = endpoints.ToMarkdown();
            await File.WriteAllTextAsync(endpointsPath, endpointsMarkdown, cancellationToken);

            return id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteEndpoint(Guid endpointId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var allServiceDirs = Directory.GetDirectories(_settings.GraphifyCodeDataPath);
            foreach (var serviceDir in allServiceDirs)
            {
                var endpointsPath = Path.Combine(serviceDir, ENDPOINTS_FILE_NAME);
                if (File.Exists(endpointsPath))
                {
                    var markdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(markdown))
                    {
                        var endpoints = Endpoints.FromMarkdown(markdown);
                        var originalLength = endpoints.EndpointList.Length;

                        endpoints.EndpointList = Array.FindAll(endpoints.EndpointList, e => e.Id != endpointId);

                        if (endpoints.EndpointList.Length != originalLength)
                        {
                            var endpointsMarkdown = endpoints.ToMarkdown();
                            await File.WriteAllTextAsync(endpointsPath, endpointsMarkdown, cancellationToken);

                            // Remove relations to this endpoint
                            foreach (var dir in allServiceDirs)
                            {
                                var relationsPath = Path.Combine(dir, RELATIONS_FILE_NAME);
                                if (File.Exists(relationsPath))
                                {
                                    var relMarkdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                                    if (!string.IsNullOrWhiteSpace(relMarkdown))
                                    {
                                        var relations = Relations.FromMarkdown(relMarkdown);
                                        var originalIdCount = relations.TargetEndpointIds.Length;

                                        relations.TargetEndpointIds = Array.FindAll(relations.TargetEndpointIds, id => id != endpointId);

                                        if (relations.TargetEndpointIds.Length != originalIdCount)
                                        {
                                            var relationsMarkdown = relations.ToMarkdown();
                                            await File.WriteAllTextAsync(relationsPath, relationsMarkdown, cancellationToken);
                                        }
                                    }
                                }
                            }
                            return;
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

    public async Task AddRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var serviceDir = Path.Combine(_settings.GraphifyCodeDataPath, sourceServiceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {sourceServiceId} does not exist");
            }

            var relationsPath = Path.Combine(serviceDir, RELATIONS_FILE_NAME);

            Relations relations;
            if (File.Exists(relationsPath))
            {
                var markdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    relations = Relations.FromMarkdown(markdown);
                }
                else
                {
                    relations = new Relations { TargetEndpointIds = [] };
                }
            }
            else
            {
                relations = new Relations { TargetEndpointIds = [] };
            }

            if (!relations.TargetEndpointIds.Contains(targetEndpointId))
            {
                var newIds = new Guid[relations.TargetEndpointIds.Length + 1];
                Array.Copy(relations.TargetEndpointIds, newIds, relations.TargetEndpointIds.Length);
                newIds[^1] = targetEndpointId;
                relations.TargetEndpointIds = newIds;

                var relationsMarkdown = relations.ToMarkdown();
                await File.WriteAllTextAsync(relationsPath, relationsMarkdown, cancellationToken);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
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
                var markdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    var relations = Relations.FromMarkdown(markdown);
                    relations.TargetEndpointIds = Array.FindAll(relations.TargetEndpointIds, id => id != targetEndpointId);

                    var relationsMarkdown = relations.ToMarkdown();
                    await File.WriteAllTextAsync(relationsPath, relationsMarkdown, cancellationToken);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
