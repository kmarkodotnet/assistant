import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { TopicsService, TopicDto } from '../../core/api/topics.service';
import { CardComponent } from '../../shared/ui/card.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { NotificationService } from '../../core/notifications/notification.service';
import { isAdmin } from '../../core/auth/auth.store';

const EMOJI_GROUPS: { label: string; emojis: string[] }[] = [
  { label: 'Dokumentum', emojis: ['📄','📃','📋','📁','📂','🗂️','📑','📊','📈','📉','📝','🗒️','🗓️'] },
  { label: 'Élet & Egészség', emojis: ['🏥','💊','🩺','🩻','❤️','🧬','👶','👨‍👩‍👧','🦷','🧠','💉','🩹'] },
  { label: 'Pénzügy', emojis: ['💰','💳','🏦','📊','💵','🧾','💸','📈','🏠','🚗','⚖️'] },
  { label: 'Jog & Hivatal', emojis: ['⚖️','🏛️','📜','🖊️','🔏','🔑','🪪','📮','🗳️','🏷️'] },
  { label: 'Oktatás', emojis: ['🎓','📚','📖','✏️','🏫','🎒','🖊️','📐','🔬','🏆'] },
  { label: 'Otthon & Ingatlan', emojis: ['🏠','🏡','🏗️','🔧','🛠️','🪟','🚪','💡','🌿','🛋️'] },
  { label: 'Munka', emojis: ['💼','🖥️','📞','✉️','🤝','📋','🏢','⏰','🗃️','📌'] },
  { label: 'Egyéb', emojis: ['⭐','📌','🔖','🏷️','✅','❗','🔔','📍','🎯','🔍','ℹ️'] },
];

function toSlug(name: string): string {
  const map: Record<string, string> = {
    á:'a',é:'e',í:'i',ó:'o',ö:'o',ő:'o',ú:'u',ü:'u',ű:'u',
    Á:'a',É:'e',Í:'i',Ó:'o',Ö:'o',Ő:'o',Ú:'u',Ü:'u',Ű:'u',
  };
  return name.toLowerCase()
    .replace(/[áéíóöőúüűÁÉÍÓÖŐÚÜŰ]/g, c => map[c] ?? c)
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

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
          <div class="bg-[var(--color-surface)] rounded-xl w-full max-w-md shadow-xl flex flex-col max-h-[90vh]" (click)="$event.stopPropagation()">
            <div class="px-6 pt-6 pb-4 border-b border-[var(--color-border)]">
              <h2 class="text-lg font-semibold">{{ editId() ? 'Téma szerkesztése' : 'Új téma' }}</h2>
            </div>
            <div class="overflow-y-auto flex-1 px-6 py-4 space-y-3">

              <!-- Név -->
              <div>
                <label class="block text-sm font-medium mb-1">Név *</label>
                <input type="text" [value]="formName()"
                  (input)="onNameInput($any($event.target).value)"
                  class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
              </div>

              <!-- Slug (csak létrehozáskor) -->
              @if (!editId()) {
                <div>
                  <label class="block text-sm font-medium mb-1">
                    URL-azonosító (slug)
                    <span class="text-xs font-normal text-[var(--color-text-muted)] ml-1">— automatikusan generálódik</span>
                  </label>
                  <input type="text" [value]="formSlug()"
                    (input)="formSlug.set($any($event.target).value); slugManual.set(true)"
                    class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm font-mono" />
                  <p class="text-xs text-[var(--color-text-muted)] mt-1">
                    Egyedi belső azonosító, kisbetűs, ékezet nélkül, kötőjelekkel.
                    A dokumentumlistában szűrésre és hivatkozásra használja a rendszer.
                  </p>
                </div>
              }

              <!-- Ikon picker -->
              <div>
                <label class="block text-sm font-medium mb-1">Ikon</label>
                <div class="flex items-center gap-2 mb-2">
                  <span class="text-2xl w-10 h-10 flex items-center justify-center border border-[var(--color-border)] rounded-lg">
                    {{ formIcon() || '—' }}
                  </span>
                  @if (formIcon()) {
                    <button (click)="formIcon.set('')"
                      class="text-xs text-[var(--color-text-muted)] hover:text-danger-600">Törlés</button>
                  }
                </div>
                <div class="border border-[var(--color-border)] rounded-lg overflow-hidden">
                  @for (group of emojiGroups; track group.label) {
                    <div class="px-3 pt-2 pb-1">
                      <p class="text-[10px] font-semibold text-[var(--color-text-muted)] uppercase tracking-wide mb-1">{{ group.label }}</p>
                      <div class="flex flex-wrap gap-0.5">
                        @for (emoji of group.emojis; track emoji) {
                          <button type="button"
                            (click)="formIcon.set(emoji)"
                            [class.ring-2]="formIcon() === emoji"
                            [class.ring-primary-500]="formIcon() === emoji"
                            class="w-8 h-8 text-lg rounded hover:bg-[var(--color-surface-hover)] flex items-center justify-center transition-colors">
                            {{ emoji }}
                          </button>
                        }
                      </div>
                    </div>
                  }
                </div>
              </div>

            </div>
            <div class="flex gap-2 justify-end px-6 py-4 border-t border-[var(--color-border)]">
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
  readonly emojiGroups = EMOJI_GROUPS;

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
  slugManual = signal(false);

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

  onNameInput(value: string): void {
    this.formName.set(value);
    if (!this.slugManual()) {
      this.formSlug.set(toSlug(value));
    }
  }

  openCreateDialog(parentId: string | null): void {
    this.editId.set(null);
    this.parentId.set(parentId);
    this.formName.set('');
    this.formSlug.set('');
    this.formIcon.set('');
    this.slugManual.set(false);
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
