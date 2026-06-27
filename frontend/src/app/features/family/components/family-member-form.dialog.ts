import { Component, ChangeDetectionStrategy, inject, output, input, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonComponent } from '../../../shared/ui/button.component';
import type { FamilyMemberDto, Relation } from '../models/family-member.dto';

@Component({
  selector: 'app-family-member-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, TranslateModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" data-testid="family-form-dialog">
      <div class="bg-[var(--color-bg)] rounded-xl p-6 max-w-md w-full shadow-xl mx-4">
        <h3 class="font-semibold text-lg mb-4">{{ member() ? ('family.edit' | translate) : ('family.add' | translate) }}</h3>
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <div>
            <label class="text-sm font-medium">{{ 'family.displayName' | translate }} *</label>
            <input data-testid="family-form-displayName" formControlName="displayName" type="text"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500" />
            @if (form.get('displayName')?.hasError('required') && form.get('displayName')?.touched) {
              <p class="text-danger-600 text-xs mt-1">A megjelenítési név kötelező.</p>
            }
          </div>
          <div>
            <label class="text-sm font-medium">{{ 'family.fullName' | translate }}</label>
            <input data-testid="family-form-fullName" formControlName="fullName" type="text"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label class="text-sm font-medium">{{ 'family.relation' | translate }} *</label>
            <select data-testid="family-form-relation" formControlName="relation"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm">
              <option value="Self">Én (Self)</option>
              <option value="Spouse">Partner</option>
              <option value="Child">Gyerek</option>
              <option value="Parent">Szülő</option>
              <option value="Other">Egyéb</option>
            </select>
          </div>
          <div>
            <label class="text-sm font-medium">{{ 'family.birthDate' | translate }}</label>
            <input data-testid="family-form-birthDate" formControlName="birthDate" type="date"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
          </div>
          <div class="flex gap-3 justify-end pt-2">
            <ui-button variant="ghost" type="button" (click)="cancelled.emit()" data-testid="family-form-cancel">
              {{ 'common.cancel' | translate }}
            </ui-button>
            <ui-button variant="primary" type="submit" [disabled]="form.invalid" data-testid="family-form-save">
              {{ 'common.save' | translate }}
            </ui-button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class FamilyMemberFormDialog implements OnInit {
  member = input<FamilyMemberDto | null>(null);
  saved = output<{ displayName: string; fullName?: string; relation: Relation; birthDate?: string; rowVersion?: string }>();
  cancelled = output<void>();

  private fb = inject(FormBuilder);
  form = this.fb.group({
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    fullName: [''],
    relation: ['Self' as Relation, Validators.required],
    birthDate: [''],
  });

  ngOnInit(): void {
    const m = this.member();
    if (m) {
      this.form.patchValue({
        displayName: m.displayName,
        fullName: m.fullName ?? '',
        relation: m.relation,
        birthDate: m.birthDate ?? '',
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: { displayName: string; relation: Relation; fullName?: string; birthDate?: string; rowVersion?: string } = {
      displayName: v.displayName ?? '',
      relation: v.relation as Relation,
    };
    if (v.fullName) payload.fullName = v.fullName;
    if (v.birthDate) payload.birthDate = v.birthDate;
    const rowVersion = this.member()?.rowVersion;
    if (rowVersion) payload.rowVersion = rowVersion;
    this.saved.emit(payload);
  }
}
