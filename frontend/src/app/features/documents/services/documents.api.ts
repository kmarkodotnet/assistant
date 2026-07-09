import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent, HttpHeaders, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import type { DocumentDto, DocumentDetailDto, DocumentListResponse, DocumentTextDto } from '../models/document.dto';
import type { DocumentFilter } from '../models/document-filter.model';

export interface ClassificationTagDto {
  id: string;
  name: string;
  color: string | null;
  origin: string;
  isApproved: boolean;
}

export interface ClassificationTopicDto {
  id: string;
  name: string;
  slug: string;
  icon: string | null;
  origin: string;
  isApproved: boolean;
}

export interface DocumentClassificationDto {
  tags: ClassificationTagDto[];
  topics: ClassificationTopicDto[];
}

@Injectable({ providedIn: 'root' })
export class DocumentsApiService {
  private http = inject(HttpClient);
  private base = '/api/v1/documents';

  list(filter: DocumentFilter): Observable<DocumentListResponse> {
    const params: Record<string, string> = {
      page: filter.page.toString(),
      pageSize: filter.pageSize.toString(),
    };
    if (filter.relatedFamilyMemberId) params['relatedFamilyMemberId'] = filter.relatedFamilyMemberId;
    if (filter.processingStatus) params['processingStatus'] = filter.processingStatus;
    return this.http.get<DocumentListResponse>(this.base, { params });
  }

  get(id: string): Observable<DocumentDetailDto> {
    return this.http.get<DocumentDetailDto>(`${this.base}/${id}`);
  }

  getText(id: string): Observable<DocumentTextDto> {
    return this.http.get<DocumentTextDto>(`${this.base}/${id}/text`);
  }

  upload(formData: FormData, idempotencyKey: string): Observable<HttpEvent<DocumentDto>> {
    const req = new HttpRequest('POST', this.base, formData, {
      reportProgress: true,
      headers: new HttpHeaders({ 'Idempotency-Key': idempotencyKey }),
    });
    return this.http.request<DocumentDto>(req);
  }

  updateText(id: string, content: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/text`, { content });
  }

  patch(id: string, data: { title?: string; documentDate?: string; isPrivate?: boolean }, rowVersion?: string): Observable<void> {
    const headers: Record<string, string> = {};
    if (rowVersion) headers['If-Match'] = rowVersion;
    return this.http.patch<void>(`${this.base}/${id}`, data, { headers });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  reprocess(id: string, jobs: string[]): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/reprocess`, { jobs });
  }

  getClassification(id: string): Observable<DocumentClassificationDto> {
    return this.http.get<DocumentClassificationDto>(`${this.base}/${id}/classification`);
  }

  addTopic(id: string, topicId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${id}/topics`, { topicId });
  }

  removeTopic(id: string, topicId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/topics/${topicId}`);
  }
}
