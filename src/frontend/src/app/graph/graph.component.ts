import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { Endpoint, FullGraph, UseCase, GraphService } from './graph.service';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { Observable, Subject, takeUntil } from 'rxjs';
import { endpointTypeLabel, formatLastAnalyzed } from './graph-layout';
import { GraphRenderer } from './graph-renderer';
import {
  GraphServiceNode,
  GraphViewModel,
  UseCaseReference,
  buildGraphViewModel,
  findRelatedServicesForEndpoint,
  findUseCasesForEndpoint
} from './graph-view-model';
import { UseCaseFlowModel, buildUseCaseFlowModel } from './use-case-flow-model';
import { FlowSelection, UseCaseFlowRenderer } from './use-case-flow-renderer';

@Component({
  selector: 'app-graph',
  standalone: true,
  imports: [
    AsyncPipe,
    MatProgressBarModule,
    MatButtonModule,
    MatIconModule,
    MatExpansionModule
  ],
  templateUrl: './graph.component.html',
  styleUrl: './graph.component.css'
})
export class GraphComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graphSvg', { static: false }) svgElement!: ElementRef<SVGSVGElement>;
  @ViewChild('flowSvg', { static: false }) flowSvgElement!: ElementRef<SVGSVGElement>;

  private destroy$ = new Subject<void>();
  private renderer: GraphRenderer | null = null;
  private flowRenderer: UseCaseFlowRenderer | null = null;

  isBusy$!: Observable<boolean>;
  error$!: Observable<string | null>;
  graphData$!: Observable<FullGraph | null>;

  viewModel: GraphViewModel | null = null;
  selectedServiceId: string | null = null;
  expandedEndpointId: string | null = null;
  expandedUseCaseId: string | null = null;
  activeFlowModel: UseCaseFlowModel | null = null;
  activeFlowStepIndex: number | null = null;
  activeFlowServiceId: string | null = null;
  activeFlowEndpointKey: string | null = null;
  activeFlowSelection: FlowSelection = null;

  constructor(
    private graphService: GraphService
  ) {
    this.isBusy$ = this.graphService.isBusy$;
    this.error$ = this.graphService.error$;
    this.graphData$ = this.graphService.graphData$;
  }

  @HostListener('window:resize')
  onViewportResize(): void {
    this.renderer?.resize();
    this.flowRenderer?.resize();
  }

  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      if (this.flowOverlayOpen) {
        this.closeFlowOverlay();
        event.preventDefault();
        return;
      }
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
    this.flowRenderer = new UseCaseFlowRenderer(this.flowSvgElement.nativeElement, {
      onServiceSelect: serviceId => this.activateFlowService(serviceId),
      onEndpointSelect: (serviceId, endpointKey) => this.activateFlowEndpoint(serviceId, endpointKey),
      onStepSelect: stepIndex => this.activateFlowStep(stepIndex)
    });

    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        if (data) {
          this.viewModel = buildGraphViewModel(data);
          this.selectedServiceId = null;
          this.expandedEndpointId = null;
          this.expandedUseCaseId = null;
          this.activeFlowServiceId = null;
          this.activeFlowEndpointKey = null;
          this.activeFlowSelection = null;
          this.closeFlowOverlay();
          this.renderer?.render(this.viewModel, null);
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
    this.flowRenderer?.destroy();
  }

  selectService(serviceId: string, focus = false): void {
    if (!this.viewModel?.servicesById.has(serviceId)) return;
    this.selectedServiceId = serviceId;
    this.expandedEndpointId = null;
    this.expandedUseCaseId = null;
    this.activeFlowServiceId = null;
    this.activeFlowEndpointKey = null;
    this.activeFlowSelection = null;
    this.closeFlowOverlay();
    this.renderer?.updateSelection(serviceId);
    if (focus) {
      this.renderer?.focusService(serviceId);
    }
  }

  expandUseCase(useCaseId: string): void {
    const useCase = this.viewModel?.useCasesById.get(useCaseId);
    if (!useCase) return;

    if (this.selectedServiceId !== useCase.service.service.id) {
      this.selectService(useCase.service.service.id, true);
    }
    this.expandedUseCaseId = useCaseId;
    this.openFlowOverlay(useCase.useCase, 0);
  }

  onUseCaseOpened(useCase: UseCase): void {
    this.expandedUseCaseId = useCase.id;
    if (this.activeFlowModel?.useCaseId === useCase.id) {
      this.flowRenderer?.updateActiveState(this.activeFlowSelection);
      return;
    }
    this.openFlowOverlay(useCase, 0);
  }

  openFlowOverlay(useCase: UseCase, stepIndex: number): void {
    if (!this.viewModel || !this.selectedService) return;

    const existingModel = this.activeFlowModel?.useCaseId === useCase.id
      ? this.activeFlowModel
      : null;
    if (existingModel) {
      this.activateFlowStep(stepIndex);
      return;
    }

    this.activeFlowModel = buildUseCaseFlowModel(useCase, this.selectedService.serviceData, this.viewModel);
    const step = this.findFlowStep(stepIndex);
    this.activeFlowStepIndex = stepIndex;
    this.activeFlowServiceId = null;
    this.activeFlowEndpointKey = null;
    this.activeFlowSelection = { type: 'step', stepIndex };
    this.flowRenderer?.render(this.activeFlowModel, this.activeFlowSelection);
    this.renderer?.highlightServices(this.activeFlowModel.serviceIds);
  }

  activateFlowStep(stepIndex: number): void {
    const step = this.findFlowStep(stepIndex);
    if (!this.activeFlowModel || !step) return;

    this.selectedServiceId = this.activeFlowModel.owningServiceId;
    this.expandedEndpointId = null;
    this.expandedUseCaseId = this.activeFlowModel.useCaseId;
    this.activeFlowStepIndex = stepIndex;
    this.activeFlowServiceId = null;
    this.activeFlowEndpointKey = null;
    this.activeFlowSelection = { type: 'step', stepIndex };
    this.flowRenderer?.updateActiveState(this.activeFlowSelection);
    this.renderer?.highlightServices(this.activeFlowModel.serviceIds);

    window.setTimeout(() => {
      document.getElementById(this.stepElementId(this.activeFlowModel?.useCaseId ?? '', stepIndex))
        ?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
    });
  }

  activateFlowEndpoint(serviceId: string, endpointKey: string): void {
    if (!this.viewModel?.servicesById.has(serviceId)) return;

    this.selectedServiceId = serviceId;
    this.expandedUseCaseId = null;
    this.expandedEndpointId = this.viewModel.endpointsById.has(endpointKey) ? endpointKey : null;
    this.activeFlowServiceId = serviceId;
    this.activeFlowEndpointKey = endpointKey;
    this.activeFlowStepIndex = null;
    this.activeFlowSelection = { type: 'endpoint', serviceId, endpointKey };
    this.flowRenderer?.updateActiveState(this.activeFlowSelection);
    if (this.activeFlowModel) {
      this.renderer?.highlightServices(this.activeFlowModel.serviceIds);
    }
  }

  activateFlowService(serviceId: string): void {
    if (!this.viewModel?.servicesById.has(serviceId)) return;

    this.selectedServiceId = serviceId;
    this.expandedEndpointId = null;
    this.expandedUseCaseId = this.activeFlowModel?.owningServiceId === serviceId
      ? this.activeFlowModel.useCaseId
      : null;
    this.activeFlowServiceId = serviceId;
    this.activeFlowEndpointKey = null;
    this.activeFlowStepIndex = null;
    this.activeFlowSelection = { type: 'service', serviceId };
    this.flowRenderer?.updateActiveState(this.activeFlowSelection);
    if (this.activeFlowModel) {
      this.renderer?.highlightServices(this.activeFlowModel.serviceIds);
    }
  }

  clearSelection(): void {
    this.selectedServiceId = null;
    this.expandedEndpointId = null;
    this.expandedUseCaseId = null;
    this.activeFlowServiceId = null;
    this.activeFlowEndpointKey = null;
    this.activeFlowSelection = null;
    this.closeFlowOverlay();
    this.renderer?.updateSelection(null);
  }

  closePanel(): void {
    this.clearSelection();
  }

  closeFlowOverlay(): void {
    this.activeFlowModel = null;
    this.activeFlowStepIndex = null;
    this.activeFlowServiceId = null;
    this.activeFlowEndpointKey = null;
    this.activeFlowSelection = null;
    this.flowRenderer?.clear();
    if (this.selectedServiceId) {
      this.renderer?.updateSelection(this.selectedServiceId);
    } else {
      this.renderer?.updateSelection(null);
    }
  }

  get selectedService(): GraphServiceNode | null {
    if (!this.selectedServiceId || !this.viewModel) return null;
    return this.viewModel.servicesById.get(this.selectedServiceId) ?? null;
  }

  get flowOverlayOpen(): boolean {
    return Boolean(this.activeFlowModel);
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

  stepElementId(useCaseId: string, index: number): string {
    return `usecase-step-${useCaseId}-${index}`;
  }

  private findFlowStep(stepIndex: number) {
    return this.activeFlowModel?.services
      .flatMap(service => service.endpoints)
      .flatMap(endpoint => endpoint.steps)
      .find(step => step.index === stepIndex) ?? null;
  }

  trackService(_: number, service: GraphServiceNode): string { return service.id; }
  trackEndpoint(_: number, endpoint: Endpoint): string { return endpoint.id; }
  trackUseCase(_: number, useCase: UseCase): string { return useCase.id; }
  trackUseCaseRef(_: number, reference: UseCaseReference): string { return reference.useCase.id; }
  trackStep(index: number): number { return index; }
}
