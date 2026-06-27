import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { DocumentsFacade } from '../services/documents.facade';
import { DropzoneComponent } from '../components/dropzone.component';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';

@Component({
  selector: 'app-document-upload-page',
  standalone: true,
  imports: [RouterLink, TranslateModule, DropzoneComponent, ButtonComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto">
      <div class="flex items-center gap-4 mb-6">
        <button (click)="back()" class="text-[var(--color-text-muted)] hover:text-[var(--color-text)]">← Vissza</button>
        <h1 class="text-xl font-bold">Dokumentum feltöltése</h1>
      </div>

      <app-dropzone (filesSelected)="onFiles($event)" />

      @if (facade.uploads().length > 0) {
        <div class="mt-6 flex flex-col gap-3">
          @for (item of facade.uploads(); track item.file.name) {
            <div class="border border-[var(--color-border)] rounded-lg p-4">
              <div class="flex items-center justify-between mb-2">
                <span class="text-sm font-medium truncate">{{ item.file.name }}</span>
                <ui-badge [variant]="item.status === 'done' ? 'success' : item.status === 'error' || item.status === 'duplicate' ? 'danger' : 'info'">
                  {{ uploadLabel(item.status) }}
                </ui-badge>
              </div>
              @if (item.status === 'uploading') {
                <div class="w-full bg-gray-200 rounded-full h-1.5">
                  <div class="bg-primary-600 h-1.5 rounded-full transition-all" [style.width]="item.progress + '%'"></div>
                </div>
              }
              @if (item.status === 'duplicate') {
                <p class="text-xs text-warn-700 mt-1">Ez a fájl már létezik.
                  @if (item.existingId) {
                    <a [routerLink]="['/documents', item.existingId]" class="underline ml-1">Megnyitom a meglévőt →</a>
                  }
                </p>
              }
              @if (item.error) {
                <p class="text-xs text-danger-600 mt-1">{{ item.error }}</p>
              }
            </div>
          }
          <ui-button variant="ghost" (click)="clearAndBack()" data-testid="upload-done-btn">Kész</ui-button>
        </div>
      }
    </div>
  `,
})
export class DocumentUploadPage {
  facade = inject(DocumentsFacade);
  private router = inject(Router);

  onFiles(files: File[]): void { void this.facade.uploadFiles(files); }
  back(): void { void this.router.navigate(['/documents']); }
  clearAndBack(): void { this.facade.clearUploads(); void this.router.navigate(['/documents']); }

  uploadLabel(status: string): string {
    switch (status) {
      case 'pending': return 'Várakozik';
      case 'uploading': return 'Feltöltés...';
      case 'done': return 'Kész';
      case 'duplicate': return 'Már létezik';
      case 'error': return 'Hiba';
      default: return status;
    }
  }
}
