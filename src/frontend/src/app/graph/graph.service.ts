import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, firstValueFrom } from 'rxjs';
import { map, distinctUntilChanged, filter } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import * as d3 from 'd3';

// ============================================================================
// Domain Types
// ============================================================================

export type Service = {
  name: string;
  description: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

export type Endpoint = {
  name: string;
  description: string;
  type: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

export type Relations = {
  targetEndpointNames: string[];
};

export type UseCaseStep = {
  name: string;
  description: string;
  serviceName: string | null;
  endpointName: string | null;
  relativeCodePath: string | null;
};

export type UseCase = {
  name: string;
  description: string;
  initiatingEndpointName: string;
  lastAnalyzedAt: string;
  steps: UseCaseStep[];
};

export type ServiceData = {
  service: Service;
  endpoint: Endpoint[];
  relations: Relations;
  useCases: UseCase[];
};

export type FullGraph = {
  services: ServiceData[];
};

export interface EndpointSidebarData {
  endpoint: Endpoint;
  service: ServiceData;
  relatedServices: ServiceData[];
  useCases: UseCase[];
  externalUseCases: Array<{ useCase: UseCase; service: ServiceData }>;
}

export interface UseCaseSidebarData {
  useCase: UseCase;
  service: ServiceData;
  allServices?: ServiceData[];
  stepIndex?: number;
}

// ============================================================================
// UI State Types
// ============================================================================

export type DisplayMode = 'compact' | 'full';

export interface ZoomState {
  transform: d3.ZoomTransform;
  scale: number;
}

export interface ZoomRequest {
  scope: 'main' | 'nested';
  targetId?: string;
  transform?: d3.ZoomTransform;
  duration?: number;
}

export interface SidebarRequest<T> {
  action: 'open' | 'close';
  data?: T;
}

export type EndpointSidebarRequest = SidebarRequest<EndpointSidebarData>;
export type UseCaseSidebarRequest = SidebarRequest<UseCaseSidebarData>;

export function endpointRefKey(serviceName: string, endpointName: string): string {
  return `${serviceName}\0${endpointName}`;
}

export function useCaseRefKey(serviceName: string, useCaseName: string): string {
  return `${serviceName}\0${useCaseName}`;
}

// ============================================================================
// GraphService
// ============================================================================

@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private readonly apiUrl = environment.apiUrl;
  private readonly SERVICE_CARD_FULL_THRESHOLD_ON = 0.95;
  private readonly SERVICE_CARD_FULL_THRESHOLD_OFF = 0.82;
  private currentDisplayMode: DisplayMode = 'compact';

  private graphDataSubject = new BehaviorSubject<FullGraph | null>(null);
  private isBusySubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<string | null>(null);

  private zoomStateSubject = new BehaviorSubject<ZoomState>({
    transform: d3.zoomIdentity,
    scale: 1
  });

  private zoomRequestSubject = new Subject<ZoomRequest>();
  private endpointSidebarRequestSubject = new Subject<EndpointSidebarRequest>();
  private useCaseSidebarRequestSubject = new Subject<UseCaseSidebarRequest>();

  private escapeState = {
    nestedGraphChanged: false,
    pendingNestedReset: false
  };

  public graphData$: Observable<FullGraph | null> = this.graphDataSubject.asObservable();
  public isBusy$: Observable<boolean> = this.isBusySubject.asObservable();
  public error$: Observable<string | null> = this.errorSubject.asObservable();

  public zoomState$: Observable<ZoomState> = this.zoomStateSubject.asObservable();
  public zoomScale$: Observable<number> = this.zoomState$.pipe(
    map(state => state.scale),
    distinctUntilChanged()
  );

  public zoomTransform$: Observable<d3.ZoomTransform> = this.zoomState$.pipe(
    map(state => state.transform),
    distinctUntilChanged()
  );

  public displayMode$: Observable<DisplayMode> = this.zoomState$.pipe(
    map(state => this.resolveDisplayMode(state.scale)),
    distinctUntilChanged()
  );

  public mainGraphZoom$: Observable<ZoomRequest> = this.zoomRequestSubject.pipe(
    filter(req => req.scope === 'main')
  );

  public nestedGraphZoom$: Observable<ZoomRequest> = this.zoomRequestSubject.pipe(
    filter(req => req.scope === 'nested')
  );

  public endpointSidebarRequest$: Observable<EndpointSidebarRequest> = this.endpointSidebarRequestSubject.asObservable();
  public useCaseSidebarRequest$: Observable<UseCaseSidebarRequest> = this.useCaseSidebarRequestSubject.asObservable();

  constructor(private http: HttpClient) {}

  public async init(): Promise<void> {
    this.isBusySubject.next(true);
    this.errorSubject.next(null);

    try {
      const fullGraph = await firstValueFrom(
        this.http.get<FullGraph>(`${this.apiUrl}/full-graph`)
      );

      this.graphDataSubject.next(fullGraph);
    } catch (error) {
      const errorMessage = error instanceof Error
        ? error.message
        : 'Failed to load graph data';
      console.error('Failed to load graph data:', error);
      this.errorSubject.next(errorMessage);
    } finally {
      this.isBusySubject.next(false);
    }
  }

  public async reload(): Promise<void> {
    await this.init();
  }

  public updateZoom(transform: d3.ZoomTransform): void {
    this.zoomStateSubject.next({
      transform,
      scale: transform.k
    });
  }

  public getMainGraphScale(): number {
    return this.zoomStateSubject.value.scale;
  }

  public requestZoom(request: ZoomRequest): void {
    this.zoomRequestSubject.next(request);
  }

  public markNestedGraphChanged(): void {
    this.escapeState.nestedGraphChanged = true;
  }

  public clearPendingNestedReset(): void {
    this.escapeState.pendingNestedReset = false;
  }

  private resetEscapeState(): void {
    this.escapeState.nestedGraphChanged = false;
    this.escapeState.pendingNestedReset = false;
  }

  public handleEscapeKey(): void {
    const { scale } = this.zoomStateSubject.value;
    const isZoomedIn = scale >= this.SERVICE_CARD_FULL_THRESHOLD_ON;

    if (!isZoomedIn) {
      this.requestZoom({ scope: 'main', duration: 750 });
      this.resetEscapeState();
      return;
    }

    const { nestedGraphChanged, pendingNestedReset } = this.escapeState;

    if (nestedGraphChanged && !pendingNestedReset) {
      this.requestZoom({ scope: 'nested', duration: 750 });
      this.escapeState.pendingNestedReset = true;
    } else {
      this.requestZoom({ scope: 'main', duration: 750 });
      this.resetEscapeState();
    }
  }

  public showEndpointDetails(endpoint: Endpoint, service: ServiceData, fullGraph: FullGraph): void {
    const relatedServices = this.findRelatedServicesForEndpoint(service.service.name, endpoint.name, fullGraph);

    const useCases = service.useCases.filter(uc =>
      uc.initiatingEndpointName === endpoint.name ||
      uc.steps.some(step =>
        step.endpointName === endpoint.name
        && (step.serviceName === null || step.serviceName === service.service.name))
    );

    const externalUseCases: Array<{ useCase: UseCase; service: ServiceData }> = [];
    fullGraph.services.forEach(s => {
      if (s.service.name === service.service.name) return;

      s.useCases.forEach(uc => {
        const usesEndpoint = uc.steps.some(step =>
          step.endpointName === endpoint.name
          && (step.serviceName === null ? false : step.serviceName === service.service.name));
        if (usesEndpoint) {
          externalUseCases.push({ useCase: uc, service: s });
        }
      });
    });

    this.endpointSidebarRequestSubject.next({
      action: 'open',
      data: { endpoint, service, relatedServices, useCases, externalUseCases }
    });
  }

  public showUseCaseDetails(useCase: UseCase, service: ServiceData, fullGraph: FullGraph, stepIndex?: number): void {
    this.useCaseSidebarRequestSubject.next({
      action: 'open',
      data: { useCase, service, allServices: fullGraph.services, stepIndex }
    });
  }

  public findRelatedServicesForEndpoint(serviceName: string, endpointName: string, fullGraph: FullGraph): ServiceData[] {
    const relatedNames = new Set<string>();

    fullGraph.services.forEach(s => {
      if (s.relations.targetEndpointNames.includes(endpointName)) {
        relatedNames.add(s.service.name);
      }
      s.useCases.forEach(uc => {
        const callsEndpoint = uc.steps.some(step =>
          step.endpointName === endpointName
          && (step.serviceName === null
            ? s.service.name === serviceName
            : step.serviceName === serviceName));
        if (callsEndpoint) {
          relatedNames.add(s.service.name);
        }
      });
    });

    return fullGraph.services.filter(s => relatedNames.has(s.service.name));
  }

  private resolveDisplayMode(scale: number): DisplayMode {
    if (this.currentDisplayMode === 'compact' && scale >= this.SERVICE_CARD_FULL_THRESHOLD_ON) {
      this.currentDisplayMode = 'full';
    } else if (this.currentDisplayMode === 'full' && scale < this.SERVICE_CARD_FULL_THRESHOLD_OFF) {
      this.currentDisplayMode = 'compact';
    }
    return this.currentDisplayMode;
  }

  public focusOnService(serviceName: string): void {
    this.requestZoom({
      scope: 'main',
      targetId: serviceName,
      duration: 750
    });
  }

  public focusOnEndpoint(endpointName: string): void {
    this.requestZoom({
      scope: 'nested',
      targetId: endpointName,
      duration: 750
    });
  }

  public handleUseCaseStepClick(step: UseCaseStep, useCase: UseCase, service: ServiceData, fullGraph: FullGraph): void {
    const parentServiceName = service.service.name;
    const initiatingEndpointName = useCase.initiatingEndpointName;

    if (step.endpointName) {
      const endpointServiceName = step.serviceName ?? parentServiceName;
      this.activateStepEndpoint(endpointServiceName, step.endpointName, fullGraph);
      return;
    }

    if (step.serviceName === parentServiceName) {
      this.activateParentEndpoint(initiatingEndpointName, parentServiceName, service, fullGraph);
      return;
    }

    if (step.serviceName) {
      this.activateService(step.serviceName);
      return;
    }

    this.activateParentEndpoint(initiatingEndpointName, parentServiceName, service, fullGraph);
  }

  private activateStepEndpoint(serviceName: string, endpointName: string, fullGraph: FullGraph): void {
    const serviceData = fullGraph.services.find(s => s.service.name === serviceName);
    const endpoint = serviceData?.endpoint.find(ep => ep.name === endpointName);
    if (serviceData && endpoint) {
      this.focusOnService(serviceData.service.name);
      this.focusOnEndpoint(endpoint.name);
      this.showEndpointDetails(endpoint, serviceData, fullGraph);
    }
  }

  private activateParentEndpoint(initiatingEndpointName: string, parentServiceName: string, service: ServiceData, fullGraph: FullGraph): void {
    const initiatingEndpoint = service.endpoint.find(ep => ep.name === initiatingEndpointName);
    if (initiatingEndpoint) {
      this.focusOnService(parentServiceName);
      this.focusOnEndpoint(initiatingEndpoint.name);
      this.showEndpointDetails(initiatingEndpoint, service, fullGraph);
    }
  }

  private activateService(serviceName: string): void {
    this.focusOnService(serviceName);
    this.closeEndpointSidebar();
  }

  public closeEndpointSidebar(): void {
    this.endpointSidebarRequestSubject.next({ action: 'close' });
  }

  public closeUseCaseSidebar(): void {
    this.useCaseSidebarRequestSubject.next({ action: 'close' });
  }
}
