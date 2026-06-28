import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface DashboardDto {
  upcomingDeadlines: DeadlineListItem[];
  overdueReminders: DeadlineListItem[];
  pendingSuggestions: SuggestionsCount;
  recentDocuments: RecentDocument[];
  savedSearches: SavedSearch[];
}

export interface DeadlineListItem {
  id: string;
  title: string;
  dueDateUtc: string;
  status: string;
  category: string;
  origin: string;
  relatedFamilyMemberId: string | null;
  createdUtc: string;
}

export interface SuggestionsCount {
  tasks: number;
  deadlines: number;
  tags: number;
  topics: number;
  total: number;
}

export interface RecentDocument {
  id: string;
  title: string;
  originalFileName: string;
  mimeType: string;
  createdUtc: string;
}

export interface SavedSearch {
  id: string;
  name: string;
  queryJson: string;
  createdUtc: string;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  async get(): Promise<DashboardDto> {
    return firstValueFrom(this.http.get<DashboardDto>('/api/v1/dashboard'));
  }
}
