import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { createStore } from '../../../core/state/create-store';
import { DeadlinesApiService } from './deadlines.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import type {
  DeadlineListItemDto,
  DeadlineListParams,
  CreateDeadlineRequest,
  PatchDeadlineRequest,
} from '../models/deadline.dto';

interface DeadlinesState {
  items: DeadlineListItemDto[];
  loading: boolean;
  error: string | null;
  actingId: string | null;
}

@Injectable({ providedIn: 'root' })
export class DeadlinesFacade {
  private api = inject(DeadlinesApiService);
  private notify = inject(NotificationService);
  private store = createStore<DeadlinesState>({
    items: [],
    loading: false,
    error: null,
    actingId: null,
  });

  readonly items = this.store.select(s => s.items);
  readonly loading = this.store.select(s => s.loading);
  readonly error = this.store.select(s => s.error);
  readonly actingId = this.store.select(s => s.actingId);

  readonly upcoming = this.store.select(s =>
    s.items.filter(d => d.status === 'Upcoming' || d.status === 'Due')
  );
  readonly passed = this.store.select(s =>
    s.items.filter(d => d.status === 'Passed')
  );
  readonly resolved = this.store.select(s =>
    s.items.filter(d => d.status === 'Resolved' || d.status === 'Dismissed')
  );

  async load(params?: DeadlineListParams): Promise<void> {
    this.store.update({ loading: true, error: null });
    try {
      const items = await firstValueFrom(this.api.list(params));
      this.store.update({ items, loading: false });
    } catch {
      this.store.update({ loading: false, error: 'Nem sikerült betölteni a határidőket.' });
    }
  }

  async create(req: CreateDeadlineRequest, onDone?: () => void): Promise<void> {
    try {
      await firstValueFrom(this.api.create(req));
      this.notify.success('Határidő sikeresen létrehozva.');
      await this.load();
      onDone?.();
    } catch {
      this.notify.error('Nem sikerült létrehozni a határidőt.');
    }
  }

  async patch(id: string, req: PatchDeadlineRequest, onDone?: () => void): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.patch(id, req));
      this.notify.success('Határidő sikeresen frissítve.');
      await this.load();
      onDone?.();
    } catch {
      this.notify.error('Nem sikerült frissíteni a határidőt.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async delete(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.delete(id));
      this.notify.success('Határidő törölve.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült törölni a határidőt.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async approve(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.approve(id));
      this.notify.success('Határidő jóváhagyva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült jóváhagyni a határidőt.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async resolve(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.resolve(id));
      this.notify.success('Határidő megoldva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült megoldottként jelölni a határidőt.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async dismiss(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.dismiss(id));
      this.notify.info('Határidő mellőzve.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült mellőzni a határidőt.');
    } finally {
      this.store.update({ actingId: null });
    }
  }
}
