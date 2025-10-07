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
  private linkSelection!: d3.Selection<SVGPathElement, GraphLink, SVGGElement, unknown>;
  private nodeSelection!: d3.Selection<SVGGElement, GraphNode, SVGGElement, unknown>;

  private readonly CARD_WIDTH = 700;
  private readonly CARD_HEIGHT = 500;

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
      .scaleExtent([0.1, 10])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        gMain.attr('transform', event.transform.toString());
        graphService.updateZoom(event.transform);
      });

    svg.call(this.zoom);

    // Set initial zoom level
    const initialTransform = d3.zoomIdentity.scale(0.4);
    svg.call(this.zoom.transform, initialTransform);
  }

  private renderGraph(data: FullGraph): void {
    this.nodes = this.createNodes(data);
    const links = this.createLinks(data, this.nodes);

    this.renderLinks(links);
    this.renderNodes(this.nodes);
    this.setupSimulation(this.nodes, links);
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
        .distance(900)
        .strength(0.5))
      .force('charge', d3.forceManyBody()
        .strength(-1200))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide<GraphNode>()
        .radius(Math.max(this.CARD_WIDTH, this.CARD_HEIGHT) / 2 + 150)
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

      // Subscribe to focus event
      componentRef.instance.focusRequested.subscribe(() => {
        this.focusOnNode(d);
      });

      // Attach component to ApplicationRef for change detection
      this.appRef.attachView(componentRef.hostView);

      // Insert component DOM element into foreignObject
      const domElement = componentRef.location.nativeElement;
      foreignObject.node()?.appendChild(domElement);
    });
  }
}
