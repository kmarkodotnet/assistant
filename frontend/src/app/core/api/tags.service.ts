import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface TagDto {
  id: string;
  name: string;
  color: string | null;
  usageCount: number;
  createdUtc: string;
}

@Injectable({ providedIn: 'root' })
export class TagsService {
  private http = inject(HttpClient);

  async list(params?: { q?: string; sort?: string; page?: number; pageSize?: number }): Promise<TagDto[]> {
    const p = new URLSearchParams();
    if (params?.q) p.set('q', params.q);
    if (params?.sort) p.set('sort', params.sort);
    if (params?.page) p.set('page', String(params.page));
    if (params?.pageSize) p.set('pageSize', String(params.pageSize));
    const qs = p.toString();
    return firstValueFrom(this.http.get<TagDto[]>(`/api/v1/tags${qs ? '?' + qs : ''}`));
  }

  async create(name: string, color?: string): Promise<TagDto> {
    return firstValueFrom(this.http.post<TagDto>('/api/v1/tags', { name, color }));
  }

  async patch(id: string, patch: { name?: string; color?: string }): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`/api/v1/tags/${id}`, patch));
  }

  async delete(id: string, force = false): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/tags/${id}?force=${force}`));
  }
}
