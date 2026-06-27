import { Component, ChangeDetectionStrategy, input } from '@angular/core';

type BadgeVariant = 'default' | 'success' | 'warn' | 'danger' | 'info';

const BADGE_CLASSES: Record<BadgeVariant, string> = {
  default: 'bg-gray-100 text-gray-700',
  success: 'bg-success-50 text-success-700',
  warn:    'bg-warn-100 text-warn-700',
  danger:  'bg-danger-50 text-danger-700',
  info:    'bg-primary-50 text-primary-700',
};

@Component({
  selector: 'ui-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span [class]="'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ' + badgeClass()">
      <ng-content />
    </span>
  `,
})
export class BadgeComponent {
  variant = input<BadgeVariant>('default');
  badgeClass() { return BADGE_CLASSES[this.variant()]; }
}
