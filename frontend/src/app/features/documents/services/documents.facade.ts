import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { HttpEventType } from '@angular/common/http';
import { createStore } from '../../../core/state/create-store';
import { DocumentsApiService } from './documents.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import type { DocumentDto, DocumentDetailDto } from '../models/document.dto';
import { emptyFilter, type DocumentFilter } from '../models/document-filter.model';

interface UploadItem {
  file: File;
  progress: number;
  status: 'pending' | 'uploading' | 'done' | 'error' | 'duplicate';
  error?: string;
  existingId?: string;
  resultId?: string;
}

interface DocsState {
  items: DocumentDto[];
  detail: DocumentDetailDto | null;
  filters: DocumentFilter;
  page: number;
  totalCount: number;
  loading: boolean;
  error: string | null;
  uploads: UploadItem[];
}

@Injectable({ providedIn: 'root' })
export class DocumentsFacade {
  private api = inject(DocumentsApiService);
  private notify = inject(NotificationService);

  private store = createStore<DocsState>({
    items: [], detail: null, filters: emptyFilter(),
    page: 1, totalCount: 0, loading: false, error: null, uploads: [],
  });

  readonly items = this.store.select(s => s.items);
  readonly detail = this.store.select(s => s.detail);
  readonly loading = this.store.select(s => s.loading);
  readonly filters = this.store.select(s => s.filters);
  readonly totalCount = this.store.select(s => s.totalCount);
  readonly uploads = this.store.select(s => s.uploads);

  async load(): Promise<void> {
    this.store.update({ loading: true, error: null });
    try {
      const res = await firstValueFrom(this.api.list(this.store.state().filters));
      this.store.update({ items: res.items, totalCount: res.totalCount, loading: false });
    } catch {
      this.store.update({ loading: false, error: 'Nem sikerült betölteni a dokumentumokat.' });
    }
  }

  setFilter(patch: Partial<DocumentFilter>): void {
    this.store.update(s => ({ filters: { ...s.filters, ...patch, page: 1 } }));
    void this.load();
  }

  async loadDetail(id: string): Promise<void> {
    try {
      const detail = await firstValueFrom(this.api.get(id));
      this.store.update({ detail });
    } catch {
      this.notify.error('Nem sikerült betölteni a dokumentum részleteit.');
    }
  }

  async uploadFiles(files: File[]): Promise<void> {
    const newUploads: UploadItem[] = files.map(f => ({ file: f, progress: 0, status: 'pending' as const }));
    this.store.update(s => ({ uploads: [...s.uploads, ...newUploads] }));

    for (let i = 0; i < files.length; i++) {
      const file = files[i]!;
      const idxInState = this.store.state().uploads.length - files.length + i;
      const key = crypto.randomUUID();
      const formData = new FormData();
      formData.append('file', file, file.name);

      this.updateUpload(idxInState, { status: 'uploading' });

      try {
        await new Promise<void>((resolve, reject) => {
          this.api.upload(formData, key).subscribe({
            next: event => {
              if (event.type === HttpEventType.UploadProgress && event.total) {
                this.updateUpload(idxInState, { progress: Math.round(100 * event.loaded / event.total) });
              } else if (event.type === HttpEventType.Response) {
                const body = event.body as DocumentDto | null;
                const patch: Partial<UploadItem> = { status: 'done', progress: 100 };
                if (body?.id) patch.resultId = body.id;
                this.updateUpload(idxInState, patch);
                resolve();
              }
            },
            error: err => reject(err),
          });
        });
      } catch (e: unknown) {
        const appErr = e as { code?: string; message?: string };
        if (appErr?.code?.includes('conflict')) {
          const msg = appErr.message ?? '';
          const match = msg.match(/[0-9a-f-]{36}/)?.[0];
          const dupPatch: Partial<UploadItem> = { status: 'duplicate' };
          if (match) dupPatch.existingId = match;
          this.updateUpload(idxInState, dupPatch);
        } else {
          this.updateUpload(idxInState, { status: 'error', error: appErr?.message ?? 'Ismeretlen hiba' });
        }
      }
    }

    await this.load();
  }

  private updateUpload(idx: number, patch: Partial<UploadItem>): void {
    this.store.update(s => {
      const uploads = [...s.uploads];
      uploads[idx] = { ...uploads[idx]!, ...patch };
      return { uploads };
    });
  }

  clearUploads(): void { this.store.update({ uploads: [] }); }

  async softDelete(id: string): Promise<void> {
    try {
      await firstValueFrom(this.api.delete(id));
      await this.load();
      this.notify.success('Dokumentum törölve.');
    } catch {
      this.notify.error('Nem sikerült törölni a dokumentumot.');
    }
  }
}
