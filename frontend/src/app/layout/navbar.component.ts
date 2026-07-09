import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';
import { NotificationsApiService } from '../features/notifications/notifications.api';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="h-14 border-b border-[var(--color-border)] flex items-center px-4 gap-3 bg-[var(--color-bg)]">
      <span class="font-bold text-primary-600 text-lg shrink-0">Family OS</span>

      <!-- Global search -->
      <div class="flex-1 max-w-sm">
        <div class="relative">
          <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)] pointer-events-none"
            fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round"
              d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 15.803 7.5 7.5 0 0015.803 15.803z" />
          </svg>
          <input
            data-testid="navbar-search"
            type="search"
            placeholder="Keresés..."
            [value]="searchQuery()"
            (input)="searchQuery.set($any($event.target).value)"
            (keydown.enter)="search()"
            class="w-full pl-9 pr-3 py-1.5 text-sm border border-[var(--color-border)] rounded-lg bg-[var(--color-surface)] focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent"
          />
        </div>
      </div>

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

      <span class="text-sm text-[var(--color-text-muted)] shrink-0">{{ auth.currentUser()?.displayName }}</span>
      <button
        data-testid="navbar-logout"
        class="text-sm text-danger-600 hover:underline shrink-0"
        (click)="auth.logout()"
      >{{ 'nav.logout' | translate }}</button>
    </header>
  `,
})
export class NavbarComponent implements OnInit {
  auth = inject(AuthService);
  private router = inject(Router);
  private notificationsApi = inject(NotificationsApiService);

  unreadCount = signal(0);
  searchQuery = signal('');

  ngOnInit(): void {
    this.refreshUnread();
  }

  search(): void {
    const q = this.searchQuery().trim();
    if (!q) return;
    void this.router.navigate(['/search'], { queryParams: { q } });
    this.searchQuery.set('');
  }

  private refreshUnread(): void {
    this.notificationsApi.getUnreadCount().subscribe({
      next: r => this.unreadCount.set(r.totalCount),
      error: () => { /* silent */ },
    });
  }
}
