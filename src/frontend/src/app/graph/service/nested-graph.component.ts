import { Component, Input, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit, ApplicationRef, createComponent, EnvironmentInjector } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EndpointCardComponent } from './endpoint/endpoint-card.component';
import { UseCaseCardComponent } from './usecase/usecase-card.component';
import { Endpoint, UseCase, DisplayMode, GraphService, ZoomRequest, ServiceData, FullGraph } from '../graph.service';
import * as d3 from 'd3';
import { BehaviorSubject, Subject, takeUntil } from 'rxjs';

interface EndpointNode extends d3.SimulationNodeDatum {
  id: string;
  type: 'endpoint';
  endpoint: Endpoint;
  componentRef?: any;
}

interface UseCaseNode extends d3.SimulationNodeDatum {
  id: string;
  type: 'usecase';
  useCase: UseCase;
  componentRef?: any;
}

type GraphNode = EndpointNode | UseCaseNode;

interface Link {
  source: string | GraphNode;
  target: string | GraphNode;
}

@Component({
  selector: 'app-nested-graph',
  standalone: true,
  imports: [CommonModule],
  template: `<svg #nestedSvg class="nested-svg"></svg>`,
  styles: [`
    :host {
      display: block;
      width: 100%;
      height: 100%;
    }
    .nested-svg {
      width: 100%;
      height: 100%;
    }
  `]
})
export class NestedGraphComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('nestedSvg', { static: false}) svgElement!: ElementRef<SVGSVGElement>;
  @Input() endpoints: Endpoint[] = [];
  @Input() useCases: UseCase[] = [];
  @Input() serviceData!: ServiceData;

  private svg!: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private gMain!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private gLinks!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private gNodes!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private simulation!: d3.Simulation<GraphNode, Link>;
  private nodes: GraphNode[] = [];
  private links: Link[] = [];
  private zoom!: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private localDisplayMode$ = new BehaviorSubject<DisplayMode>('compact');
  private currentScale = 0.2;
  private initialScale = 0.08;
  private destroy$ = new Subject<void>();
  private fullGraph: FullGraph | null = null;

  private readonly DISPLAY_MODE_THRESHOLD = 0.1;

  private getCardWidth(): number {
    const vw = window.innerWidth;
    return Math.min(Math.max(1800, vw * 1.20), 3000);
  }

  private getCardHeight(): number {
    const vh = window.innerHeight;
    return Math.min(Math.max(400, vh * 0.36), 800);
  }

  constructor(
    private appRef: ApplicationRef,
    private injector: EnvironmentInjector,
    private graphService: GraphService
  ) {}

  ngOnInit(): void {
    // Subscribe to full graph data for sidebar operations
    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        this.fullGraph = data;
      });
  }

  ngAfterViewInit(): void {
    this.initSvg();
    this.setupZoom();
    this.renderEndpoints();

    // Subscribe to nested graph zoom requests
    this.graphService.nestedGraphZoom$
      .pipe(takeUntil(this.destroy$))
      .subscribe(request => {
        this.handleZoomRequest(request);
      });

    // Handle window resize
    window.addEventListener('resize', () => this.onWindowResize());
  }

  private onWindowResize(): void {
    // Redraw cards with new dimensions
    if (this.nodes.length > 0) {
      this.updateCardSizes();
    }
  }

  private updateCardSizes(): void {
    const cardWidth = this.getCardWidth();
    const cardHeight = this.getCardHeight();

    this.gNodes.selectAll<SVGGElement, EndpointNode>('g')
      .select('foreignObject')
      .attr('width', cardWidth)
      .attr('height', cardHeight)
      .attr('x', -cardWidth / 2)
      .attr('y', -cardHeight / 2);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();

    if (this.simulation) {
      this.simulation.stop();
    }
    this.nodes.forEach(node => {
      if (node.componentRef) {
        this.appRef.detachView(node.componentRef.hostView);
        node.componentRef.destroy();
      }
    });
    this.localDisplayMode$.complete();
    // Remove resize listener
    window.removeEventListener('resize', () => this.onWindowResize());
  }

  private initSvg(): void {
    this.svg = d3.select(this.svgElement.nativeElement);
    this.gMain = this.svg.append('g').attr('class', 'nested-main');
    this.gLinks = this.gMain.append('g').attr('class', 'nested-links');
    this.gNodes = this.gMain.append('g').attr('class', 'nested-nodes');
  }

  private setupZoom(): void {
    this.zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.05, 2])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        this.gMain.attr('transform', event.transform.toString());
        this.currentScale = event.transform.k;

        // Check if zoom was changed from initial state
        const scaleDiff = Math.abs(this.currentScale - this.initialScale);
        if (scaleDiff > 0.001) {
          this.graphService.markNestedGraphChanged();
        }

        // Update local display mode based on zoom scale
        const newMode: DisplayMode = this.currentScale >= this.DISPLAY_MODE_THRESHOLD ? 'full' : 'compact';
        if (this.localDisplayMode$.value !== newMode) {
          this.localDisplayMode$.next(newMode);
        }
      });

    this.svg.call(this.zoom);

    // Set initial zoom level based on viewport - reduced for wider cards
    const baseScale = Math.min(window.innerWidth, window.innerHeight) / 15000;
    this.initialScale = Math.max(0.025, Math.min(0.08, baseScale));
    const initialTransform = d3.zoomIdentity.scale(this.initialScale);
    this.svg.call(this.zoom.transform, initialTransform);
  }

  /**
   * Handle zoom request from GraphService
   */
  private handleZoomRequest(request: ZoomRequest): void {
    if (!this.svg) return;

    const duration = request.duration ?? 750;
    let transform: d3.ZoomTransform;

    if (request.transform) {
      // Explicit transform provided
      transform = request.transform;
    } else if (request.targetId) {
      // Focus on specific node
      const node = this.nodes.find(n => n.id === request.targetId);
      if (!node) return;

      const width = this.svgElement.nativeElement.clientWidth;
      const height = this.svgElement.nativeElement.clientHeight;
      const x = node.x ?? 0;
      const y = node.y ?? 0;
      const scale = 0.18; // Higher than DISPLAY_MODE_THRESHOLD (0.1) to trigger full mode
      const translateX = width / 2 - scale * x;
      const translateY = height / 2 - scale * y;

      transform = d3.zoomIdentity
        .translate(translateX, translateY)
        .scale(scale);
    } else {
      // Reset to initial view
      const baseScale = Math.min(window.innerWidth, window.innerHeight) / 15000;
      const initialScale = Math.max(0.025, Math.min(0.08, baseScale));
      transform = d3.zoomIdentity.scale(initialScale);
    }

    this.svg
      .transition()
      .duration(duration)
      .call(this.zoom.transform, transform);
  }

  private renderEndpoints(): void {
    if ((!this.endpoints || this.endpoints.length === 0) &&
        (!this.useCases || this.useCases.length === 0)) {
      return;
    }

    // Create nodes for both endpoints and use cases
    const endpointNodes: EndpointNode[] = (this.endpoints || []).map(endpoint => ({
      id: endpoint.id,
      type: 'endpoint',
      endpoint: endpoint
    }));

    const useCaseNodes: UseCaseNode[] = (this.useCases || []).map(useCase => ({
      id: useCase.id,
      type: 'usecase',
      useCase: useCase
    }));

    this.nodes = [...endpointNodes, ...useCaseNodes];

    // No links in nested graph
    this.links = [];

    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;
    const initialScale = Math.max(0.025, Math.min(0.08, Math.min(window.innerWidth, window.innerHeight) / 15000));

    // Center point in scaled coordinates
    const centerX = (width / 2) / initialScale;
    const centerY = (height / 2) / initialScale;

    // Simple vertical layout with tight spacing
    const cardHeight = this.getCardHeight();
    const spacing = cardHeight + Math.min(100, window.innerHeight * 0.05); // Small gap between cards
    const totalHeight = this.nodes.length * spacing;
    const startY = centerY - totalHeight / 2 + cardHeight / 2;

    this.nodes.forEach((node, i) => {
      node.x = centerX;
      node.y = startY + i * spacing;
      node.fx = node.x;
      node.fy = node.y;
    });

    // Render links
    this.renderLinks();

    // Render nodes
    const nodeSelection = this.gNodes
      .selectAll<SVGGElement, GraphNode>('g')
      .data(this.nodes, d => d.id)
      .join('g')
      .attr('class', d => d.type === 'endpoint' ? 'endpoint-node' : 'usecase-node');

    nodeSelection.each((d, i, groups) => {
      const group = d3.select(groups[i]);

      const cardWidth = this.getCardWidth();
      const cardHeight = this.getCardHeight();
      const foreignObject = group.append('foreignObject')
        .attr('width', cardWidth)
        .attr('height', cardHeight)
        .attr('x', -cardWidth / 2)
        .attr('y', -cardHeight / 2);

      if (d.type === 'endpoint') {
        const componentRef = createComponent(EndpointCardComponent, {
          environmentInjector: this.injector
        });

        d.componentRef = componentRef;
        componentRef.setInput('endpoint', d.endpoint);
        componentRef.setInput('displayMode$', this.localDisplayMode$.asObservable());
        componentRef.setInput('serviceData', this.serviceData);

        // Note: Click handling is now done directly in EndpointCardComponent via GraphService

        this.appRef.attachView(componentRef.hostView);

        const domElement = componentRef.location.nativeElement;
        foreignObject.node()?.appendChild(domElement);
      } else {
        const componentRef = createComponent(UseCaseCardComponent, {
          environmentInjector: this.injector
        });

        d.componentRef = componentRef;
        componentRef.setInput('useCase', d.useCase);
        componentRef.setInput('displayMode$', this.localDisplayMode$.asObservable());
        componentRef.setInput('serviceData', this.serviceData);

        // Note: Click handling is now done directly in UseCaseCardComponent via GraphService

        this.appRef.attachView(componentRef.hostView);

        const domElement = componentRef.location.nativeElement;
        foreignObject.node()?.appendChild(domElement);
      }
    });

    // Set initial positions
    nodeSelection.attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  private renderLinks(): void {
    this.gLinks
      .selectAll<SVGLineElement, Link>('line')
      .data(this.links)
      .join('line')
      .attr('class', 'usecase-link')
      .attr('stroke', '#8a2be2')
      .attr('stroke-width', 2)
      .attr('stroke-opacity', 0.4)
      .attr('x1', d => {
        const source = typeof d.source === 'string' ?
          this.nodes.find(n => n.id === d.source) : d.source;
        return source?.x ?? 0;
      })
      .attr('y1', d => {
        const source = typeof d.source === 'string' ?
          this.nodes.find(n => n.id === d.source) : d.source;
        return source?.y ?? 0;
      })
      .attr('x2', d => {
        const target = typeof d.target === 'string' ?
          this.nodes.find(n => n.id === d.target) : d.target;
        return target?.x ?? 0;
      })
      .attr('y2', d => {
        const target = typeof d.target === 'string' ?
          this.nodes.find(n => n.id === d.target) : d.target;
        return target?.y ?? 0;
      });
  }

  private onTick(): void {
    this.gNodes
      .selectAll<SVGGElement, GraphNode>('g')
      .attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }
}
