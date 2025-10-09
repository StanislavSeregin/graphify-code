import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { Endpoint, ServiceData, UseCase, GraphService, FullGraph } from '../../graph.service';

export interface EndpointSidebarData {
  endpoint: Endpoint;
  service: ServiceData;
  relatedServices: ServiceData[];
  useCases: UseCase[];
  externalUseCases: Array<{ useCase: UseCase; service: ServiceData }>;
}

@Component({
  selector: 'app-endpoint-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatDividerModule
  ],
  templateUrl: './endpoint-sidebar.component.html',
  styleUrl: './endpoint-sidebar.component.css'
})
export class EndpointSidebarComponent {
  @Input() data: EndpointSidebarData | null = null;

  constructor(private graphService: GraphService) {}

  onClose(): void {
    this.graphService.closeEndpointSidebar();
  }

  onServiceClick(serviceId: string): void {
    this.graphService.focusOnService(serviceId);
  }

  onUseCaseClick(useCaseId: string): void {
    // Find the use case and open its sidebar
    if (!this.data) return;

    const useCase = this.data.useCases.find(uc => uc.id === useCaseId);
    if (!useCase) return;

    // Get full graph for showUseCaseDetails
    this.graphService.graphData$.subscribe(fullGraph => {
      if (fullGraph) {
        this.graphService.showUseCaseDetails(useCase, this.data!.service, fullGraph);
        // Also focus on the service
        this.graphService.focusOnService(this.data!.service.service.id);
      }
    }).unsubscribe();
  }

  onExternalUseCaseClick(useCaseId: string, serviceId: string): void {
    // Find the external use case and its service
    if (!this.data) return;

    const externalUseCase = this.data.externalUseCases.find(
      euc => euc.useCase.id === useCaseId && euc.service.service.id === serviceId
    );
    if (!externalUseCase) return;

    // Get full graph for showUseCaseDetails
    this.graphService.graphData$.subscribe(fullGraph => {
      if (fullGraph) {
        this.graphService.showUseCaseDetails(
          externalUseCase.useCase,
          externalUseCase.service,
          fullGraph
        );
        // Focus on the service first
        this.graphService.focusOnService(externalUseCase.service.service.id);

        // Then focus on the initiating endpoint
        const initiatingEndpoint = externalUseCase.service.endpoint.find(
          ep => ep.id === externalUseCase.useCase.initiatingEndpointId
        );
        if (initiatingEndpoint) {
          this.graphService.focusOnEndpoint(initiatingEndpoint.id);
        }
      }
    }).unsubscribe();
  }
}
