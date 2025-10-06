import { Component, ElementRef, OnInit, OnDestroy, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import * as d3 from 'd3';
import { GraphService, GraphData, ServiceFullData, Endpoint } from './graph.service';

interface GraphNode {
  id: string;
  type: 'service' | 'endpoint';
  name: string;
  description: string;
  serviceId?: string; // for endpoints
  data: ServiceFullData | Endpoint;
  x?: number;
  y?: number;
  fx?: number | null;
  fy?: number | null;
}

interface GraphLink {
  source: string | GraphNode;
  target: string | GraphNode;
  type: 'service-relation' | 'endpoint-relation';
}

@Component({
  selector: 'app-graph-canvas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './graph-canvas.component.html',
  styleUrl: './graph-canvas.component.css'
})
export class GraphCanvasComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('svgCanvas', { static: false }) svgCanvas!: ElementRef<SVGSVGElement>;

  private graphData: GraphData = new Map();
  public isBusy = false;
  private subscriptions = new Subscription();

  private selectedServiceId: string | null = null;
  private zoomLevel = 1;

  private svg: any;
  private g: any;
  private simulation: any;

  constructor(
    private graphService: GraphService,
    private elementRef: ElementRef
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.graphService.graphData$.subscribe(data => {
        this.graphData = data;
        this.renderGraph();
      })
    );

    this.subscriptions.add(
      this.graphService.isBusy$.subscribe(busy => {
        this.isBusy = busy;
      })
    );
  }

  ngAfterViewInit(): void {
    this.initSvg();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.simulation) {
      this.simulation.stop();
    }
  }

  private initSvg(): void {
    const element = this.svgCanvas.nativeElement;
    const width = element.clientWidth;
    const height = element.clientHeight;

    this.svg = d3.select(element)
      .attr('width', width)
      .attr('height', height);

    // Create main group for zoom/pan
    this.g = this.svg.append('g');

    // Setup zoom behavior
    const zoom = d3.zoom()
      .scaleExtent([0.1, 4])
      .filter((event: any) => {
        // Don't pan if clicking on a node
        if (event.target.tagName === 'circle' || event.target.tagName === 'text') {
          return event.type === 'wheel';
        }
        // Allow zoom with mouse wheel
        if (event.type === 'wheel') return true;
        // Allow pan with left mouse button (button === 0) or middle mouse button (button === 1)
        if (event.type === 'mousedown') return event.button === 0 || event.button === 1;
        if (event.type === 'mousemove' || event.type === 'mouseup') {
          return (event.buttons === 1) || (event.buttons === 4);
        }
        return false;
      })
      .on('zoom', (event) => {
        this.g.attr('transform', event.transform);
        this.zoomLevel = event.transform.k;
      });

    this.svg.call(zoom);
  }

  private renderGraph(): void {
    if (!this.svg || this.graphData.size === 0) {
      return;
    }

    // Clear existing elements
    this.g.selectAll('*').remove();

    const { nodes, links } = this.prepareGraphData();

    // Create force simulation
    const width = this.svgCanvas.nativeElement.clientWidth;
    const height = this.svgCanvas.nativeElement.clientHeight;

    this.simulation = d3.forceSimulation(nodes)
      .force('link', d3.forceLink(links)
        .id((d: any) => d.id)
        .distance(250)
        .strength(1)
      )
      .force('charge', d3.forceManyBody()
        .strength(-800)
        .distanceMax(500)
      )
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide()
        .radius(90)
        .strength(0.8)
      )
      .force('x', d3.forceX(width / 2).strength(0.05))
      .force('y', d3.forceY(height / 2).strength(0.05))
      .alphaDecay(0.02)
      .velocityDecay(0.3);

    // Create arrow marker (define before using)
    this.svg.select('defs').remove(); // Remove old defs if exist
    this.svg.append('defs').append('marker')
      .attr('id', 'arrowhead')
      .attr('viewBox', '0 0 10 10')
      .attr('refX', 10)
      .attr('refY', 5)
      .attr('orient', 'auto')
      .attr('markerWidth', 8)
      .attr('markerHeight', 8)
      .append('svg:path')
      .attr('d', 'M 0,0 L 0,10 L 10,5 z')
      .attr('fill', '#333');

    // Create links (arrows)
    const link = this.g.append('g')
      .selectAll('line')
      .data(links)
      .join('line')
      .attr('class', 'link')
      .attr('stroke', '#666')
      .attr('stroke-opacity', 0.5)
      .attr('stroke-width', 2)
      .attr('marker-end', 'url(#arrowhead)');

    // Create nodes
    const node = this.g.append('g')
      .selectAll('g')
      .data(nodes)
      .join('g')
      .attr('class', 'node');

    // Add circles for nodes
    node.append('circle')
      .attr('r', 60)
      .attr('fill', (d: GraphNode) => d.type === 'service' ? '#4A90E2' : '#50C878')
      .attr('stroke', '#fff')
      .attr('stroke-width', 3)
      .on('click', (event: any, d: GraphNode) => this.onNodeClick(d));

    // Add labels
    node.append('text')
      .text((d: GraphNode) => d.name)
      .attr('text-anchor', 'middle')
      .attr('dy', 85)
      .attr('font-size', '24px')
      .attr('fill', '#333');

    // Add tooltips
    node.append('title')
      .text((d: GraphNode) => `${d.name}\n${d.description}`);

    // Update positions on simulation tick
    this.simulation.on('tick', () => {
      link.attr('x1', (d: any) => {
        const dx = d.target.x - d.source.x;
        const dy = d.target.y - d.source.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const nodeRadius = 60;
        return d.source.x + (dx / distance) * nodeRadius;
      })
      .attr('y1', (d: any) => {
        const dx = d.target.x - d.source.x;
        const dy = d.target.y - d.source.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const nodeRadius = 60;
        return d.source.y + (dy / distance) * nodeRadius;
      })
      .attr('x2', (d: any) => {
        const dx = d.target.x - d.source.x;
        const dy = d.target.y - d.source.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const nodeRadius = 60;
        return d.target.x - (dx / distance) * nodeRadius;
      })
      .attr('y2', (d: any) => {
        const dx = d.target.x - d.source.x;
        const dy = d.target.y - d.source.y;
        const distance = Math.sqrt(dx * dx + dy * dy);
        const nodeRadius = 60;
        return d.target.y - (dy / distance) * nodeRadius;
      });

      node.attr('transform', (d: any) => `translate(${d.x},${d.y})`);
    });

    // Fix positions after initial simulation stabilizes
    let initialStabilized = false;
    this.simulation.on('end', () => {
      if (!initialStabilized) {
        nodes.forEach(n => {
          n.fx = n.x;
          n.fy = n.y;
        });
        initialStabilized = true;
      }
    });
  }

  private prepareGraphData(): { nodes: GraphNode[], links: GraphLink[] } {
    const nodes: GraphNode[] = [];
    const links: GraphLink[] = [];

    // Create service nodes
    this.graphData.forEach((serviceData, serviceId) => {
      nodes.push({
        id: serviceId,
        type: 'service',
        name: serviceData.service.name,
        description: serviceData.service.description,
        data: serviceData
      });

      // Create links based on relations
      serviceData.relations.targetEndpointIds.forEach(targetEndpointId => {
        // Find which service contains this endpoint
        this.graphData.forEach((otherServiceData, otherServiceId) => {
          if (otherServiceData.endpoints.some(ep => ep.id === targetEndpointId)) {
            links.push({
              source: serviceId,
              target: otherServiceId,
              type: 'service-relation'
            });
          }
        });
      });
    });

    return { nodes, links };
  }

  private onNodeClick(node: GraphNode): void {
    if (node.type === 'service') {
      this.selectedServiceId = this.selectedServiceId === node.id ? null : node.id;
      this.focusOnService(node.id, node);
    }
  }

  private focusOnService(serviceId: string, node: GraphNode): void {
    const width = this.svgCanvas.nativeElement.clientWidth;
    const height = this.svgCanvas.nativeElement.clientHeight;

    // Unfreeze all nodes before reorganization
    this.simulation.nodes().forEach((n: any) => {
      n.fx = null;
      n.fy = null;
    });

    if (this.selectedServiceId) {
      // Reconfigure simulation to center selected node
      this.simulation
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('radial', d3.forceRadial(0, width / 2, height / 2).strength((d: any) => {
          return d.id === serviceId ? 1 : 0;
        }));

      // Restart simulation to reorganize nodes
      this.simulation.alpha(0.3).restart();

      // Re-freeze positions after simulation settles
      setTimeout(() => {
        this.simulation.nodes().forEach((n: any) => {
          n.fx = n.x;
          n.fy = n.y;
        });
      }, 2000);
    } else {
      // Reset to default layout
      this.simulation
        .force('center', d3.forceCenter(width / 2, height / 2))
        .force('radial', null);

      this.simulation.alpha(0.3).restart();

      // Re-freeze positions after simulation settles
      setTimeout(() => {
        this.simulation.nodes().forEach((n: any) => {
          n.fx = n.x;
          n.fy = n.y;
        });
      }, 2000);
    }

    // Fade out unrelated nodes
    this.g.selectAll('.node circle')
      .transition()
      .duration(300)
      .attr('opacity', (d: GraphNode) => {
        if (!this.selectedServiceId) return 1;
        return this.isRelatedToService(d, serviceId) ? 1 : 0.2;
      });

    this.g.selectAll('.link')
      .transition()
      .duration(300)
      .attr('opacity', (d: GraphLink) => {
        if (!this.selectedServiceId) return 0.5;
        const sourceId = typeof d.source === 'string' ? d.source : d.source.id;
        const targetId = typeof d.target === 'string' ? d.target : d.target.id;
        return sourceId === serviceId || targetId === serviceId ? 0.5 : 0.1;
      });
  }

  private isRelatedToService(node: GraphNode, serviceId: string): boolean {
    if (node.id === serviceId) return true;

    const serviceData = this.graphData.get(serviceId);
    if (!serviceData) return false;

    // Check if this node is connected to the selected service
    const relatedServiceIds = new Set<string>();
    serviceData.relations.targetEndpointIds.forEach(targetEndpointId => {
      this.graphData.forEach((otherServiceData, otherServiceId) => {
        if (otherServiceData.endpoints.some(ep => ep.id === targetEndpointId)) {
          relatedServiceIds.add(otherServiceId);
        }
      });
    });

    return relatedServiceIds.has(node.id);
  }

}
