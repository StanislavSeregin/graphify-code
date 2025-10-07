using GraphifyCode.Data.Entities;
using GraphifyCode.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GraphifyCode.Data.Services;

public interface IDataService
{
    Task<Models.Services> GetServices(CancellationToken cancellationToken);

    Task<Endpoints> GetEndpoints(Guid serviceId, CancellationToken cancellationToken);

    Task<Relations> GetRelations(Guid serviceId, CancellationToken cancellationToken);

    Task<Guid> CreateOrUpdateService(string name, string description, Guid? serviceId, string? codePath, CancellationToken cancellationToken);

    Task DeleteService(Guid serviceId, CancellationToken cancellationToken);

    Task<Guid> CreateOrUpdateEndpoint(Guid serviceId, string name, string description, string type, Guid? endpointId, string? codePath, CancellationToken cancellationToken);

    Task DeleteEndpoint(Guid endpointId, CancellationToken cancellationToken);

    Task AddRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken);

    Task DeleteRelation(Guid sourceServiceId, Guid targetEndpointId, CancellationToken cancellationToken);

    Task<UseCases> GetUseCases(Guid serviceId, CancellationToken cancellationToken);

    Task<UseCase> GetUseCaseDetails(Guid useCaseId, CancellationToken cancellationToken);

    Task<Guid> CreateOrUpdateUseCase(Guid serviceId, string name, string description, Guid initiatingEndpointId, Guid? useCaseId, CancellationToken cancellationToken);

    Task<int> AddStep(Guid useCaseId, string name, string description, Guid? serviceId, Guid? endpointId, string? relativeCodePath, CancellationToken cancellationToken);

    Task UpdateStep(Guid useCaseId, int stepIndex, string? name, string? description, Guid? serviceId, Guid? endpointId, string? relativeCodePath, CancellationToken cancellationToken);

    Task DeleteAllSteps(Guid useCaseId, CancellationToken cancellationToken);

    Task DeleteUseCase(Guid useCaseId, CancellationToken cancellationToken);

    Task<FullGraph> GetFullGraph(CancellationToken cancellationToken);
}
