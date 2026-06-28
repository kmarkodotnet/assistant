import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';
import { NotificationsApiService } from '../features/notifications/notifications.api';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="h-14 border-b border-[var(--color-border)] flex items-center px-4 gap-4 bg-[var(--color-bg)]">
      <span class="font-bold text-primary-600 text-lg">Family OS</span>
      <div class="flex-1"></div>

      <!-- Bell icon with unread notification count -->
      <a
        routerLink="/reminders"
        data-testid="navbar-bell"
        class="relative p-2 rounded-lg hover:bg-[var(--color-surface)] text-[var(--color-text-muted)] hover:text-[var(--color-text)]"
        title="Emlékeztetők"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
        @if (unreadCount() > 0) {
          <span
            data-testid="navbar-bell-badge"
            class="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] rounded-full bg-danger-600 text-white text-[10px] font-bold flex items-center justify-center px-1"
          >{{ unreadCount() > 99 ? '99+' : unreadCount() }}</span>
        }
      </a>

      <span class="text-sm text-[var(--color-text-muted)]">{{ auth.currentUser()?.displayName }}</span>
      <button
        data-testid="navbar-logout"
        class="text-sm text-danger-600 hover:underline"
        (click)="auth.logout()"
      >{{ 'nav.logout' | translate }}</button>
    </header>
  `,
})
export class NavbarComponent implements OnInit {
  auth = inject(AuthService);
  private notificationsApi = inject(NotificationsApiService);

  unreadCount = signal(0);

  ngOnInit(): void {
    this.refreshUnread();
  }

  private refreshUnread(): void {
    this.notificationsApi.getUnreadCount().subscribe({
      next: r => this.unreadCount.set(r.totalCount),
      error: () => { /* silent — badge stays at 0 */ },
    });
  }
}
