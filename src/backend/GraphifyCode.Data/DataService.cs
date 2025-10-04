using GraphifyCode.Core.Settings;
using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Models;
using Microsoft.Extensions.Options;
using System;
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

    public async Task AddRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Services> GetServices(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            throw new NotImplementedException();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
