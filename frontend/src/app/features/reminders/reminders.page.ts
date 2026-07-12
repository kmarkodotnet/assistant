import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { RemindersApiService, ReminderDto, ReminderGroupDto } from './reminders.api';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-reminders-page',
  standalone: true,
  imports: [CommonModule, TranslateModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto space-y-6">

      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">{{ 'nav.reminders' | translate }}</h1>
        <button
          class="text-sm px-3 py-1.5 rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)]"
          (click)="toggleUpcoming()"
        >{{ showUpcoming ? 'Összes' : 'Közelgő (30 nap)' }}</button>
      </div>

      @if (loading()) {
        <div class="text-center py-12 text-[var(--color-text-muted)]">{{ 'common.loading' | translate }}</div>
      } @else if (error()) {
        <div class="text-center py-12">
          <p class="text-danger-600 mb-3">{{ 'common.error' | translate }}</p>
          <button class="btn-secondary" (click)="load()">{{ 'common.retry' | translate }}</button>
        </div>
      } @else {
        @if (groups()?.missed?.length) {
          <section>
            <h2 class="text-xs font-semibold text-danger-600 uppercase tracking-wider mb-2">
              Lecsúszott · {{ groups()!.missed.length }}
            </h2>
            <div class="space-y-2">
              @for (r of groups()!.missed; track r.id) {
                <div class="rounded-xl border border-danger-200 bg-danger-50/30 p-4">
                  <ng-container [ngTemplateOutlet]="actions" [ngTemplateOutletContext]="{ $implicit: r }" />
                </div>
              }
            </div>
          </section>
        }

        @if (groups()?.now?.length) {
          <section>
            <h2 class="text-xs font-semibold text-warning-600 uppercase tracking-wider mb-2">
              Most esedékes · {{ groups()!.now.length }}
            </h2>
            <div class="space-y-2">
              @for (r of groups()!.now; track r.id) {
                <div class="rounded-xl border border-warning-200 bg-[var(--color-bg)] p-4">
                  <ng-container [ngTemplateOutlet]="actions" [ngTemplateOutletContext]="{ $implicit: r }" />
                </div>
              }
            </div>
          </section>
        }

        @if (groups()?.week?.length) {
          <section>
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">
              A héten · {{ groups()!.week.length }}
            </h2>
            <div class="space-y-2">
              @for (r of groups()!.week; track r.id) {
                <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-4">
                  <ng-container [ngTemplateOutlet]="actions" [ngTemplateOutletContext]="{ $implicit: r }" />
                </div>
              }
            </div>
          </section>
        }

        @if (groups()?.later?.length) {
          <section>
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2">
              Később · {{ groups()!.later.length }}
            </h2>
            <div class="space-y-2">
              @for (r of groups()!.later; track r.id) {
                <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-4">
                  <ng-container [ngTemplateOutlet]="actions" [ngTemplateOutletContext]="{ $implicit: r }" />
                </div>
              }
            </div>
          </section>
        }

        @if (isEmpty()) {
          <div class="text-center py-16 text-[var(--color-text-muted)]">
            <p class="text-4xl mb-3">🔔</p>
            <p class="font-medium">Nincsenek emlékeztetők</p>
            <p class="text-sm mt-1">Elfogadott feladatokhoz és határidőkhöz automatikusan generálódnak.</p>
          </div>
        }
      }
    </div>

    <ng-template #actions let-r>
      <div class="flex items-start justify-between gap-3">
        <div class="flex-1 min-w-0">
          <div class="flex flex-wrap items-center gap-2">
            <span class="text-sm font-medium">
              {{ r.taskId ? 'Feladat emlékeztető' : (r.deadlineId ? 'Határidő emlékeztető' : 'Emlékeztető') }}
            </span>
            @if (r.escalationLevel > 0) {
              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                Eszkalált ({{ r.escalationLevel }}×)
              </span>
            }
            @if (r.rruleExpression) {
              <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-primary-50 text-primary-700">
                Ismétlődő
              </span>
            }
          </div>
          <p class="text-xs text-[var(--color-text-muted)] mt-1">
            {{ r.triggerUtc | date:'yyyy. MM. dd. HH:mm' }} · {{ r.status }}
          </p>
        </div>
        <div class="flex items-center gap-1 shrink-0">
          @if (r.status === 'Fired' || r.status === 'Scheduled') {
            <button
              data-testid="reminder-acknowledge"
              class="px-2 py-1 text-xs rounded-lg bg-success-600 text-white hover:bg-success-700 disabled:opacity-50"
              [disabled]="actingId() === r.id"
              (click)="acknowledge(r.id)"
            >Kész</button>
            <button
              data-testid="reminder-snooze"
              class="px-2 py-1 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] disabled:opacity-50"
              [disabled]="actingId() === r.id"
              (click)="openSnooze(r.id)"
            >Halaszt</button>
            <button
              data-testid="reminder-skip"
              class="px-2 py-1 text-xs rounded-lg text-[var(--color-text-muted)] hover:text-danger-600 disabled:opacity-50"
              [disabled]="actingId() === r.id"
              (click)="skip(r.id)"
            >Mellőz</button>
          }
        </div>
      </div>
    </ng-template>

    @if (snoozeTarget()) {
      <div
        class="fixed inset-0 bg-black/40 z-50 flex items-end md:items-center justify-center"
        (click)="closeSnooze()"
      >
        <div
          class="bg-[var(--color-bg)] rounded-t-2xl md:rounded-2xl p-6 w-full max-w-sm shadow-xl"
          (click)="$event.stopPropagation()"
        >
          <h3 class="font-semibold mb-4">Halasztás</h3>
          <div class="space-y-2">
            @for (opt of snoozeOptions; track opt.label) {
              <button
                class="w-full text-left px-4 py-3 rounded-xl border border-[var(--color-border)] hover:bg-[var(--color-surface)]"
                (click)="snooze(snoozeTarget()!, opt.minutes)"
              >{{ opt.label }}</button>
            }
          </div>
          <button class="mt-4 w-full text-center text-sm text-[var(--color-text-muted)]" (click)="closeSnooze()">
            Mégse
          </button>
        </div>
      </div>
    }
  `,
})
export class RemindersPage implements OnInit {
  private api = inject(RemindersApiService);
  private notify = inject(NotificationService);

  groups = signal<ReminderGroupDto | null>(null);
  loading = signal(false);
  error = signal(false);
  actingId = signal<string | null>(null);
  snoozeTarget = signal<string | null>(null);
  showUpcoming = false;

  readonly snoozeOptions = [
    { label: '1 óra', minutes: 60 },
    { label: '4 óra', minutes: 240 },
    { label: 'Holnap reggel (12 óra)', minutes: 720 },
    { label: 'Holnap (24 óra)', minutes: 1440 },
  ];

  isEmpty = computed(() => {
    const g = this.groups();
    if (!g) return true;
    return !g.now.length && !g.week.length && !g.later.length && !g.missed.length;
  });

  ngOnInit(): void {
    this.load();
  }

  toggleUpcoming(): void {
    this.showUpcoming = !this.showUpcoming;
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(false);
    this.api.list(this.showUpcoming).subscribe({
      next: g => { this.groups.set(g); this.loading.set(false); },
      error: () => { this.error.set(true); this.loading.set(false); },
    });
  }

  acknowledge(id: string): void {
    this.actingId.set(id);
    this.api.acknowledge(id).subscribe({
      next: () => {
        this.notify.success('Emlékeztető nyugtázva');
        this.actingId.set(null);
        this.load();
      },
      error: () => {
        this.notify.error('Hiba a nyugtázásnál');
        this.actingId.set(null);
      },
    });
  }

  openSnooze(id: string): void {
    this.snoozeTarget.set(id);
  }

  closeSnooze(): void {
    this.snoozeTarget.set(null);
  }

  snooze(id: string, minutes: number): void {
    this.closeSnooze();
    this.actingId.set(id);
    this.api.snooze(id, minutes).subscribe({
      next: () => {
        this.notify.success('Emlékeztető halasztva');
        this.actingId.set(null);
        this.load();
      },
      error: () => {
        this.notify.error('Hiba a halasztásnál');
        this.actingId.set(null);
      },
    });
  }

  skip(id: string): void {
    this.actingId.set(id);
    this.api.skip(id).subscribe({
      next: () => {
        this.notify.info('Emlékeztető mellőzve');
        this.actingId.set(null);
        this.load();
      },
      error: () => {
        this.notify.error('Hiba a mellőzésnél');
        this.actingId.set(null);
      },
    });
  }
}
