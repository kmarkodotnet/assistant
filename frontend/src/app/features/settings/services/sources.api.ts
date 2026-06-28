import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SourceDto {
  id: string;
  name: string;
  kind: string;
  isActive: boolean;
  lastSyncUtc: string | null;
}

@Injectable({ providedIn: 'root' })
export class SourcesApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/sources';

  list(): Observable<SourceDto[]> {
    return this.http.get<SourceDto[]>(this.base);
  }

  connectGmail(): Observable<{ redirectUrl: string }> {
    return this.http.post<{ redirectUrl: string }>(`${this.base}/gmail/connect`, {});
  }

  sync(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/sync`, {});
  }

  disconnect(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
