export type SearchMode = 'Auto' | 'Filter' | 'Text' | 'Semantic' | 'Qa' | 'Command';

export interface SearchRequest {
  query: string;
  mode: SearchMode;
  entityTypes?: string[];
  topicSlugs?: string[];
  tagNames?: string[];
  from?: string;
  to?: string;
  relatedFamilyMemberId?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchHit {
  entityType: string;
  entityId: string;
  title: string;
  snippet?: string;
  score: number;
  metadata?: Record<string, unknown>;
}

export interface SearchResponse {
  hits: SearchHit[];
  totalCount: number;
  modeUsed: SearchMode;
  answer?: string;
  answerSources?: string[];
  confidence?: number;
  toolCallProposal?: ToolCallProposal | null;
}

/** Egy whitelistelt tool-hívási javaslat megjelenítéshez (api-design.md §16.1). */
export interface ToolCallProposal {
  proposalToken: string;
  toolName: string;
  summary: string;
  parameters: ToolParamDisplay[];
  warnings: string[];
  expiresUtc: string;
}

export interface ToolParamDisplay {
  label: string;
  value: string;
}

/** A `/tool-calls/confirm` sikeres válasza (api-design.md §16.3.1). */
export interface ToolCallResult {
  executed: boolean;
  resultType: string;
  resultId: string;
  summary: string;
}

/** A megerősítő kártya kliensoldali állapota — a §16.3 flow-t tükrözi. */
export type ToolCallStatus = 'pending' | 'executing' | 'executed' | 'rejected';

export interface SavedSearchDto {
  id: string;
  name: string;
  queryJson: string;
  createdUtc: string;
}

export interface SearchEntry {
  query: SearchRequest;
  response: SearchResponse;
  timestamp: Date;
  /** Csak akkor releváns, ha response.toolCallProposal jelen van. */
  toolCallStatus?: ToolCallStatus;
  toolCallResult?: ToolCallResult;
}
