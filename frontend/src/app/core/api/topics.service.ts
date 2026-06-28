import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface TopicDto {
  id: string;
  name: string;
  slug: string;
  parentId: string | null;
  icon: string | null;
  sortOrder: number;
  createdUtc: string;
  children: TopicDto[];
}

@Injectable({ providedIn: 'root' })
export class TopicsService {
  private http = inject(HttpClient);

  async list(flat = false): Promise<TopicDto[]> {
    return firstValueFrom(this.http.get<TopicDto[]>(`/api/v1/topics?flat=${flat}`));
  }

  async create(body: { name: string; slug: string; parentId?: string; icon?: string; sortOrder?: number }): Promise<TopicDto> {
    return firstValueFrom(this.http.post<TopicDto>('/api/v1/topics', body));
  }

  async patch(id: string, patch: { name?: string; icon?: string; sortOrder?: number }): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`/api/v1/topics/${id}`, patch));
  }

  async delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/topics/${id}`));
  }
}
