import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AiJobDto {
  id: string;
  jobType: string;
  targetEntityType: string;
  targetEntityId: string;
  status: string;
  attemptCount: number;
  errorMessage: string | null;
  startedUtc: string | null;
  finishedUtc: string | null;
  createdUtc: string;
}

export interface QueueStatEntry {
  jobType: string;
  status: string;
  count: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class AiJobsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/ai-jobs';

  list(status?: string, jobType?: string, page = 1, pageSize = 50): Observable<PagedResult<AiJobDto>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (jobType) params = params.set('jobType', jobType);
    return this.http.get<PagedResult<AiJobDto>>(this.base, { params });
  }

  retry(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/retry`, {});
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/cancel`, {});
  }

  queueStats(): Observable<QueueStatEntry[]> {
    return this.http.get<QueueStatEntry[]>(`${this.base}/queue-stats`);
  }
}
