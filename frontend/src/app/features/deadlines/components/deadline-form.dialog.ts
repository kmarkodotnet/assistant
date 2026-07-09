import {
  Component,
  ChangeDetectionStrategy,
  inject,
  input,
  output,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FamilyApiService } from '../../family/services/family.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import type { FamilyMemberDto } from '../../family/models/family-member.dto';
import type {
  DeadlineListItemDto,
  CreateDeadlineRequest,
  PatchDeadlineRequest,
  DeadlineCategory,
} from '../models/deadline.dto';

@Component({
  selector: 'app-deadline-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      (click)="cancel.emit()"
    >
      <div
        class="bg-[var(--color-bg)] rounded-xl p-6 max-w-lg w-full shadow-xl mx-4 max-h-[90vh] overflow-y-auto"
        (click)="$event.stopPropagation()"
      >
        <h3 class="font-semibold text-lg mb-4">
          {{ deadline() ? 'Határidő szerkesztése' : 'Új határidő' }}
        </h3>

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <!-- Title -->
          <div>
            <label class="text-sm font-medium">Cím *</label>
            <input
              data-testid="deadline-form-title"
              formControlName="title"
              type="text"
              placeholder="Határidő neve..."
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)]"
            />
            @if (form.get('title')?.hasError('required') && form.get('title')?.touched) {
              <p class="text-danger-600 text-xs mt-1">A cím megadása kötelező.</p>
            }
          </div>

          <!-- Description -->
          <div>
            <label class="text-sm font-medium">Leírás</label>
            <textarea
              formControlName="description"
              rows="3"
              placeholder="Részletek..."
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)] resize-none"
            ></textarea>
          </div>

          <!-- Due date -->
          <div>
            <label class="text-sm font-medium">Határidő dátuma *</label>
            <input
              data-testid="deadline-form-dueDate"
              formControlName="dueDateUtc"
              type="date"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)]"
            />
            @if (form.get('dueDateUtc')?.hasError('required') && form.get('dueDateUtc')?.touched) {
              <p class="text-danger-600 text-xs mt-1">A dátum megadása kötelező.</p>
            }
          </div>

          <!-- Category -->
          <div>
            <label class="text-sm font-medium">Kategória</label>
            <select
              formControlName="category"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm bg-[var(--color-bg)] focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="Insurance">Biztosítás</option>
              <option value="Invoice">Számla</option>
              <option value="Inspection">Szemle</option>
              <option value="School">Iskola</option>
              <option value="Medical">Orvosi</option>
              <option value="Subscription">Előfizetés</option>
              <option value="Personal">Személyes</option>
              <option value="Other">Egyéb</option>
            </select>
          </div>

          <!-- Related family member -->
          <div>
            <label class="text-sm font-medium">Érintett családtag</label>
            <select
              formControlName="relatedFamilyMemberId"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm bg-[var(--color-bg)] focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="">— Nincs hozzárendelve —</option>
              @for (m of familyMembers(); track m.id) {
                <option [value]="m.id">{{ m.displayName }}</option>
              }
            </select>
          </div>

          <!-- isPrivate -->
          <div class="flex items-center gap-2">
            <input
              formControlName="isPrivate"
              type="checkbox"
              id="deadline-isPrivate"
              class="rounded border-[var(--color-border)]"
            />
            <label for="deadline-isPrivate" class="text-sm">Privát határidő</label>
          </div>

          <!-- Buttons -->
          <div class="flex gap-3 justify-end pt-2">
            <ui-button variant="ghost" type="button" (click)="cancel.emit()">
              Mégse
            </ui-button>
            <ui-button
              data-testid="deadline-form-submit"
              variant="primary"
              type="submit"
              [disabled]="form.invalid"
            >
              {{ deadline() ? 'Mentés' : 'Létrehozás' }}
            </ui-button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class DeadlineFormDialogComponent implements OnInit {
  deadline = input<DeadlineListItemDto | null>(null);

  save = output<CreateDeadlineRequest | PatchDeadlineRequest>();
  cancel = output<void>();

  private fb = inject(FormBuilder);
  private familyApi = inject(FamilyApiService);

  familyMembers = signal<FamilyMemberDto[]>([]);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(500)]],
    description: [''],
    dueDateUtc: ['', [Validators.required]],
    category: ['Other' as DeadlineCategory],
    relatedFamilyMemberId: [''],
    isPrivate: [false],
  });

  ngOnInit(): void {
    this.familyApi.list().subscribe({
      next: members => this.familyMembers.set(members),
    });

    const d = this.deadline();
    if (d) {
      this.form.patchValue({
        title: d.title,
        category: d.category,
        relatedFamilyMemberId: d.relatedFamilyMemberId ?? '',
        dueDateUtc: d.dueDateUtc ? d.dueDateUtc.substring(0, 10) : '',
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();

    const existing = this.deadline();
    if (existing) {
      const req: PatchDeadlineRequest = {};
      if (v.title) req.title = v.title;
      if (v.description) req.description = v.description;
      if (v.dueDateUtc) req.dueDateUtc = new Date(v.dueDateUtc).toISOString();
      if (v.category) req.category = v.category as DeadlineCategory;
      if (v.relatedFamilyMemberId) req.relatedFamilyMemberId = v.relatedFamilyMemberId;
      req.isPrivate = v.isPrivate ?? false;
      this.save.emit(req);
    } else {
      const req: CreateDeadlineRequest = {
        title: v.title ?? '',
        dueDateUtc: v.dueDateUtc ? new Date(v.dueDateUtc).toISOString() : '',
        category: (v.category as DeadlineCategory) ?? 'Other',
        isPrivate: v.isPrivate ?? false,
      };
      if (v.description) req.description = v.description;
      if (v.relatedFamilyMemberId) req.relatedFamilyMemberId = v.relatedFamilyMemberId;
      this.save.emit(req);
    }
  }
}
