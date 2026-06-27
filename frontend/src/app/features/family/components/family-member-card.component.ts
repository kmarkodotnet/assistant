import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { CardComponent } from '../../../shared/ui/card.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import type { FamilyMemberDto } from '../models/family-member.dto';

@Component({
  selector: 'app-family-member-card',
  standalone: true,
  imports: [TranslateModule, CardComponent, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ui-card>
      <div class="flex items-start justify-between gap-4">
        <div>
          <p class="font-semibold" data-testid="family-card-name">{{ member().displayName }}</p>
          @if (member().fullName) {
            <p class="text-sm text-[var(--color-text-muted)]">{{ member().fullName }}</p>
          }
          <ui-badge [variant]="member().hasUserAccount ? 'success' : 'default'" class="mt-2">
            {{ member().relation }}
          </ui-badge>
        </div>
        <div class="flex gap-2">
          <button data-testid="family-card-edit" (click)="edit.emit(member())"
            class="text-sm text-primary-600 hover:underline">{{ 'common.edit' | translate }}</button>
          <button data-testid="family-card-delete" (click)="delete.emit(member().id)"
            class="text-sm text-danger-600 hover:underline">{{ 'common.delete' | translate }}</button>
        </div>
      </div>
    </ui-card>
  `,
})
export class FamilyMemberCardComponent {
  member = input.required<FamilyMemberDto>();
  edit = output<FamilyMemberDto>();
  delete = output<string>();
}
