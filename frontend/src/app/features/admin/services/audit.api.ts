import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AuditLogDto {
  id: string;
  occurredUtc: string;
  userAccountId: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  detailsJson: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuditFilter {
  from?: string;
  to?: string;
  userAccountId?: string;
  action?: string;
  entityType?: string;
  entityId?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/audit-log';

  list(filter: AuditFilter = {}): Observable<PagedResult<AuditLogDto>> {
    let params = new HttpParams();
    Object.entries(filter).forEach(([k, v]) => {
      if (v != null) params = params.set(k, String(v));
    });
    return this.http.get<PagedResult<AuditLogDto>>(this.base, { params });
  }

  securityEvents(): Observable<AuditLogDto[]> {
    return this.http.get<AuditLogDto[]>(`${this.base}/security-events`);
  }

  exportUrl(from?: string, to?: string, format: 'csv' | 'json' = 'csv'): string {
    let url = `${this.base}/export?format=${format}`;
    if (from) url += `&from=${from}`;
    if (to) url += `&to=${to}`;
    return url;
  }
}
