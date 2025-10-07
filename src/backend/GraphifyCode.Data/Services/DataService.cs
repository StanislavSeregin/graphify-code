using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Models;
using GraphifyCode.Data.Settings;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Services;

public class DataService(IOptions<MarkdownStorageSettings> options) : IDataService
{
    private const string SERVICE_FILE_NAME = "service.md";

    private const string ENDPOINTS_FILE_NAME = "endpoints.md";

    private const string RELATIONS_FILE_NAME = "relations.md";

    private const string USECASES_DIR_NAME = "usecases";

    private readonly MarkdownStorageSettings _settings = options.Value;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<Models.Services> GetServices(CancellationToken cancellationToken)
    {
        var serviceDirs = Directory.GetDirectories(_settings.Path);
        var services = new List<Models.Service>();

        foreach (var serviceDir in serviceDirs)
        {
            var servicePath = Path.Combine(serviceDir, SERVICE_FILE_NAME);
            if (File.Exists(servicePath))
            {
                var markdown = await File.ReadAllTextAsync(servicePath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    var originalService = Entities.Service.FromMarkdown(markdown);
                    var service = new Models.Service()
                    {
                        Id = originalService.Id,
                        Name = originalService.Name,
                        Description = originalService.Description,
                        RelativeCodePath = originalService.RelativeCodePath,
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

    public async Task<Endpoints> GetEndpoints(Guid serviceId, CancellationToken cancellationToken)
    {
        var endpointsPath = Path.Combine(_settings.Path, serviceId.ToString(), ENDPOINTS_FILE_NAME);
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

    public async Task<Relations> GetRelations(Guid serviceId, CancellationToken cancellationToken)
    {
        var relationsPath = Path.Combine(_settings.Path, serviceId.ToString(), RELATIONS_FILE_NAME);
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

    public async Task<Guid> CreateOrUpdateService(string name, string description, Guid? serviceId, string? codePath, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var id = serviceId ?? Guid.NewGuid();
            var serviceDir = Path.Combine(_settings.Path, id.ToString());

            Directory.CreateDirectory(serviceDir);

            var service = new Entities.Service
            {
                Id = id,
                Name = name,
                Description = description,
                LastAnalyzedAt = DateTime.UtcNow,
                RelativeCodePath = codePath
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
            var serviceDir = Path.Combine(_settings.Path, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            var endpoints = await GetEndpoints(serviceId, cancellationToken);
            var endpointIds = endpoints.EndpointList.Select(e => e.Id).ToHashSet();

            var allServiceDirs = Directory.GetDirectories(_settings.Path);
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
            var serviceDir = Path.Combine(_settings.Path, serviceId.ToString());
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
            var allServiceDirs = Directory.GetDirectories(_settings.Path);
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

                            // Remove use cases where this endpoint is the initiating endpoint
                            foreach (var dir in allServiceDirs)
                            {
                                var usecasesDir = Path.Combine(dir, USECASES_DIR_NAME);
                                if (Directory.Exists(usecasesDir))
                                {
                                    var usecaseFiles = Directory.GetFiles(usecasesDir, "*.md");
                                    foreach (var usecaseFile in usecaseFiles)
                                    {
                                        var ucMarkdown = await File.ReadAllTextAsync(usecaseFile, cancellationToken);
                                        if (!string.IsNullOrWhiteSpace(ucMarkdown))
                                        {
                                            var useCase = UseCase.FromMarkdown(ucMarkdown);
                                            if (useCase.InitiatingEndpointId == endpointId)
                                            {
                                                File.Delete(usecaseFile);
                                            }
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
            var serviceDir = Path.Combine(_settings.Path, sourceServiceId.ToString());
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
            var serviceDir = Path.Combine(_settings.Path, sourceServiceId.ToString());
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

    public async Task<UseCases> GetUseCases(Guid serviceId, CancellationToken cancellationToken)
    {
        var usecasesDir = Path.Combine(_settings.Path, serviceId.ToString(), USECASES_DIR_NAME);
        if (!Directory.Exists(usecasesDir))
        {
            return new UseCases { UseCaseList = [] };
        }

        var usecaseFiles = Directory.GetFiles(usecasesDir, "*.md");
        var usecases = new List<UseCaseSummary>();

        foreach (var usecaseFile in usecaseFiles)
        {
            var markdown = await File.ReadAllTextAsync(usecaseFile, cancellationToken);
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                var useCase = UseCase.FromMarkdown(markdown);
                var summary = new UseCaseSummary
                {
                    Id = useCase.Id,
                    Name = useCase.Name,
                    Description = useCase.Description,
                    InitiatingEndpointId = useCase.InitiatingEndpointId,
                    LastAnalyzed = useCase.LastAnalyzedAt,
                    StepCount = useCase.Steps.Length
                };
                usecases.Add(summary);
            }
        }

        return new UseCases { UseCaseList = [.. usecases] };
    }

    public async Task<UseCase> GetUseCaseDetails(Guid useCaseId, CancellationToken cancellationToken)
    {
        var allServiceDirs = Directory.GetDirectories(_settings.Path);
        foreach (var serviceDir in allServiceDirs)
        {
            var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
            if (Directory.Exists(usecasesDir))
            {
                var usecasePath = Path.Combine(usecasesDir, $"{useCaseId}.md");
                if (File.Exists(usecasePath))
                {
                    var markdown = await File.ReadAllTextAsync(usecasePath, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(markdown))
                    {
                        return UseCase.FromMarkdown(markdown);
                    }
                }
            }
        }

        throw new InvalidOperationException($"Use case with ID {useCaseId} does not exist");
    }

    public async Task<Guid> CreateOrUpdateUseCase(Guid serviceId, string name, string description, Guid initiatingEndpointId, Guid? useCaseId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var serviceDir = Path.Combine(_settings.Path, serviceId.ToString());
            if (!Directory.Exists(serviceDir))
            {
                throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
            }

            // Validate that initiatingEndpointId exists and belongs to the service
            var endpoints = await GetEndpoints(serviceId, cancellationToken);
            if (!endpoints.EndpointList.Any(e => e.Id == initiatingEndpointId))
            {
                throw new InvalidOperationException($"Endpoint with ID {initiatingEndpointId} does not exist in service {serviceId}");
            }

            var id = useCaseId ?? Guid.NewGuid();
            var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
            Directory.CreateDirectory(usecasesDir);

            var usecasePath = Path.Combine(usecasesDir, $"{id}.md");

            UseCase useCase;
            if (File.Exists(usecasePath))
            {
                // Update existing
                var markdown = await File.ReadAllTextAsync(usecasePath, cancellationToken);
                useCase = UseCase.FromMarkdown(markdown);
                useCase.Name = name;
                useCase.Description = description;
                useCase.InitiatingEndpointId = initiatingEndpointId;
                useCase.LastAnalyzedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new
                useCase = new UseCase
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    InitiatingEndpointId = initiatingEndpointId,
                    LastAnalyzedAt = DateTime.UtcNow,
                    Steps = []
                };
            }

            var useCaseMarkdown = useCase.ToMarkdown();
            await File.WriteAllTextAsync(usecasePath, useCaseMarkdown, cancellationToken);

            return id;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<int> AddStep(Guid useCaseId, string name, string description, Guid? serviceId, Guid? endpointId, string? relativeCodePath, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Validate serviceId if provided
            if (serviceId.HasValue)
            {
                var serviceDir = Path.Combine(_settings.Path, serviceId.Value.ToString());
                if (!Directory.Exists(serviceDir))
                {
                    throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
                }
            }

            // Validate endpointId if provided
            if (endpointId.HasValue)
            {
                bool endpointExists = false;
                foreach (var dir in Directory.GetDirectories(_settings.Path))
                {
                    var endpointsPath = Path.Combine(dir, ENDPOINTS_FILE_NAME);
                    if (File.Exists(endpointsPath))
                    {
                        var markdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(markdown))
                        {
                            var endpoints = Endpoints.FromMarkdown(markdown);
                            if (endpoints.EndpointList.Any(e => e.Id == endpointId.Value))
                            {
                                endpointExists = true;
                                break;
                            }
                        }
                    }
                }

                if (!endpointExists)
                {
                    throw new InvalidOperationException($"Endpoint with ID {endpointId} does not exist");
                }
            }

            var useCase = await GetUseCaseDetails(useCaseId, cancellationToken);

            var newStep = new UseCaseStep
            {
                Name = name,
                Description = description,
                ServiceId = serviceId,
                EndpointId = endpointId,
                RelativeCodePath = relativeCodePath
            };

            var newSteps = new UseCaseStep[useCase.Steps.Length + 1];
            Array.Copy(useCase.Steps, newSteps, useCase.Steps.Length);
            newSteps[^1] = newStep;
            useCase.Steps = newSteps;
            useCase.LastAnalyzedAt = DateTime.UtcNow;

            foreach (var serviceDir in Directory.GetDirectories(_settings.Path))
            {
                var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
                if (Directory.Exists(usecasesDir))
                {
                    var usecasePath = Path.Combine(usecasesDir, $"{useCaseId}.md");
                    if (File.Exists(usecasePath))
                    {
                        var useCaseMarkdown = useCase.ToMarkdown();
                        await File.WriteAllTextAsync(usecasePath, useCaseMarkdown, cancellationToken);
                        return useCase.Steps.Length - 1;
                    }
                }
            }

            throw new InvalidOperationException($"Use case with ID {useCaseId} does not exist");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateStep(Guid useCaseId, int stepIndex, string? name, string? description, Guid? serviceId, Guid? endpointId, string? relativeCodePath, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var useCase = await GetUseCaseDetails(useCaseId, cancellationToken);

            if (stepIndex < 0 || stepIndex >= useCase.Steps.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(stepIndex), $"Step index {stepIndex} is out of range. Use case has {useCase.Steps.Length} steps.");
            }

            // Validate serviceId if provided and not empty string
            if (serviceId.HasValue && serviceId.Value != Guid.Empty)
            {
                var serviceDir = Path.Combine(_settings.Path, serviceId.Value.ToString());
                if (!Directory.Exists(serviceDir))
                {
                    throw new InvalidOperationException($"Service with ID {serviceId} does not exist");
                }
            }

            // Validate endpointId if provided and not empty string
            if (endpointId.HasValue && endpointId.Value != Guid.Empty)
            {
                bool endpointExists = false;
                var allServiceDirs = Directory.GetDirectories(_settings.Path);
                foreach (var dir in allServiceDirs)
                {
                    var endpointsPath = Path.Combine(dir, ENDPOINTS_FILE_NAME);
                    if (File.Exists(endpointsPath))
                    {
                        var markdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(markdown))
                        {
                            var endpoints = Endpoints.FromMarkdown(markdown);
                            if (endpoints.EndpointList.Any(e => e.Id == endpointId.Value))
                            {
                                endpointExists = true;
                                break;
                            }
                        }
                    }
                }

                if (!endpointExists)
                {
                    throw new InvalidOperationException($"Endpoint with ID {endpointId} does not exist");
                }
            }

            var step = useCase.Steps[stepIndex];

            if (name != null) step.Name = name;
            if (description != null) step.Description = description;
            if (serviceId.HasValue) step.ServiceId = serviceId.Value == Guid.Empty ? null : serviceId.Value;
            if (endpointId.HasValue) step.EndpointId = endpointId.Value == Guid.Empty ? null : endpointId.Value;
            if (relativeCodePath != null) step.RelativeCodePath = string.IsNullOrEmpty(relativeCodePath) ? null : relativeCodePath;

            useCase.LastAnalyzedAt = DateTime.UtcNow;

            var allServiceDirsUpdate = Directory.GetDirectories(_settings.Path);
            foreach (var serviceDir in allServiceDirsUpdate)
            {
                var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
                if (Directory.Exists(usecasesDir))
                {
                    var usecasePath = Path.Combine(usecasesDir, $"{useCaseId}.md");
                    if (File.Exists(usecasePath))
                    {
                        var useCaseMarkdown = useCase.ToMarkdown();
                        await File.WriteAllTextAsync(usecasePath, useCaseMarkdown, cancellationToken);
                        return;
                    }
                }
            }

            throw new InvalidOperationException($"Use case with ID {useCaseId} does not exist");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteAllSteps(Guid useCaseId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var useCase = await GetUseCaseDetails(useCaseId, cancellationToken);
            useCase.Steps = [];
            useCase.LastAnalyzedAt = DateTime.UtcNow;

            var allServiceDirs = Directory.GetDirectories(_settings.Path);
            foreach (var serviceDir in allServiceDirs)
            {
                var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
                if (Directory.Exists(usecasesDir))
                {
                    var usecasePath = Path.Combine(usecasesDir, $"{useCaseId}.md");
                    if (File.Exists(usecasePath))
                    {
                        var useCaseMarkdown = useCase.ToMarkdown();
                        await File.WriteAllTextAsync(usecasePath, useCaseMarkdown, cancellationToken);
                        return;
                    }
                }
            }

            throw new InvalidOperationException($"Use case with ID {useCaseId} does not exist");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DeleteUseCase(Guid useCaseId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var allServiceDirs = Directory.GetDirectories(_settings.Path);
            foreach (var serviceDir in allServiceDirs)
            {
                var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
                if (Directory.Exists(usecasesDir))
                {
                    var usecasePath = Path.Combine(usecasesDir, $"{useCaseId}.md");
                    if (File.Exists(usecasePath))
                    {
                        File.Delete(usecasePath);
                        return;
                    }
                }
            }

            throw new InvalidOperationException($"Use case with ID {useCaseId} does not exist");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<FullGraph> GetFullGraph(CancellationToken cancellationToken)
    {
        var serviceDirs = Directory.GetDirectories(_settings.Path);
        var serviceDataList = new List<FullGraph.ServiceData>();

        foreach (var serviceDir in serviceDirs)
        {
            var servicePath = Path.Combine(serviceDir, SERVICE_FILE_NAME);
            if (!File.Exists(servicePath))
            {
                continue;
            }

            var serviceMarkdown = await File.ReadAllTextAsync(servicePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(serviceMarkdown))
            {
                continue;
            }

            var service = Entities.Service.FromMarkdown(serviceMarkdown);

            // Read endpoints
            var endpoints = Array.Empty<Endpoint>();
            var endpointsPath = Path.Combine(serviceDir, ENDPOINTS_FILE_NAME);
            if (File.Exists(endpointsPath))
            {
                var endpointsMarkdown = await File.ReadAllTextAsync(endpointsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(endpointsMarkdown))
                {
                    endpoints = Endpoints.FromMarkdown(endpointsMarkdown).EndpointList;
                }
            }

            // Read relations
            var relations = new Relations { TargetEndpointIds = [] };
            var relationsPath = Path.Combine(serviceDir, RELATIONS_FILE_NAME);
            if (File.Exists(relationsPath))
            {
                var relationsMarkdown = await File.ReadAllTextAsync(relationsPath, cancellationToken);
                if (!string.IsNullOrWhiteSpace(relationsMarkdown))
                {
                    relations = Relations.FromMarkdown(relationsMarkdown);
                }
            }

            // Read use cases
            var useCases = new List<UseCase>();
            var usecasesDir = Path.Combine(serviceDir, USECASES_DIR_NAME);
            if (Directory.Exists(usecasesDir))
            {
                var usecaseFiles = Directory.GetFiles(usecasesDir, "*.md");
                foreach (var usecaseFile in usecaseFiles)
                {
                    var usecaseMarkdown = await File.ReadAllTextAsync(usecaseFile, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(usecaseMarkdown))
                    {
                        useCases.Add(UseCase.FromMarkdown(usecaseMarkdown));
                    }
                }
            }

            var serviceData = new FullGraph.ServiceData
            {
                Service = service,
                Endpoint = endpoints,
                Relations = relations,
                UseCases = [.. useCases]
            };

            serviceDataList.Add(serviceData);
        }

        return new FullGraph
        {
            Services = [.. serviceDataList]
        };
    }
}
