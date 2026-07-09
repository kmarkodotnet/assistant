import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type {
  DeadlineListItemDto,
  DeadlineDto,
  DeadlineListParams,
  CreateDeadlineRequest,
  PatchDeadlineRequest,
} from '../models/deadline.dto';

@Injectable({ providedIn: 'root' })
export class DeadlinesApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/deadlines';

  list(params?: DeadlineListParams): Observable<DeadlineListItemDto[]> {
    const query: Record<string, string> = {};
    if (params?.from) query['from'] = params.from;
    if (params?.to) query['to'] = params.to;
    if (params?.category) query['category'] = params.category;
    if (params?.status) query['status'] = params.status;
    if (params?.page != null) query['page'] = String(params.page);
    if (params?.pageSize != null) query['pageSize'] = String(params.pageSize);
    return this.http.get<DeadlineListItemDto[]>(this.base, { params: query });
  }

  get(id: string): Observable<DeadlineDto> {
    return this.http.get<DeadlineDto>(`${this.base}/${id}`);
  }

  create(req: CreateDeadlineRequest): Observable<DeadlineDto> {
    return this.http.post<DeadlineDto>(this.base, req);
  }

  patch(id: string, req: PatchDeadlineRequest): Observable<DeadlineDto> {
    return this.http.patch<DeadlineDto>(`${this.base}/${id}`, req);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  approve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/approve`, {});
  }

  resolve(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/resolve`, {});
  }

  dismiss(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/dismiss`, {});
  }
}
