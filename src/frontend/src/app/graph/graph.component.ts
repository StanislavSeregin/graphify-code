import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Endpoint, FullGraph, ServiceData, UseCase, UseCaseStep, GraphService } from './graph.service';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Observable, Subject, takeUntil } from 'rxjs';
import { endpointTypeLabel, formatLastAnalyzed } from './graph-layout';
import { GraphRenderer } from './graph-renderer';
import {
  EndpointReference,
  GraphServiceNode,
  GraphViewModel,
  UseCaseReference,
  buildGraphViewModel,
  findRelatedServicesForEndpoint,
  findUseCasesForEndpoint
} from './graph-view-model';

type DetailMode = 'service' | 'endpoint' | 'useCase';

@Component({
  selector: 'app-graph',
  standalone: true,
  imports: [
    AsyncPipe,
    MatProgressBarModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './graph.component.html',
  styleUrl: './graph.component.css'
})
export class GraphComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graphSvg', { static: false }) svgElement!: ElementRef<SVGSVGElement>;

  private destroy$ = new Subject<void>();
  private renderer: GraphRenderer | null = null;

  isBusy$!: Observable<boolean>;
  error$!: Observable<string | null>;
  graphData$!: Observable<FullGraph | null>;

  viewModel: GraphViewModel | null = null;
  detailMode: DetailMode = 'service';
  selectedServiceId: string | null = null;
  selectedEndpointId: string | null = null;
  selectedUseCaseId: string | null = null;

  constructor(
    private graphService: GraphService
  ) {
    this.isBusy$ = this.graphService.isBusy$;
    this.error$ = this.graphService.error$;
    this.graphData$ = this.graphService.graphData$;
  }

  @HostListener('window:resize')
  onViewportResize(): void { this.renderer?.resize(); }

  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.resetView();
    }
  }

  ngOnInit(): void {
    this.graphService.init();
  }

  ngAfterViewInit(): void {
    this.renderer = new GraphRenderer(this.svgElement.nativeElement, {
      onServiceSelect: serviceId => this.selectService(serviceId, true),
      onCanvasClick: () => this.clearSelection(),
      onZoom: transform => this.graphService.updateZoom(transform)
    });

    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        if (data) {
          this.viewModel = buildGraphViewModel(data);
          this.selectedServiceId = this.viewModel.services[0]?.id ?? null;
          this.detailMode = 'service';
          this.renderer?.render(this.viewModel, this.selectedServiceId);
        }
      });
  }

  onReload(): void {
    void this.graphService.reload();
  }

  resetView(): void {
    this.clearSelection();
    this.renderer?.resetView();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.renderer?.destroy();
  }

  selectService(serviceId: string, focus = false): void {
    if (!this.viewModel?.servicesById.has(serviceId)) return;
    this.selectedServiceId = serviceId;
    this.selectedEndpointId = null;
    this.selectedUseCaseId = null;
    this.detailMode = 'service';
    this.renderer?.updateSelection(serviceId);
    if (focus) {
      this.renderer?.focusService(serviceId);
    }
  }

  selectEndpoint(endpointId: string): void {
    const endpoint = this.viewModel?.endpointsById.get(endpointId);
    if (!endpoint) return;
    this.selectedEndpointId = endpointId;
    this.selectedUseCaseId = null;
    this.selectedServiceId = endpoint.service.service.id;
    this.detailMode = 'endpoint';
    this.renderer?.updateSelection(this.selectedServiceId);
    this.renderer?.focusService(this.selectedServiceId);
  }

  selectUseCase(useCaseId: string): void {
    const useCase = this.viewModel?.useCasesById.get(useCaseId);
    if (!useCase) return;
    this.selectedUseCaseId = useCaseId;
    this.selectedEndpointId = null;
    this.selectedServiceId = useCase.service.service.id;
    this.detailMode = 'useCase';
    this.renderer?.updateSelection(this.selectedServiceId);
    this.renderer?.focusService(this.selectedServiceId);
  }

  selectStep(step: UseCaseStep): void {
    if (step.endpointId && this.viewModel?.endpointsById.has(step.endpointId)) {
      this.selectEndpoint(step.endpointId);
      return;
    }

    if (step.serviceId && this.viewModel?.servicesById.has(step.serviceId)) {
      this.selectService(step.serviceId, true);
    }
  }

  clearSelection(): void {
    this.selectedEndpointId = null;
    this.selectedUseCaseId = null;
    this.selectedServiceId = null;
    this.detailMode = 'service';
    this.renderer?.updateSelection(null);
  }

  closePanel(): void {
    this.clearSelection();
  }

  get selectedService(): GraphServiceNode | null {
    if (!this.selectedServiceId || !this.viewModel) return null;
    return this.viewModel.servicesById.get(this.selectedServiceId) ?? null;
  }

  get selectedEndpoint(): EndpointReference | null {
    if (!this.selectedEndpointId || !this.viewModel) return null;
    return this.viewModel.endpointsById.get(this.selectedEndpointId) ?? null;
  }

  get selectedUseCase(): UseCaseReference | null {
    if (!this.selectedUseCaseId || !this.viewModel) return null;
    return this.viewModel.useCasesById.get(this.selectedUseCaseId) ?? null;
  }

  get panelOpen(): boolean {
    return Boolean(this.selectedServiceId);
  }

  get serviceCount(): number {
    return this.viewModel?.services.length ?? 0;
  }

  get endpointCount(): number {
    return this.viewModel?.services.reduce((sum, service) => sum + service.endpointCount, 0) ?? 0;
  }

  get useCaseCount(): number {
    return this.viewModel?.services.reduce((sum, service) => sum + service.useCaseCount, 0) ?? 0;
  }

  endpointTypeLabel(type: string): string {
    return endpointTypeLabel(type);
  }

  formatLastAnalyzed(value: string | null | undefined): string {
    return formatLastAnalyzed(value) ?? 'Unknown';
  }

  endpointUseCases(endpointId: string): UseCaseReference[] {
    return this.viewModel ? findUseCasesForEndpoint(this.viewModel, endpointId) : [];
  }

  relatedServices(endpointId: string): GraphServiceNode[] {
    return this.viewModel ? findRelatedServicesForEndpoint(this.viewModel, endpointId) : [];
  }

  trackService(_: number, service: GraphServiceNode): string { return service.id; }
  trackEndpoint(_: number, endpoint: Endpoint): string { return endpoint.id; }
  trackUseCase(_: number, useCase: UseCase): string { return useCase.id; }
  trackUseCaseRef(_: number, reference: UseCaseReference): string { return reference.useCase.id; }
  trackStep(index: number): number { return index; }
}
