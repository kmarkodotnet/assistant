import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { FamilyFacade } from '../services/family.facade';
import { FamilyMemberFormDialog } from '../components/family-member-form.dialog';
import { FamilyMemberCardComponent } from '../components/family-member-card.component';
import { ConfirmDialogComponent } from '../../../shared/ui/confirm-dialog.component';
import { EmptyStateComponent } from '../../../shared/ui/empty-state.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { ButtonComponent } from '../../../shared/ui/button.component';
import type { FamilyMemberDto, Relation } from '../models/family-member.dto';

@Component({
  selector: 'app-family-list-page',
  standalone: true,
  imports: [TranslateModule, FamilyMemberFormDialog, FamilyMemberCardComponent, ConfirmDialogComponent, EmptyStateComponent, SkeletonComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-bold">{{ 'family.title' | translate }}</h1>
        <ui-button (click)="openCreate()" data-testid="family-add-btn">
          {{ 'family.add' | translate }}
        </ui-button>
      </div>

      @if (facade.loading()) {
        <div class="grid gap-4">
          @for (i of [1,2,3]; track i) {
            <ui-skeleton height="80px" />
          }
        </div>
      } @else if (facade.members().length === 0) {
        <ui-empty-state icon="👨‍👩‍👧" title="Nincsenek még családtagok" message="Kattints a 'Hozzáadás' gombra az első tag felvételéhez." />
      } @else {
        <div class="grid gap-4">
          @for (m of facade.members(); track m.id) {
            <app-family-member-card [member]="m" (edit)="openEdit($event)" (delete)="confirmDelete($event)" />
          }
        </div>
      }
    </div>

    @if (showForm()) {
      <app-family-member-form-dialog
        [member]="editingMember()"
        (saved)="onSaved($event)"
        (cancelled)="closeForm()"
      />
    }

    @if (deleteId()) {
      <ui-confirm-dialog
        title="Családtag törlése"
        message="Biztosan törlöd ezt a családtagot? A törlés visszavonható."
        confirmLabel="Törlés"
        (confirm)="onDeleteConfirmed()"
        (cancel)="deleteId.set(null)"
      />
    }
  `,
})
export class FamilyListPage implements OnInit {
  facade = inject(FamilyFacade);
  showForm = signal(false);
  editingMember = signal<FamilyMemberDto | null>(null);
  deleteId = signal<string | null>(null);

  ngOnInit(): void { void this.facade.load(); }

  openCreate(): void { this.editingMember.set(null); this.showForm.set(true); }
  openEdit(m: FamilyMemberDto): void { this.editingMember.set(m); this.showForm.set(true); }
  closeForm(): void { this.showForm.set(false); }
  confirmDelete(id: string): void { this.deleteId.set(id); }

  async onSaved(data: { displayName: string; fullName?: string; relation: Relation; birthDate?: string; rowVersion?: string }): Promise<void> {
    const editing = this.editingMember();
    let success: boolean;
    if (editing) {
      success = await this.facade.update(editing.id, { ...data, rowVersion: data.rowVersion ?? '' });
    } else {
      success = await this.facade.create(data);
    }
    if (success) this.closeForm();
  }

  async onDeleteConfirmed(): Promise<void> {
    const id = this.deleteId();
    if (id) await this.facade.softDelete(id);
    this.deleteId.set(null);
  }
}
