using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data;

public interface IDataService
{
    Task<Services> GetServices(CancellationToken cancellationToken);

    Task<Endpoints> GetEndpoints(Guid serviceId, CancellationToken cancellationToken);

    Task<Relations> GetRelations(Guid serviceId, CancellationToken cancellationToken);

    Task<Guid> CreateOrUpdateService(string name, string description, Guid? serviceId, string? codePath, CancellationToken cancellationToken);

    Task DeleteService(Guid serviceId, CancellationToken cancellationToken);

    Task<Guid> CreateOrUpdateEndpoint(Guid serviceId, string name, string description, string type, Guid? endpointId, string? codePath, CancellationToken cancellationToken);

    Task DeleteEndpoint(Guid endpointId, CancellationToken cancellationToken);

    Task AddRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken);

    Task DeleteRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken);
}
