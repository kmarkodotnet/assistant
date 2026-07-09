import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { createStore } from '../../../core/state/create-store';
import { TasksApiService } from './tasks.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import type {
  TaskListItemDto,
  TaskListParams,
  CreateTaskRequest,
  PatchTaskRequest,
} from '../models/task.dto';

interface TasksState {
  tasks: TaskListItemDto[];
  loading: boolean;
  error: string | null;
  actingId: string | null;
}

@Injectable({ providedIn: 'root' })
export class TasksFacade {
  private api = inject(TasksApiService);
  private notify = inject(NotificationService);
  private store = createStore<TasksState>({
    tasks: [],
    loading: false,
    error: null,
    actingId: null,
  });

  readonly tasks = this.store.select(s => s.tasks);
  readonly loading = this.store.select(s => s.loading);
  readonly error = this.store.select(s => s.error);
  readonly actingId = this.store.select(s => s.actingId);

  readonly suggested = this.store.select(s => s.tasks.filter(t => t.status === 'Suggested'));
  readonly open = this.store.select(s => s.tasks.filter(t => t.status === 'Open'));
  readonly inProgress = this.store.select(s => s.tasks.filter(t => t.status === 'InProgress'));
  readonly done = this.store.select(s => s.tasks.filter(t => t.status === 'Done'));

  async load(params?: TaskListParams): Promise<void> {
    this.store.update({ loading: true, error: null });
    try {
      const tasks = await firstValueFrom(this.api.list(params));
      this.store.update({ tasks, loading: false });
    } catch {
      this.store.update({ loading: false, error: 'Nem sikerült betölteni a feladatokat.' });
    }
  }

  async create(req: CreateTaskRequest, onDone?: () => void): Promise<void> {
    try {
      await firstValueFrom(this.api.create(req));
      this.notify.success('Feladat sikeresen létrehozva.');
      await this.load();
      onDone?.();
    } catch {
      this.notify.error('Nem sikerült létrehozni a feladatot.');
    }
  }

  async patch(id: string, req: PatchTaskRequest, onDone?: () => void): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.patch(id, req));
      this.notify.success('Feladat sikeresen frissítve.');
      await this.load();
      onDone?.();
    } catch {
      this.notify.error('Nem sikerült frissíteni a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async delete(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.delete(id));
      this.notify.success('Feladat törölve.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült törölni a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async approve(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.approve(id));
      this.notify.success('Feladat jóváhagyva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült jóváhagyni a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async reject(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.reject(id));
      this.notify.info('Feladat elutasítva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült elutasítani a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async start(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.start(id));
      this.notify.success('Feladat elindítva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült elindítani a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async complete(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.complete(id));
      this.notify.success('Feladat befejezve.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült befejezni a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }

  async cancel(id: string): Promise<void> {
    this.store.update({ actingId: id });
    try {
      await firstValueFrom(this.api.cancel(id));
      this.notify.info('Feladat visszavonva.');
      await this.load();
    } catch {
      this.notify.error('Nem sikerült visszavonni a feladatot.');
    } finally {
      this.store.update({ actingId: null });
    }
  }
}
