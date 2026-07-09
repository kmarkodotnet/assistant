import { Component, ChangeDetectionStrategy, inject, signal, computed, input, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { DocumentsFacade } from '../services/documents.facade';
import { DocumentsApiService, ClassificationTagDto, ClassificationTopicDto } from '../services/documents.api';
import { TopicsService, TopicDto } from '../../../core/api/topics.service';
import { NotificationService } from '../../../core/notifications/notification.service';
import { HuDatePipe } from '../../../shared/pipes/hu-date.pipe';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { firstValueFrom } from 'rxjs';
import type { DocumentTextDto } from '../models/document.dto';

type Tab = 'overview' | 'text' | 'tags';

const STATUS_LABEL: Record<string, string> = {
  Pending: 'Várakozik', Extracting: 'Szöveg kinyerés', Analyzing: 'Elemzés', Done: 'Kész', Failed: 'Hiba',
};
const STATUS_BADGE: Record<string, 'success' | 'danger' | 'info' | 'default'> = {
  Done: 'success', Failed: 'danger', Extracting: 'info', Analyzing: 'info', Pending: 'default',
};
const EXTRACTION_LABEL: Record<string, string> = {
  PdfTextLayer: 'PDF szövegréteg', TesseractOcr: 'OCR (Tesseract)', ManualPaste: 'Kézi bevitel', EmailBody: 'E-mail törzs',
};
const SOURCE_LABEL: Record<string, string> = {
  Upload: 'Feltöltve', Email: 'E-mailből', Manual: 'Kézi',
};

@Component({
  selector: 'app-document-detail-page',
  standalone: true,
  imports: [RouterLink, FormsModule, DecimalPipe, TranslateModule, HuDatePipe, FileSizePipe, SkeletonComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto">
      <a routerLink="/documents" class="text-[var(--color-text-muted)] hover:underline text-sm">← Dokumentumok</a>

      @if (facade.detail(); as doc) {
        <div class="mt-4">
          <div class="flex items-start justify-between gap-4">
            <h1 class="text-2xl font-bold" data-testid="detail-title">{{ doc.title }}</h1>
            <ui-badge [variant]="statusBadge(doc.processingStatus)">
              {{ statusLabel(doc.processingStatus) }}
            </ui-badge>
          </div>
          <div class="flex gap-4 mt-2 text-sm text-[var(--color-text-muted)]">
            <span>{{ doc.sizeBytes | fileSize }}</span>
            @if (doc.documentDate) { <span>{{ doc.documentDate | huDate }}</span> }
            <span>{{ doc.createdUtc | huDate }}</span>
            <span>{{ sourceLabel(doc.sourceType) }}</span>
            @if (doc.isPrivate) { <span class="text-warning-600 font-medium">Privát</span> }
          </div>

          <!-- Tabs -->
          <div class="flex gap-2 mt-6 border-b border-[var(--color-border)]">
            @for (tab of tabs; track tab.id) {
              <button
                [attr.data-testid]="'detail-tab-' + tab.id"
                (click)="selectTab(tab.id)"
                [class.border-b-2]="activeTab() === tab.id"
                [class.border-primary-600]="activeTab() === tab.id"
                [class.text-primary-700]="activeTab() === tab.id"
                class="px-4 py-2 text-sm font-medium text-[var(--color-text-muted)] transition-colors"
              >{{ tab.label }}</button>
            }
          </div>

          <!-- Overview tab -->
          @if (activeTab() === 'overview') {
            <div class="mt-5 space-y-4">

              <!-- Text extraction metadata -->
              @if (doc.textSummary) {
                <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4">
                  <p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wide mb-3">Szöveg kinyerés</p>
                  <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
                    <div>
                      <p class="text-xs text-[var(--color-text-muted)]">Karakterszám</p>
                      <p class="text-sm font-medium mt-0.5">{{ doc.textSummary.charCount | number }}</p>
                    </div>
                    <div>
                      <p class="text-xs text-[var(--color-text-muted)]">Nyelv</p>
                      <p class="text-sm font-medium mt-0.5">{{ doc.textSummary.languageDetected ?? 'Ismeretlen' }}</p>
                    </div>
                    <div>
                      <p class="text-xs text-[var(--color-text-muted)]">Módszer</p>
                      <p class="text-sm font-medium mt-0.5">{{ extractionLabel(doc.textSummary.extractionMethod) }}</p>
                    </div>
                    <div>
                      <p class="text-xs text-[var(--color-text-muted)]">Szerkesztve</p>
                      <p class="text-sm font-medium mt-0.5">{{ doc.textSummary.isManuallyEdited ? 'Igen' : 'Nem' }}</p>
                    </div>
                  </div>
                </div>
              } @else if (doc.processingStatus !== 'Done' && doc.processingStatus !== 'Failed') {
                <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4 flex items-center gap-3">
                  <div class="w-2 h-2 bg-primary-400 rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                  <p class="text-sm text-[var(--color-text-muted)]">Szöveg kinyerés folyamatban...</p>
                </div>
              }

              <!-- AI summary -->
              <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4">
                <p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wide mb-2">AI összefoglaló</p>
                @if (doc.aiSummary) {
                  <p class="text-sm leading-relaxed whitespace-pre-wrap">{{ doc.aiSummary }}</p>
                } @else if (doc.processingStatus === 'Failed') {
                  <p class="text-sm text-danger-600">A feldolgozás meghiúsult. Próbáld meg újraindítani az újrafeldolgozást.</p>
                } @else if (doc.processingStatus === 'Done' || doc.processingStatus === 'Analyzing') {
                  <p class="text-sm text-[var(--color-text-muted)]">Az összefoglaló generálása folyamatban van...</p>
                } @else {
                  <p class="text-sm text-[var(--color-text-muted)]">Az összefoglaló a feldolgozás befejezése után lesz elérhető.</p>
                }
              </div>

              <!-- Reprocess -->
              <div class="flex justify-end">
                <button
                  data-testid="detail-reprocess"
                  class="px-4 py-2 text-sm border border-[var(--color-border)] rounded-xl text-[var(--color-text-muted)] hover:bg-[var(--color-surface)] transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  [disabled]="reprocessing()"
                  (click)="reprocess()"
                >
                  {{ reprocessing() ? 'Elindítás...' : '↺ Újrafeldolgozás' }}
                </button>
              </div>
            </div>
          }

          <!-- Text tab -->
          @if (activeTab() === 'text') {
            <div class="mt-4">
              @if (loadingText()) {
                <ui-skeleton height="200px" />
              } @else if (docText()) {
                @if (editingText()) {
                  <textarea data-testid="detail-text-editor"
                    [(ngModel)]="editedContent"
                    class="w-full h-64 border border-[var(--color-border)] rounded-lg p-3 text-sm font-mono resize-y"></textarea>
                  <div class="flex gap-2 mt-2">
                    <button data-testid="detail-text-save" (click)="saveText()" class="text-sm bg-primary-600 text-white px-4 py-2 rounded-lg">Mentés</button>
                    <button (click)="editingText.set(false)" class="text-sm text-[var(--color-text-muted)]">Mégse</button>
                  </div>
                } @else {
                  <pre class="text-sm whitespace-pre-wrap bg-[var(--color-surface)] rounded-lg p-4 overflow-auto max-h-96">{{ docText()!.content }}</pre>
                  <button data-testid="detail-text-edit" (click)="startEditText()" class="mt-2 text-sm text-primary-600 hover:underline">Szerkesztem</button>
                }
              } @else {
                <p class="text-sm text-[var(--color-text-muted)]">A szöveg kinyerése folyamatban van.</p>
              }
            </div>
          }

          <!-- Tags tab -->
          @if (activeTab() === 'tags') {
            <div class="mt-5 space-y-5">
              @if (loadingTagsTopics()) {
                <ui-skeleton height="60px" cssClass="mb-3" />
                <ui-skeleton height="120px" />
              } @else {
                <!-- Info banner -->
                <div class="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 text-xs text-amber-800">
                  <svg class="w-4 h-4 mt-0.5 shrink-0 text-amber-600" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                  </svg>
                  <p>A dokumentumhoz rendelt specifikus címkék és témák az AI feldolgozás alapján jelennek meg. A kézi hozzárendelés hamarosan elérhető.</p>
                </div>

                <!-- Document tags -->
                <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4">
                  <p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wide mb-3">Hozzárendelt címkék</p>
                  @if (tags().length === 0) {
                    <p class="text-sm text-[var(--color-text-muted)]">Még nem kerültek felismerésre az AI által.</p>
                  } @else {
                    <div class="flex flex-wrap gap-2">
                      @for (tag of tags(); track tag.id) {
                        <span
                          class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium border"
                          [style.background-color]="tag.color ? tag.color + '22' : ''"
                          [style.border-color]="tag.color ?? 'var(--color-border)'"
                          [style.color]="tag.color ?? 'var(--color-text)'"
                        >
                          {{ tag.name }}
                        </span>
                      }
                    </div>
                  }
                </div>

                <!-- Document topics -->
                <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4">
                  <p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wide mb-3">Kapcsolódó témák</p>
                  @if (topics().length === 0) {
                    <p class="text-sm text-[var(--color-text-muted)] mb-3">Még nincs hozzárendelt téma.</p>
                  } @else {
                    <div class="space-y-1 mb-3">
                      @for (topic of topics(); track topic.id) {
                        <div class="flex items-center gap-2 py-1">
                          @if (topic.icon) { <span class="text-base">{{ topic.icon }}</span> }
                          <span class="text-sm flex-1">{{ topic.name }}</span>
                          <span class="text-xs text-[var(--color-text-muted)]">/{{ topic.slug }}</span>
                          <button
                            (click)="removeTopic(topic.id)"
                            class="text-xs text-danger-600 hover:text-danger-800 px-1.5 py-0.5 rounded hover:bg-danger-50 transition-colors"
                            title="Téma eltávolítása"
                          >×</button>
                        </div>
                      }
                    </div>
                  }
                  <!-- Add topic -->
                  @if (availableTopics().length > 0) {
                    <div class="flex gap-2 items-center pt-2 border-t border-[var(--color-border)]">
                      <select
                        [(ngModel)]="selectedTopicId"
                        class="flex-1 text-sm border border-[var(--color-border)] rounded-lg px-3 py-1.5 bg-[var(--color-bg)]"
                      >
                        <option value="">— Válassz témát —</option>
                        @for (topic of availableTopics(); track topic.id) {
                          <option [value]="topic.id">{{ topic.icon ? topic.icon + ' ' : '' }}{{ topic.name }}</option>
                        }
                      </select>
                      <button
                        (click)="addTopic()"
                        [disabled]="!selectedTopicId || savingTopic()"
                        class="text-sm px-3 py-1.5 bg-primary-600 text-white rounded-lg disabled:opacity-40 hover:bg-primary-700 transition-colors"
                      >Hozzáad</button>
                    </div>
                  }
                </div>
              }
            </div>
          }
        </div>
      } @else {
        <div class="mt-6">
          <ui-skeleton height="40px" cssClass="mb-4" />
          <ui-skeleton height="200px" />
        </div>
      }
    </div>
  `,
})
export class DocumentDetailPage implements OnInit {
  id = input.required<string>();

  facade = inject(DocumentsFacade);
  private api = inject(DocumentsApiService);
  private topicsApi = inject(TopicsService);
  private notify = inject(NotificationService);

  activeTab = signal<Tab>('overview');
  loadingText = signal(false);
  docText = signal<DocumentTextDto | null>(null);
  editingText = signal(false);
  editedContent = '';
  reprocessing = signal(false);
  tags = signal<ClassificationTagDto[]>([]);
  topics = signal<ClassificationTopicDto[]>([]);
  loadingTagsTopics = signal(false);
  allTopics = signal<TopicDto[]>([]);
  savingTopic = signal(false);
  selectedTopicId = '';

  availableTopics = computed(() => {
    const assigned = new Set(this.topics().map(t => t.id));
    return this.allTopics().filter(t => !assigned.has(t.id));
  });

  tabs = [
    { id: 'overview' as Tab, label: 'Áttekintés' },
    { id: 'text' as Tab, label: 'Szöveg' },
    { id: 'tags' as Tab, label: 'Címkék & Témák' },
  ];

  ngOnInit(): void { void this.facade.loadDetail(this.id()); }

  selectTab(tab: Tab): void {
    this.activeTab.set(tab);
    if (tab === 'text' && !this.docText() && !this.loadingText()) {
      void this.loadText();
    }
    if (tab === 'tags' && this.tags().length === 0 && !this.loadingTagsTopics()) {
      void this.loadTagsTopics();
    }
  }

  statusBadge(s: string): 'success' | 'danger' | 'info' | 'default' {
    return STATUS_BADGE[s] ?? 'default';
  }

  statusLabel(s: string): string { return STATUS_LABEL[s] ?? s; }
  extractionLabel(s: string): string { return EXTRACTION_LABEL[s] ?? s; }
  sourceLabel(s: string): string { return SOURCE_LABEL[s] ?? s; }

  async reprocess(): Promise<void> {
    this.reprocessing.set(true);
    try {
      await firstValueFrom(this.api.reprocess(this.id(), []));
      this.notify.success('Újrafeldolgozás elindítva.');
      void this.facade.loadDetail(this.id());
    } catch {
      this.notify.error('Nem sikerült elindítani az újrafeldolgozást.');
    } finally {
      this.reprocessing.set(false);
    }
  }

  private async loadText(): Promise<void> {
    this.loadingText.set(true);
    try {
      const text = await firstValueFrom(this.api.getText(this.id()));
      this.docText.set(text);
    } catch {
      // 404: szöveg még nincs kinyerve
    } finally {
      this.loadingText.set(false);
    }
  }

  private async loadTagsTopics(): Promise<void> {
    this.loadingTagsTopics.set(true);
    try {
      const [classification, allTopics] = await Promise.all([
        firstValueFrom(this.api.getClassification(this.id())),
        this.topicsApi.list(true),
      ]);
      this.tags.set(classification.tags);
      this.topics.set(classification.topics);
      this.allTopics.set(allTopics);
    } catch {
      // ignore
    } finally {
      this.loadingTagsTopics.set(false);
    }
  }

  async addTopic(): Promise<void> {
    if (!this.selectedTopicId || this.savingTopic()) return;
    this.savingTopic.set(true);
    try {
      await firstValueFrom(this.api.addTopic(this.id(), this.selectedTopicId));
      const added = this.allTopics().find(t => t.id === this.selectedTopicId);
      if (added) {
        this.topics.update(list => [...list, {
          id: added.id, name: added.name, slug: added.slug,
          icon: added.icon, origin: 'Manual', isApproved: true,
        }]);
      }
      this.selectedTopicId = '';
    } catch {
      this.notify.error('Nem sikerült hozzáadni a témát.');
    } finally {
      this.savingTopic.set(false);
    }
  }

  async removeTopic(topicId: string): Promise<void> {
    try {
      await firstValueFrom(this.api.removeTopic(this.id(), topicId));
      this.topics.update(list => list.filter(t => t.id !== topicId));
    } catch {
      this.notify.error('Nem sikerült eltávolítani a témát.');
    }
  }

  async startEditText(): Promise<void> {
    if (!this.docText()) {
      await this.loadText();
      if (!this.docText()) return;
    }
    this.editedContent = this.docText()?.content ?? '';
    this.editingText.set(true);
  }

  async saveText(): Promise<void> {
    try {
      await firstValueFrom(this.api.updateText(this.id(), this.editedContent));
      this.docText.update(t => t ? { ...t, content: this.editedContent, isManuallyEdited: true } : t);
      this.editingText.set(false);
      this.notify.success('Szöveg mentve. A keresési index és az AI összefoglaló újragenerálódik a háttérben.');
    } catch {
      this.notify.error('Nem sikerült menteni a szöveget.');
    }
  }
}
