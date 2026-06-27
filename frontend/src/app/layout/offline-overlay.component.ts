import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { HeartbeatService } from '../core/realtime/heartbeat.service';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-offline-overlay',
  standalone: true,
  imports: [TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="fixed inset-0 z-50 bg-[var(--color-bg)] flex flex-col items-center justify-center text-center p-8" data-testid="offline-overlay">
      <span class="text-6xl mb-6">📡</span>
      <h2 class="text-xl font-bold mb-2">{{ 'offline.title' | translate }}</h2>
      <p class="text-[var(--color-text-muted)] max-w-sm mb-8">{{ 'offline.message' | translate }}</p>
      <div class="flex gap-4">
        <button
          data-testid="offline-retry"
          class="bg-primary-600 text-white px-6 py-2 rounded-lg hover:bg-primary-700"
          (click)="heartbeat.retry()"
        >{{ 'offline.retry' | translate }}</button>
        <button
          data-testid="offline-logout"
          class="text-[var(--color-text-muted)] px-6 py-2 hover:underline"
          (click)="auth.logout()"
        >{{ 'offline.logout' | translate }}</button>
      </div>
    </div>
  `,
})
export class OfflineOverlayComponent {
  heartbeat = inject(HeartbeatService);
  auth = inject(AuthService);
}
