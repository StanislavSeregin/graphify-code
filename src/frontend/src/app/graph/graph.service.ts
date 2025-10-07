import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, firstValueFrom } from 'rxjs';
import { map, distinctUntilChanged } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import * as d3 from 'd3';

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

export type DisplayMode = 'compact' | 'full';

export interface ZoomState {
  transform: d3.ZoomTransform;
  scale: number;
}

@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private readonly apiUrl = environment.apiUrl;
  private readonly SERVICE_CARD_FULL_THRESHOLD = 1.0;
  private readonly ENDPOINT_CARD_FULL_THRESHOLD = 3.0;

  // Data state
  private graphDataSubject = new BehaviorSubject<FullGraph | null>(null);
  private isBusySubject = new BehaviorSubject<boolean>(false);
  private errorSubject = new BehaviorSubject<string | null>(null);

  // Zoom state
  private zoomStateSubject = new BehaviorSubject<ZoomState>({
    transform: d3.zoomIdentity,
    scale: 1
  });


  // Public observables - Data
  public graphData$: Observable<FullGraph | null> = this.graphDataSubject.asObservable();
  public isBusy$: Observable<boolean> = this.isBusySubject.asObservable();
  public error$: Observable<string | null> = this.errorSubject.asObservable();

  // Public observables - Zoom
  public zoomState$: Observable<ZoomState> = this.zoomStateSubject.asObservable();
  public zoomScale$: Observable<number> = this.zoomState$.pipe(
    map(state => state.scale),
    distinctUntilChanged()
  );
  public zoomTransform$: Observable<d3.ZoomTransform> = this.zoomState$.pipe(
    map(state => state.transform),
    distinctUntilChanged()
  );

  // Public observables - Display mode
  public displayMode$: Observable<DisplayMode> = this.zoomState$.pipe(
    map(state => state.scale >= this.SERVICE_CARD_FULL_THRESHOLD ? 'full' : 'compact'),
    distinctUntilChanged()
  );

  public endpointDisplayMode$: Observable<DisplayMode> = this.zoomState$.pipe(
    map(state => state.scale >= this.ENDPOINT_CARD_FULL_THRESHOLD ? 'full' : 'compact'),
    distinctUntilChanged()
  );

  constructor(private http: HttpClient) {}

  // Data methods
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

  // Zoom methods
  public updateZoom(transform: d3.ZoomTransform): void {
    this.zoomStateSubject.next({
      transform,
      scale: transform.k
    });
  }
}
