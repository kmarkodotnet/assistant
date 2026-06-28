import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ReminderDto {
  id: string;
  taskId?: string;
  deadlineId?: string;
  targetUserAccountId: string;
  channel: string;
  status: string;
  triggerUtc: string;
  firedUtc?: string;
  acknowledgedUtc?: string;
  rruleExpression?: string;
  escalationLevel: number;
  snoozeNote?: string;
  createdByUserAccountId: string;
  createdUtc: string;
  updatedUtc: string;
}

export interface ReminderGroupDto {
  now: ReminderDto[];
  week: ReminderDto[];
  later: ReminderDto[];
  missed: ReminderDto[];
}

export interface CreateReminderRequest {
  taskId?: string;
  deadlineId?: string;
  targetUserAccountId?: string;
  channel?: string;
  triggerUtc: string;
  rruleExpression?: string;
}

@Injectable({ providedIn: 'root' })
export class RemindersApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/reminders';

  list(upcoming = false, status?: string): Observable<ReminderGroupDto> {
    const params: Record<string, string> = { upcoming: upcoming.toString() };
    if (status) params['status'] = status;
    return this.http.get<ReminderGroupDto>(this.base, { params });
  }

  create(req: CreateReminderRequest): Observable<ReminderDto> {
    return this.http.post<ReminderDto>(this.base, req);
  }

  acknowledge(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/acknowledge`, {});
  }

  snooze(id: string, snoozeMinutes: number): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/snooze`, { snoozeMinutes });
  }

  skip(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/skip`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
