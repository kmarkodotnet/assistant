import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AiProviderDto {
  name: string;
  enabled: boolean;
  model: string | null;
  lastHealth: string | null;
}

@Injectable({ providedIn: 'root' })
export class AiProvidersApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/ai-providers';

  list(): Observable<AiProviderDto[]> {
    return this.http.get<AiProviderDto[]>(this.base);
  }

  patch(name: string, patch: { enabled?: boolean; model?: string }): Observable<void> {
    return this.http.patch<void>(`${this.base}/${name}`, patch);
  }
}
