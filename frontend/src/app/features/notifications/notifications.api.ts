import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  body?: string;
  actionUrl?: string;
  readUtc?: string;
  createdUtc: string;
}

export interface NotificationFeedResponse {
  items: NotificationDto[];
  totalCount: number;
  hasMore: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/notifications';

  getUnreadCount(): Observable<NotificationFeedResponse> {
    return this.http.get<NotificationFeedResponse>(this.base, {
      params: { onlyUnread: 'true', pageSize: '1' },
    });
  }

  getFeed(onlyUnread = false, page = 1, pageSize = 50): Observable<NotificationFeedResponse> {
    return this.http.get<NotificationFeedResponse>(this.base, {
      params: { onlyUnread: onlyUnread.toString(), page: page.toString(), pageSize: pageSize.toString() },
    });
  }

  markRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/read`, {});
  }

  markAllRead(): Observable<void> {
    return this.http.post<void>(`${this.base}/read-all`, {});
  }
}
