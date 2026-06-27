import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'ui-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center py-12 text-center">
      <span class="text-4xl mb-4">{{ icon() }}</span>
      <p class="font-semibold text-[var(--color-text)]">{{ title() }}</p>
      @if (message()) {
        <p class="text-sm text-[var(--color-text-muted)] mt-1">{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
})
export class EmptyStateComponent {
  icon = input('📭');
  title = input('Nincs találat');
  message = input<string | undefined>(undefined);
}
