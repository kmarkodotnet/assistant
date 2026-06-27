import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { DocumentsFacade } from '../services/documents.facade';
import { DocumentCardComponent } from '../components/document-card.component';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog.component';

@Component({
  selector: 'app-documents-list-page',
  standalone: true,
  imports: [TranslateModule, DocumentCardComponent, EmptyStateComponent, SkeletonComponent, ButtonComponent, ConfirmDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-bold">{{ 'nav.documents' | translate }}</h1>
        <ui-button (click)="navigateUpload()" data-testid="documents-upload-btn">
          Új feltöltés
        </ui-button>
      </div>

      @if (facade.loading()) {
        <div class="flex flex-col gap-3">
          @for (i of [1,2,3,4,5]; track i) { <ui-skeleton height="80px" /> }
        </div>
      } @else if (facade.items().length === 0) {
        <ui-empty-state icon="📄" title="Nincsenek dokumentumok" message="Tölts fel az első dokumentumot a 'Új feltöltés' gombbal." />
      } @else {
        <div class="flex flex-col gap-3">
          @for (doc of facade.items(); track doc.id) {
            <app-document-card [doc]="doc" (delete)="confirmDelete($event)" />
          }
        </div>
      }
    </div>

    @if (deleteId()) {
      <ui-confirm-dialog
        title="Dokumentum törlése"
        message="Biztosan törlöd ezt a dokumentumot?"
        confirmLabel="Törlés"
        (confirm)="onDeleteConfirmed()"
        (cancel)="deleteId.set(null)"
      />
    }
  `,
})
export class DocumentsListPage implements OnInit {
  facade = inject(DocumentsFacade);
  private router = inject(Router);
  deleteId = signal<string | null>(null);

  ngOnInit(): void { void this.facade.load(); }
  navigateUpload(): void { void this.router.navigate(['/documents', 'upload']); }

  confirmDelete(id: string): void { this.deleteId.set(id); }

  async onDeleteConfirmed(): Promise<void> {
    const id = this.deleteId();
    if (id) await this.facade.softDelete(id);
    this.deleteId.set(null);
  }
}
