import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AuditApiService, AuditLogDto } from '../services/audit.api';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';

@Component({
  selector: 'app-security-events-page',
  standalone: true,
  imports: [DatePipe, TranslateModule, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto">
      <h1 class="text-xl font-bold mb-6">{{ 'admin.security.title' | translate }}</h1>

      @if (!events()) {
        <div class="flex flex-col gap-2">
          @for (i of [1,2,3,4,5]; track i) {
            <ui-skeleton height="56px" />
          }
        </div>
      } @else if (events()!.length === 0) {
        <div class="text-center py-16 text-[var(--color-text-muted)] text-sm">
          Nincsenek biztonsági események.
        </div>
      } @else {
        <div class="flex flex-col gap-2">
          @for (item of events()!; track item.id) {
            <div [class]="'rounded-xl border px-4 py-3 ' + (item.action === 'LoginFailed' ? 'bg-red-50 border-red-200' : 'bg-white border-[var(--color-border)]')">
              <div class="flex items-center justify-between gap-4">
                <div class="flex items-center gap-3 min-w-0">
                  <span [class]="'inline-flex px-2 py-0.5 rounded-full text-xs font-semibold ' + (item.action === 'LoginFailed' ? 'bg-red-100 text-red-700' : 'bg-gray-100 text-gray-700')">
                    {{ item.action }}
                  </span>
                  <span class="text-xs text-[var(--color-text-muted)] font-mono truncate">
                    {{ item.userAccountId ?? 'ismeretlen' }}
                  </span>
                </div>
                <span class="text-xs text-[var(--color-text-muted)] whitespace-nowrap">
                  {{ item.occurredUtc | date:'yyyy-MM-dd HH:mm:ss' }}
                </span>
              </div>
              @if (item.ipAddress || item.userAgent) {
                <div class="mt-1.5 text-xs text-[var(--color-text-muted)] space-y-0.5">
                  @if (item.ipAddress) {
                    <div>IP: <span class="font-mono">{{ item.ipAddress }}</span></div>
                  }
                  @if (item.userAgent) {
                    <div class="truncate">UA: {{ item.userAgent }}</div>
                  }
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class SecurityEventsPage {
  private readonly api = inject(AuditApiService);

  readonly events = toSignal<AuditLogDto[] | null>(
    this.api.securityEvents().pipe(catchError(() => of([] as AuditLogDto[]))),
    { initialValue: null }
  );
}
