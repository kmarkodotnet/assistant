import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface SuggestionsAggregateDto {
  tasks: SuggestedTaskDto[];
  deadlines: SuggestedDeadlineDto[];
  tags: DocumentTagSuggestionDto[];
  topics: DocumentTopicSuggestionDto[];
  totalCount: number;
}

export interface SuggestedTaskDto {
  id: string;
  title: string;
  dueDateUtc: string | null;
  status: string;
  priority: string;
  origin: string;
  assignedToFamilyMemberId: string | null;
  createdUtc: string;
}

export interface SuggestedDeadlineDto {
  id: string;
  title: string;
  dueDateUtc: string;
  status: string;
  category: string;
  origin: string;
  relatedFamilyMemberId: string | null;
  createdUtc: string;
}

export interface DocumentTagSuggestionDto {
  documentId: string;
  tagId: string;
  tagName: string;
}

export interface DocumentTopicSuggestionDto {
  documentId: string;
  topicId: string;
  topicName: string;
  topicSlug: string;
}

export interface BatchApproveResult {
  approved: number;
  rejected: number;
  errors: string[];
}

@Injectable({ providedIn: 'root' })
export class SuggestionsService {
  private http = inject(HttpClient);

  async getAll(): Promise<SuggestionsAggregateDto> {
    return firstValueFrom(this.http.get<SuggestionsAggregateDto>('/api/v1/suggestions'));
  }

  async batch(body: {
    approve?: { tasks?: string[]; deadlines?: string[]; tags?: Array<{ documentId: string; tagId: string }>; topics?: Array<{ documentId: string; topicId: string }> };
    reject?: { tasks?: string[]; deadlines?: string[]; tags?: Array<{ documentId: string; tagId: string }>; topics?: Array<{ documentId: string; topicId: string }> };
  }): Promise<BatchApproveResult> {
    return firstValueFrom(this.http.post<BatchApproveResult>('/api/v1/suggestions/batch', body));
  }
}
