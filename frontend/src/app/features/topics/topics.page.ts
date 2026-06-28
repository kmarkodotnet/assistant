import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { TopicsService, TopicDto } from '../../core/api/topics.service';
import { CardComponent } from '../../shared/ui/card.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { NotificationService } from '../../core/notifications/notification.service';
import { isAdmin } from '../../core/auth/auth.store';

interface FlatTopic extends TopicDto {
  depth: number;
}

function flattenTopics(topics: TopicDto[], depth = 0): FlatTopic[] {
  return topics.flatMap(t => [
    { ...t, depth },
    ...flattenTopics(t.children, depth + 1),
  ]);
}

@Component({
  selector: 'app-topics-page',
  standalone: true,
  imports: [CardComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-3xl mx-auto p-4 space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">Témák</h1>
        @if (isAdmin()) {
          <button (click)="openCreateDialog(null)"
            class="px-3 py-1.5 bg-primary-600 text-white text-sm rounded-lg hover:bg-primary-700">
            + Új gyökér téma
          </button>
        }
      </div>

      @if (loading()) {
        <div class="space-y-2">
          @for (_ of [1,2,3,4,5,6]; track $index) {
            <ui-skeleton height="2.5rem" />
          }
        </div>
      } @else if (error()) {
        <div class="text-danger-600 p-4">Nem sikerült betölteni a témákat.</div>
      } @else {
        <ui-card>
          @if (flatTopics().length === 0) {
            <p class="text-sm text-[var(--color-text-muted)]">Nincsenek témák.</p>
          }
          @for (topic of flatTopics(); track topic.id) {
            <div class="flex items-center justify-between py-2 border-b border-[var(--color-border)] last:border-0"
                 [style.padding-left.rem]="1 + topic.depth * 1.25">
              <div class="flex items-center gap-2">
                @if (topic.icon) {
                  <span>{{ topic.icon }}</span>
                }
                <span class="text-sm font-medium">{{ topic.name }}</span>
                <span class="text-xs text-[var(--color-text-muted)]">/{{ topic.slug }}</span>
              </div>
              @if (isAdmin()) {
                <div class="flex gap-1">
                  @if (topic.depth < 2) {
                    <button (click)="openCreateDialog(topic.id)"
                      class="text-xs text-primary-600 hover:underline px-1">+ Alt</button>
                  }
                  <button (click)="openEditDialog(topic)"
                    class="text-xs text-[var(--color-text-muted)] hover:underline px-1">Szerk.</button>
                  <button (click)="deleteTopic(topic)"
                    class="text-xs text-danger-600 hover:underline px-1">Törl.</button>
                </div>
              }
            </div>
          }
        </ui-card>
      }

      @if (dialogOpen()) {
        <div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50" (click)="closeDialog()">
          <div class="bg-[var(--color-surface)] rounded-xl p-6 w-full max-w-md shadow-xl" (click)="$event.stopPropagation()">
            <h2 class="text-lg font-semibold mb-4">{{ editId() ? 'Téma szerkesztése' : 'Új téma' }}</h2>
            <div class="space-y-3">
              <div>
                <label class="block text-sm font-medium mb-1">Név *</label>
                <input type="text" [value]="formName()"
                  (input)="formName.set($any($event.target).value)"
                  class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
              </div>
              @if (!editId()) {
                <div>
                  <label class="block text-sm font-medium mb-1">Slug *</label>
                  <input type="text" [value]="formSlug()"
                    (input)="formSlug.set($any($event.target).value)"
                    placeholder="pl. egeszsegugy"
                    class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm font-mono" />
                </div>
              }
              <div>
                <label class="block text-sm font-medium mb-1">Ikon (emoji)</label>
                <input type="text" [value]="formIcon()"
                  (input)="formIcon.set($any($event.target).value)"
                  placeholder="pl. 🏥"
                  class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
              </div>
            </div>
            <div class="flex gap-2 justify-end mt-5">
              <button (click)="closeDialog()"
                class="px-4 py-2 text-sm border border-[var(--color-border)] rounded-lg hover:bg-[var(--color-surface-hover)]">
                Mégsem
              </button>
              <button (click)="saveDialog()" [disabled]="saving()"
                class="px-4 py-2 text-sm bg-primary-600 text-white rounded-lg hover:bg-primary-700 disabled:opacity-60">
                {{ saving() ? 'Mentés...' : 'Mentés' }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
})
export class TopicsPage implements OnInit {
  private topicsService = inject(TopicsService);
  private notificationService = inject(NotificationService);

  readonly isAdmin = isAdmin;

  loading = signal(true);
  error = signal(false);
  topics = signal<TopicDto[]>([]);
  flatTopics = signal<FlatTopic[]>([]);
  dialogOpen = signal(false);
  saving = signal(false);
  editId = signal<string | null>(null);
  parentId = signal<string | null>(null);
  formName = signal('');
  formSlug = signal('');
  formIcon = signal('');

  async ngOnInit(): Promise<void> {
    await this.loadTopics();
  }

  private async loadTopics(): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const result = await this.topicsService.list(false);
      this.topics.set(result);
      this.flatTopics.set(flattenTopics(result));
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  openCreateDialog(parentId: string | null): void {
    this.editId.set(null);
    this.parentId.set(parentId);
    this.formName.set('');
    this.formSlug.set('');
    this.formIcon.set('');
    this.dialogOpen.set(true);
  }

  openEditDialog(topic: FlatTopic): void {
    this.editId.set(topic.id);
    this.formName.set(topic.name);
    this.formSlug.set(topic.slug);
    this.formIcon.set(topic.icon ?? '');
    this.dialogOpen.set(true);
  }

  closeDialog(): void {
    this.dialogOpen.set(false);
  }

  async saveDialog(): Promise<void> {
    if (!this.formName()) return;
    this.saving.set(true);
    try {
      if (this.editId()) {
        const patch: { name?: string; icon?: string } = { name: this.formName() };
        const icon = this.formIcon();
        if (icon) patch.icon = icon;
        await this.topicsService.patch(this.editId()!, patch);
        this.notificationService.success('Téma frissítve.');
      } else {
        if (!this.formSlug()) {
          this.notificationService.error('A slug megadása kötelező.');
          this.saving.set(false);
          return;
        }
        const createBody: { name: string; slug: string; parentId?: string; icon?: string } = {
          name: this.formName(),
          slug: this.formSlug(),
        };
        const parentId = this.parentId();
        if (parentId) createBody.parentId = parentId;
        const icon = this.formIcon();
        if (icon) createBody.icon = icon;
        await this.topicsService.create(createBody);
        this.notificationService.success('Téma létrehozva.');
      }
      this.closeDialog();
      await this.loadTopics();
    } catch {
      this.notificationService.error('Nem sikerült menteni.');
    } finally {
      this.saving.set(false);
    }
  }

  async deleteTopic(topic: FlatTopic): Promise<void> {
    if (!confirm(`Biztosan törli a(z) "${topic.name}" témát?`)) return;
    try {
      await this.topicsService.delete(topic.id);
      this.notificationService.success('Téma törölve.');
      await this.loadTopics();
    } catch {
      this.notificationService.error('Nem sikerült törölni. Lehet, hogy altémái vannak.');
    }
  }
}
