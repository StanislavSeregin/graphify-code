import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy, ApplicationRef, createComponent, EnvironmentInjector } from '@angular/core';
import { GraphService, FullGraph, ServiceData, DisplayMode } from './graph.service';
import { ServiceCardComponent } from './components/service-card.component';
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
  imports: [],
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

  private readonly CARD_WIDTH = 400;
  private readonly CARD_HEIGHT = 300;

  constructor(
    private graphService: GraphService,
    private appRef: ApplicationRef,
    private injector: EnvironmentInjector
  ) {}

  ngOnInit(): void {
    this.graphService.init();
  }

  ngAfterViewInit(): void {
    this.initSvg();
    this.setupZoom();

    this.graphService.graphData$
      .pipe(takeUntil(this.destroy$))
      .subscribe(data => {
        if (data) {
          this.renderGraph(data);
        }
      });
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
      .scaleExtent([0.1, 4])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        gMain.attr('transform', event.transform.toString());
        graphService.updateZoom(event.transform);
      });

    svg.call(this.zoom);
  }

  private renderGraph(data: FullGraph): void {
    this.nodes = this.createNodes(data);
    const links = this.createLinks(data, this.nodes);

    this.setupSimulation(this.nodes, links);
    this.renderLinks(links);
    this.renderNodes(this.nodes);
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

    this.simulation = d3.forceSimulation<GraphNode, GraphLink>(nodes)
      .force('link', d3.forceLink<GraphNode, GraphLink>(links)
        .id(d => d.id)
        .distance(500)       // Increase distance for linked nodes
        .strength(0.5))      // Reduce link strength for more flexibility
      .force('charge', d3.forceManyBody()
        .strength(-500))     // Reduce repulsion to allow unlinked nodes closer
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide<GraphNode>()
        .radius(Math.max(this.CARD_WIDTH, this.CARD_HEIGHT) / 2 + 50)  // Increase padding
        .strength(0.8));     // Strong collision to prevent overlap
  }

  private renderLinks(links: GraphLink[]): void {
    const linkSelection = this.gLinks
      .selectAll<SVGLineElement, GraphLink>('line')
      .data(links)
      .join('line')
      .attr('stroke', '#666')
      .attr('stroke-opacity', 0.6)
      .attr('stroke-width', 2);

    this.simulation.on('tick', () => {
      linkSelection
        .attr('x1', d => (d.source as GraphNode).x ?? 0)
        .attr('y1', d => (d.source as GraphNode).y ?? 0)
        .attr('x2', d => (d.target as GraphNode).x ?? 0)
        .attr('y2', d => (d.target as GraphNode).y ?? 0);
    });
  }

  private renderNodes(nodes: GraphNode[]): void {
    const nodeSelection = this.gNodes
      .selectAll<SVGGElement, GraphNode>('g')
      .data(nodes, d => d.id)
      .join('g')
      .attr('class', 'node');

    // Render Angular components via foreignObject
    nodeSelection.each((d, i, groups) => {
      const group = d3.select(groups[i]);

      // Create foreignObject for HTML insertion
      const foreignObject = group.append('foreignObject')
        .attr('width', this.CARD_WIDTH)
        .attr('height', this.CARD_HEIGHT)
        .attr('x', -this.CARD_WIDTH / 2)
        .attr('y', -this.CARD_HEIGHT / 2);

      // Create Angular component dynamically
      const componentRef = createComponent(ServiceCardComponent, {
        environmentInjector: this.injector
      });

      // Save component reference for cleanup
      d.componentRef = componentRef;

      // Set input data
      componentRef.setInput('serviceData', d.serviceData);

      // Attach component to ApplicationRef for change detection
      this.appRef.attachView(componentRef.hostView);

      // Insert component DOM element into foreignObject
      const domElement = componentRef.location.nativeElement;
      foreignObject.node()?.appendChild(domElement);
    });

    this.simulation.on('tick', () => {
      nodeSelection.attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
    });
  }
}
