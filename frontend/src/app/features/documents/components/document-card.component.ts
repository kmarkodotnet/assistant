import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { HuDatePipe } from '../../../shared/pipes/hu-date.pipe';
import { FileSizePipe } from '../../../shared/pipes/file-size.pipe';
import { DocumentIconPipe } from '../../../shared/pipes/document-icon.pipe';
import type { DocumentDto, ProcessingStatus } from '../models/document.dto';

type BadgeVariant = 'default' | 'info' | 'warn' | 'danger' | 'success';

@Component({
  selector: 'app-document-card',
  standalone: true,
  imports: [RouterLink, TranslateModule, BadgeComponent, HuDatePipe, FileSizePipe, DocumentIconPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl p-4 hover:shadow-md transition-shadow" [attr.data-testid]="'doc-card-' + doc().id">
      <div class="flex items-start gap-3">
        <span class="text-2xl">{{ doc().mimeType | documentIcon }}</span>
        <div class="flex-1 min-w-0">
          <a [routerLink]="['/documents', doc().id]" data-testid="doc-card-title"
            class="font-medium text-primary-700 hover:underline truncate block">{{ doc().title }}</a>
          <p class="text-xs text-[var(--color-text-muted)] mt-0.5">
            {{ doc().originalFileName }} · {{ doc().sizeBytes | fileSize }}
            @if (doc().documentDate) { · {{ doc().documentDate | huDate }} }
          </p>
          <div class="flex gap-2 mt-2 flex-wrap">
            <ui-badge [variant]="statusVariant(doc().processingStatus)">
              {{ doc().processingStatus }}
            </ui-badge>
            @if (doc().isPrivate) {
              <ui-badge variant="warn">Privát</ui-badge>
            }
          </div>
        </div>
        <button data-testid="doc-card-delete" (click)="delete.emit(doc().id)"
          class="text-danger-600 hover:text-danger-800 text-xs shrink-0">Törlés</button>
      </div>
    </div>
  `,
})
export class DocumentCardComponent {
  doc = input.required<DocumentDto>();
  delete = output<string>();

  statusVariant(status: ProcessingStatus): BadgeVariant {
    switch (status) {
      case 'Done': return 'success';
      case 'Failed': return 'danger';
      case 'Pending':
      case 'Extracting':
      case 'Analyzing': return 'info';
      default: return 'default';
    }
  }
}
