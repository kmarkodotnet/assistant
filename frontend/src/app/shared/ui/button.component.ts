import { Component, ChangeDetectionStrategy, input } from '@angular/core';

type ButtonVariant = 'primary' | 'ghost' | 'danger' | 'warning-suggested';

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  'primary': 'bg-primary-600 text-white hover:bg-primary-700 focus:ring-primary-500',
  'ghost': 'bg-transparent text-[var(--color-text)] hover:bg-gray-100 dark:hover:bg-gray-700',
  'danger': 'bg-danger-600 text-white hover:bg-danger-700 focus:ring-danger-500',
  'warning-suggested': 'bg-warn-100 text-warn-800 border border-warn-300 hover:bg-warn-200',
};

@Component({
  selector: 'ui-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      [class]="baseClass + ' ' + variantClass()"
      [disabled]="disabled()"
      [attr.data-testid]="testId()"
    ><ng-content /></button>
  `,
})
export class ButtonComponent {
  variant = input<ButtonVariant>('primary');
  disabled = input(false);
  testId = input<string | undefined>(undefined);

  baseClass = 'inline-flex items-center justify-center px-4 py-2 rounded-lg text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';

  variantClass() {
    return VARIANT_CLASSES[this.variant()];
  }
}
