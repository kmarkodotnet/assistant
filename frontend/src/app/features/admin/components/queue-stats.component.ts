import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { interval, startWith, switchMap, catchError, of } from 'rxjs';
import { AiJobsApiService, QueueStatEntry } from '../services/ai-jobs.api';

@Component({
  selector: 'app-queue-stats',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white border border-[var(--color-border)] rounded-xl p-4">
      <h2 class="text-sm font-semibold mb-3 text-[var(--color-text-muted)]">
        Sor állapot <span class="text-xs font-normal">(10 mp-ként frissül)</span>
      </h2>
      @if (stats().length === 0) {
        <p class="text-xs text-[var(--color-text-muted)]">Nincs adat.</p>
      } @else {
        <div class="overflow-x-auto">
          <table class="min-w-full text-xs">
            <thead>
              <tr class="border-b border-[var(--color-border)]">
                <th class="text-left py-1.5 pr-4 font-medium text-[var(--color-text-muted)]">Típus</th>
                <th class="text-left py-1.5 pr-4 font-medium text-[var(--color-text-muted)]">Státusz</th>
                <th class="text-right py-1.5 font-medium text-[var(--color-text-muted)]">Db</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-[var(--color-border)]">
              @for (entry of stats(); track entry.jobType + entry.status) {
                <tr>
                  <td class="py-1.5 pr-4">{{ entry.jobType }}</td>
                  <td class="py-1.5 pr-4">
                    <span [class]="statusClass(entry.status)">{{ entry.status }}</span>
                  </td>
                  <td class="py-1.5 text-right font-mono font-semibold">{{ entry.count }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class QueueStatsComponent {
  private readonly api = inject(AiJobsApiService);

  private readonly stats$ = interval(10000).pipe(
    startWith(0),
    switchMap(() => this.api.queueStats().pipe(catchError(() => of([] as QueueStatEntry[]))))
  );

  readonly stats = toSignal(this.stats$, { initialValue: [] as QueueStatEntry[] });

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Running: 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-primary-50 text-primary-700',
      Queued: 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-warn-100 text-warn-700',
      Failed: 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-danger-50 text-danger-700',
      Completed: 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-success-50 text-success-700',
      Cancelled: 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-500',
    };
    return map[status] ?? 'inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-700';
  }
}
