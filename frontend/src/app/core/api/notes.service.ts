import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface NoteListItemDto {
  id: string;
  title: string;
  relatedFamilyMemberId: string | null;
  createdByUserAccountId: string;
  isPrivate: boolean;
  createdUtc: string;
  updatedUtc: string;
}

export interface NoteDto extends NoteListItemDto {
  body: string;
}

@Injectable({ providedIn: 'root' })
export class NotesService {
  private http = inject(HttpClient);

  async list(params?: { topicSlug?: string; tagId?: string; page?: number }): Promise<NoteListItemDto[]> {
    const p = new URLSearchParams();
    if (params?.topicSlug) p.set('topicSlug', params.topicSlug);
    if (params?.tagId) p.set('tagId', params.tagId);
    if (params?.page) p.set('page', String(params.page));
    p.set('includeBody', 'false');
    const qs = p.toString();
    return firstValueFrom(this.http.get<NoteListItemDto[]>(`/api/v1/notes?${qs}`));
  }

  async get(id: string): Promise<NoteDto> {
    return firstValueFrom(this.http.get<NoteDto>(`/api/v1/notes/${id}`));
  }

  async create(body: { title: string; body: string; isPrivate?: boolean }): Promise<NoteDto> {
    return firstValueFrom(this.http.post<NoteDto>('/api/v1/notes', body));
  }

  async patch(id: string, patch: { title?: string; body?: string; isPrivate?: boolean }): Promise<void> {
    return firstValueFrom(this.http.patch<void>(`/api/v1/notes/${id}`, patch));
  }

  async delete(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/notes/${id}`));
  }

  async getRendered(id: string): Promise<{ html: string }> {
    return firstValueFrom(this.http.get<{ html: string }>(`/api/v1/notes/${id}/rendered`));
  }
}
