import {
  Endpoint,
  FullGraph,
  ServiceData,
  UseCase,
  UseCaseStep,
  endpointRefKey,
  useCaseRefKey
} from './graph.service';

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
  servicesByName: Map<string, GraphServiceNode>;
  endpointToServiceName: Map<string, string>;
  endpointsByKey: Map<string, EndpointReference>;
  useCasesByKey: Map<string, UseCaseReference>;
}

export function buildGraphViewModel(graph: FullGraph): GraphViewModel {
  const services = graph.services.map(serviceData => ({
    id: serviceData.service.name,
    serviceData,
    endpointCount: serviceData.endpoint.length,
    useCaseCount: serviceData.useCases.length,
    isExternal: serviceData.service.relativeCodePath === null
  }));

  const servicesByName = new Map(services.map(service => [service.id, service]));
  const endpointToServiceName = new Map<string, string>();
  const endpointsByKey = new Map<string, EndpointReference>();
  const useCasesByKey = new Map<string, UseCaseReference>();

  graph.services.forEach(serviceData => {
    const serviceName = serviceData.service.name;
    serviceData.endpoint.forEach(endpoint => {
      const key = endpointRefKey(serviceName, endpoint.name);
      endpointToServiceName.set(key, serviceName);
      endpointsByKey.set(key, { endpoint, service: serviceData });
    });

    serviceData.useCases.forEach(useCase => {
      useCasesByKey.set(useCaseRefKey(serviceName, useCase.name), { useCase, service: serviceData });
    });
  });

  const edgeCounts = new Map<string, GraphDependencyEdge>();
  const addEdge = (sourceId: string, targetId: string) => {
    if (sourceId === targetId) return;
    if (!servicesByName.has(sourceId) || !servicesByName.has(targetId)) return;

    const id = `${sourceId}->${targetId}`;
    const existing = edgeCounts.get(id);
    if (existing) {
      existing.count += 1;
      return;
    }

    edgeCounts.set(id, { id, sourceId, targetId, count: 1 });
  };

  graph.services.forEach(serviceData => {
    const sourceId = serviceData.service.name;

    serviceData.relations.targetEndpointNames.forEach(endpointName => {
      for (const [key, targetServiceName] of endpointToServiceName) {
        if (key.endsWith(`\0${endpointName}`)) {
          addEdge(sourceId, targetServiceName);
        }
      }
    });

    serviceData.useCases.forEach(useCase => {
      useCase.steps.forEach(step => {
        if (step.endpointName) {
          const endpointServiceName = step.serviceName ?? sourceId;
          const targetServiceName = endpointToServiceName.get(
            endpointRefKey(endpointServiceName, step.endpointName)
          );
          if (targetServiceName) {
            addEdge(sourceId, targetServiceName);
          }
          return;
        }

        if (step.serviceName) {
          addEdge(sourceId, step.serviceName);
        }
      });
    });
  });

  return {
    services,
    edges: [...edgeCounts.values()],
    servicesByName,
    endpointToServiceName,
    endpointsByKey,
    useCasesByKey
  };
}

export function findUseCasesForEndpoint(
  vm: GraphViewModel,
  serviceName: string,
  endpointName: string
): UseCaseReference[] {
  const references: UseCaseReference[] = [];

  vm.useCasesByKey.forEach(reference => {
    const { useCase, service } = reference;
    const matchesInitiating =
      service.service.name === serviceName
      && useCase.initiatingEndpointName === endpointName;
    const matchesStep = useCase.steps.some(step =>
      step.endpointName === endpointName
      && (step.serviceName === null
        ? service.service.name === serviceName
        : step.serviceName === serviceName));

    if (matchesInitiating || matchesStep) {
      references.push(reference);
    }
  });

  return references;
}

export function findRelatedServicesForEndpoint(
  vm: GraphViewModel,
  serviceName: string,
  endpointName: string
): GraphServiceNode[] {
  const relatedServiceNames = new Set<string>();

  vm.services.forEach(service => {
    const { serviceData } = service;
    if (serviceData.relations.targetEndpointNames.includes(endpointName)) {
      relatedServiceNames.add(service.id);
    }

    for (const useCase of serviceData.useCases) {
      if (useCase.steps.some(step =>
        step.endpointName === endpointName
        && (step.serviceName === null
          ? serviceData.service.name === serviceName
          : step.serviceName === serviceName))) {
        relatedServiceNames.add(service.id);
        break;
      }
    }
  });

  return [...relatedServiceNames]
    .map(name => vm.servicesByName.get(name))
    .filter((service): service is GraphServiceNode => Boolean(service));
}
