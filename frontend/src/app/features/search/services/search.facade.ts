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

function findPendingEntry(
  history: SearchEntry[],
  proposalToken: string,
): SearchEntry | undefined {
  return history.find(
    e =>
      e.response.toolCallProposal?.proposalToken === proposalToken &&
      (e.toolCallStatus ?? 'pending') === 'pending',
  );
}

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
      const entry: SearchEntry = {
        query,
        response,
        timestamp: new Date(),
        ...(response.toolCallProposal ? { toolCallStatus: 'pending' as const } : {}),
      };
      this.history.update(h => [...h, entry]);
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

  /** api-design.md §16.3.1 — a javaslat végrehajtása jóváhagyás után. */
  async confirmToolCall(proposalToken: string): Promise<void> {
    if (!findPendingEntry(this.history(), proposalToken)) return;

    this.history.update(h =>
      h.map(e =>
        e.response.toolCallProposal?.proposalToken === proposalToken
          ? { ...e, toolCallStatus: 'executing' }
          : e,
      ),
    );

    try {
      const result = await firstValueFrom(this.api.confirmToolCall(proposalToken));
      this.history.update(h =>
        h.map(e =>
          e.response.toolCallProposal?.proposalToken === proposalToken
            ? { ...e, toolCallStatus: 'executed', toolCallResult: result }
            : e,
        ),
      );
    } catch {
      // Hiba esetén nulla módosítás történt a backenden — a kártya visszaáll
      // pending állapotba, hogy a felhasználó újrapróbálhassa vagy elutasíthassa.
      this.history.update(h =>
        h.map(e =>
          e.response.toolCallProposal?.proposalToken === proposalToken
            ? { ...e, toolCallStatus: 'pending' }
            : e,
        ),
      );
      this.notify.error('Nem sikerült végrehajtani a parancsot.');
    }
  }

  /** api-design.md §16.3.2 — a javaslat elvetése, nulla adatváltozással. */
  async rejectToolCall(proposalToken: string, reason?: string): Promise<void> {
    if (!findPendingEntry(this.history(), proposalToken)) return;

    this.history.update(h =>
      h.map(e =>
        e.response.toolCallProposal?.proposalToken === proposalToken
          ? { ...e, toolCallStatus: 'executing' }
          : e,
      ),
    );

    try {
      await firstValueFrom(this.api.rejectToolCall(proposalToken, reason));
      this.history.update(h =>
        h.map(e =>
          e.response.toolCallProposal?.proposalToken === proposalToken
            ? { ...e, toolCallStatus: 'rejected' }
            : e,
        ),
      );
    } catch {
      this.history.update(h =>
        h.map(e =>
          e.response.toolCallProposal?.proposalToken === proposalToken
            ? { ...e, toolCallStatus: 'pending' }
            : e,
        ),
      );
      this.notify.error('Nem sikerült elutasítani a parancsot.');
    }
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
