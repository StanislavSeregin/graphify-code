# GraphifyCode UI Vision & Implementation Plan

## Overview

Zoom-based interactive graph visualization with Level of Detail (LOD) rendering. Users can explore the entire system architecture from high-level service overview to detailed endpoint-level connections through intuitive zoom and focus interactions.

---

## Core Concepts

### 1. Zoom-based Level of Detail (LOD)

The graph adapts its visualization based on zoom level:

| Zoom Level | Range | Display |
|------------|-------|---------|
| **Far** | 0.1x - 0.5x | Service icons, names, simplified connections |
| **Medium** | 0.5x - 1.5x | Service cards with metadata, endpoint count badges |
| **Close** | 1.5x - 4.0x | Service containers with endpoint nodes inside, detailed connections |

### 2. Hierarchical Node Structure

```
┌─────────────────────────────────────────┐
│  Service A                              │
│  Description: API Gateway               │
│                                         │
│  ┌──────────┐  ┌──────────┐           │
│  │ POST     │  │ GET      │           │──────┐
│  │ /login   │  │ /profile │           │      │
│  └──────────┘  └──────────┘           │      │
│                                         │      │
│  ┌──────────┐                          │      │
│  │ DELETE   │                          │      │
│  │ /logout  │                          │      │
│  └──────────┘                          │      │
└─────────────────────────────────────────┘      │
                                                 │
                                                 ├──→ Service B
                                                 │    (specific endpoint)
                                                 │
                                                 └──→ Service C
                                                      (specific endpoint)
```

### 3. Interactive States

#### Idle State
- All nodes visible with normal opacity (1.0)
- All connections visible with normal opacity (0.6)
- Hovering shows highlights

#### Service Focus
- **Clicked service**: opacity 1.0, highlighted border
- **Connected services**: opacity 1.0
- **Connections to/from service**: opacity 0.8
- **Unrelated nodes**: opacity 0.2
- **Unrelated connections**: opacity 0.1

#### Endpoint Focus (at close zoom)
- **Clicked endpoint**: opacity 1.0, highlighted
- **Parent service**: highlighted container
- **Target endpoint(s)**: opacity 1.0, highlighted
- **Target service(s)**: highlighted container
- **Related connections**: opacity 0.8
- **Unrelated nodes**: opacity 0.2

---

## Technical Architecture

### Data Structure

**Backend API Types** (from `graph.service.ts`):

```typescript
// Core entities from API
type Service = {
  id: string;
  name: string;
  description: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

type Endpoint = {
  id: string;
  name: string;
  description: string;
  type: string;
  lastAnalyzedAt: string;
  relativeCodePath: string | null;
};

type Relations = {
  targetEndpointIds: string[];
};

type UseCase = {
  id: string;
  name: string;
  description: string;
  initiatingEndpointId: string;
  lastAnalyzedAt: string;
  steps: UseCaseStep[];
};

type UseCaseStep = {
  name: string;
  description: string;
  serviceId: string | null;
  endpointId: string | null;
  relativeCodePath: string | null;
};

type ServiceData = {
  service: Service;
  endpoint: Endpoint[];
  relations: Relations;
  useCases: UseCase[];
};

type FullGraph = {
  services: ServiceData[];
};
```

**UI/Canvas Types** (for rendering):

```typescript
interface GraphNode {
  id: string;
  type: 'service';
  name: string;
  description: string;
  sourceData: ServiceData; // Reference to original data

  // Visual properties
  x?: number;
  y?: number;
  width: number;
  height: number;

  // Children endpoints
  endpoints: EndpointNode[];

  // Layout
  endpointLayout: 'grid' | 'vertical-list';
}

interface EndpointNode {
  id: string;
  type: 'endpoint';
  name: string;
  description: string;
  endpointType: string; // 'http', 'queue', 'job'
  parentServiceId: string;

  // Position relative to parent
  relativeX: number;
  relativeY: number;

  // Visual
  width: number;
  height: number;
}

interface GraphLink {
  id: string;
  source: string; // service ID or endpoint ID
  target: string; // service ID or endpoint ID
  type: 'service-to-service' | 'endpoint-to-endpoint';

  // For endpoint-to-endpoint, we need to know parent services
  sourceServiceId?: string;
  targetServiceId?: string;
}
```

### Rendering Strategy

#### LOD Manager
```typescript
class LODManager {
  getCurrentLevel(zoomScale: number): 'far' | 'medium' | 'close' {
    if (zoomScale < 0.5) return 'far';
    if (zoomScale < 1.5) return 'medium';
    return 'close';
  }

  shouldShowEndpoints(zoomScale: number): boolean {
    return zoomScale >= 1.5;
  }

  shouldShowDetailedLabels(zoomScale: number): boolean {
    return zoomScale >= 1.0;
  }

  getNodeDetailLevel(zoomScale: number): 'minimal' | 'standard' | 'detailed' {
    if (zoomScale < 0.5) return 'minimal';
    if (zoomScale < 1.5) return 'standard';
    return 'detailed';
  }
}
```

---

## Implementation Plan

### Phase 1: Refactor Current Structure
**Goal**: Prepare codebase for hierarchical rendering

#### Task 1.1: Update Data Models
- [ ] Add `EndpointNode` interface
- [ ] Update `GraphNode` to include `endpoints: EndpointNode[]`
- [ ] Update `GraphLink` to support endpoint-level connections
- [ ] Add layout calculation properties (`width`, `height`, positions)

#### Task 1.2: Create LOD Manager Service
- [ ] Create `lod-manager.service.ts`
- [ ] Implement zoom level detection
- [ ] Implement visibility rules per zoom level
- [ ] Add configuration for zoom thresholds

#### Task 1.3: Refactor Data Preparation
- [ ] Update `prepareGraphData()` to create `EndpointNode` objects
- [ ] Calculate endpoint positions relative to parent service
- [ ] Create endpoint-to-endpoint links (not just service-to-service)
- [ ] Implement endpoint layout algorithm (grid or vertical list)

---

### Phase 2: Implement Service Container Rendering
**Goal**: Render services as containers with dynamic sizing

#### Task 2.1: Service Container Visual
- [ ] Replace circles with rounded rectangles for services
- [ ] Calculate service container size based on endpoint count
- [ ] Add service header section (name, description)
- [ ] Add service body section (endpoint container area)
- [ ] Style service borders and backgrounds

#### Task 2.2: Service Metadata Display
- [ ] Add endpoint count badge (visible at medium+ zoom)
- [ ] Add last analyzed timestamp
- [ ] Add service type indicator (if applicable)
- [ ] Add description tooltip/text

#### Task 2.3: Dynamic Sizing
- [ ] Calculate min/max container sizes
- [ ] Implement padding and spacing constants
- [ ] Update force simulation to use rectangular collision
- [ ] Adjust node positioning logic

---

### Phase 3: Implement Endpoint Rendering
**Goal**: Render endpoints inside service containers at close zoom

#### Task 3.1: Endpoint Layout Algorithm
- [ ] Implement grid layout (for many endpoints)
- [ ] Implement vertical list layout (for few endpoints)
- [ ] Calculate endpoint positions within parent container
- [ ] Handle overflow (scrollable? collapse?)

#### Task 3.2: Endpoint Visual Components
- [ ] Create endpoint node SVG elements (small rectangles)
- [ ] Add HTTP method indicator (color-coded badges: GET=green, POST=blue, DELETE=red, etc.)
- [ ] Add endpoint name/path labels
- [ ] Add endpoint hover effects
- [ ] Style based on endpoint type

#### Task 3.3: Conditional Rendering by Zoom
- [ ] Show/hide endpoints based on zoom level (>= 1.5x)
- [ ] Smooth transitions when endpoints appear/disappear
- [ ] Update container sizes dynamically
- [ ] Handle force simulation restart when structure changes

---

### Phase 4: Implement Detailed Connection Rendering
**Goal**: Show endpoint-to-endpoint connections at close zoom

#### Task 4.1: Connection Data Preparation
- [ ] Parse `relations.targetEndpointIds` to find source endpoints
- [ ] Create endpoint-to-endpoint links
- [ ] Store both service-level and endpoint-level connections
- [ ] Map endpoint IDs to their parent service IDs

#### Task 4.2: Multi-level Link Rendering
- [ ] Render service-to-service links (far/medium zoom)
- [ ] Render endpoint-to-endpoint links (close zoom)
- [ ] Calculate connection points:
  - From: endpoint position (absolute = parent.x + relative.x)
  - To: target endpoint position
- [ ] Handle links crossing service boundaries

#### Task 4.3: Visual Link Differentiation
- [ ] Different stroke styles for service vs endpoint links
- [ ] Arrow markers sized appropriately per zoom
- [ ] Link labels (optional, at very close zoom)
- [ ] Curved paths for better readability

---

### Phase 5: Implement Zoom-based LOD System
**Goal**: Dynamic rendering based on zoom level

#### Task 5.1: Zoom Event Integration
- [ ] Listen to d3.zoom transform events
- [ ] Track current zoom scale in component
- [ ] Trigger LOD updates on zoom change
- [ ] Add debouncing to prevent excessive re-renders

#### Task 5.2: LOD Rendering Logic
- [ ] **Far zoom (0.1-0.5x)**:
  - Show service icons/simple shapes
  - Show service names only
  - Show simplified service-to-service links
  - Hide all endpoints
  - Hide detailed metadata

- [ ] **Medium zoom (0.5-1.5x)**:
  - Show service containers (collapsed)
  - Show endpoint count badges
  - Show service descriptions
  - Show service-to-service links
  - Hide individual endpoints

- [ ] **Close zoom (1.5-4.0x)**:
  - Show expanded service containers
  - Render all endpoint nodes inside
  - Show endpoint-to-endpoint links
  - Show all detailed labels
  - Show full metadata

#### Task 5.3: Smooth Transitions
- [ ] CSS/D3 transitions for opacity changes
- [ ] Fade in/out for elements appearing/disappearing
- [ ] Animate container size changes
- [ ] Smooth link path transitions

---

### Phase 6: Implement Interactive Focus States
**Goal**: Highlight relevant nodes and connections on click

#### Task 6.1: State Management
- [ ] Add focus state tracking:
  ```typescript
  focusState: {
    type: 'none' | 'service' | 'endpoint';
    targetId: string | null;
  }
  ```
- [ ] Add click handlers for services
- [ ] Add click handlers for endpoints
- [ ] Add click handler for canvas (reset focus)

#### Task 6.2: Service Focus Logic
- [ ] On service click:
  - Set focusState = { type: 'service', targetId: serviceId }
  - Find all connected services (via relations)
  - Fade unrelated nodes to opacity 0.2
  - Highlight focused service
  - Highlight related connections
  - Keep connected services at opacity 1.0

#### Task 6.3: Endpoint Focus Logic
- [ ] On endpoint click:
  - Set focusState = { type: 'endpoint', targetId: endpointId }
  - Find target endpoints from relations
  - Highlight focused endpoint
  - Highlight parent service container
  - Highlight target endpoint(s)
  - Highlight target service(s)
  - Highlight connection paths
  - Fade all unrelated nodes

#### Task 6.4: Visual Feedback
- [ ] Add hover effects (cursor: pointer, slight highlight)
- [ ] Add selection indicator (border, glow, or background change)
- [ ] Add tooltips with full information
- [ ] Add click feedback animation (pulse, scale)

---

### Phase 7: Polish & Optimization

#### Task 7.1: Performance Optimization
- [ ] Implement node culling (don't render off-screen nodes)
- [ ] Use D3 data binding efficiently (enter/update/exit)
- [ ] Optimize force simulation parameters
- [ ] Consider using Canvas for large graphs (fallback)
- [ ] Profile and optimize re-render triggers

#### Task 7.2: Visual Polish
- [ ] Color scheme refinement
- [ ] Typography improvements
- [ ] Icon additions (service types, endpoint methods)
- [ ] Shadow/depth effects for hierarchy
- [ ] Smooth animations throughout

#### Task 7.3: Accessibility
- [ ] Keyboard navigation support
- [ ] Screen reader labels
- [ ] High contrast mode support
- [ ] Focus indicators for keyboard users

#### Task 7.4: Responsive Design
- [ ] Handle window resize gracefully
- [ ] Adjust layout for different screen sizes
- [ ] Mobile touch support (if needed)
- [ ] Test on different resolutions

---

## Technical Decisions & Notes

### Force Simulation Considerations

**Hierarchical Force Layout:**
- Use `d3.forceSimulation()` for service nodes
- Services repel each other (charge force)
- Links between services create attraction
- Endpoints positioned **statically** relative to parent (no separate simulation)

**Collision Detection:**
- Services use rectangular collision (`d3.forceCollide()` with custom radius function)
- Calculate collision radius based on container size: `Math.max(width, height) / 2`

### Layout Algorithms

**Endpoint Grid Layout:**
```
┌─────────────────────┐
│  Service            │
│  ┌────┐ ┌────┐     │
│  │ EP1│ │ EP2│     │
│  └────┘ └────┘     │
│  ┌────┐ ┌────┐     │
│  │ EP3│ │ EP4│     │
│  └────┘ └────┘     │
└─────────────────────┘
```
- Grid with 2-3 columns
- Calculate rows based on endpoint count
- Fixed endpoint size (e.g., 80x40 px)
- Padding between endpoints

**Endpoint Vertical List:**
```
┌─────────────────────┐
│  Service            │
│  ┌────────────────┐ │
│  │ Endpoint 1     │ │
│  └────────────────┘ │
│  ┌────────────────┐ │
│  │ Endpoint 2     │ │
│  └────────────────┘ │
└─────────────────────┘
```
- Single column
- Full-width endpoint cards
- Better for few endpoints with long names

### Connection Routing

**Service-to-Service Links:**
- Connect center of service containers
- Curved paths (d3.linkHorizontal or custom bezier)
- Arrow at target edge

**Endpoint-to-Endpoint Links:**
- Connect specific endpoints
- Calculate absolute positions: `parentService.x + endpoint.relativeX`
- Ensure links visually exit/enter service containers cleanly
- Possible to use different line styles (dashed, colored by type)

### Color Coding

**Services:**
- Default: `#4A90E2` (blue)
- Focused: `#2E7DD2` (darker blue)
- Unfocused: `#4A90E2` with opacity 0.2

**Endpoints by HTTP Method:**
- GET: `#50C878` (green)
- POST: `#4A90E2` (blue)
- PUT: `#FFA500` (orange)
- PATCH: `#FFD700` (gold)
- DELETE: `#E74C3C` (red)
- Other: `#95A5A6` (gray)

**Links:**
- Default: `#666` opacity 0.6
- Focused: `#333` opacity 0.8
- Unfocused: `#666` opacity 0.1

---

## Example Component Structure (After Implementation)

```typescript
export class GraphCanvasComponent {
  // LOD
  private lodManager: LODManager;
  private currentZoomLevel: number = 1;

  // Focus state
  private focusState = { type: 'none', targetId: null };

  // D3 elements
  private svg: any;
  private g: any;
  private serviceGroups: any;
  private endpointGroups: any;
  private links: any;
  private simulation: any;

  // Methods
  private initSvg(): void { ... }
  private setupZoomBehavior(): void { ... }
  private onZoomChanged(transform: any): void { ... }

  private renderGraph(): void { ... }
  private renderServices(nodes: GraphNode[]): void { ... }
  private renderEndpoints(node: GraphNode): void { ... }
  private renderLinks(links: GraphLink[]): void { ... }

  private updateLOD(zoomScale: number): void { ... }
  private applyFocusState(): void { ... }

  private onServiceClick(node: GraphNode): void { ... }
  private onEndpointClick(endpoint: EndpointNode): void { ... }
  private onCanvasClick(): void { ... }

  private calculateServiceSize(node: GraphNode): { width: number, height: number } { ... }
  private calculateEndpointLayout(endpoints: EndpointNode[]): void { ... }
}
```

---

## Testing Strategy

### Visual Testing
- [ ] Test at all zoom levels (0.1x, 0.5x, 1x, 1.5x, 2x, 4x)
- [ ] Test with 1, 5, 10, 20+ services
- [ ] Test with services having 1-50 endpoints
- [ ] Test complex connection graphs

### Interaction Testing
- [ ] Click on services at different zoom levels
- [ ] Click on endpoints at close zoom
- [ ] Click on canvas to deselect
- [ ] Pan and zoom gestures
- [ ] Hover effects

### Performance Testing
- [ ] Measure FPS during zoom/pan
- [ ] Test with large datasets (100+ services)
- [ ] Profile rendering bottlenecks
- [ ] Test memory usage over time

---

## Future Enhancements (Post-MVP)

- [ ] Use Cases visualization mode
  - Timeline view of use case steps
  - Path highlighting on graph
  - Playback/step-through animation

- [ ] Search and filter
  - Search services by name
  - Filter by endpoint type
  - Filter by last analyzed date

- [ ] Minimap for navigation
- [ ] Graph layout persistence (save positions)
- [ ] Multiple layout algorithms (hierarchical, circular, force-directed)
- [ ] Export to image/PDF
- [ ] Collaborative features (multi-user viewing)

---

## Questions & Decisions Log

### Open Questions
- Should we support more than 2 levels of hierarchy? (e.g., Service > Module > Endpoint)
- How to handle very large services with 100+ endpoints?
- Should endpoints be draggable within their parent service?
- Do we need a search/filter UI component, or is zoom/pan sufficient?

### Resolved Decisions
- ✅ Use zoom-based LOD instead of mode toggles
- ✅ Render endpoints inside parent services (not as separate force nodes)
- ✅ Support both service-level and endpoint-level focus
- ✅ Defer Use Cases visualization to post-MVP

---

## References & Inspiration

- D3.js Force Layout: https://d3js.org/d3-force
- D3.js Zoom Behavior: https://d3js.org/d3-zoom
- Hierarchical Graph Layouts: https://en.wikipedia.org/wiki/Hierarchical_graph_drawing
- Level of Detail Rendering: https://en.wikipedia.org/wiki/Level_of_detail_(computer_graphics)

---

**Document Version**: 1.0
**Last Updated**: 2025-10-07
**Status**: Planning Phase
