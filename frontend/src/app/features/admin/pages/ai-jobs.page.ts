import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { combineLatest, interval, startWith, switchMap, catchError, of, tap } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AiJobsApiService, AiJobDto, PagedResult } from '../services/ai-jobs.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { NotificationService } from '../../../core/notifications/notification.service';
import { QueueStatsComponent } from '../components/queue-stats.component';

type BadgeVariant = 'default' | 'success' | 'warn' | 'danger' | 'info';

const STATUS_BADGE: Record<string, BadgeVariant> = {
  Running: 'info',
  Queued: 'warn',
  Failed: 'danger',
  Completed: 'success',
  Cancelled: 'default',
};

@Component({
  selector: 'app-ai-jobs-page',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslateModule, ButtonComponent, SkeletonComponent, BadgeComponent, QueueStatsComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-6xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-bold">{{ 'admin.aiJobs.title' | translate }}</h1>
        <span class="text-xs text-[var(--color-text-muted)]">Automatikus frissítés: 30 mp</span>
      </div>

      <!-- Queue stats widget -->
      <div class="mb-4">
        <app-queue-stats />
      </div>

      <!-- Filters -->
      <div class="bg-white border border-[var(--color-border)] rounded-xl p-4 mb-4 flex flex-wrap gap-3 items-end">
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Státusz</label>
          <select [(ngModel)]="statusFilter" (ngModelChange)="onFilterChange()"
            class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm">
            <option value="">Minden</option>
            <option value="Queued">Queued</option>
            <option value="Running">Running</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Feladat típus</label>
          <input type="text" [(ngModel)]="jobTypeFilter" (ngModelChange)="onFilterChange()"
            placeholder="pl. DocumentAnalysis"
            class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm w-48" />
        </div>
      </div>

      <!-- Loading -->
      @if (loading()) {
        <div class="flex flex-col gap-2">
          @for (i of [1,2,3,4,5]; track i) { <ui-skeleton height="56px" /> }
        </div>
      }

      <!-- Error -->
      @if (error() && !loading()) {
        <div class="bg-danger-50 text-danger-700 rounded-xl px-4 py-3 text-sm">
          Betöltés sikertelen.
        </div>
      }

      <!-- Jobs list -->
      @if (!loading() && !error() && result()) {
        @if (result()!.items.length === 0) {
          <div class="text-center py-16 text-[var(--color-text-muted)] text-sm">Nincs találat.</div>
        } @else {
          <div class="flex flex-col gap-2">
            @for (job of result()!.items; track job.id) {
              <div class="bg-white border border-[var(--color-border)] rounded-xl px-4 py-3">
                <div class="flex items-start justify-between gap-4">
                  <div class="min-w-0">
                    <div class="flex items-center gap-2 flex-wrap">
                      <span class="font-mono text-xs text-[var(--color-text-muted)]">{{ job.id | slice:0:8 }}…</span>
                      <span class="text-sm font-medium">{{ job.jobType }}</span>
                      <ui-badge [variant]="statusBadge(job.status)">{{ job.status }}</ui-badge>
                      <span class="text-xs text-[var(--color-text-muted)]">{{ job.attemptCount }} kísérlet</span>
                    </div>
                    <div class="text-xs text-[var(--color-text-muted)] mt-1">
                      {{ job.targetEntityType }} / {{ job.targetEntityId | slice:0:8 }}
                      — {{ job.createdUtc | date:'yyyy-MM-dd HH:mm' }}
                    </div>
                    @if (job.errorMessage) {
                      <button (click)="toggleError(job.id)"
                        class="text-xs text-danger-600 underline mt-1">
                        {{ expandedErrors().has(job.id) ? 'Hiba elrejtése' : 'Hiba megtekintése' }}
                      </button>
                      @if (expandedErrors().has(job.id)) {
                        <pre class="mt-1 text-xs bg-red-50 rounded p-2 whitespace-pre-wrap break-all max-w-xl">{{ job.errorMessage }}</pre>
                      }
                    }
                  </div>
                  <div class="flex gap-2 shrink-0">
                    @if (job.status === 'Failed' || job.status === 'Queued') {
                      <ui-button variant="ghost" [disabled]="actionLoading()" (click)="retry(job.id)">
                        {{ 'admin.aiJobs.retry' | translate }}
                      </ui-button>
                    }
                    @if (job.status === 'Running') {
                      <ui-button variant="danger" [disabled]="actionLoading()" (click)="cancel(job.id)">
                        {{ 'admin.aiJobs.cancel' | translate }}
                      </ui-button>
                    }
                  </div>
                </div>
              </div>
            }
          </div>

          <!-- Pagination -->
          <div class="flex items-center justify-between mt-4 text-sm">
            <span class="text-[var(--color-text-muted)]">
              Összesen: {{ result()!.totalCount }} feladat
            </span>
            <div class="flex gap-2">
              <ui-button variant="ghost" [disabled]="page() <= 1" (click)="prevPage()">
                &lsaquo; Előző
              </ui-button>
              <ui-button variant="ghost" [disabled]="page() >= result()!.totalPages" (click)="nextPage()">
                Következő &rsaquo;
              </ui-button>
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class AiJobsPage {
  private readonly api = inject(AiJobsApiService);
  private readonly notify = inject(NotificationService);

  statusFilter = '';
  jobTypeFilter = '';

  readonly page = signal(1);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly actionLoading = signal(false);
  readonly expandedErrors = signal(new Set<string>());

  private readonly filterSignal = signal({ status: '', jobType: '', page: 1 });

  private readonly result$ = combineLatest([
    toObservable(this.filterSignal),
    interval(30000).pipe(startWith(0)),
  ]).pipe(
    switchMap(([f]) => {
      this.loading.set(true);
      this.error.set(null);
      return this.api.list(f.status || undefined, f.jobType || undefined, f.page).pipe(
        tap(() => this.loading.set(false)),
        catchError(() => {
          this.loading.set(false);
          this.error.set('Betöltés sikertelen');
          return of(null);
        })
      );
    })
  );

  readonly result = toSignal(this.result$, { initialValue: null as PagedResult<AiJobDto> | null });

  onFilterChange(): void {
    this.page.set(1);
    this.filterSignal.set({ status: this.statusFilter, jobType: this.jobTypeFilter, page: 1 });
  }

  prevPage(): void {
    const p = Math.max(1, this.page() - 1);
    this.page.set(p);
    this.filterSignal.update(f => ({ ...f, page: p }));
  }

  nextPage(): void {
    const p = this.page() + 1;
    this.page.set(p);
    this.filterSignal.update(f => ({ ...f, page: p }));
  }

  statusBadge(status: string): BadgeVariant {
    return STATUS_BADGE[status] ?? 'default';
  }

  toggleError(id: string): void {
    this.expandedErrors.update(set => {
      const next = new Set(set);
      if (next.has(id)) { next.delete(id); } else { next.add(id); }
      return next;
    });
  }

  async retry(id: string): Promise<void> {
    this.actionLoading.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.api.retry(id).subscribe({ complete: resolve, error: reject })
      );
      this.notify.success('Feladat újraindítva.');
      this.filterSignal.update(f => ({ ...f }));
    } catch {
      this.notify.error('Újraindítás sikertelen.');
    } finally {
      this.actionLoading.set(false);
    }
  }

  async cancel(id: string): Promise<void> {
    this.actionLoading.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.api.cancel(id).subscribe({ complete: resolve, error: reject })
      );
      this.notify.success('Feladat törölve.');
      this.filterSignal.update(f => ({ ...f }));
    } catch {
      this.notify.error('Törlés sikertelen.');
    } finally {
      this.actionLoading.set(false);
    }
  }
}
