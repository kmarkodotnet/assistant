import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type {
  SearchRequest,
  SearchResponse,
  SavedSearchDto,
} from '../models/search.dto';

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/search';

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
}
