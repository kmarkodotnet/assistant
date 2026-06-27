import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonComponent } from './button.component';

@Component({
  selector: 'ui-confirm-dialog',
  standalone: true,
  imports: [TranslateModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50" data-testid="confirm-dialog-overlay">
      <div class="bg-[var(--color-bg)] rounded-xl p-6 max-w-sm w-full shadow-xl mx-4">
        <h3 class="font-semibold text-lg mb-2">{{ title() }}</h3>
        <p class="text-[var(--color-text-muted)] text-sm mb-6">{{ message() }}</p>
        <div class="flex gap-3 justify-end">
          <ui-button variant="ghost" (click)="cancel.emit()" data-testid="confirm-cancel">
            {{ cancelLabel() }}
          </ui-button>
          <ui-button [variant]="confirmVariant()" (click)="confirm.emit()" data-testid="confirm-ok">
            {{ confirmLabel() }}
          </ui-button>
        </div>
      </div>
    </div>
  `,
})
export class ConfirmDialogComponent {
  title = input('Megerősítés szükséges');
  message = input('Biztosan folytatod?');
  confirmLabel = input('Igen');
  cancelLabel = input('Mégse');
  confirmVariant = input<'primary' | 'danger'>('danger');

  confirm = output<void>();
  cancel = output<void>();
}
