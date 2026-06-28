import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SmtpSettingsDto {
  host: string | null;
  port: number | null;
  from: string | null;
}

export interface SystemSettingsDto {
  privacyMode: string;
  auditRetentionDays: number | null;
  notificationFeedRetentionDays: number | null;
  smtp: SmtpSettingsDto | null;
}

@Injectable({ providedIn: 'root' })
export class SystemSettingsApiService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/settings/system';

  get(): Observable<SystemSettingsDto> {
    return this.http.get<SystemSettingsDto>(this.base);
  }

  patch(patch: Partial<SystemSettingsDto>): Observable<void> {
    return this.http.patch<void>(this.base, patch);
  }
}
