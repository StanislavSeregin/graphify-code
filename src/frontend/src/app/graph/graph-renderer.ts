import * as d3 from 'd3';
import {
  GRAPH_FIT_PADDING,
  GRAPH_MAX_INITIAL_SCALE,
  GRAPH_MIN_READABLE_SCALE,
  SERVICE_NODE_RADIUS,
  formatLastAnalyzed
} from './graph-layout';
import { GraphDependencyEdge, GraphServiceNode, GraphViewModel } from './graph-view-model';

const SERVICE_NODE_MIN_WIDTH = 360;
const SERVICE_NODE_MAX_WIDTH = 520;
const SERVICE_NODE_HORIZONTAL_PADDING = 24;
const SERVICE_NODE_TOP_PADDING = 24;
const SERVICE_NODE_BOTTOM_PADDING = 22;
const SERVICE_TITLE_LINE_HEIGHT = 19;
const SERVICE_DESCRIPTION_LINE_HEIGHT = 16;
const SERVICE_META_HEIGHT = 16;
const SERVICE_BADGE_HEIGHT = 22;
const SERVICE_BADGE_GAP = 8;
const AVERAGE_CHAR_WIDTH = 6.7;

interface TextBlock {
  lines: string[];
  width: number;
  height: number;
}

interface RenderNode extends d3.SimulationNodeDatum, GraphServiceNode {
  layoutWidth: number;
  layoutHeight: number;
  titleText: TextBlock;
  descriptionText: TextBlock;
}

interface RenderEdge extends d3.SimulationLinkDatum<RenderNode> {
  id: string;
  sourceId: string;
  targetId: string;
  count: number;
  source: RenderNode;
  target: RenderNode;
}

export interface GraphRendererCallbacks {
  onServiceSelect(serviceId: string): void;
  onCanvasClick(): void;
  onZoom(transform: d3.ZoomTransform): void;
}

export class GraphRenderer {
  private readonly svg: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private readonly root: d3.Selection<SVGGElement, unknown, null, undefined>;
  private readonly linksLayer: d3.Selection<SVGGElement, unknown, null, undefined>;
  private readonly nodesLayer: d3.Selection<SVGGElement, unknown, null, undefined>;
  private readonly zoom: d3.ZoomBehavior<SVGSVGElement, unknown>;
  private readonly callbacks: GraphRendererCallbacks;

  private nodes: RenderNode[] = [];
  private edges: RenderEdge[] = [];
  private simulation: d3.Simulation<RenderNode, RenderEdge> | null = null;
  private selectedServiceId: string | null = null;
  private highlightedServiceIds: Set<string> | null = null;

  constructor(svgElement: SVGSVGElement, callbacks: GraphRendererCallbacks) {
    this.callbacks = callbacks;
    this.svg = d3.select(svgElement);
    this.svg.selectAll('*').remove();

    this.svg
      .attr('role', 'img')
      .attr('aria-label', 'Service dependency graph')
      .on('click', event => {
        if (event.target === svgElement) {
          this.callbacks.onCanvasClick();
        }
      });

    this.createDefs();
    this.root = this.svg.append('g').attr('class', 'graph-root');
    this.linksLayer = this.root.append('g').attr('class', 'graph-links');
    this.nodesLayer = this.root.append('g').attr('class', 'graph-nodes');

    this.zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.45, 2.2])
      .on('zoom', event => {
        this.root.attr('transform', event.transform.toString());
        this.callbacks.onZoom(event.transform);
      });

    this.svg.call(this.zoom);
  }

  render(vm: GraphViewModel, selectedServiceId: string | null): void {
    this.selectedServiceId = selectedServiceId;
    this.stopSimulation();

    this.nodes = vm.services.map((service, index) => {
      const layout = buildServiceNodeLayout(service);
      return {
        ...service,
        ...layout,
        x: serviceCountPosition(index, vm.services.length).x,
        y: serviceCountPosition(index, vm.services.length).y
      };
    });

    const nodeById = new Map(this.nodes.map(node => [node.id, node]));
    this.edges = vm.edges
      .map(edge => {
        const source = nodeById.get(edge.sourceId);
        const target = nodeById.get(edge.targetId);
        if (!source || !target) return null;
        return { ...edge, source, target };
      })
      .filter((edge): edge is RenderEdge => Boolean(edge));

    this.drawLinks();
    this.drawNodes();
    this.runLayout();
  }

  updateSelection(serviceId: string | null): void {
    this.selectedServiceId = serviceId;
    this.highlightedServiceIds = null;
    this.nodesLayer
      .selectAll<SVGGElement, RenderNode>('g.service-node')
      .classed('selected', node => node.id === serviceId)
      .classed('flow-highlight', false)
      .classed('dimmed', node => Boolean(serviceId) && node.id !== serviceId);
  }

  highlightServices(serviceIds: Set<string>): void {
    this.highlightedServiceIds = serviceIds;
    this.nodesLayer
      .selectAll<SVGGElement, RenderNode>('g.service-node')
      .classed('selected', node => node.id === this.selectedServiceId)
      .classed('flow-highlight', node => serviceIds.has(node.id))
      .classed('dimmed', node => !serviceIds.has(node.id));
  }

  focusService(serviceId: string): void {
    const node = this.nodes.find(n => n.id === serviceId);
    if (!node) return;

    const { width, height } = this.viewport();
    const scale = 1.08;
    const transform = d3.zoomIdentity
      .translate(width / 2 - scale * (node.x ?? 0), height / 2 - scale * (node.y ?? 0))
      .scale(scale);

    this.svg.transition().duration(450).call(this.zoom.transform, transform);
  }

  resetView(): void {
    this.fitToView(450);
  }

  resize(): void {
    if (!this.nodes.length) return;
    this.fitToView(0);
  }

  destroy(): void {
    this.stopSimulation();
    this.svg.on('.zoom', null);
    this.svg.selectAll('*').remove();
  }

  private createDefs(): void {
    const defs = this.svg.append('defs');

    defs.append('marker')
      .attr('id', 'service-arrow')
      .attr('viewBox', '0 -5 10 10')
      .attr('refX', 10)
      .attr('refY', 0)
      .attr('markerWidth', 7)
      .attr('markerHeight', 7)
      .attr('markerUnits', 'strokeWidth')
      .attr('orient', 'auto')
      .append('path')
      .attr('d', 'M0,-5L10,0L0,5')
      .attr('fill', '#5b6b7c');

    const shadow = defs.append('filter')
      .attr('id', 'service-node-shadow')
      .attr('x', '-20%')
      .attr('y', '-20%')
      .attr('width', '140%')
      .attr('height', '140%');
    shadow.append('feDropShadow')
      .attr('dx', 0)
      .attr('dy', 8)
      .attr('stdDeviation', 8)
      .attr('flood-color', '#0f172a')
      .attr('flood-opacity', 0.12);

  }

  private drawLinks(): void {
    this.linksLayer
      .selectAll<SVGPathElement, RenderEdge>('path.service-link')
      .data(this.edges, edge => edge.id)
      .join('path')
      .attr('class', 'service-link')
      .attr('marker-end', 'url(#service-arrow)');
  }

  private drawNodes(): void {
    const nodeSelection = this.nodesLayer
      .selectAll<SVGGElement, RenderNode>('g.service-node')
      .data(this.nodes, node => node.id)
      .join(enter => {
        const group = enter.append('g')
          .attr('class', 'service-node')
          .attr('tabindex', 0)
          .attr('role', 'button')
          .on('click', (event, node) => {
            event.stopPropagation();
            this.callbacks.onServiceSelect(node.id);
          })
          .on('keydown', (event, node) => {
            if (event.key === 'Enter' || event.key === ' ') {
              event.preventDefault();
              this.callbacks.onServiceSelect(node.id);
            }
          });

        group.append('rect')
          .attr('class', 'service-node-card')
          .attr('rx', SERVICE_NODE_RADIUS);

        group.append('rect')
          .attr('class', 'service-node-accent')
          .attr('width', 6)
          .attr('rx', 3);

        group.append('text').attr('class', 'service-node-title');
        group.append('text').attr('class', 'service-node-description');
        group.append('text').attr('class', 'service-node-meta');
        group.append('g').attr('class', 'service-node-badges');

        return group;
      });

    nodeSelection
      .classed('external', node => node.isExternal)
      .classed('selected', node => node.id === this.selectedServiceId)
      .classed('flow-highlight', node => this.highlightedServiceIds?.has(node.id) ?? false)
      .classed('dimmed', node => this.highlightedServiceIds
        ? !this.highlightedServiceIds.has(node.id)
        : Boolean(this.selectedServiceId) && node.id !== this.selectedServiceId)
      .each((node, index, groups) => this.populateNode(d3.select(groups[index]), node));
  }

  private populateNode(group: d3.Selection<SVGGElement, RenderNode, any, any>, node: RenderNode): void {
    const left = -node.layoutWidth / 2 + SERVICE_NODE_HORIZONTAL_PADDING;
    const top = -node.layoutHeight / 2;

    group.select<SVGRectElement>('rect.service-node-card')
      .attr('x', -node.layoutWidth / 2)
      .attr('y', top)
      .attr('width', node.layoutWidth)
      .attr('height', node.layoutHeight);

    group.select<SVGRectElement>('rect.service-node-accent')
      .attr('x', -node.layoutWidth / 2)
      .attr('y', top)
      .attr('height', node.layoutHeight);

    group.select<SVGTextElement>('text.service-node-title')
      .attr('x', left)
      .attr('y', top + SERVICE_NODE_TOP_PADDING)
      .call(selection => appendLines(selection, node.titleText.lines, SERVICE_TITLE_LINE_HEIGHT));

    group.select<SVGTextElement>('text.service-node-description')
      .attr('x', left)
      .attr('y', top + SERVICE_NODE_TOP_PADDING + node.titleText.height + 16)
      .call(selection => appendLines(selection, node.descriptionText.lines, SERVICE_DESCRIPTION_LINE_HEIGHT));

    const analyzed = formatLastAnalyzed(node.serviceData.service.lastAnalyzedAt);
    const meta = analyzed ? `Last analyzed: ${analyzed}` : 'Last analyzed: unknown';
    group.select<SVGTextElement>('text.service-node-meta')
      .attr('x', left)
      .attr('y', top + SERVICE_NODE_TOP_PADDING + node.titleText.height + 16 + node.descriptionText.height + 22)
      .text(meta);

    const badges = [
      `${node.endpointCount} endpoint${node.endpointCount !== 1 ? 's' : ''}`,
      `${node.useCaseCount} use case${node.useCaseCount !== 1 ? 's' : ''}`,
      ...(node.isExternal ? ['External'] : [])
    ];

    const badgeGroup = group.select<SVGGElement>('g.service-node-badges')
      .attr('transform', `translate(${left}, ${node.layoutHeight / 2 - SERVICE_NODE_BOTTOM_PADDING - SERVICE_BADGE_HEIGHT})`);
    const badgeSelection = badgeGroup.selectAll<SVGGElement, string>('g.badge')
      .data(badges)
      .join(enter => {
        const badge = enter.append('g').attr('class', 'badge');
        badge.append('rect').attr('height', 22).attr('rx', 11);
        badge.append('text').attr('y', 15);
        return badge;
      });

    let x = 0;
    badgeSelection.each(function(label) {
      const badge = d3.select(this);
      const width = Math.max(58, label.length * 6.3 + 18);
      badge.attr('transform', `translate(${x}, 0)`);
      badge.select('rect').attr('width', width);
      badge.select('text').attr('x', 9).text(label);
      x += width + 8;
    });
  }

  private runLayout(): void {
    const { width, height } = this.viewport();
    const avg = (width + height) / 2;

    this.simulation = d3.forceSimulation<RenderNode, RenderEdge>(this.nodes)
      .force('link', d3.forceLink<RenderNode, RenderEdge>(this.edges)
        .id(node => node.id)
        .distance(Math.max(460, avg * 0.36))
        .strength(0.42))
      .force('charge', d3.forceManyBody().strength(-1300))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('collision', d3.forceCollide<RenderNode>()
        .radius(node => Math.max(node.layoutWidth, node.layoutHeight) / 2 + 48)
        .strength(1))
      .on('tick', () => this.updatePositions());

    this.simulation.tick(90);
    this.updatePositions();
    this.fitToView(0);
    this.simulation.alpha(0.35).restart();
  }

  private updatePositions(): void {
    this.linksLayer
      .selectAll<SVGPathElement, RenderEdge>('path.service-link')
      .attr('d', edge => serviceLinkPath(edge.source, edge.target));

    this.nodesLayer
      .selectAll<SVGGElement, RenderNode>('g.service-node')
      .attr('transform', node => `translate(${node.x ?? 0},${node.y ?? 0})`);
  }

  private fitToView(duration: number): void {
    const { width, height } = this.viewport();
    if (!this.nodes.length || width === 0 || height === 0) return;

    const minX = d3.min(this.nodes, node => (node.x ?? 0) - node.layoutWidth / 2) ?? 0;
    const maxX = d3.max(this.nodes, node => (node.x ?? 0) + node.layoutWidth / 2) ?? width;
    const minY = d3.min(this.nodes, node => (node.y ?? 0) - node.layoutHeight / 2) ?? 0;
    const maxY = d3.max(this.nodes, node => (node.y ?? 0) + node.layoutHeight / 2) ?? height;

    const graphWidth = Math.max(1, maxX - minX);
    const graphHeight = Math.max(1, maxY - minY);
    const fitScale = Math.min(
      (width - GRAPH_FIT_PADDING) / graphWidth,
      (height - GRAPH_FIT_PADDING) / graphHeight
    );
    const scale = Math.min(GRAPH_MAX_INITIAL_SCALE, Math.max(GRAPH_MIN_READABLE_SCALE, fitScale));
    const centerX = (minX + maxX) / 2;
    const centerY = (minY + maxY) / 2;

    const transform = d3.zoomIdentity
      .translate(width / 2 - scale * centerX, height / 2 - scale * centerY)
      .scale(scale);

    this.svg.transition().duration(duration).call(this.zoom.transform, transform);
  }

  private stopSimulation(): void {
    if (this.simulation) {
      this.simulation.stop();
      this.simulation = null;
    }
  }

  private viewport(): { width: number; height: number } {
    const element = this.svg.node();
    return {
      width: element?.clientWidth ?? 0,
      height: element?.clientHeight ?? 0
    };
  }
}

function serviceCountPosition(index: number, count: number): { x: number; y: number } {
  const columns = Math.max(1, Math.ceil(Math.sqrt(count)));
  const row = Math.floor(index / columns);
  const col = index % columns;
  return {
    x: col * (SERVICE_NODE_MAX_WIDTH + 180),
    y: row * 360
  };
}

function serviceLinkPath(source: RenderNode, target: RenderNode): string {
  const sx = source.x ?? 0;
  const sy = source.y ?? 0;
  const tx = target.x ?? 0;
  const ty = target.y ?? 0;
  const dx = tx - sx;
  const dy = ty - sy;
  const sourceOffset = rectangleIntersectionOffset(dx, dy, source.layoutWidth, source.layoutHeight);
  const targetOffset = rectangleIntersectionOffset(-dx, -dy, target.layoutWidth, target.layoutHeight);
  const x1 = sx + sourceOffset.x;
  const y1 = sy + sourceOffset.y;
  const x2 = tx + targetOffset.x;
  const y2 = ty + targetOffset.y;
  const curve = Math.min(120, Math.hypot(dx, dy) * 0.18);
  const normalX = dy === 0 ? 0 : -dy / Math.hypot(dx, dy);
  const normalY = dx === 0 ? 0 : dx / Math.hypot(dx, dy);
  const mx = (x1 + x2) / 2 + normalX * curve;
  const my = (y1 + y2) / 2 + normalY * curve;
  return `M ${x1},${y1} Q ${mx},${my} ${x2},${y2}`;
}

function rectangleIntersectionOffset(dx: number, dy: number, width: number, height: number): { x: number; y: number } {
  if (dx === 0 && dy === 0) return { x: 0, y: 0 };
  const halfW = width / 2 + 8;
  const halfH = height / 2 + 8;
  const scale = Math.min(
    Math.abs(dx) > 0 ? halfW / Math.abs(dx) : Number.POSITIVE_INFINITY,
    Math.abs(dy) > 0 ? halfH / Math.abs(dy) : Number.POSITIVE_INFINITY
  );
  return { x: dx * scale, y: dy * scale };
}

function buildServiceNodeLayout(service: GraphServiceNode): Pick<RenderNode, 'layoutWidth' | 'layoutHeight' | 'titleText' | 'descriptionText'> {
  const titleText = buildTextBlock(service.serviceData.service.name, 44, SERVICE_TITLE_LINE_HEIGHT);
  const descriptionText = buildTextBlock(
    service.serviceData.service.description || 'No description',
    58,
    SERVICE_DESCRIPTION_LINE_HEIGHT
  );
  const contentWidth = Math.max(titleText.width, descriptionText.width);
  const layoutWidth = clamp(
    contentWidth + SERVICE_NODE_HORIZONTAL_PADDING * 2,
    SERVICE_NODE_MIN_WIDTH,
    SERVICE_NODE_MAX_WIDTH
  );
  const chars = charsForWidth(layoutWidth - SERVICE_NODE_HORIZONTAL_PADDING * 2);
  const reflowedTitleText = buildTextBlock(service.serviceData.service.name, chars, SERVICE_TITLE_LINE_HEIGHT);
  const reflowedDescriptionText = buildTextBlock(
    service.serviceData.service.description || 'No description',
    chars,
    SERVICE_DESCRIPTION_LINE_HEIGHT
  );
  const layoutHeight =
    SERVICE_NODE_TOP_PADDING +
    reflowedTitleText.height +
    16 +
    reflowedDescriptionText.height +
    22 +
    SERVICE_META_HEIGHT +
    18 +
    SERVICE_BADGE_HEIGHT +
    SERVICE_NODE_BOTTOM_PADDING;

  return {
    layoutWidth,
    layoutHeight,
    titleText: reflowedTitleText,
    descriptionText: reflowedDescriptionText
  };
}

function buildTextBlock(value: string, maxCharsPerLine: number, lineHeight: number): TextBlock {
  const words = value.trim().split(/\s+/).filter(Boolean);
  const lines: string[] = [];
  let current = '';

  for (const word of words) {
    const chunks = splitLongWord(word, maxCharsPerLine);
    for (const chunk of chunks) {
      const next = current ? `${current} ${chunk}` : chunk;
      if (next.length <= maxCharsPerLine || !current) {
        current = next;
        continue;
      }
      lines.push(current);
      current = chunk;
    }
  }
  if (current) {
    lines.push(current);
  }
  if (!lines.length) {
    lines.push('');
  }

  return {
    lines,
    width: Math.ceil(Math.max(...lines.map(line => line.length)) * AVERAGE_CHAR_WIDTH),
    height: lines.length * lineHeight
  };
}

function splitLongWord(word: string, maxChars: number): string[] {
  if (word.length <= maxChars) return [word];
  const chunks: string[] = [];
  for (let index = 0; index < word.length; index += maxChars) {
    chunks.push(word.slice(index, index + maxChars));
  }
  return chunks;
}

function appendLines(
  selection: d3.Selection<SVGTextElement, RenderNode, any, any>,
  lines: string[],
  lineHeight: number
): void {
  selection.each(function() {
    const textSelection = d3.select(this);
    textSelection.selectAll('tspan').remove();
    const x = Number(textSelection.attr('x'));
    const y = Number(textSelection.attr('y'));
    lines.forEach((line, index) => {
      textSelection.append('tspan')
        .attr('x', x)
        .attr('y', y + index * lineHeight)
        .text(line);
    });
  });
}

function charsForWidth(width: number): number {
  return Math.max(18, Math.floor(width / AVERAGE_CHAR_WIDTH));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
