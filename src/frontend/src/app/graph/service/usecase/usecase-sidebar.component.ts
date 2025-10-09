import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
import { UseCase, UseCaseStep, ServiceData, GraphService } from '../../graph.service';

export interface UseCaseSidebarData {
  useCase: UseCase;
  service: ServiceData;
  allServices?: ServiceData[];  // For looking up service names
  stepIndex?: number;
}

@Component({
  selector: 'app-usecase-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    MatSidenavModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule,
    MatDividerModule
  ],
  templateUrl: './usecase-sidebar.component.html',
  styleUrl: './usecase-sidebar.component.css'
})
export class UseCaseSidebarComponent implements OnChanges {
  @Input() data: UseCaseSidebarData | null = null;

  expandedStepIndex: number | null = this.data?.stepIndex ?? null;

  constructor(private graphService: GraphService) {}

  ngOnChanges(changes: SimpleChanges): void {
    // Reset expanded step when use case data changes
    const data = changes['data'];
    if (data && !data.firstChange) {
      this.expandedStepIndex = this.data?.stepIndex ?? null;
    }
  }

  onClose(): void {
    this.graphService.closeUseCaseSidebar();
  }

  onStepExpanded(index: number, step: UseCaseStep): void {
    this.expandedStepIndex = index;

    if (!this.data) return;

    // Get full graph for step click handling
    this.graphService.graphData$.subscribe(fullGraph => {
      if (fullGraph) {
        this.graphService.handleUseCaseStepClick(
          step,
          this.data!.useCase,
          this.data!.service,
          fullGraph
        );
      }
    }).unsubscribe();
  }

  onStepClosed(index: number): void {
    if (this.expandedStepIndex === index) {
      this.expandedStepIndex = null;
    }
  }

  getStepTargetInfo(step: UseCaseStep): { label: string, value: string } | null {
    if (!this.data) return null;

    // Priority: endpointId > serviceId
    if (step.endpointId) {
      // Find endpoint in current service first
      const localEndpoint = this.data.service.endpoint.find(ep => ep.id === step.endpointId);
      if (localEndpoint) {
        return { label: 'Refers to endpoint', value: localEndpoint.name };
      }

      // Search in all services if provided
      if (this.data.allServices) {
        for (const serviceData of this.data.allServices) {
          const endpoint = serviceData.endpoint.find(ep => ep.id === step.endpointId);
          if (endpoint) {
            return {
              label: 'Refers to endpoint',
              value: `${endpoint.name} (${serviceData.service.name})`
            };
          }
        }
      }

      return { label: 'Endpoint ID', value: step.endpointId };
    } else if (step.serviceId) {
      // Search in all services if provided
      if (this.data.allServices) {
        const targetService = this.data.allServices.find(s => s.service.id === step.serviceId);
        if (targetService) {
          return { label: 'Contained in service', value: targetService.service.name };
        }
      }

      return { label: 'Service ID', value: step.serviceId };
    }

    return null;
  }

  get initiatingEndpointName(): string | null {
    if (!this.data) return null;

    const endpoint = this.data.service.endpoint.find(
      ep => ep.id === this.data!.useCase.initiatingEndpointId
    );

    return endpoint ? endpoint.name : null;
  }
}
