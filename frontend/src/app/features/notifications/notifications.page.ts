import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { NotificationsApiService, NotificationDto } from './notifications.api';
import { NotificationService } from '../../core/notifications/notification.service';

/** Type -> megjelenítési metaadat. Ismeretlen type esetén a default-ra esik vissza. */
const TYPE_META: Record<string, { icon: string; label: string }> = {
  DailyDigest: { icon: '📋', label: 'Napi összefoglaló' },
};
const DEFAULT_TYPE_META = { icon: '🔔', label: 'Értesítés' };

export function notificationIcon(type: string): string {
  return TYPE_META[type]?.icon ?? DEFAULT_TYPE_META.icon;
}

export function notificationLabel(type: string): string {
  return TYPE_META[type]?.label ?? DEFAULT_TYPE_META.label;
}

export function isUnread(n: Pick<NotificationDto, 'readUtc'>): boolean {
  return !n.readUtc;
}

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto space-y-6">

      <div class="flex items-center justify-between gap-2 flex-wrap">
        <h1 class="text-2xl font-semibold">Értesítések</h1>
        <div class="flex items-center gap-2">
          <button
            data-testid="notifications-toggle-unread"
            class="text-sm px-3 py-1.5 rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)]"
            (click)="toggleUnread()"
          >{{ onlyUnread() ? 'Összes' : 'Csak olvasatlan' }}</button>
          <button
            data-testid="notifications-mark-all-read"
            class="text-sm px-3 py-1.5 rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)]"
            (click)="markAllRead()"
          >Összes olvasottnak jelölése</button>
        </div>
      </div>

      @if (loading()) {
        <div class="text-center py-12 text-[var(--color-text-muted)]">Betöltés…</div>
      } @else if (error()) {
        <div class="text-center py-12">
          <p class="text-danger-600 mb-3">Hiba történt</p>
          <button class="btn-secondary" (click)="load()">Újra</button>
        </div>
      } @else if (isEmpty()) {
        <div data-testid="notifications-empty" class="text-center py-16 text-[var(--color-text-muted)]">
          <p class="text-4xl mb-3">🔔</p>
          <p class="font-medium">Nincsenek értesítések</p>
        </div>
      } @else {
        <div class="space-y-2">
          @for (n of items(); track n.id) {
            <div
              data-testid="notification-item"
              [attr.data-type]="n.type"
              [attr.data-unread]="isUnread(n)"
              class="rounded-xl border p-4 cursor-pointer"
              [class.border-primary-300]="isUnread(n)"
              [class.bg-primary-50/30]="isUnread(n)"
              [class.border-[var(--color-border)]]="!isUnread(n)"
              (click)="open(n)"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <span class="text-lg" aria-hidden="true">{{ notificationIcon(n.type) }}</span>
                    <span class="text-xs uppercase tracking-wider text-[var(--color-text-muted)]">{{ notificationLabel(n.type) }}</span>
                    @if (isUnread(n)) {
                      <span data-testid="notification-unread-dot" class="w-2 h-2 rounded-full bg-primary-600"></span>
                    }
                  </div>
                  <p class="font-medium mt-1">{{ n.title }}</p>
                  @if (n.body) {
                    <p class="text-sm text-[var(--color-text-muted)] mt-1" style="white-space: pre-line">{{ n.body }}</p>
                  }
                  <p class="text-xs text-[var(--color-text-muted)] mt-2">{{ n.createdUtc | date:'yyyy. MM. dd. HH:mm' }}</p>
                </div>
                @if (isUnread(n)) {
                  <button
                    data-testid="notification-mark-read"
                    class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50 shrink-0"
                    [disabled]="actingId() === n.id"
                    (click)="$event.stopPropagation(); markRead(n.id)"
                  >Olvasott</button>
                }
              </div>
            </div>
          }
        </div>
        @if (hasMore()) {
          <div class="text-center">
            <button
              data-testid="notifications-load-more"
              class="text-sm px-3 py-1.5 rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)]"
              (click)="loadMore()"
            >Továbbiak</button>
          </div>
        }
      }
    </div>
  `,
})
export class NotificationsPage implements OnInit {
  private api = inject(NotificationsApiService);
  private router = inject(Router);
  private notify = inject(NotificationService);

  readonly pageSize = 20;
  private currentPage = 1;

  items = signal<NotificationDto[]>([]);
  loading = signal(false);
  error = signal(false);
  onlyUnread = signal(false);
  hasMore = signal(false);
  actingId = signal<string | null>(null);

  isEmpty = computed(() => this.items().length === 0);

  readonly notificationIcon = notificationIcon;
  readonly notificationLabel = notificationLabel;
  readonly isUnread = isUnread;

  ngOnInit(): void {
    this.load();
  }

  toggleUnread(): void {
    this.onlyUnread.set(!this.onlyUnread());
    this.load();
  }

  load(): void {
    this.currentPage = 1;
    this.loading.set(true);
    this.error.set(false);
    this.api.getFeed(this.onlyUnread(), this.currentPage, this.pageSize).subscribe({
      next: r => {
        this.items.set(r.items);
        this.hasMore.set(r.hasMore);
        this.loading.set(false);
      },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  loadMore(): void {
    const nextPage = this.currentPage + 1;
    this.api.getFeed(this.onlyUnread(), nextPage, this.pageSize).subscribe({
      next: r => {
        this.currentPage = nextPage;
        this.items.set([...this.items(), ...r.items]);
        this.hasMore.set(r.hasMore);
      },
      error: () => this.notify.error('Hiba a betöltésnél'),
    });
  }

  /** Kattintás egy értesítésre: olvasottá jelöli (ha még nem az), majd navigál az actionUrl-re, ha van. */
  open(n: NotificationDto): void {
    if (isUnread(n)) {
      this.markRead(n.id, true);
    }
    if (n.actionUrl) {
      void this.router.navigateByUrl(n.actionUrl);
    }
  }

  markRead(id: string, silent = false): void {
    this.actingId.set(id);
    this.api.markRead(id).subscribe({
      next: () => {
        this.items.update(items =>
          items.map(i => (i.id === id ? { ...i, readUtc: new Date().toISOString() } : i)),
        );
        this.actingId.set(null);
      },
      error: () => {
        this.actingId.set(null);
        if (!silent) this.notify.error('Hiba az olvasottá jelölésnél');
      },
    });
  }

  markAllRead(): void {
    this.api.markAllRead().subscribe({
      next: () => {
        this.items.update(items =>
          items.map(i => (i.readUtc ? i : { ...i, readUtc: new Date().toISOString() })),
        );
        this.notify.success('Minden értesítés olvasottnak jelölve');
      },
      error: () => this.notify.error('Hiba a művelet során'),
    });
  }
}
