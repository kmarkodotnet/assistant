import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'ui-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl p-4 shadow-sm">
      <ng-content />
    </div>
  `,
})
export class CardComponent {}
