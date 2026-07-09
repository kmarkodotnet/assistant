import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SearchApiService } from './search.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import type {
  SearchRequest,
  SearchResponse,
  SavedSearchDto,
  SearchEntry,
} from '../models/search.dto';

@Injectable({ providedIn: 'root' })
export class SearchFacade {
  private api = inject(SearchApiService);
  private notify = inject(NotificationService);

  readonly history = signal<SearchEntry[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly savedSearches = signal<SavedSearchDto[]>([]);

  async ask(query: SearchRequest): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response: SearchResponse = await firstValueFrom(this.api.search(query));
      this.history.update(h => [...h, { query, response, timestamp: new Date() }]);
    } catch {
      this.error.set('Nem sikerült végrehajtani a keresést.');
      this.notify.error('Nem sikerült végrehajtani a keresést.');
    } finally {
      this.loading.set(false);
    }
  }

  clearHistory(): void {
    this.history.set([]);
  }

  loadSaved(): void {
    this.api.getSaved().subscribe({
      next: saved => this.savedSearches.set(saved),
      error: () => this.notify.error('Nem sikerült betölteni a mentett kereséseket.'),
    });
  }

  async saveCurrentSearch(name: string, query: SearchRequest): Promise<void> {
    try {
      const saved = await firstValueFrom(this.api.saveSearch(name, query));
      this.savedSearches.update(s => [...s, saved]);
      this.notify.success('Keresés elmentve.');
    } catch {
      this.notify.error('Nem sikerült menteni a keresést.');
    }
  }

  async deleteSaved(id: string): Promise<void> {
    try {
      await firstValueFrom(this.api.deleteSaved(id));
      this.savedSearches.update(s => s.filter(x => x.id !== id));
      this.notify.success('Mentett keresés törölve.');
    } catch {
      this.notify.error('Nem sikerült törölni a mentett keresést.');
    }
  }
}
