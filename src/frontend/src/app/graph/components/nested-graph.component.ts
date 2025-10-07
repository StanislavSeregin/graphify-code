import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit, ApplicationRef, createComponent, EnvironmentInjector } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EndpointCardComponent } from './endpoint-card.component';
import { Endpoint, DisplayMode } from '../graph.service';
import * as d3 from 'd3';
import { BehaviorSubject } from 'rxjs';

interface EndpointNode extends d3.SimulationNodeDatum {
  id: string;
  endpoint: Endpoint;
  componentRef?: any;
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
  @Output() endpointClick = new EventEmitter<Endpoint>();

  private svg!: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private gMain!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private gNodes!: d3.Selection<SVGGElement, unknown, null, undefined>;
  private simulation!: d3.Simulation<EndpointNode, undefined>;
  private nodes: EndpointNode[] = [];
  private zoom!: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private localDisplayMode$ = new BehaviorSubject<DisplayMode>('compact');
  private currentScale = 0.2;

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
    private injector: EnvironmentInjector
  ) {}

  ngOnInit(): void {}

  ngAfterViewInit(): void {
    this.initSvg();
    this.setupZoom();
    this.renderEndpoints();

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
    this.gNodes = this.gMain.append('g').attr('class', 'nested-nodes');
  }

  private setupZoom(): void {
    this.zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.05, 2])
      .on('zoom', (event: d3.D3ZoomEvent<SVGSVGElement, unknown>) => {
        this.gMain.attr('transform', event.transform.toString());
        this.currentScale = event.transform.k;

        // Update local display mode based on zoom scale
        const newMode: DisplayMode = this.currentScale >= this.DISPLAY_MODE_THRESHOLD ? 'full' : 'compact';
        if (this.localDisplayMode$.value !== newMode) {
          this.localDisplayMode$.next(newMode);
        }
      });

    this.svg.call(this.zoom);

    // Set initial zoom level based on viewport - reduced for wider cards
    const baseScale = Math.min(window.innerWidth, window.innerHeight) / 15000;
    const initialScale = Math.max(0.025, Math.min(0.08, baseScale));
    const initialTransform = d3.zoomIdentity.scale(initialScale);
    this.svg.call(this.zoom.transform, initialTransform);
  }

  private renderEndpoints(): void {
    if (!this.endpoints || this.endpoints.length === 0) {
      return;
    }

    this.nodes = this.endpoints.map(endpoint => ({
      id: endpoint.id,
      endpoint: endpoint
    }));

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

    console.log('Nested graph setup:', {
      width, height, centerX, centerY,
      cardWidth: this.getCardWidth(),
      cardHeight: this.getCardHeight(),
      spacing,
      nodeCount: this.nodes.length
    });

    // Render nodes
    const nodeSelection = this.gNodes
      .selectAll<SVGGElement, EndpointNode>('g')
      .data(this.nodes, d => d.id)
      .join('g')
      .attr('class', 'endpoint-node');

    nodeSelection.each((d, i, groups) => {
      const group = d3.select(groups[i]);

      const cardWidth = this.getCardWidth();
      const cardHeight = this.getCardHeight();
      const foreignObject = group.append('foreignObject')
        .attr('width', cardWidth)
        .attr('height', cardHeight)
        .attr('x', -cardWidth / 2)
        .attr('y', -cardHeight / 2);

      const componentRef = createComponent(EndpointCardComponent, {
        environmentInjector: this.injector
      });

      d.componentRef = componentRef;
      componentRef.setInput('endpoint', d.endpoint);
      componentRef.setInput('displayMode$', this.localDisplayMode$.asObservable());

      // Subscribe to focus event - emit endpoint click for sidebar AND focus
      componentRef.instance.focusRequested.subscribe(() => {
        this.endpointClick.emit(d.endpoint);
        this.focusOnNode(d);
      });

      this.appRef.attachView(componentRef.hostView);

      const domElement = componentRef.location.nativeElement;
      foreignObject.node()?.appendChild(domElement);
    });

    // Set initial positions
    nodeSelection.attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  private onTick(): void {
    this.gNodes
      .selectAll<SVGGElement, EndpointNode>('g')
      .attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
  }

  private focusOnNode(node: EndpointNode): void {
    const width = this.svgElement.nativeElement.clientWidth;
    const height = this.svgElement.nativeElement.clientHeight;

    const x = node.x ?? 0;
    const y = node.y ?? 0;

    // Calculate transform to center the node and zoom to full mode
    // Use a scale higher than DISPLAY_MODE_THRESHOLD (0.1) to trigger full mode
    const scale = 0.18;
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
}
