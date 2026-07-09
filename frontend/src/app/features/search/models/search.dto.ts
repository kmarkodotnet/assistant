export type SearchMode = 'Auto' | 'Filter' | 'Text' | 'Semantic' | 'Qa';

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
}

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
}
