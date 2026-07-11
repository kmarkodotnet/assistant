import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import type { TaskListItemDto } from '../models/task.dto';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule, DatePipe, BadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="rounded-xl border p-3 bg-[var(--color-bg)] transition-shadow hover:shadow-md"
      [class.border-primary-400]="isAi()"
      [class.border-[var(--color-border)]]="!isAi()"
    >
      <!-- AI badge + Priority badge -->
      <div class="flex items-center gap-1.5 mb-2 flex-wrap">
        @if (isAi()) {
          <span class="inline-flex items-center px-1.5 py-0.5 rounded text-xs font-semibold bg-primary-100 text-primary-700 border border-primary-300">
            AI
          </span>
        }
        <ui-badge [variant]="priorityVariant()">{{ priorityLabel() }}</ui-badge>
      </div>

      <!-- Title -->
      <p class="text-sm font-medium leading-snug mb-1">{{ task().title }}</p>

      <!-- Card summary / description -->
      @if (task().cardSummary) {
        <p class="text-xs text-[var(--color-text-muted)] line-clamp-2 mb-1">{{ task().cardSummary }}</p>
      }

      <!-- Due date -->
      @if (task().dueDateUtc) {
        <p
          class="text-xs mt-1"
          [class.text-danger-600]="isOverdue()"
          [class.font-semibold]="isOverdue()"
          [class.text-[var(--color-text-muted)]]="!isOverdue()"
        >
          Határidő: {{ task().dueDateUtc | date:'yyyy. MM. dd.' }}
          @if (isOverdue()) { <span>(lejárt)</span> }
        </p>
      }

      <!-- Actions -->
      <div class="flex items-center gap-1 mt-3 flex-wrap">
        @switch (task().status) {
          @case ('Suggested') {
            <button
              data-testid="task-approve"
              class="px-2 py-1 text-xs rounded-lg bg-success-600 text-white hover:bg-success-700 disabled:opacity-50"
              [disabled]="acting()"
              (click)="approve.emit(task().id)"
            >Jóváhagy</button>
            <button
              data-testid="task-reject"
              class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50"
              [disabled]="acting()"
              (click)="reject.emit(task().id)"
            >Elutasít</button>
          }
          @case ('Open') {
            <button
              data-testid="task-start"
              class="px-2 py-1 text-xs rounded-lg bg-primary-600 text-white hover:bg-primary-700 disabled:opacity-50"
              [disabled]="acting()"
              (click)="start.emit(task().id)"
            >Indítás</button>
            <button
              data-testid="task-cancel"
              class="px-2 py-1 text-xs rounded-lg text-[var(--color-text-muted)] hover:text-danger-600 disabled:opacity-50"
              [disabled]="acting()"
              (click)="cancel.emit(task().id)"
            >Visszavon</button>
          }
          @case ('InProgress') {
            <button
              data-testid="task-complete"
              class="px-2 py-1 text-xs rounded-lg bg-success-600 text-white hover:bg-success-700 disabled:opacity-50"
              [disabled]="acting()"
              (click)="complete.emit(task().id)"
            >Kész</button>
            <button
              data-testid="task-cancel"
              class="px-2 py-1 text-xs rounded-lg text-[var(--color-text-muted)] hover:text-danger-600 disabled:opacity-50"
              [disabled]="acting()"
              (click)="cancel.emit(task().id)"
            >Visszavon</button>
          }
        }
        <button
          data-testid="task-view"
          class="px-2 py-1 text-xs rounded-lg bg-transparent text-[var(--color-text-muted)] hover:bg-[var(--color-surface)] disabled:opacity-50 ml-auto"
          [disabled]="acting()"
          (click)="view.emit(task())"
        >👁 Megtekint</button>
        <button
          data-testid="task-edit"
          class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50"
          [disabled]="acting()"
          (click)="edit.emit(task())"
        >Szerkeszt</button>
      </div>
    </div>
  `,
})
export class TaskCardComponent {
  task = input.required<TaskListItemDto>();
  acting = input(false);

  approve = output<string>();
  reject = output<string>();
  start = output<string>();
  complete = output<string>();
  cancel = output<string>();
  edit = output<TaskListItemDto>();
  view = output<TaskListItemDto>();

  isAi = computed(() => {
    const o = this.task().origin;
    return o === 'AiSuggested' || o === 'AiApproved';
  });

  isOverdue = computed(() => {
    const due = this.task().dueDateUtc;
    if (!due) return false;
    return new Date(due) < new Date();
  });

  priorityVariant = computed(() => {
    switch (this.task().priority) {
      case 'High': return 'danger' as const;
      case 'Low': return 'default' as const;
      default: return 'info' as const;
    }
  });

  priorityLabel = computed(() => {
    switch (this.task().priority) {
      case 'High': return 'Magas';
      case 'Low': return 'Alacsony';
      default: return 'Normal';
    }
  });
}
