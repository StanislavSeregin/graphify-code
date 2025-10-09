import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject, firstValueFrom } from 'rxjs';
import { map, distinctUntilChanged, filter } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { EndpointSidebarData } from './service/endpoint/endpoint-sidebar.component';
import { UseCaseSidebarData } from './service/usecase/usecase-sidebar.component';
import * as d3 from 'd3';

// ============================================================================
// Domain Types
// ============================================================================

export type Service = {
  id: string;
  name: string;
  description: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

export type Endpoint = {
  id: string;
  name: string;
  description: string;
  type: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

export type Relations = {
  targetEndpointIds: string[];
};

export type UseCaseStep = {
  name: string;
  description: string;
  serviceId: string | null;
  endpointId: string | null;
  relativeCodePath: string | null;
};

export type UseCase = {
  id: string;
  name: string;
  description: string;
  initiatingEndpointId: string;
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
  targetId?: string; // Node ID to focus on, or undefined for reset
  transform?: d3.ZoomTransform; // Explicit transform, or computed automatically
  duration?: number; // Animation duration in ms (default: 750)
}

export interface SidebarRequest<T> {
  action: 'open' | 'close';
  data?: T;
}

export type EndpointSidebarRequest = SidebarRequest<EndpointSidebarData>;
export type UseCaseSidebarRequest = SidebarRequest<UseCaseSidebarData>;

// ============================================================================
// GraphService
// ============================================================================

/**
 * Central service for managing graph data, visualization state, and UI interactions.
 *
 * Responsibilities:
 * - Loading and caching graph data from API
 * - Managing zoom/pan state for main and nested graphs
 * - Coordinating sidebar interactions (endpoint and use case)
 * - Handling keyboard navigation (Escape key logic)
 */
@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private readonly apiUrl = environment.apiUrl;
  private readonly SERVICE_CARD_FULL_THRESHOLD = 1.0;

  // ============================================================================
  // Private State
  // ============================================================================

  // Data state
  private graphDataSubject = new BehaviorSubject<FullGraph | null>(null);
  private isBusySubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<string | null>(null);

  // Zoom state (for main graph only - used for display mode calculation)
  private zoomStateSubject = new BehaviorSubject<ZoomState>({
    transform: d3.zoomIdentity,
    scale: 1
  });

  // Zoom event stream
  private zoomRequestSubject = new Subject<ZoomRequest>();

  // Sidebar event streams
  private endpointSidebarRequestSubject = new Subject<EndpointSidebarRequest>();
  private useCaseSidebarRequestSubject = new Subject<UseCaseSidebarRequest>();

  // Escape key state tracking
  private escapeState = {
    nestedGraphChanged: false,
    pendingNestedReset: false
  };

  // ============================================================================
  // Public Observables
  // ============================================================================

  // Data observables
  public graphData$: Observable<FullGraph | null> = this.graphDataSubject.asObservable();
  public isBusy$: Observable<boolean> = this.isBusySubject.asObservable();
  public error$: Observable<string | null> = this.errorSubject.asObservable();

  // Zoom state observables (main graph only)
  public zoomState$: Observable<ZoomState> = this.zoomStateSubject.asObservable();
  public zoomScale$: Observable<number> = this.zoomState$.pipe(
    map(state => state.scale),
    distinctUntilChanged()
  );

  public zoomTransform$: Observable<d3.ZoomTransform> = this.zoomState$.pipe(
    map(state => state.transform),
    distinctUntilChanged()
  );

  // Display mode observables
  public displayMode$: Observable<DisplayMode> = this.zoomState$.pipe(
    map(state => state.scale >= this.SERVICE_CARD_FULL_THRESHOLD ? 'full' : 'compact'),
    distinctUntilChanged()
  );

  // Zoom event streams (filtered by scope)
  public mainGraphZoom$: Observable<ZoomRequest> = this.zoomRequestSubject.pipe(
    filter(req => req.scope === 'main')
  );

  public nestedGraphZoom$: Observable<ZoomRequest> = this.zoomRequestSubject.pipe(
    filter(req => req.scope === 'nested')
  );

  // Sidebar event streams
  public endpointSidebarRequest$: Observable<EndpointSidebarRequest> = this.endpointSidebarRequestSubject.asObservable();
  public useCaseSidebarRequest$: Observable<UseCaseSidebarRequest> = this.useCaseSidebarRequestSubject.asObservable();

  // ============================================================================
  // Constructor
  // ============================================================================

  constructor(private http: HttpClient) {}

  // ============================================================================
  // Data Methods
  // ============================================================================
  public async init(): Promise<void> {
    this.isBusySubject.next(true);
    this.errorSubject.next(null);

    try {
      const fullGraph = await firstValueFrom(
        this.http.get<FullGraph>(`${this.apiUrl}/full-graph`)
      );

      this.graphDataSubject.next(fullGraph);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Failed to load graph data';
      console.error('Failed to load graph data:', error);
      this.errorSubject.next(errorMessage);
    } finally {
      this.isBusySubject.next(false);
    }
  }

  public async reload(): Promise<void> {
    await this.init();
  }

  // ============================================================================
  // Zoom Methods
  // ============================================================================
  public updateZoom(transform: d3.ZoomTransform): void {
    this.zoomStateSubject.next({
      transform,
      scale: transform.k
    });
  }

  /**
   * Request a zoom operation (focus on node or reset)
   * Single entry point for all zoom/pan operations
   */
  public requestZoom(request: ZoomRequest): void {
    this.zoomRequestSubject.next(request);
  }

  // ============================================================================
  // Escape Key Handling
  // ============================================================================

  /**
   * Mark nested graph as changed (zoomed/panned)
   */
  public markNestedGraphChanged(): void {
    this.escapeState.nestedGraphChanged = true;
  }

  /**
   * Clear pending nested reset flag when user focuses on a new node
   */
  public clearPendingNestedReset(): void {
    this.escapeState.pendingNestedReset = false;
  }

  /**
   * Reset escape state flags
   */
  private resetEscapeState(): void {
    this.escapeState.nestedGraphChanged = false;
    this.escapeState.pendingNestedReset = false;
  }

  /**
   * Handle Esc key press
   * Implements smart reset logic:
   * - If zoomed in (scale >= 1.0):
   *   - First Esc: reset nested graph (if changed)
   *   - Second Esc: reset main graph to overview
   * - If at overview: reset main graph position/zoom
   */
  public handleEscapeKey(): void {
    const { scale } = this.zoomStateSubject.value;
    const isZoomedIn = scale >= this.SERVICE_CARD_FULL_THRESHOLD;

    if (!isZoomedIn) {
      // At overview level: reset main graph
      this.requestZoom({ scope: 'main', duration: 750 });
      this.resetEscapeState();
      return;
    }

    // Zoomed in on service card
    const { nestedGraphChanged, pendingNestedReset } = this.escapeState;

    if (nestedGraphChanged && !pendingNestedReset) {
      // First Esc: reset nested graph
      this.requestZoom({ scope: 'nested', duration: 750 });
      this.escapeState.pendingNestedReset = true;
    } else {
      // Second Esc or nested wasn't changed: zoom out to overview
      this.requestZoom({ scope: 'main', duration: 750 });
      this.resetEscapeState();
    }
  }

  // ============================================================================
  // Sidebar Methods
  // ============================================================================

  /**
   * Open endpoint sidebar with endpoint details
   */
  public showEndpointDetails(endpoint: Endpoint, service: ServiceData, fullGraph: FullGraph): void {
    // Find related services (services that call this endpoint)
    const relatedServices = fullGraph.services.filter(s =>
      s.relations.targetEndpointIds.includes(endpoint.id)
    );

    // Find use cases where this endpoint is involved (within the same service)
    const useCases = service.useCases.filter(uc =>
      uc.initiatingEndpointId === endpoint.id ||
      uc.steps.some(step => step.endpointId === endpoint.id)
    );

    // Find use cases from other services that use this endpoint
    const externalUseCases: Array<{ useCase: UseCase; service: ServiceData }> = [];
    fullGraph.services.forEach(s => {
      // Skip the current service to avoid duplicates
      if (s.service.id === service.service.id) return;

      s.useCases.forEach(uc => {
        // Check if this use case uses the endpoint in any step
        const usesEndpoint = uc.steps.some(step => step.endpointId === endpoint.id);
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

  /**
   * Open use case sidebar with use case details
   */
  public showUseCaseDetails(useCase: UseCase, service: ServiceData, fullGraph: FullGraph, stepIndex?: number): void {
    this.useCaseSidebarRequestSubject.next({
      action: 'open',
      data: { useCase, service, allServices: fullGraph.services, stepIndex }
    });

    // Also open endpoint sidebar with initiating endpoint
    const initiatingEndpoint = service.endpoint.find(
      ep => ep.id === useCase.initiatingEndpointId
    );
    if (initiatingEndpoint) {
      this.showEndpointDetails(initiatingEndpoint, service, fullGraph);
    }
  }

  /**
   * Focus on service by ID (main graph)
   */
  public focusOnService(serviceId: string): void {
    this.requestZoom({
      scope: 'main',
      targetId: serviceId,
      duration: 750
    });
  }

  /**
   * Focus on endpoint in nested graph
   */
  public focusOnEndpoint(endpointId: string): void {
    this.requestZoom({
      scope: 'nested',
      targetId: endpointId,
      duration: 750
    });
  }

  /**
   * Handle use case step click
   *
   * Invariants:
   * 1. Step references endpoint -> activate that endpoint
   * 2. Step references parent service (no endpoint) -> activate parent's initiating endpoint
   * 3. Step references other service (no endpoint) -> activate that service
   */
  public handleUseCaseStepClick(step: UseCaseStep, useCase: UseCase, service: ServiceData, fullGraph: FullGraph): void {
    const parentServiceId = service.service.id;
    const initiatingEndpointId = useCase.initiatingEndpointId;

    // Invariant 1: Step references endpoint -> activate that endpoint
    if (step.endpointId) {
      this.activateStepEndpoint(step.endpointId, parentServiceId, fullGraph);
      return;
    }

    // Invariant 2: Step references parent service -> activate parent's initiating endpoint
    if (step.serviceId === parentServiceId) {
      this.activateParentEndpoint(initiatingEndpointId, parentServiceId, service, fullGraph);
      return;
    }

    // Invariant 3: Step references other service -> activate that service
    if (step.serviceId) {
      this.activateService(step.serviceId);
      return;
    }

    // Fallback: No references -> activate parent's initiating endpoint
    this.activateParentEndpoint(initiatingEndpointId, parentServiceId, service, fullGraph);
  }

  /**
   * Activate a specific endpoint referenced by a step
   */
  private activateStepEndpoint(endpointId: string, parentServiceId: string, fullGraph: FullGraph): void {
    for (const serviceData of fullGraph.services) {
      const endpoint = serviceData.endpoint.find(ep => ep.id === endpointId);
      if (endpoint) {
        // If endpoint is in a different service, focus on that service first
        if (serviceData.service.id !== parentServiceId) {
          this.focusOnService(serviceData.service.id);
        }
        this.focusOnEndpoint(endpoint.id);
        this.showEndpointDetails(endpoint, serviceData, fullGraph);
        break;
      }
    }
  }

  /**
   * Activate parent service's initiating endpoint
   */
  private activateParentEndpoint(initiatingEndpointId: string, parentServiceId: string, service: ServiceData, fullGraph: FullGraph): void {
    const initiatingEndpoint = service.endpoint.find(ep => ep.id === initiatingEndpointId);
    if (initiatingEndpoint) {
      this.focusOnService(parentServiceId);
      this.focusOnEndpoint(initiatingEndpoint.id);
      this.showEndpointDetails(initiatingEndpoint, service, fullGraph);
    }
  }

  /**
   * Activate a service (without focusing on a specific endpoint)
   */
  private activateService(serviceId: string): void {
    this.focusOnService(serviceId);
    this.closeEndpointSidebar();
  }

  public closeEndpointSidebar(): void {
    this.endpointSidebarRequestSubject.next({ action: 'close' });
  }

  public closeUseCaseSidebar(): void {
    this.useCaseSidebarRequestSubject.next({ action: 'close' });
  }
}
