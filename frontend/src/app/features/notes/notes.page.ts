import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { NotesService, NoteListItemDto, NoteDto } from '../../core/api/notes.service';
import { CardComponent } from '../../shared/ui/card.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { HuRelativeDatePipe } from '../../shared/pipes/hu-relative-date.pipe';
import { NotificationService } from '../../core/notifications/notification.service';
import { isAdmin, isAdult } from '../../core/auth/auth.store';

@Component({
  selector: 'app-notes-page',
  standalone: true,
  imports: [CardComponent, SkeletonComponent, HuRelativeDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto p-4 space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Feljegyzések</h1>
        @if (isAdult()) {
          <button (click)="openCreateDialog()"
            class="px-3 py-1.5 bg-primary-600 text-white text-sm rounded-lg hover:bg-primary-700">
            + Új feljegyzés
          </button>
        }
      </div>

      @if (loading()) {
        <div class="space-y-2">
          @for (_ of [1,2,3,4]; track $index) {
            <ui-skeleton height="4rem" />
          }
        </div>
      } @else if (error()) {
        <div class="text-danger-600 p-4">Nem sikerült betölteni a feljegyzéseket.</div>
      } @else if (notes().length === 0) {
        <ui-card>
          <div class="flex flex-col items-center py-8 text-center">
            <span class="text-4xl mb-3">📝</span>
            <p class="font-medium">Nincs feljegyzés.</p>
            @if (isAdult()) {
              <p class="text-sm text-[var(--color-text-muted)] mt-1">
                Kattints az „Új feljegyzés" gombra az első létrehozásához.
              </p>
            }
          </div>
        </ui-card>
      } @else {
        <div class="space-y-2">
          @for (note of notes(); track note.id) {
            <ui-card>
              <div class="flex items-start justify-between gap-4">
                <div class="flex-1 min-w-0">
                  <button (click)="openDetail(note.id)"
                    class="text-left w-full">
                    <h3 class="font-medium text-sm truncate hover:underline">{{ note.title }}</h3>
                    <p class="text-xs text-[var(--color-text-muted)] mt-0.5">
                      {{ note.updatedUtc | huRelativeDate }}
                      @if (note.isPrivate) {
                        · <span class="text-warn-600">Privát</span>
                      }
                    </p>
                  </button>
                </div>
                @if (isAdult()) {
                  <div class="flex gap-1 shrink-0">
                    <button (click)="openEditDialog(note.id)"
                      class="text-xs text-[var(--color-text-muted)] hover:underline">Szerk.</button>
                    <button (click)="deleteNote(note)"
                      class="text-xs text-danger-600 hover:underline">Törl.</button>
                  </div>
                }
              </div>
            </ui-card>
          }
        </div>
      }

      <!-- Detail/Create/Edit Dialog -->
      @if (dialogOpen()) {
        <div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4" (click)="closeDialog()">
          <div class="bg-[var(--color-surface)] rounded-xl w-full max-w-2xl max-h-[90vh] flex flex-col shadow-xl"
               (click)="$event.stopPropagation()">
            <div class="p-5 border-b border-[var(--color-border)]">
              <h2 class="text-lg font-semibold">
                {{ viewMode() ? 'Feljegyzés' : (editId() ? 'Szerkesztés' : 'Új feljegyzés') }}
              </h2>
            </div>
            <div class="p-5 flex-1 overflow-y-auto space-y-4">
              @if (viewMode() && detailLoading()) {
                <ui-skeleton height="10rem" />
              } @else if (viewMode() && detail()) {
                <h3 class="font-semibold text-base">{{ detail()!.title }}</h3>
                <div class="prose prose-sm max-w-none text-sm whitespace-pre-wrap text-[var(--color-text)]">
                  {{ detail()!.body }}
                </div>
                @if (isAdult()) {
                  <div class="flex gap-2 pt-2">
                    <button (click)="switchToEdit()"
                      class="text-sm text-primary-600 hover:underline">Szerkesztés</button>
                  </div>
                }
              } @else {
                <div>
                  <label class="block text-sm font-medium mb-1">Cím *</label>
                  <input type="text" [value]="formTitle()"
                    (input)="formTitle.set($any($event.target).value)"
                    class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
                </div>
                <div>
                  <label class="block text-sm font-medium mb-1">Tartalom *</label>
                  <textarea rows="8" [value]="formBody()"
                    (input)="formBody.set($any($event.target).value)"
                    class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm font-mono resize-y">
                  </textarea>
                </div>
                <label class="flex items-center gap-2 text-sm">
                  <input type="checkbox" [checked]="formPrivate()"
                    (change)="formPrivate.set($any($event.target).checked)" />
                  Privát feljegyzés
                </label>
              }
            </div>
            <div class="p-5 border-t border-[var(--color-border)] flex gap-2 justify-end">
              <button (click)="closeDialog()"
                class="px-4 py-2 text-sm border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-surface-hover)]">
                Bezárás
              </button>
              @if (!viewMode()) {
                <button (click)="saveDialog()" [disabled]="saving()"
                  class="px-4 py-2 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-60">
                  {{ saving() ? 'Mentés...' : 'Mentés' }}
                </button>
              }
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class NotesPage implements OnInit {
  private notesService = inject(NotesService);
  private notificationService = inject(NotificationService);

  readonly isAdmin = isAdmin;
  readonly isAdult = isAdult;

  loading = signal(true);
  error = signal(false);
  notes = signal<NoteListItemDto[]>([]);
  dialogOpen = signal(false);
  viewMode = signal(false);
  saving = signal(false);
  editId = signal<string | null>(null);
  detail = signal<NoteDto | null>(null);
  detailLoading = signal(false);
  formTitle = signal('');
  formBody = signal('');
  formPrivate = signal(false);

  async ngOnInit(): Promise<void> {
    await this.loadNotes();
  }

  private async loadNotes(): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const result = await this.notesService.list({ page: 1 });
      this.notes.set(result);
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  openCreateDialog(): void {
    this.editId.set(null);
    this.viewMode.set(false);
    this.formTitle.set('');
    this.formBody.set('');
    this.formPrivate.set(false);
    this.detail.set(null);
    this.dialogOpen.set(true);
  }

  async openDetail(id: string): Promise<void> {
    this.editId.set(id);
    this.viewMode.set(true);
    this.detail.set(null);
    this.dialogOpen.set(true);
    this.detailLoading.set(true);
    try {
      const note = await this.notesService.get(id);
      this.detail.set(note);
    } catch {
      this.notificationService.error('Nem sikerült betölteni a feljegyzést.');
    } finally {
      this.detailLoading.set(false);
    }
  }

  async openEditDialog(id: string): Promise<void> {
    this.editId.set(id);
    this.viewMode.set(false);
    this.detail.set(null);
    this.dialogOpen.set(true);
    this.detailLoading.set(true);
    try {
      const note = await this.notesService.get(id);
      this.detail.set(note);
      this.formTitle.set(note.title);
      this.formBody.set(note.body);
      this.formPrivate.set(note.isPrivate);
    } catch {
      this.notificationService.error('Nem sikerült betölteni a feljegyzést.');
    } finally {
      this.detailLoading.set(false);
    }
  }

  switchToEdit(): void {
    if (this.detail()) {
      this.formTitle.set(this.detail()!.title);
      this.formBody.set(this.detail()!.body);
      this.formPrivate.set(this.detail()!.isPrivate);
    }
    this.viewMode.set(false);
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
  }

  async saveDialog(): Promise<void> {
    if (!this.formTitle() || !this.formBody()) return;
    this.saving.set(true);
    try {
      if (this.editId()) {
        await this.notesService.patch(this.editId()!, {
          title: this.formTitle(),
          body: this.formBody(),
          isPrivate: this.formPrivate(),
        });
        this.notificationService.success('Feljegyzés frissítve.');
      } else {
        await this.notesService.create({
          title: this.formTitle(),
          body: this.formBody(),
          isPrivate: this.formPrivate(),
        });
        this.notificationService.success('Feljegyzés létrehozva.');
      }
      this.closeDialog();
      await this.loadNotes();
    } catch {
      this.notificationService.error('Nem sikerült menteni.');
    } finally {
      this.saving.set(false);
    }
  }

  async deleteNote(note: NoteListItemDto): Promise<void> {
    if (!confirm(`Biztosan törli a(z) "${note.title}" feljegyzést?`)) return;
    try {
      await this.notesService.delete(note.id);
      this.notificationService.success('Feljegyzés törölve.');
      await this.loadNotes();
    } catch {
      this.notificationService.error('Nem sikerült törölni.');
    }
  }
}
