import { ServiceData, UseCase, UseCaseStep, endpointRefKey } from './graph.service';
import { GraphViewModel } from './graph-view-model';

const UNASSIGNED_ENDPOINT_KEY = '__unassigned__';
const SERVICE_LEVEL_LABEL = 'Service-level';

export interface FlowStepNode {
  index: number;
  step: UseCaseStep;
  serviceId: string;
  serviceName: string;
  endpointKey: string;
  endpointLabel: string;
}

export interface FlowEndpointGroup {
  endpointKey: string;
  label: string;
  steps: FlowStepNode[];
}

export interface FlowServiceGroup {
  serviceId: string;
  serviceName: string;
  isExternal: boolean;
  endpoints: FlowEndpointGroup[];
}

export interface FlowSequenceEdge {
  fromIndex: number;
  toIndex: number;
}

export interface UseCaseFlowModel {
  useCaseId: string;
  useCaseName: string;
  owningServiceId: string;
  services: FlowServiceGroup[];
  sequenceEdges: FlowSequenceEdge[];
  serviceIds: Set<string>;
}

export function buildUseCaseFlowModel(
  useCase: UseCase,
  owningService: ServiceData,
  vm: GraphViewModel
): UseCaseFlowModel {
  const serviceGroups = new Map<string, FlowServiceGroup>();
  const endpointGroupsByService = new Map<string, Map<string, FlowEndpointGroup>>();
  const latestEndpointContextByService = new Map<string, { endpointKey: string; endpointLabel: string }>();

  const ensureServiceGroup = (serviceId: string): FlowServiceGroup => {
    const existing = serviceGroups.get(serviceId);
    if (existing) return existing;

    const service = vm.servicesByName.get(serviceId);
    const group: FlowServiceGroup = {
      serviceId,
      serviceName: service?.serviceData.service.name ?? serviceId,
      isExternal: service?.isExternal ?? false,
      endpoints: []
    };
    serviceGroups.set(serviceId, group);
    endpointGroupsByService.set(serviceId, new Map<string, FlowEndpointGroup>());
    return group;
  };

  const ensureEndpointGroup = (
    serviceGroup: FlowServiceGroup,
    endpointKey: string,
    endpointLabel: string
  ): FlowEndpointGroup => {
    const endpointGroups = endpointGroupsByService.get(serviceGroup.serviceId);
    const existing = endpointGroups?.get(endpointKey);
    if (existing) return existing;

    const group: FlowEndpointGroup = {
      endpointKey,
      label: endpointLabel,
      steps: []
    };
    serviceGroup.endpoints.push(group);
    endpointGroups?.set(endpointKey, group);
    return group;
  };

  useCase.steps.forEach((step, index) => {
    const endpointServiceName = step.endpointName
      ? (step.serviceName ?? owningService.service.name)
      : null;
    const endpointRef = step.endpointName && endpointServiceName
      ? vm.endpointsByKey.get(endpointRefKey(endpointServiceName, step.endpointName))
      : null;
    const serviceId = endpointRef?.service.service.name ?? step.serviceName ?? owningService.service.name;
    const inheritedEndpoint = latestEndpointContextByService.get(serviceId);
    const endpointKey = endpointRef?.endpoint.name ?? inheritedEndpoint?.endpointKey ?? UNASSIGNED_ENDPOINT_KEY;
    const endpointLabel = endpointRef?.endpoint.name ?? inheritedEndpoint?.endpointLabel ?? SERVICE_LEVEL_LABEL;
    const serviceGroup = ensureServiceGroup(serviceId);
    const endpointGroup = ensureEndpointGroup(serviceGroup, endpointKey, endpointLabel);

    if (endpointRef) {
      latestEndpointContextByService.set(serviceId, {
        endpointKey: endpointRef.endpoint.name,
        endpointLabel: endpointRef.endpoint.name
      });
    }

    endpointGroup.steps.push({
      index,
      step,
      serviceId,
      serviceName: serviceGroup.serviceName,
      endpointKey,
      endpointLabel
    });
  });

  return {
    useCaseId: useCase.name,
    useCaseName: useCase.name,
    owningServiceId: owningService.service.name,
    services: [...serviceGroups.values()],
    sequenceEdges: useCase.steps.slice(1).map((_, index) => ({
      fromIndex: index,
      toIndex: index + 1
    })),
    serviceIds: new Set(serviceGroups.keys())
  };
}
