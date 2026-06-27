import { Component, ChangeDetectionStrategy, inject, signal, input, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { DocumentsFacade } from '../services/documents.facade';
import { DocumentsApiService } from '../services/documents.api';
import { NotificationService } from '../../../core/notifications/notification.service';
import { HuDatePipe } from '../../../shared/pipes/hu-date.pipe';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { firstValueFrom } from 'rxjs';
import type { DocumentTextDto } from '../models/document.dto';

type Tab = 'overview' | 'text' | 'tags';

@Component({
  selector: 'app-document-detail-page',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslateModule, HuDatePipe, FileSizePipe, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto">
      <a routerLink="/documents" class="text-[var(--color-text-muted)] hover:underline text-sm">← Dokumentumok</a>

      @if (facade.detail(); as doc) {
        <div class="mt-4">
          <h1 class="text-2xl font-bold" data-testid="detail-title">{{ doc.title }}</h1>
          <div class="flex gap-4 mt-2 text-sm text-[var(--color-text-muted)]">
            <span>{{ doc.sizeBytes | fileSize }}</span>
            @if (doc.documentDate) { <span>{{ doc.documentDate | huDate }}</span> }
            <span>{{ doc.createdUtc | huDate }}</span>
          </div>

          <!-- Tabs -->
          <div class="flex gap-2 mt-6 border-b border-[var(--color-border)]">
            @for (tab of tabs; track tab.id) {
              <button
                [attr.data-testid]="'detail-tab-' + tab.id"
                (click)="activeTab.set(tab.id)"
                [class.border-b-2]="activeTab() === tab.id"
                [class.border-primary-600]="activeTab() === tab.id"
                [class.text-primary-700]="activeTab() === tab.id"
                class="px-4 py-2 text-sm font-medium text-[var(--color-text-muted)] transition-colors"
              >{{ tab.label }}</button>
            }
          </div>

          <!-- Tab tartalom -->
          @if (activeTab() === 'overview') {
            <div class="mt-4">
              <p class="text-sm text-[var(--color-text-muted)]">AI összefoglaló: hamarosan elérhető (Epic D után).</p>
              @if (doc.textSummary) {
                <p class="text-sm mt-2">Karakterszám: {{ doc.textSummary.charCount }} · Nyelv: {{ doc.textSummary.languageDetected ?? 'ismeretlen' }}</p>
              }
            </div>
          }

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

          @if (activeTab() === 'tags') {
            <div class="mt-4">
              <p class="text-sm text-[var(--color-text-muted)]">Címkék és témák: hamarosan elérhető (Epic I után).</p>
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
  private notify = inject(NotificationService);

  activeTab = signal<Tab>('overview');
  loadingText = signal(false);
  docText = signal<DocumentTextDto | null>(null);
  editingText = signal(false);
  editedContent = '';

  tabs = [
    { id: 'overview' as Tab, label: 'Áttekintés' },
    { id: 'text' as Tab, label: 'Szöveg' },
    { id: 'tags' as Tab, label: 'Címkék' },
  ];

  ngOnInit(): void { void this.facade.loadDetail(this.id()); }

  async startEditText(): Promise<void> {
    if (!this.docText()) {
      this.loadingText.set(true);
      try {
        const text = await firstValueFrom(this.api.getText(this.id()));
        this.docText.set(text);
      } finally {
        this.loadingText.set(false);
      }
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
