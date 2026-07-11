import { describe, it, expect, vi } from 'vitest';
import { signal } from '@angular/core';
import {
  NotificationsPage,
  notificationIcon,
  notificationLabel,
  isUnread,
} from './notifications.page';
import { NotificationsApiService, NotificationDto, NotificationFeedResponse } from './notifications.api';

// NOTE: this project's vitest setup does not call TestBed.initTestEnvironment()
// (see src/test-setup.ts — only '@angular/compiler' is imported, no
// platform-browser-dynamic/testing bootstrap), so TestBed is unavailable here,
// consistent with every other *.spec.ts in this codebase (tasks.spec.ts,
// deadlines.spec.ts, search.spec.ts, …). Component logic is instead tested by
// invoking the prototype methods against a hand-built fake `this` — this
// exercises the exact same code paths (including the feed list driving the
// template's @for, and the markRead HTTP call) without needing Angular DI.

// ─── Class existence ──────────────────────────────────────────────────────────

describe('Notifications feature — class existence', () => {
  it('NotificationsPage is defined', () => {
    expect(NotificationsPage).toBeDefined();
    expect(typeof NotificationsPage).toBe('function');
  });

  it('NotificationsApiService is defined', () => {
    expect(NotificationsApiService).toBeDefined();
    expect(typeof NotificationsApiService).toBe('function');
  });

  it('NotificationsApiService has the expected method signatures', () => {
    expect(typeof NotificationsApiService.prototype.getFeed).toBe('function');
    expect(typeof NotificationsApiService.prototype.getUnreadCount).toBe('function');
    expect(typeof NotificationsApiService.prototype.markRead).toBe('function');
    expect(typeof NotificationsApiService.prototype.markAllRead).toBe('function');
  });

  it('NotificationsPage exposes the expected method signatures', () => {
    expect(typeof NotificationsPage.prototype.load).toBe('function');
    expect(typeof NotificationsPage.prototype.loadMore).toBe('function');
    expect(typeof NotificationsPage.prototype.open).toBe('function');
    expect(typeof NotificationsPage.prototype.markRead).toBe('function');
    expect(typeof NotificationsPage.prototype.markAllRead).toBe('function');
    expect(typeof NotificationsPage.prototype.toggleUnread).toBe('function');
  });
});

// ─── notificationIcon / notificationLabel — DailyDigest type recognition ─────

describe('notificationIcon', () => {
  it('DailyDigest → 📋', () => {
    expect(notificationIcon('DailyDigest')).toBe('📋');
  });

  it('unknown type → default bell icon (does not throw)', () => {
    expect(() => notificationIcon('SomeFutureType')).not.toThrow();
    expect(notificationIcon('SomeFutureType')).toBe('🔔');
  });

  it('empty string → default bell icon', () => {
    expect(notificationIcon('')).toBe('🔔');
  });
});

describe('notificationLabel', () => {
  it('DailyDigest → "Napi összefoglaló"', () => {
    expect(notificationLabel('DailyDigest')).toBe('Napi összefoglaló');
  });

  it('unknown type → generic fallback label (does not throw)', () => {
    expect(() => notificationLabel('SomeFutureType')).not.toThrow();
    expect(notificationLabel('SomeFutureType')).toBe('Értesítés');
  });
});

// ─── isUnread ─────────────────────────────────────────────────────────────────

describe('isUnread', () => {
  it('no readUtc → unread', () => {
    expect(isUnread({ readUtc: undefined })).toBe(true);
  });

  it('readUtc set → read', () => {
    expect(isUnread({ readUtc: '2026-07-11T08:00:00Z' })).toBe(false);
  });
});

// ─── Component method behaviour (fake `this`, no Angular DI required) ────────

function makeNotification(overrides: Partial<NotificationDto> = {}): NotificationDto {
  return {
    id: 'n-1',
    type: 'DailyDigest',
    title: 'Napi összefoglaló – 2026. 07. 11.',
    body: 'Jó reggelt!\n\n📅 Mai és holnapi emlékeztetők (1):\n- 08:00 · Orvosi vizit',
    actionUrl: '/dashboard',
    createdUtc: '2026-07-11T05:00:00Z',
    ...overrides,
  };
}

function feedResponse(items: NotificationDto[], hasMore = false): NotificationFeedResponse {
  return { items, totalCount: items.length, hasMore };
}

function ofValue<T>(value: T) {
  return { subscribe: (observer: { next: (v: T) => void }) => observer.next(value) };
}

/**
 * Builds a fake `this` context with the same fields NotificationsPage relies
 * on, then binds the real prototype methods to it — so cross-calls like
 * open() -> this.markRead() or toggleUnread() -> this.load() exercise the
 * actual implementation, not a stub.
 */
function makeFakeCtx(overrides: Partial<Record<string, unknown>> = {}) {
  const ctx: Record<string, unknown> = {
    api: {
      getFeed: vi.fn(),
      markRead: vi.fn(),
      markAllRead: vi.fn(),
    },
    router: { navigateByUrl: vi.fn() },
    notify: { success: vi.fn(), error: vi.fn(), info: vi.fn() },
    pageSize: 20,
    currentPage: 1,
    items: signal<NotificationDto[]>([]),
    loading: signal(false),
    error: signal(false),
    onlyUnread: signal(false),
    hasMore: signal(false),
    actingId: signal<string | null>(null),
    ...overrides,
  };
  ctx['load'] = NotificationsPage.prototype.load.bind(ctx);
  ctx['loadMore'] = NotificationsPage.prototype.loadMore.bind(ctx);
  ctx['markRead'] = NotificationsPage.prototype.markRead.bind(ctx);
  ctx['markAllRead'] = NotificationsPage.prototype.markAllRead.bind(ctx);
  ctx['open'] = NotificationsPage.prototype.open.bind(ctx);
  ctx['toggleUnread'] = NotificationsPage.prototype.toggleUnread.bind(ctx);
  return ctx as unknown as {
    api: { getFeed: ReturnType<typeof vi.fn>; markRead: ReturnType<typeof vi.fn>; markAllRead: ReturnType<typeof vi.fn> };
    router: { navigateByUrl: ReturnType<typeof vi.fn> };
    notify: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };
    items: ReturnType<typeof signal<NotificationDto[]>>;
    loading: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<boolean>>;
    onlyUnread: ReturnType<typeof signal<boolean>>;
    hasMore: ReturnType<typeof signal<boolean>>;
    actingId: ReturnType<typeof signal<string | null>>;
    load: () => void;
    loadMore: () => void;
    markRead: (id: string, silent?: boolean) => void;
    markAllRead: () => void;
    open: (n: NotificationDto) => void;
    toggleUnread: () => void;
  };
}

describe('NotificationsPage.load — populates the items signal that drives the feed list', () => {
  it('sets items from the API response (this is what the @for in the template renders)', () => {
    const digest = makeNotification();
    const other = makeNotification({ id: 'n-2', type: 'ReminderDue', title: 'Feladat esedékes' });
    const ctx = makeFakeCtx();
    (ctx.api.getFeed as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(feedResponse([digest, other], true)));

    ctx.load();

    expect(ctx.api.getFeed).toHaveBeenCalledWith(false, 1, 20);
    expect(ctx.items()).toHaveLength(2);
    expect(ctx.items().map(i => i.title)).toEqual(['Napi összefoglaló – 2026. 07. 11.', 'Feladat esedékes']);
    expect(ctx.hasMore()).toBe(true);
    expect(ctx.loading()).toBe(false);
  });

  it('DailyDigest item keeps its type through to the rendered list', () => {
    const digest = makeNotification();
    const ctx = makeFakeCtx();
    (ctx.api.getFeed as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(feedResponse([digest])));

    ctx.load();

    expect(ctx.items()[0].type).toBe('DailyDigest');
    expect(notificationIcon(ctx.items()[0].type)).toBe('📋');
    expect(notificationLabel(ctx.items()[0].type)).toBe('Napi összefoglaló');
  });

  it('an empty feed results in an empty items list (drives the empty-state branch)', () => {
    const ctx = makeFakeCtx();
    (ctx.api.getFeed as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(feedResponse([])));

    ctx.load();

    expect(ctx.items()).toHaveLength(0);
  });

  it('sets error state when the API call fails', () => {
    const ctx = makeFakeCtx();
    (ctx.api.getFeed as ReturnType<typeof vi.fn>).mockReturnValue({
      subscribe: (observer: { error: () => void }) => observer.error(),
    });

    ctx.load();

    expect(ctx.error()).toBe(true);
    expect(ctx.loading()).toBe(false);
  });
});

describe('NotificationsPage.markRead — calls the API and updates local state', () => {
  it('calls api.markRead with the given id', () => {
    const ctx = makeFakeCtx({ items: signal([makeNotification()]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(undefined));

    ctx.markRead('n-1');

    expect(ctx.api.markRead).toHaveBeenCalledWith('n-1');
    expect(ctx.api.markRead).toHaveBeenCalledTimes(1);
  });

  it('marks the matching item as read (readUtc becomes set) after a successful call', () => {
    const ctx = makeFakeCtx({ items: signal([makeNotification()]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(undefined));

    expect(isUnread(ctx.items()[0])).toBe(true);
    ctx.markRead('n-1');
    expect(isUnread(ctx.items()[0])).toBe(false);
  });

  it('does not touch other items in the list', () => {
    const other = makeNotification({ id: 'n-2' });
    const ctx = makeFakeCtx({ items: signal([makeNotification(), other]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(undefined));

    ctx.markRead('n-1');

    expect(isUnread(ctx.items()[1])).toBe(true);
  });

  it('on failure (non-silent) it reports an error via the notification service', () => {
    const ctx = makeFakeCtx({ items: signal([makeNotification()]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue({
      subscribe: (observer: { error: () => void }) => observer.error(),
    });

    ctx.markRead('n-1');

    expect(ctx.notify.error).toHaveBeenCalled();
  });

  it('on failure with silent=true it does not report an error', () => {
    const ctx = makeFakeCtx({ items: signal([makeNotification()]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue({
      subscribe: (observer: { error: () => void }) => observer.error(),
    });

    ctx.markRead('n-1', true);

    expect(ctx.notify.error).not.toHaveBeenCalled();
  });
});

describe('NotificationsPage.open — click behaviour', () => {
  it('marks an unread item as read and navigates to its actionUrl', () => {
    const item = makeNotification();
    const ctx = makeFakeCtx({ items: signal([item]) });
    (ctx.api.markRead as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(undefined));

    ctx.open(item);

    expect(ctx.api.markRead).toHaveBeenCalledWith('n-1');
    expect(ctx.router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('does not call markRead again for an already-read item, but still navigates', () => {
    const item = makeNotification({ readUtc: '2026-07-11T06:00:00Z' });
    const ctx = makeFakeCtx({ items: signal([item]) });

    ctx.open(item);

    expect(ctx.api.markRead).not.toHaveBeenCalled();
    expect(ctx.router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
  });

  it('does not navigate when actionUrl is absent', () => {
    const item = makeNotification({ actionUrl: undefined, readUtc: '2026-07-11T06:00:00Z' });
    const ctx = makeFakeCtx({ items: signal([item]) });

    ctx.open(item);

    expect(ctx.router.navigateByUrl).not.toHaveBeenCalled();
  });
});

describe('NotificationsPage.markAllRead', () => {
  it('marks every item as read and shows a success toast', () => {
    const ctx = makeFakeCtx({ items: signal([makeNotification(), makeNotification({ id: 'n-2' })]) });
    (ctx.api.markAllRead as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(undefined));

    ctx.markAllRead();

    expect(ctx.items().every(i => !isUnread(i))).toBe(true);
    expect(ctx.notify.success).toHaveBeenCalled();
  });
});

describe('NotificationsPage.toggleUnread', () => {
  it('flips the onlyUnread signal and reloads via getFeed with the new value', () => {
    const ctx = makeFakeCtx();
    (ctx.api.getFeed as ReturnType<typeof vi.fn>).mockReturnValue(ofValue(feedResponse([])));

    expect(ctx.onlyUnread()).toBe(false);
    ctx.toggleUnread();
    expect(ctx.onlyUnread()).toBe(true);
    expect(ctx.api.getFeed).toHaveBeenCalledWith(true, 1, 20);
  });
});
