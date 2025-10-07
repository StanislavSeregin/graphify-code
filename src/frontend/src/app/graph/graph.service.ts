import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

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

@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  private graphDataSubject = new BehaviorSubject<FullGraph | null>(null);
  private isBusySubject = new BehaviorSubject<boolean>(false);

  public graphData$ = this.graphDataSubject.asObservable();
  public isBusy$ = this.isBusySubject.asObservable();

  public async init(): Promise<void> {
    this.isBusySubject.next(true);

    try {
      const fullGraph = await firstValueFrom(
        this.http.get<FullGraph>(`${this.apiUrl}/full-graph`)
      );

      this.graphDataSubject.next(fullGraph);
    } catch (error) {
      console.error('Failed to load graph data:', error);
    } finally {
      this.isBusySubject.next(false);
    }
  }

}
