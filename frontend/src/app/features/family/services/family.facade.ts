import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { createStore } from '../../../core/state/create-store';
import { FamilyApiService } from './family.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import type { FamilyMemberDto, CreateFamilyMemberDto, UpdateFamilyMemberDto } from '../models/family-member.dto';

interface FamilyState {
  members: FamilyMemberDto[];
  loading: boolean;
  error: string | null;
}

@Injectable({ providedIn: 'root' })
export class FamilyFacade {
  private api = inject(FamilyApiService);
  private notify = inject(NotificationService);
  private store = createStore<FamilyState>({ members: [], loading: false, error: null });

  readonly members = this.store.select(s => s.members);
  readonly loading = this.store.select(s => s.loading);
  readonly error = this.store.select(s => s.error);

  async load(): Promise<void> {
    this.store.update({ loading: true, error: null });
    try {
      const members = await firstValueFrom(this.api.list());
      this.store.update({ members, loading: false });
    } catch (e) {
      this.store.update({ loading: false, error: 'Nem sikerült betölteni a családtagokat.' });
    }
  }

  async create(dto: CreateFamilyMemberDto): Promise<boolean> {
    try {
      await firstValueFrom(this.api.create(dto));
      await this.load();
      this.notify.success('Családtag sikeresen létrehozva.');
      return true;
    } catch {
      this.notify.error('Nem sikerült létrehozni a családtagot.');
      return false;
    }
  }

  async update(id: string, dto: UpdateFamilyMemberDto): Promise<boolean> {
    try {
      await firstValueFrom(this.api.update(id, dto));
      await this.load();
      this.notify.success('Családtag sikeresen frissítve.');
      return true;
    } catch {
      this.notify.error('Nem sikerült frissíteni a családtagot.');
      return false;
    }
  }

  async softDelete(id: string): Promise<boolean> {
    try {
      await firstValueFrom(this.api.delete(id));
      await this.load();
      this.notify.success('Családtag törölve.');
      return true;
    } catch (e: unknown) {
      const err = e as { code?: string; message?: string };
      if (err?.code?.includes('conflict')) {
        this.notify.error('Ennek a családtagnak van élő felhasználói fiókja. Előbb deaktiváld.', true);
      } else {
        this.notify.error('Nem sikerült törölni a családtagot.');
      }
      return false;
    }
  }
}
