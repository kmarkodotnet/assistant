import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import type { PreferencesDto } from '../models/preferences.dto';

@Injectable({ providedIn: 'root' })
export class PreferencesApiService {
  private http = inject(HttpClient);

  get(): Observable<PreferencesDto> {
    return this.http.get<PreferencesDto>('/api/v1/auth/me');
  }

  patch(dto: Partial<PreferencesDto>): Observable<void> {
    return this.http.patch<void>('/api/v1/auth/me/preferences', dto);
  }
}
