import * as d3 from 'd3';
import { FlowEndpointGroup, FlowServiceGroup, FlowStepNode, UseCaseFlowModel } from './use-case-flow-model';

const SERVICE_PADDING_X = 22;
const SERVICE_PADDING_TOP = 44;
const SERVICE_PADDING_BOTTOM = 20;
const SERVICE_GAP = 56;
const ENDPOINT_PADDING_X = 16;
const ENDPOINT_PADDING_TOP = 34;
const ENDPOINT_PADDING_BOTTOM = 16;
const ENDPOINT_GAP = 16;
const STEP_PADDING_X = 14;
const STEP_PADDING_TOP = 14;
const STEP_PADDING_BOTTOM = 14;
const STEP_GAP = 14;
const FLOW_PADDING = 72;
const MIN_SCALE = 0.35;
const MAX_SCALE = 1.35;
const MAX_FIT_SCALE = 1;

const STEP_MIN_WIDTH = 220;
const STEP_MAX_WIDTH = 340;
const ENDPOINT_MIN_WIDTH = 260;
const SERVICE_MIN_WIDTH = 300;

const SERVICE_TITLE_LINE_HEIGHT = 18;
const ENDPOINT_TITLE_LINE_HEIGHT = 14;
const STEP_NAME_LINE_HEIGHT = 15;
const STEP_DESCRIPTION_LINE_HEIGHT = 13;
const STEP_META_HEIGHT = 18;
const AVERAGE_CHAR_WIDTH = 6.6;

export type FlowSelection =
  | { type: 'service'; serviceId: string }
  | { type: 'endpoint'; serviceId: string; endpointKey: string }
  | { type: 'step'; stepIndex: number }
  | null;

export interface UseCaseFlowRendererCallbacks {
  onServiceSelect(serviceId: string): void;
  onEndpointSelect(serviceId: string, endpointKey: string): void;
  onStepSelect(stepIndex: number): void;
}

interface TextBlock {
  lines: string[];
  width: number;
  height: number;
}

interface LayoutStep extends FlowStepNode {
  x: number;
  y: number;
  width: number;
  height: number;
  nameText: TextBlock;
  descriptionText: TextBlock;
}

interface LayoutEndpoint extends FlowEndpointGroup {
  x: number;
  y: number;
  width: number;
  height: number;
  titleText: TextBlock;
  steps: LayoutStep[];
}

interface LayoutService extends Omit<FlowServiceGroup, 'endpoints'> {
  x: number;
  y: number;
  width: number;
  height: number;
  titleText: TextBlock;
  endpoints: LayoutEndpoint[];
}

interface FlowLayout {
  services: LayoutService[];
  stepsByIndex: Map<number, LayoutStep>;
  width: number;
  height: number;
}

export class UseCaseFlowRenderer {
  private readonly svg: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private root: d3.Selection<SVGGElement, unknown, null, undefined> | null = null;
  private zoom: d3.ZoomBehavior<SVGSVGElement, unknown> | null = null;
  private model: UseCaseFlowModel | null = null;
  private layout: FlowLayout | null = null;
  private selection: FlowSelection = null;

  constructor(
    svgElement: SVGSVGElement,
    private readonly callbacks: UseCaseFlowRendererCallbacks
  ) {
    this.svg = d3.select(svgElement)
      .attr('role', 'img')
      .attr('aria-label', 'Use case step flow');
  }

  render(model: UseCaseFlowModel, selection: FlowSelection): void {
    this.model = model;
    this.selection = selection;
    this.svg.selectAll('*').remove();

    this.createDefs();
    this.svg.append('rect')
      .attr('class', 'flow-backdrop')
      .attr('width', '100%')
      .attr('height', '100%');
    this.root = this.svg.append('g').attr('class', 'flow-root');
    this.zoom = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([MIN_SCALE, MAX_SCALE])
      .on('zoom', event => this.root?.attr('transform', event.transform.toString()));
    this.svg.call(this.zoom);

    const layout = buildLayout(model);
    this.layout = layout;
    this.drawContainers(layout);
    this.drawEdges(model, layout);
    this.drawSteps(layout);
    this.updateActiveState(selection);
    if (selection?.type === 'step') {
      this.focusOnStep(selection.stepIndex, 250, false);
    } else {
      this.fitToView(layout, 250);
    }
  }

  focusOnStep(stepIndex: number, duration = 450, preserveScale = true): void {
    if (!this.zoom || !this.layout) return;
    const step = this.layout.stepsByIndex.get(stepIndex);
    if (!step) return;

    const { width, height } = this.viewportSize();
    if (width === 0 || height === 0) return;

    const scale = preserveScale
      ? d3.zoomTransform(this.svg.node()!).k
      : this.computeFitScale(this.layout);
    const centerX = step.x + step.width / 2;
    const centerY = step.y + step.height / 2;
    const transform = d3.zoomIdentity
      .translate(width / 2 - scale * centerX, height / 2 - scale * centerY)
      .scale(scale);

    this.applyTransform(transform, duration);
  }

  updateActiveState(selection: FlowSelection): void {
    this.selection = selection;
    this.root?.selectAll<SVGGElement, LayoutService>('g.flow-service')
      .classed('active', service => selection?.type === 'service' && service.serviceId === selection.serviceId);
    this.root?.selectAll<SVGGElement, LayoutEndpoint>('g.flow-endpoint')
      .classed('active', endpoint => selection?.type === 'endpoint' && endpoint.endpointKey === selection.endpointKey);
    this.root?.selectAll<SVGGElement, LayoutStep>('g.flow-step')
      .classed('active', step => selection?.type === 'step' && step.index === selection.stepIndex);
  }

  resize(): void {
    if (this.model) {
      this.render(this.model, this.selection);
    }
  }

  clear(): void {
    this.model = null;
    this.layout = null;
    this.selection = null;
    this.svg.on('.zoom', null);
    this.svg.selectAll('*').remove();
    this.root = null;
    this.zoom = null;
  }

  destroy(): void {
    this.clear();
  }

  private createDefs(): void {
    const defs = this.svg.append('defs');
    defs.append('marker')
      .attr('id', 'flow-step-arrow')
      .attr('viewBox', '0 -5 10 10')
      .attr('refX', 10)
      .attr('refY', 0)
      .attr('markerWidth', 7)
      .attr('markerHeight', 7)
      .attr('markerUnits', 'strokeWidth')
      .attr('orient', 'auto')
      .append('path')
      .attr('d', 'M0,-5L10,0L0,5')
      .attr('fill', '#7c3aed');
  }

  private drawEdges(model: UseCaseFlowModel, layout: FlowLayout): void {
    this.root?.append('g')
      .attr('class', 'flow-edges')
      .selectAll<SVGPathElement, { from: LayoutStep; to: LayoutStep }>('path.flow-edge')
      .data(model.sequenceEdges
        .map(edge => {
          const from = layout.stepsByIndex.get(edge.fromIndex);
          const to = layout.stepsByIndex.get(edge.toIndex);
          return from && to ? { from, to } : null;
        })
        .filter((edge): edge is { from: LayoutStep; to: LayoutStep } => Boolean(edge)))
      .join('path')
      .attr('class', 'flow-edge')
      .attr('marker-end', 'url(#flow-step-arrow)')
      .attr('d', edge => stepEdgePath(edge.from, edge.to));
  }

  private drawContainers(layout: FlowLayout): void {
    const services = this.root?.append('g')
      .attr('class', 'flow-services')
      .selectAll<SVGGElement, LayoutService>('g.flow-service')
      .data(layout.services)
      .join('g')
      .attr('class', service => `flow-service${service.isExternal ? ' external' : ''}`)
      .attr('transform', service => `translate(${service.x},${service.y})`)
      .attr('tabindex', 0)
      .attr('role', 'button')
      .on('click', (event, service) => {
        event.stopPropagation();
        this.callbacks.onServiceSelect(service.serviceId);
      })
      .on('keydown', (event, service) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          this.callbacks.onServiceSelect(service.serviceId);
        }
      });

    services?.append('rect')
      .attr('class', 'flow-service-card')
      .attr('width', service => service.width)
      .attr('height', service => service.height)
      .attr('rx', 18);

    services?.append('text')
      .attr('class', 'flow-service-title')
      .attr('x', SERVICE_PADDING_X)
      .attr('y', 26)
      .each(function(service) {
        appendLines(d3.select(this), service.titleText.lines, SERVICE_TITLE_LINE_HEIGHT);
      });

    const endpointGroups = services?.selectAll<SVGGElement, LayoutEndpoint>('g.flow-endpoint')
      .data(service => service.endpoints)
      .join('g')
      .attr('class', 'flow-endpoint')
      .attr('transform', endpoint => `translate(${endpoint.x},${endpoint.y})`)
      .attr('tabindex', 0)
      .attr('role', 'button')
      .on('click', (event, endpoint) => {
        event.stopPropagation();
        const service = d3.select<SVGGElement, LayoutService>(event.currentTarget.parentNode as SVGGElement).datum();
        this.callbacks.onEndpointSelect(service.serviceId, endpoint.endpointKey);
      })
      .on('keydown', (event, endpoint) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          const service = d3.select<SVGGElement, LayoutService>(event.currentTarget.parentNode as SVGGElement).datum();
          this.callbacks.onEndpointSelect(service.serviceId, endpoint.endpointKey);
        }
      });

    endpointGroups?.append('rect')
      .attr('class', 'flow-endpoint-card')
      .attr('width', endpoint => endpoint.width)
      .attr('height', endpoint => endpoint.height)
      .attr('rx', 14);

    endpointGroups?.append('text')
      .attr('class', 'flow-endpoint-title')
      .attr('x', ENDPOINT_PADDING_X)
      .attr('y', 22)
      .each(function(endpoint) {
        appendLines(d3.select(this), endpoint.titleText.lines, ENDPOINT_TITLE_LINE_HEIGHT);
      });
  }

  private drawSteps(layout: FlowLayout): void {
    const steps = this.root?.append('g')
      .attr('class', 'flow-steps')
      .selectAll<SVGGElement, LayoutStep>('g.flow-step')
      .data([...layout.stepsByIndex.values()].sort((a, b) => a.index - b.index))
      .join('g')
      .attr('class', 'flow-step')
      .attr('transform', step => `translate(${step.x},${step.y})`)
      .attr('tabindex', 0)
      .attr('role', 'button')
      .on('click', (event, step) => {
        event.stopPropagation();
        this.callbacks.onStepSelect(step.index);
      })
      .on('keydown', (event, step) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          this.callbacks.onStepSelect(step.index);
        }
      });

    steps?.append('rect')
      .attr('class', 'flow-step-card')
      .attr('width', step => step.width)
      .attr('height', step => step.height)
      .attr('rx', 12);

    steps?.append('text')
      .attr('class', 'flow-step-index')
      .attr('x', STEP_PADDING_X)
      .attr('y', STEP_PADDING_TOP + 8)
      .text(step => `#${step.index + 1}`);

    steps?.append('text')
      .attr('class', 'flow-step-name')
      .attr('x', STEP_PADDING_X)
      .attr('y', STEP_PADDING_TOP + STEP_META_HEIGHT + 8)
      .each(function(step) {
        appendLines(d3.select(this), step.nameText.lines, STEP_NAME_LINE_HEIGHT);
      });

    steps?.append('text')
      .attr('class', 'flow-step-description')
      .attr('x', STEP_PADDING_X)
      .attr('y', step => STEP_PADDING_TOP + STEP_META_HEIGHT + 8 + step.nameText.height + 8)
      .each(function(step) {
        appendLines(d3.select(this), step.descriptionText.lines, STEP_DESCRIPTION_LINE_HEIGHT);
      });
  }

  private viewportSize(): { width: number; height: number } {
    const element = this.svg.node();
    return {
      width: element?.clientWidth ?? 0,
      height: element?.clientHeight ?? 0
    };
  }

  private computeFitScale(layout: FlowLayout): number {
    const { width, height } = this.viewportSize();
    if (width === 0 || height === 0) return MAX_FIT_SCALE;

    return Math.min(
      MAX_FIT_SCALE,
      Math.max(MIN_SCALE, Math.min(
        (width - FLOW_PADDING) / Math.max(1, layout.width),
        (height - FLOW_PADDING) / Math.max(1, layout.height)
      ))
    );
  }

  private applyTransform(transform: d3.ZoomTransform, duration: number): void {
    if (!this.zoom) return;
    if (duration > 0) {
      this.svg.transition().duration(duration).call(this.zoom.transform, transform);
    } else {
      this.svg.call(this.zoom.transform, transform);
    }
  }

  private fitToView(layout: FlowLayout, duration: number): void {
    const { width, height } = this.viewportSize();
    if (!this.zoom || width === 0 || height === 0) return;

    const scale = this.computeFitScale(layout);
    const transform = d3.zoomIdentity
      .translate((width - layout.width * scale) / 2, (height - layout.height * scale) / 2)
      .scale(scale);

    this.applyTransform(transform, duration);
  }
}

function buildLayout(model: UseCaseFlowModel): FlowLayout {
  const stepsByIndex = new Map<number, LayoutStep>();
  const services: LayoutService[] = [];
  let x = 0;
  let maxHeight = 0;

  for (const service of model.services) {
    const titleText = buildTextBlock(service.serviceName, 28, 3, SERVICE_TITLE_LINE_HEIGHT);
    let endpointY = SERVICE_PADDING_TOP + titleText.height;
    const endpoints: LayoutEndpoint[] = [];
    let serviceWidth = Math.max(SERVICE_MIN_WIDTH, titleText.width + SERVICE_PADDING_X * 2);

    for (const endpoint of service.endpoints) {
      const endpointTitleText = buildTextBlock(endpoint.label, 28, 4, ENDPOINT_TITLE_LINE_HEIGHT);
      const steps = endpoint.steps.map(step => {
        const nameText = buildTextBlock(step.step.name || 'Unnamed step', 30, Number.POSITIVE_INFINITY, STEP_NAME_LINE_HEIGHT);
        const descriptionText = buildTextBlock(step.step.description || 'No description provided.', 34, Number.POSITIVE_INFINITY, STEP_DESCRIPTION_LINE_HEIGHT);
        const contentWidth = Math.max(nameText.width, descriptionText.width, STEP_MIN_WIDTH - STEP_PADDING_X * 2);
        const width = clamp(contentWidth + STEP_PADDING_X * 2, STEP_MIN_WIDTH, STEP_MAX_WIDTH);
        const reflowedNameText = buildTextBlock(step.step.name || 'Unnamed step', charsForWidth(width - STEP_PADDING_X * 2), Number.POSITIVE_INFINITY, STEP_NAME_LINE_HEIGHT);
        const reflowedDescriptionText = buildTextBlock(step.step.description || 'No description provided.', charsForWidth(width - STEP_PADDING_X * 2), Number.POSITIVE_INFINITY, STEP_DESCRIPTION_LINE_HEIGHT);
        return {
          ...step,
          x: ENDPOINT_PADDING_X,
          y: 0,
          width,
          height: STEP_PADDING_TOP + STEP_META_HEIGHT + 8 + reflowedNameText.height + 8 + reflowedDescriptionText.height + STEP_PADDING_BOTTOM,
          nameText: reflowedNameText,
          descriptionText: reflowedDescriptionText
        };
      });
      const endpointWidth = Math.max(
        ENDPOINT_MIN_WIDTH,
        endpointTitleText.width + ENDPOINT_PADDING_X * 2,
        ...steps.map(step => step.width + ENDPOINT_PADDING_X * 2)
      );
      let stepY = ENDPOINT_PADDING_TOP + endpointTitleText.height;
      steps.forEach(step => {
        step.y = stepY;
        step.x = (endpointWidth - step.width) / 2;
        stepY += step.height + STEP_GAP;
      });
      const endpointHeight = stepY - STEP_GAP + ENDPOINT_PADDING_BOTTOM;
      const layoutEndpoint: LayoutEndpoint = {
        ...endpoint,
        x: SERVICE_PADDING_X,
        y: endpointY,
        width: endpointWidth,
        height: endpointHeight,
        titleText: endpointTitleText,
        steps
      };

      for (const step of steps) {
        stepsByIndex.set(step.index, {
          ...step,
          x: x + layoutEndpoint.x + step.x,
          y: layoutEndpoint.y + step.y,
          width: step.width,
          height: step.height
        });
      }

      endpointY += endpointHeight + ENDPOINT_GAP;
      serviceWidth = Math.max(serviceWidth, endpointWidth + SERVICE_PADDING_X * 2);
      endpoints.push(layoutEndpoint);
    }

    const serviceHeight = Math.max(120, endpointY - ENDPOINT_GAP + SERVICE_PADDING_BOTTOM);
    services.push({
      ...service,
      x,
      y: 0,
      width: serviceWidth,
      height: serviceHeight,
      titleText,
      endpoints
    });
    x += serviceWidth + SERVICE_GAP;
    maxHeight = Math.max(maxHeight, serviceHeight);
  }

  return {
    services,
    stepsByIndex,
    width: Math.max(1, x - SERVICE_GAP),
    height: Math.max(1, maxHeight)
  };
}

function stepEdgePath(from: LayoutStep, to: LayoutStep): string {
  const fromCenterX = from.x + from.width / 2;
  const fromCenterY = from.y + from.height / 2;
  const toCenterX = to.x + to.width / 2;
  const toCenterY = to.y + to.height / 2;
  const horizontalDistance = toCenterX - fromCenterX;
  const verticalDistance = toCenterY - fromCenterY;

  if (Math.abs(horizontalDistance) < Math.min(from.width, to.width) * 0.65) {
    const x1 = fromCenterX;
    const y1 = verticalDistance >= 0 ? from.y + from.height : from.y;
    const x2 = toCenterX;
    const y2 = verticalDistance >= 0 ? to.y : to.y + to.height;
    return `M ${x1},${y1} L ${x2},${y2}`;
  }

  if (horizontalDistance > 0) {
    const x1 = from.x + from.width;
    const y1 = fromCenterY;
    const x2 = to.x;
    const y2 = toCenterY;
    const midX = x1 + Math.max(40, (x2 - x1) / 2);
    return `M ${x1},${y1} C ${midX},${y1} ${midX},${y2} ${x2},${y2}`;
  }

  const x1 = from.x;
  const y1 = fromCenterY;
  const x2 = to.x + to.width;
  const y2 = toCenterY;
  const midX = x1 - Math.max(40, (x1 - x2) / 2);
  return `M ${x1},${y1} C ${midX},${y1} ${midX},${y2} ${x2},${y2}`;
}

function buildTextBlock(
  value: string,
  maxCharsPerLine: number,
  maxLines: number,
  lineHeight: number
): TextBlock {
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
      if (lines.length >= maxLines) break;
    }
    if (lines.length >= maxLines) break;
  }

  if (current && lines.length < maxLines) {
    lines.push(current);
  }

  if (!lines.length) {
    lines.push('');
  }

  const longestLine = Math.max(...lines.map(line => line.length));
  return {
    lines,
    width: Math.ceil(longestLine * AVERAGE_CHAR_WIDTH),
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
  selection: d3.Selection<SVGTextElement, any, any, any>,
  lines: string[],
  lineHeight: number
): void {
  selection.selectAll('tspan').remove();
  const x = Number(selection.attr('x'));
  const y = Number(selection.attr('y'));
  lines.forEach((line, index) => {
    selection.append('tspan')
      .attr('x', x)
      .attr('y', y + index * lineHeight)
      .text(line);
  });
}

function charsForWidth(width: number): number {
  return Math.max(16, Math.floor(width / AVERAGE_CHAR_WIDTH));
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
