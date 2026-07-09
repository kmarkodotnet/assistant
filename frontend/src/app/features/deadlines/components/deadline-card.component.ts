import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import type { DeadlineListItemDto, DeadlineCategory } from '../models/deadline.dto';

type BadgeVariant = 'info' | 'warn' | 'default' | 'success' | 'danger';

const CATEGORY_VARIANT: Record<DeadlineCategory, BadgeVariant> = {
  Insurance: 'info',
  Invoice: 'warn',
  Inspection: 'default',
  School: 'success',
  Medical: 'danger',
  Subscription: 'info',
  Personal: 'default',
  Other: 'default',
};

const CATEGORY_LABEL: Record<DeadlineCategory, string> = {
  Insurance: 'Biztosítás',
  Invoice: 'Számla',
  Inspection: 'Szemle',
  School: 'Iskola',
  Medical: 'Orvosi',
  Subscription: 'Előfizetés',
  Personal: 'Személyes',
  Other: 'Egyéb',
};

@Component({
  selector: 'app-deadline-card',
  standalone: true,
  imports: [CommonModule, DatePipe, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="rounded-xl border p-3 bg-[var(--color-bg)] transition-shadow hover:shadow-md"
      [class.border-primary-400]="isAi()"
      [class.border-[var(--color-border)]]="!isAi()"
    >
      <!-- AI badge + Category badge -->
      <div class="flex items-center gap-1.5 mb-2 flex-wrap">
        @if (isAi()) {
          <span class="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-semibold bg-primary-100 text-primary-700 border border-primary-300">
            AI
          </span>
        }
        <ui-badge [variant]="categoryVariant()">{{ categoryLabel() }}</ui-badge>
      </div>

      <!-- Title -->
      <p class="text-sm font-medium leading-snug mb-1">{{ deadline().title }}</p>

      <!-- Due date -->
      <p
        class="text-xs mt-1"
        [class.text-danger-600]="isExpired()"
        [class.font-semibold]="isExpired()"
        [class.text-[var(--color-text-muted)]]="!isExpired()"
      >
        Határidő: {{ deadline().dueDateUtc | date:'yyyy. MM. dd.' }}
        @if (isExpired()) { <span>— Lejárt</span> }
      </p>

      <!-- Actions -->
      <div class="flex items-center gap-1 mt-3 flex-wrap">
        @if (showActionButtons()) {
          @if (isAiSuggested()) {
            <button
              data-testid="deadline-approve"
              class="px-2 py-1 text-xs rounded-lg bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-50"
              [disabled]="acting()"
              (click)="approve.emit(deadline().id)"
            >Jóváhagy</button>
          }
          <button
            data-testid="deadline-resolve"
            class="px-2 py-1 text-xs rounded-lg bg-success-600 text-white hover:bg-success-700 disabled:opacity-50"
            [disabled]="acting()"
            (click)="resolve.emit(deadline().id)"
          >Megoldva</button>
          <button
            data-testid="deadline-dismiss"
            class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50"
            [disabled]="acting()"
            (click)="dismiss.emit(deadline().id)"
          >Mellőz</button>
        }
        <button
          data-testid="deadline-edit"
          class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50 ml-auto"
          [disabled]="acting()"
          (click)="edit.emit(deadline())"
        >Szerkeszt</button>
      </div>
    </div>
  `,
})
export class DeadlineCardComponent {
  deadline = input.required<DeadlineListItemDto>();
  acting = input(false);

  approve = output<string>();
  resolve = output<string>();
  dismiss = output<string>();
  edit = output<DeadlineListItemDto>();

  isAi = computed(() => {
    const o = this.deadline().origin;
    return o === 'AiSuggested' || o === 'AiApproved';
  });

  isAiSuggested = computed(() => this.deadline().origin === 'AiSuggested');

  isExpired = computed(() => {
    const s = this.deadline().status;
    return s === 'Passed' || s === 'Due';
  });

  showActionButtons = computed(() => {
    const s = this.deadline().status;
    return s === 'Upcoming' || s === 'Due' || s === 'Passed';
  });

  categoryVariant = computed((): BadgeVariant => CATEGORY_VARIANT[this.deadline().category]);

  categoryLabel = computed(() => CATEGORY_LABEL[this.deadline().category]);
}
