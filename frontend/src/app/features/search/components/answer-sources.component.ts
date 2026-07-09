import {
  Component,
  ChangeDetectionStrategy,
  input,
} from '@angular/core';

@Component({
  selector: 'app-answer-sources',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-1.5 text-xs text-[var(--color-text-muted)]">
      <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
        <path stroke-linecap="round" stroke-linejoin="round"
          d="M13.828 10.172a4 4 0 00-5.656 0l-4 4a4 4 0 105.656 5.656l1.102-1.101m-.758-4.899a4 4 0 005.656 0l4-4a4 4 0 00-5.656-5.656l-1.1 1.1" />
      </svg>
      <span>Forrás: {{ count() }} dokumentum</span>
      @if (sources().length > 0) {
        <span class="text-[var(--color-text-muted)]">
          ({{ sources().slice(0, 3).join(', ') }}{{ sources().length > 3 ? '...' : '' }})
        </span>
      }
    </div>
  `,
})
export class AnswerSourcesComponent {
  sources = input.required<string[]>();
  count = input.required<number>();
}
