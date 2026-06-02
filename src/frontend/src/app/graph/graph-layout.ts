/** Shared layout tokens for the graph-first redesign. */
export const SERVICE_NODE_WIDTH = 360;
export const SERVICE_NODE_HEIGHT = 176;
export const SERVICE_NODE_RADIUS = 14;

export const GRAPH_FIT_PADDING = 72;
export const GRAPH_MIN_READABLE_SCALE = 0.78;
export const GRAPH_MAX_INITIAL_SCALE = 1.1;
export const DETAILS_PANEL_WIDTH = 420;

// Kept for disconnected legacy graph-card components until they are removed.
export const SERVICE_CARD_WIDTH = SERVICE_NODE_WIDTH;
export const SERVICE_CARD_HEIGHT = 380;
export const NESTED_CARD_WIDTH = 280;
export const NESTED_CARD_HEIGHT_COMPACT = 72;
export const NESTED_CARD_HEIGHT_FULL = 128;

export function formatLastAnalyzed(iso: string | null | undefined): string | null {
  if (!iso) return null;
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return null;
  return date.toLocaleString('en-US', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

export function endpointTypeLabel(type: string): string {
  const normalized = type?.toLowerCase() ?? '';
  switch (normalized) {
    case 'http': return 'HTTP';
    case 'queue': return 'Queue';
    case 'job': return 'Job';
    default: return type || 'Unknown type';
  }
}
