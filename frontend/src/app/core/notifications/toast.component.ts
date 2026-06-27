import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-sm">
      @for (toast of notify.toasts(); track toast.id) {
        <div
          data-testid="toast-message"
          [class]="toastClass(toast.type)"
          class="flex items-start gap-3 p-4 rounded-xl shadow-lg text-sm"
        >
          <span>{{ toast.type === 'success' ? '✅' : toast.type === 'error' ? '❌' : 'ℹ️' }}</span>
          <span class="flex-1">{{ toast.message }}</span>
          <button
            data-testid="toast-dismiss"
            class="text-current opacity-60 hover:opacity-100 ml-2"
            (click)="notify.dismiss(toast.id)"
          >✕</button>
        </div>
      }
    </div>
  `,
})
export class ToastComponent {
  notify = inject(NotificationService);

  toastClass(type: string): string {
    switch (type) {
      case 'success': return 'bg-success-50 border border-success-200 text-success-800';
      case 'error':   return 'bg-danger-50 border border-danger-200 text-danger-800';
      default:        return 'bg-primary-50 border border-primary-200 text-primary-800';
    }
  }
}
