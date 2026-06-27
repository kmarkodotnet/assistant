import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
  sticky: boolean;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly toasts = signal<ToastMessage[]>([]);

  private show(type: ToastMessage['type'], message: string, sticky = false): void {
    const id = crypto.randomUUID();
    this.toasts.update(t => [...t, { id, type, message, sticky }]);
    if (!sticky) {
      setTimeout(() => this.dismiss(id), 3000);
    }
  }

  success(message: string): void { this.show('success', message); }
  error(message: string, sticky = false): void { this.show('error', message, sticky); }
  info(message: string): void { this.show('info', message); }

  dismiss(id: string): void {
    this.toasts.update(t => t.filter(toast => toast.id !== id));
  }
}
