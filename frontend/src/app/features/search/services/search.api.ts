import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type {
  SearchRequest,
  SearchResponse,
  SavedSearchDto,
  ToolCallResult,
} from '../models/search.dto';

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/search';
  private toolCallsBase = '/api/v1/tool-calls';

  search(req: SearchRequest): Observable<SearchResponse> {
    return this.http.post<SearchResponse>(this.base, req);
  }

  getSaved(): Observable<SavedSearchDto[]> {
    return this.http.get<SavedSearchDto[]>(`${this.base}/saved`);
  }

  saveSearch(name: string, query: SearchRequest): Observable<SavedSearchDto> {
    return this.http.post<SavedSearchDto>(`${this.base}/saved`, { name, query });
  }

  deleteSaved(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/saved/${id}`);
  }

  /** api-design.md §16.3.1 — a javaslat végrehajtása. */
  confirmToolCall(proposalToken: string): Observable<ToolCallResult> {
    return this.http.post<ToolCallResult>(`${this.toolCallsBase}/confirm`, { proposalToken });
  }

  /** api-design.md §16.3.2 — a javaslat elvetése, nulla adatváltozással. */
  rejectToolCall(proposalToken: string, reason?: string): Observable<void> {
    return this.http.post<void>(`${this.toolCallsBase}/reject`, { proposalToken, reason });
  }
}
