import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';

export type Service = {
  id: string;
  name: string;
  description: string;
  hasEndpoints: boolean;
  hasRelations: boolean;
  lastAnalyzed: string;
  relativeCodePath: string | null;
};

export type Services = {
  serviceList: Service[];
};

export type Endpoint = {
  id: string;
  name: string;
  description: string;
  type: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

export type Endpoints = {
  endpointList: Endpoint[];
};

export type Relations = {
  targetEndpointIds: string[];
};

export type UseCaseSummary = {
  id: string;
  name: string;
  description: string;
  initiatingEndpointId: string;
  lastAnalyzed: string;
  stepCount: number;
};

export type UseCases = {
  useCaseList: UseCaseSummary[];
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

export type ServiceFullData = {
  service: Service;
  endpoints: Endpoint[];
  relations: Relations;
  useCaseDetails: Map<string, UseCase>;
};

export type GraphData = Map<string, ServiceFullData>;

@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) { }

  private graphDataSubject = new BehaviorSubject<GraphData>(new Map());
  private isBusySubject = new BehaviorSubject<boolean>(false);

  public graphData$ = this.graphDataSubject.asObservable();
  public isBusy$ = this.isBusySubject.asObservable();

  public async init(): Promise<void> {
    this.isBusySubject.next(true);

    try {
      // Load all services
      const servicesResponse = await firstValueFrom(
        this.http.get<Services>(`${this.apiUrl}/services`)
      );

      const services = servicesResponse.serviceList;

      // Load data for each service in parallel
      const serviceDataPromises = services.map(async (service) => {
        try {
          const [endpoints, relations, useCases] = await Promise.all([
            firstValueFrom(this.http.get<Endpoints>(`${this.apiUrl}/endpoints/${service.id}`)),
            firstValueFrom(this.http.get<Relations>(`${this.apiUrl}/relations/${service.id}`)),
            firstValueFrom(this.http.get<UseCases>(`${this.apiUrl}/use-cases/${service.id}`))
          ]);

          // Load details for all use cases
          const useCaseDetailsPromises = useCases.useCaseList.map(async (useCaseSummary) => {
            try {
              const useCase = await firstValueFrom(
                this.http.get<UseCase>(`${this.apiUrl}/use-case/${useCaseSummary.id}`)
              );
              return { success: true, useCaseId: useCaseSummary.id, useCase };
            } catch (error) {
              console.error(`Failed to load use case details for ${useCaseSummary.id}:`, error);
              return { success: false, useCaseId: useCaseSummary.id };
            }
          });

          const useCaseDetailsResults = await Promise.allSettled(useCaseDetailsPromises);

          const useCaseDetailsMap = new Map<string, UseCase>();
          useCaseDetailsResults.forEach((result) => {
            if (result.status === 'fulfilled' && result.value.success) {
              useCaseDetailsMap.set(result.value.useCaseId, result.value.useCase!);
            }
          });

          const fullData: ServiceFullData = {
            service,
            endpoints: endpoints.endpointList,
            relations,
            useCaseDetails: useCaseDetailsMap
          };

          // Update graphData with new service data
          const currentData = this.graphDataSubject.value;
          const newData = new Map(currentData);
          newData.set(service.id, fullData);
          this.graphDataSubject.next(newData);

          return { success: true, serviceId: service.id };
        } catch (error) {
          console.error(`Failed to load data for service ${service.name} (${service.id}):`, error);
          return { success: false, serviceId: service.id, error };
        }
      });

      await Promise.allSettled(serviceDataPromises);
    } catch (error) {
      console.error('Failed to load services:', error);
    } finally {
      this.isBusySubject.next(false);
    }
  }

}
