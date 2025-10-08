import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy, ApplicationRef, createComponent, EnvironmentInjector } from '@angular/core';
import { GraphService, FullGraph, ServiceData, DisplayMode, UseCase, UseCaseStep } from './graph.service';
import { SidebarService } from './services/sidebar.service';
import { ServiceCardComponent } from './components/service-card.component';
import { EndpointSidebarComponent, EndpointSidebarData } from './components/endpoint-sidebar.component';
import { UseCaseSidebarComponent, UseCaseSidebarData } from './components/usecase-sidebar.component';
import { MatSidenavModule } from '@angular/material/sidenav';
import * as d3 from 'd3';
import { Subject, takeUntil } from 'rxjs';

interface GraphNode extends d3.SimulationNodeDatum {
  id: string;
  serviceData: ServiceData;
  componentRef?: any;
}

interface GraphLink extends d3.SimulationLinkDatum<GraphNode> {
  source: GraphNode;
  target: GraphNode;
}

@Component({
  selector: 'app-graph',
  standalone: true,
  imports: [MatSidenavModule, EndpointSidebarComponent, UseCaseSidebarComponent],
  templateUrl: './graph.component.html',
  styleUrl: './graph.component.css'
})
export class GraphComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('graphSvg', { static: false }) svgElement!: ElementRef<SVGSVGElement>;

  private destroy$ = new Subject<void>();
  private svg!: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private gMain!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private gLinks!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private gNodes!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private simulation!: d3.Simulation<GraphNode, GraphLink>;
  private zoom!: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private nodes: GraphNode[] = [];
  private fullGraphData: FullGraph | null = null;
  private linkSelection!: d3.Selection<SVGPathElement, GraphLink, SVGGElement, unknown>;
  private nodeSelection!: d3.Selection<SVGGElement, GraphNode, SVGGElement, unknown>;

  endpointSidebarOpen = false;
  endpointSidebarData: EndpointSidebarData | null = null;
  useCaseSidebarOpen = false;
  useCaseSidebarData: UseCaseSidebarData | null = null;
  private currentZoomScale = 1;
  private nestedGraphWasReset = false;

  private getCardWidth(): number {
    const vw = window.innerWidth;
    return Math.min(Math.max(500, vw * 0.30), 700);
  }

  private getCardHeight(): number {
    const vh = window.innerHeight;
    return Math.min(Math.max(400, vh * 0.25), 500);
  }

  constructor(
    private graphService: GraphService,
    private sidebarService: SidebarService,
    private appRef: ApplicationRef,
    private injector: EnvironmentInjector
  ) {}

  ngOnInit(): void {
    this.graphService.init();

    // Subscribe to sidebar state
    this.sidebarService.endpointSidebarOpen$
      .pipe(takeUntil(this.destroy$))
      .subscribe(open => {
        this.endpointSidebarOpen = open;
      });

    this.sidebarService.endpointSidebarData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        this.endpointSidebarData = data;
      });

    this.sidebarService.useCaseSidebarOpen$
      .pipe(takeUntil(this.destroy$))
      .subscribe(open => {
        this.useCaseSidebarOpen = open;
      });

    this.sidebarService.useCaseSidebarData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        this.useCaseSidebarData = data;
      });

    // Subscribe to zoom scale to track current zoom level
    this.graphService.zoomScale$
      .pipe(takeUntil(this.destroy$))
      .subscribe(scale => {
        this.currentZoomScale = scale;
      });
  }

  ngAfterViewInit(): void {
    this.initSvg();
    this.setupZoom();

    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        if (data) {
          this.fullGraphData = data;
          this.renderGraph(data);
        }
      });

    // Handle window resize
    window.addEventListener('resize', () => this.onWindowResize());

    // Handle Esc key to reset to initial view
    window.addEventListener('keydown', (event) => this.onKeyDown(event));
  }

  private onWindowResize(): void {
    if (this.simulation) {
      const width = this.svgElement.nativeElement.clientWidth;
      const height = this.svgElement.nativeElement.clientHeight;
      this.simulation.force('center', d3.forceCenter(width / 2, height / 2));
      this.simulation.force('collision', d3.forceCollide<GraphNode>()
        .radius(Math.max(this.getCardWidth(), this.getCardHeight()) / 2 + 150)
        .strength(1));
      this.simulation.alpha(0.3).restart();
    }
  }

  private onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      // If we're zoomed in on a service card (scale >= 1.0)
      if (this.currentZoomScale >= 1.0) {
        // Check if any nested graph was changed
        const anyNestedGraphChanged = this.nodes.some(node =>
          node.componentRef &&
          node.componentRef.instance.nestedGraph &&
          node.componentRef.instance.nestedGraph.isZoomChanged()
        );

        // If nested graph wasn't touched, skip to main graph reset
        if (!anyNestedGraphChanged && !this.nestedGraphWasReset) {
          this.resetToInitialView();
          this.nestedGraphWasReset = false;
          return;
        }

        // First Esc: reset nested graph
        if (!this.nestedGraphWasReset) {
          this.resetNestedGraphs();
          this.nestedGraphWasReset = true;
        } else {
          // Second Esc: zoom out to overview
          this.resetToInitialView();
          this.nestedGraphWasReset = false;
        }
      } else {
        // Already at overview level, just reset position/zoom
        this.resetToInitialView();
        this.nestedGraphWasReset = false;
      }
    }
  }

  private resetNestedGraphs(): void {
    // Find all service cards and reset their nested graphs
    this.nodes.forEach(node => {
      if (node.componentRef && node.componentRef.instance.nestedGraph) {
        node.componentRef.instance.nestedGraph.resetToDefault();
      }
    });
  }

  private resetToInitialView(): void {
    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;

    // Calculate initial zoom (same as on page load)
    const initialScale = Math.min(window.innerWidth, window.innerHeight) / 5000;
    const clampedScale = Math.max(0.1, Math.min(0.3, initialScale));

    // Center on the graph center
    const centerX = width / 2;
    const centerY = height / 2;
    const translateX = width / 2 - clampedScale * centerX;
    const translateY = height / 2 - clampedScale * centerY;

    const initialTransform = d3.zoomIdentity
      .translate(translateX, translateY)
      .scale(clampedScale);

    this.svg
      .transition()
      .duration(750)
      .call(this.zoom.transform, initialTransform);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.simulation) {
      this.simulation.stop();
    }
    // Clean up all Angular components
    this.nodes.forEach(node => {
      if (node.componentRef) {
        this.appRef.detachView(node.componentRef.hostView);
        node.componentRef.destroy();
      }
    });
    // Remove event listeners
    window.removeEventListener('resize', () => this.onWindowResize());
    window.removeEventListener('keydown', (event) => this.onKeyDown(event));
  }

  private initSvg(): void {
    this.svg = d3.select(this.svgElement.nativeElement);

    // Main group for zoom/pan
    this.gMain = this.svg.append('g').attr('class', 'main-group');

    // Links group (drawn first to be under nodes)
    this.gLinks = this.gMain.append('g').attr('class', 'links');

    // Nodes group
    this.gNodes = this.gMain.append('g').attr('class', 'nodes');
  }

  private setupZoom(): void {
    const svg = this.svg;
    const gMain = this.gMain;
    const graphService = this.graphService;

    this.zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 10])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        gMain.attr('transform', event.transform.toString());
        graphService.updateZoom(event.transform);
      });

    svg.call(this.zoom);

    // Initial transform will be set after nodes are positioned
    // This is done in renderGraph() to ensure proper centering
  }

  private renderGraph(data: FullGraph): void {
    this.nodes = this.createNodes(data);
    const links = this.createLinks(data, this.nodes);

    // Set initial positions near center to avoid clustering in corner
    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;
    this.nodes.forEach((node, i) => {
      const angle = (i / this.nodes.length) * 2 * Math.PI;
      const radius = Math.min(width, height) / 4;
      node.x = width / 2 + radius * Math.cos(angle);
      node.y = height / 2 + radius * Math.sin(angle);
    });

    this.renderLinks(links);
    this.renderNodes(this.nodes);
    this.setupSimulation(this.nodes, links);

    // Set initial zoom level after nodes are positioned
    const initialScale = Math.min(window.innerWidth, window.innerHeight) / 5000;
    const clampedScale = Math.max(0.1, Math.min(0.3, initialScale));

    // Center the view on the graph center
    // The nodes are positioned around (width/2, height/2), so we need to translate accordingly
    const centerX = width / 2;
    const centerY = height / 2;
    const translateX = width / 2 - clampedScale * centerX;
    const translateY = height / 2 - clampedScale * centerY;

    const initialTransform = d3.zoomIdentity
      .translate(translateX, translateY)
      .scale(clampedScale);

    this.svg.call(this.zoom.transform, initialTransform);
  }

  private createNodes(data: FullGraph): GraphNode[] {
    return data.services.map(serviceData => ({
      id: serviceData.service.id,
      serviceData: serviceData
    }));
  }

  private createLinks(data: FullGraph, nodes: GraphNode[]): GraphLink[] {
    const links: GraphLink[] = [];
    const nodeMap = new Map(nodes.map(n => [n.id, n]));

    // Create index: endpointId -> serviceId
    const endpointToService = new Map<string, string>();
    data.services.forEach(serviceData => {
      serviceData.endpoint.forEach(endpoint => {
        endpointToService.set(endpoint.id, serviceData.service.id);
      });
    });

    // Create service-to-service links based on Relations
    data.services.forEach(serviceData => {
      const sourceNode = nodeMap.get(serviceData.service.id);
      if (!sourceNode) return;

      serviceData.relations.targetEndpointIds.forEach(targetEndpointId => {
        const targetServiceId = endpointToService.get(targetEndpointId);
        if (targetServiceId) {
          const targetNode = nodeMap.get(targetServiceId);
          if (targetNode && targetNode !== sourceNode) {
            links.push({
              source: sourceNode,
              target: targetNode
            });
          }
        }
      });
    });

    return links;
  }

  private setupSimulation(nodes: GraphNode[], links: GraphLink[]): void {
    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;

    // Calculate forces based on viewport
    const avgDimension = (width + height) / 2;
    const linkDistance = Math.max(400, avgDimension * 0.3);
    const chargeStrength = -Math.max(800, avgDimension * 0.4);
    const collisionPadding = Math.max(50, avgDimension * 0.05);

    this.simulation = d3.forceSimulation<GraphNode, GraphLink>(nodes)
      .force('link', d3.forceLink<GraphNode, GraphLink>(links)
        .id(d => d.id)
        .distance(linkDistance)
        .strength(0.5))
      .force('charge', d3.forceManyBody()
        .strength(chargeStrength))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide<GraphNode>()
        .radius(Math.max(this.getCardWidth(), this.getCardHeight()) / 2 + collisionPadding)
        .strength(1))
      .on('tick', () => this.onTick());
  }

  private onTick(): void {
    // Update links
    if (this.linkSelection) {
      this.linkSelection.attr('d', d => {
        const source = d.source as GraphNode;
        const target = d.target as GraphNode;

        const sx = source.x ?? 0;
        const sy = source.y ?? 0;
        const tx = target.x ?? 0;
        const ty = target.y ?? 0;

        return `M ${sx},${sy} L ${tx},${ty}`;
      });
    }

    // Update nodes
    if (this.nodeSelection) {
      this.nodeSelection.attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
    }
  }

  private focusOnNode(node: GraphNode): void {
    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;

    const x = node.x ?? 0;
    const y = node.y ?? 0;

    // Calculate transform to center the node and zoom to full mode (2.5)
    const scale = 2.5;
    const translateX = width / 2 - scale * x;
    const translateY = height / 2 - scale * y;

    const transform = d3.zoomIdentity
      .translate(translateX, translateY)
      .scale(scale);

    // Reset the Esc counter and nested graph zoom flag when focusing on a new node
    this.nestedGraphWasReset = false;
    if (node.componentRef && node.componentRef.instance.nestedGraph) {
      node.componentRef.instance.nestedGraph.resetZoomChangedFlag();
    }

    this.svg
      .transition()
      .duration(750)
      .call(this.zoom.transform, transform);
  }

  private renderLinks(links: GraphLink[]): void {
    this.linkSelection = this.gLinks
      .selectAll<SVGPathElement, GraphLink>('path')
      .data(links)
      .join('path')
      .attr('stroke', '#B0B0B0')
      .attr('stroke-opacity', 0.6)
      .attr('stroke-width', 6)
      .attr('fill', 'none')
      .attr('pointer-events', 'none')
      .attr('class', 'link')
      .attr('marker-end', 'url(#arrow)');
  }

  private renderNodes(nodes: GraphNode[]): void {
    this.nodeSelection = this.gNodes
      .selectAll<SVGGElement, GraphNode>('g')
      .data(nodes, d => d.id)
      .join('g')
      .attr('class', 'node')
      .attr('filter', 'url(#card-shadow)');

    // Add hover effect for shadow
    this.nodeSelection
      .on('mouseenter', function() {
        d3.select(this).attr('filter', 'url(#card-shadow-hover)');
      })
      .on('mouseleave', function() {
        d3.select(this).attr('filter', 'url(#card-shadow)');
      });

    // Render Angular components via foreignObject
    this.nodeSelection.each((d, i, groups) => {
      const group = d3.select(groups[i]);

      // Create foreignObject for HTML insertion
      const cardWidth = this.getCardWidth();
      const cardHeight = this.getCardHeight();
      const foreignObject = group.append('foreignObject')
        .attr('width', cardWidth)
        .attr('height', cardHeight)
        .attr('x', -cardWidth / 2)
        .attr('y', -cardHeight / 2);

      // Create Angular component dynamically
      const componentRef = createComponent(ServiceCardComponent, {
        environmentInjector: this.injector
      });

      // Save component reference for cleanup
      d.componentRef = componentRef;

      // Set input data
      componentRef.setInput('serviceData', d.serviceData);

      // Subscribe to focus event
      componentRef.instance.focusRequested.subscribe(() => {
        this.focusOnNode(d);
      });

      // Subscribe to endpoint click event
      componentRef.instance.endpointClick.subscribe((data: {endpoint: any, service: ServiceData}) => {
        this.onEndpointClick(data.endpoint, data.service);
      });

      // Subscribe to use case click event
      componentRef.instance.useCaseClick.subscribe((data: {useCase: UseCase, service: ServiceData}) => {
        this.onUseCaseClick(data.useCase, data.service);
      });

      // Attach component to ApplicationRef for change detection
      this.appRef.attachView(componentRef.hostView);

      // Insert component DOM element into foreignObject
      const domElement = componentRef.location.nativeElement;
      foreignObject.node()?.appendChild(domElement);
    });
  }

  // Sidebar methods
  closeEndpointSidebar(): void {
    this.sidebarService.closeEndpointSidebar();
  }

  closeUseCaseSidebar(): void {
    this.sidebarService.closeUseCaseSidebar();
  }

  onEndpointClick(endpoint: any, service: ServiceData): void {
    if (!this.fullGraphData) return;

    // Find related services (services that call this endpoint)
    const relatedServices = this.fullGraphData.services.filter(s =>
      s.relations.targetEndpointIds.includes(endpoint.id)
    );

    // Find use cases where this endpoint is involved
    const useCases = service.useCases.filter(uc =>
      uc.initiatingEndpointId === endpoint.id ||
      uc.steps.some(step => step.endpointId === endpoint.id)
    );

    // Open sidebar with data
    this.sidebarService.openEndpointSidebar({
      endpoint,
      service,
      relatedServices,
      useCases
    });
  }

  onUseCaseClick(useCase: UseCase, service: ServiceData): void {
    // Open use case sidebar with data
    this.sidebarService.openUseCaseSidebar({
      useCase,
      service
    });
  }

  onSidebarServiceClick(serviceId: string): void {
    // Find and focus on the service with the same zoom level as clicking on service card
    const node = this.nodes.find(n => n.id === serviceId);
    if (node) {
      this.focusOnNode(node);
    }
  }

  onSidebarUseCaseClick(useCaseId: string): void {
    // Find the use case and its service, then open use case sidebar
    if (!this.fullGraphData) return;

    for (const serviceData of this.fullGraphData.services) {
      const useCase = serviceData.useCases.find(uc => uc.id === useCaseId);
      if (useCase) {
        this.onUseCaseClick(useCase, serviceData);
        // Also focus on the service containing this use case
        const node = this.nodes.find(n => n.id === serviceData.service.id);
        if (node) {
          this.focusOnNode(node);
        }
        break;
      }
    }
  }

  onUseCaseStepClick(event: {step: UseCaseStep, index: number}): void {
    const { step } = event;

    // Priority: endpointId > serviceId
    if (step.endpointId && this.fullGraphData) {
      // Find the endpoint and its service
      for (const serviceData of this.fullGraphData.services) {
        const endpoint = serviceData.endpoint.find(ep => ep.id === step.endpointId);
        if (endpoint) {
          // Focus on the service containing this endpoint
          const node = this.nodes.find(n => n.id === serviceData.service.id);
          if (node) {
            this.focusOnNode(node);
          }
          // Optionally open endpoint sidebar
          // this.onEndpointClick(endpoint, serviceData);
          break;
        }
      }
    } else if (step.serviceId) {
      // Focus on the service
      const node = this.nodes.find(n => n.id === step.serviceId);
      if (node) {
        this.focusOnNode(node);
      }
    }
  }
}
