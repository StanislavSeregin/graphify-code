import { Endpoint, FullGraph, ServiceData, UseCase, UseCaseStep } from './graph.service';

export interface GraphServiceNode {
  id: string;
  serviceData: ServiceData;
  endpointCount: number;
  useCaseCount: number;
  isExternal: boolean;
}

export interface GraphDependencyEdge {
  id: string;
  sourceId: string;
  targetId: string;
  count: number;
}

export interface EndpointReference {
  endpoint: Endpoint;
  service: ServiceData;
}

export interface UseCaseReference {
  useCase: UseCase;
  service: ServiceData;
}

export interface StepReference {
  step: UseCaseStep;
  index: number;
  useCase: UseCase;
  service: ServiceData;
}

export interface GraphViewModel {
  services: GraphServiceNode[];
  edges: GraphDependencyEdge[];
  servicesById: Map<string, GraphServiceNode>;
  endpointToServiceId: Map<string, string>;
  endpointsById: Map<string, EndpointReference>;
  useCasesById: Map<string, UseCaseReference>;
}

export function buildGraphViewModel(graph: FullGraph): GraphViewModel {
  const services = graph.services.map(serviceData => ({
    id: serviceData.service.id,
    serviceData,
    endpointCount: serviceData.endpoint.length,
    useCaseCount: serviceData.useCases.length,
    isExternal: serviceData.service.relativeCodePath === null
  }));

  const servicesById = new Map(services.map(service => [service.id, service]));
  const endpointToServiceId = new Map<string, string>();
  const endpointsById = new Map<string, EndpointReference>();
  const useCasesById = new Map<string, UseCaseReference>();

  graph.services.forEach(serviceData => {
    serviceData.endpoint.forEach(endpoint => {
      endpointToServiceId.set(endpoint.id, serviceData.service.id);
      endpointsById.set(endpoint.id, { endpoint, service: serviceData });
    });

    serviceData.useCases.forEach(useCase => {
      useCasesById.set(useCase.id, { useCase, service: serviceData });
    });
  });

  const edgeCounts = new Map<string, GraphDependencyEdge>();
  const addEdge = (sourceId: string, targetId: string) => {
    if (sourceId === targetId) return;
    if (!servicesById.has(sourceId) || !servicesById.has(targetId)) return;

    const id = `${sourceId}->${targetId}`;
    const existing = edgeCounts.get(id);
    if (existing) {
      existing.count += 1;
      return;
    }

    edgeCounts.set(id, { id, sourceId, targetId, count: 1 });
  };

  graph.services.forEach(serviceData => {
    const sourceId = serviceData.service.id;

    serviceData.relations.targetEndpointIds.forEach(endpointId => {
      const targetServiceId = endpointToServiceId.get(endpointId);
      if (targetServiceId) {
        addEdge(sourceId, targetServiceId);
      }
    });

    serviceData.useCases.forEach(useCase => {
      useCase.steps.forEach(step => {
        if (step.endpointId) {
          const targetServiceId = endpointToServiceId.get(step.endpointId);
          if (targetServiceId) {
            addEdge(sourceId, targetServiceId);
          }
          return;
        }

        if (step.serviceId) {
          addEdge(sourceId, step.serviceId);
        }
      });
    });
  });

  return {
    services,
    edges: [...edgeCounts.values()],
    servicesById,
    endpointToServiceId,
    endpointsById,
    useCasesById
  };
}

export function findUseCasesForEndpoint(vm: GraphViewModel, endpointId: string): UseCaseReference[] {
  const references: UseCaseReference[] = [];

  vm.useCasesById.forEach(reference => {
    if (
      reference.useCase.initiatingEndpointId === endpointId ||
      reference.useCase.steps.some(step => step.endpointId === endpointId)
    ) {
      references.push(reference);
    }
  });

  return references;
}

export function findRelatedServicesForEndpoint(vm: GraphViewModel, endpointId: string): GraphServiceNode[] {
  const relatedServiceIds = new Set<string>();

  vm.services.forEach(service => {
    const { serviceData } = service;
    if (serviceData.relations.targetEndpointIds.includes(endpointId)) {
      relatedServiceIds.add(service.id);
    }

    for (const useCase of serviceData.useCases) {
      if (useCase.steps.some(step => step.endpointId === endpointId)) {
        relatedServiceIds.add(service.id);
        break;
      }
    }
  });

  return [...relatedServiceIds]
    .map(serviceId => vm.servicesById.get(serviceId))
    .filter((service): service is GraphServiceNode => Boolean(service));
}
