import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { DatePipe, SlicePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap, catchError, of, tap } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AuditApiService, AuditFilter, AuditLogDto, PagedResult } from '../services/audit.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { NotificationService } from '../../../core/notifications/notification.service';

function entityRouterLink(entityType: string | null | undefined, entityId: string | null | undefined): string[] | null {
  if (!entityType || !entityId) return null;
  const t = entityType.toLowerCase();
  if (t.includes('document'))    return ['/documents', entityId];
  if (t.includes('note'))        return ['/notes'];
  if (t.includes('task'))        return ['/tasks'];
  if (t.includes('deadline'))    return ['/deadlines'];
  if (t.includes('reminder'))    return ['/reminders'];
  if (t.includes('suggestion'))  return ['/suggestions'];
  if (t.includes('familymember')) return ['/family'];
  if (t.includes('source'))      return ['/settings'];
  return null;
}

const ACTIONS = [
  '', 'Create', 'Update', 'Delete', 'Login', 'LoginFailed',
  'Approve', 'Reject', 'AiCall', 'FileAccess', 'PermissionChange', 'ExternalApiCall',
];

@Component({
  selector: 'app-audit-log-page',
  standalone: true,
  imports: [DatePipe, SlicePipe, FormsModule, TranslateModule, ButtonComponent, SkeletonComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-bold">{{ 'admin.audit.title' | translate }}</h1>
        <ui-button (click)="exportCsv()" variant="ghost">
          {{ 'admin.audit.export' | translate }}
        </ui-button>
      </div>

      <!-- Filters -->
      <div class="bg-white border border-[var(--color-border)] rounded-xl p-4 mb-4 grid grid-cols-2 md:grid-cols-4 gap-3">
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Dátumtól</label>
          <input type="date" [(ngModel)]="filterFrom"
            class="w-full border border-[var(--color-border)] rounded-lg px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Dátumig</label>
          <input type="date" [(ngModel)]="filterTo"
            class="w-full border border-[var(--color-border)] rounded-lg px-2 py-1.5 text-sm" />
        </div>
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Esemény</label>
          <select [(ngModel)]="filterAction"
            class="w-full border border-[var(--color-border)] rounded-lg px-2 py-1.5 text-sm">
            @for (action of actions; track action) {
              <option [value]="action">{{ action || 'Minden' }}</option>
            }
          </select>
        </div>
        <div>
          <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Entitás típus</label>
          <input type="text" [(ngModel)]="filterEntityType" placeholder="pl. Document"
            class="w-full border border-[var(--color-border)] rounded-lg px-2 py-1.5 text-sm" />
        </div>
        <div class="col-span-2 md:col-span-4 flex justify-end gap-2">
          <ui-button variant="ghost" (click)="resetFilter()">Törlés</ui-button>
          <ui-button (click)="applyFilter()">{{ 'admin.audit.filter' | translate }}</ui-button>
        </div>
      </div>

      <!-- Loading -->
      @if (loading()) {
        <div class="flex flex-col gap-2">
          @for (i of [1,2,3,4,5]; track i) {
            <ui-skeleton height="44px" />
          }
        </div>
      }

      <!-- Error -->
      @if (error() && !loading()) {
        <div class="bg-danger-50 text-danger-700 rounded-xl px-4 py-3 text-sm">
          Betöltés sikertelen. <button class="underline ml-1" (click)="applyFilter()">Újra</button>
        </div>
      }

      <!-- Table -->
      @if (!loading() && !error() && result()) {
        @if (result()!.items.length === 0) {
          <div class="text-center py-16 text-[var(--color-text-muted)] text-sm">
            {{ 'admin.audit.noResults' | translate }}
          </div>
        } @else {
          <div class="overflow-x-auto bg-white border border-[var(--color-border)] rounded-xl">
            <table class="min-w-full text-sm">
              <thead class="bg-gray-50 border-b border-[var(--color-border)]">
                <tr>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Időpont</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Esemény</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Felhasználó</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Entitás típus</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Entitás ID</th>
                  <th class="px-4 py-3 text-left text-xs font-semibold text-[var(--color-text-muted)]">Részletek</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-[var(--color-border)]">
                @for (item of result()!.items; track item.id) {
                  <tr class="hover:bg-gray-50 transition-colors">
                    <td class="px-4 py-3 whitespace-nowrap text-xs text-[var(--color-text-muted)]">
                      {{ item.occurredUtc | date:'yyyy-MM-dd HH:mm:ss' }}
                    </td>
                    <td class="px-4 py-3">
                      <span class="inline-flex px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-700">
                        {{ item.action }}
                      </span>
                    </td>
                    <td class="px-4 py-3 text-xs font-mono text-[var(--color-text-muted)]">
                      {{ item.userAccountId ? (item.userAccountId | slice:0:8) + '…' : '—' }}
                    </td>
                    <td class="px-4 py-3 text-xs">{{ item.entityType ?? '—' }}</td>
                    <td class="px-4 py-3 text-xs font-mono">
                      @if (item.entityId) {
                        @if (entityLink(item.entityType, item.entityId); as link) {
                          <a [routerLink]="link"
                             class="text-primary-600 hover:text-primary-800 hover:underline transition-colors"
                             title="{{ item.entityId }}">
                            {{ item.entityId | slice:0:8 }}…
                          </a>
                        } @else {
                          <span class="text-[var(--color-text-muted)]" title="{{ item.entityId }}">
                            {{ item.entityId | slice:0:8 }}…
                          </span>
                        }
                      } @else {
                        <span class="text-[var(--color-text-muted)]">—</span>
                      }
                    </td>
                    <td class="px-4 py-3 text-xs">
                      @if (item.detailsJson) {
                        <button (click)="toggleRow(item.id)"
                          class="text-primary-600 underline text-xs">
                          {{ expandedRows().has(item.id) ? 'Bezárás' : 'Megjelenítés' }}
                        </button>
                        @if (expandedRows().has(item.id)) {
                          <pre class="mt-1 text-xs bg-gray-50 rounded p-2 max-w-xs overflow-auto whitespace-pre-wrap break-all">{{ item.detailsJson }}</pre>
                        }
                      } @else {
                        <span class="text-[var(--color-text-muted)]">—</span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Pagination -->
          <div class="flex items-center justify-between mt-4 text-sm">
            <span class="text-[var(--color-text-muted)]">
              Összesen: {{ result()!.totalCount }} bejegyzés
              ({{ result()!.page }}. oldal / {{ result()!.totalPages }})
            </span>
            <div class="flex gap-2">
              <ui-button variant="ghost" [disabled]="filter().page! <= 1" (click)="prevPage()">
                &lsaquo; Előző
              </ui-button>
              <ui-button variant="ghost" [disabled]="filter().page! >= result()!.totalPages" (click)="nextPage()">
                Következő &rsaquo;
              </ui-button>
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class AuditLogPage {
  private readonly api = inject(AuditApiService);
  private readonly notify = inject(NotificationService);

  readonly actions = ACTIONS;

  // Filter draft (form bindings)
  filterFrom = '';
  filterTo = '';
  filterAction = '';
  filterEntityType = '';

  // Applied filter (drives query)
  readonly filter = signal<AuditFilter>({ page: 1, pageSize: 20 });
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly expandedRows = signal(new Set<string>());

  private readonly result$ = toObservable(this.filter).pipe(
    switchMap(f => {
      this.loading.set(true);
      this.error.set(null);
      return this.api.list(f).pipe(
        tap(() => this.loading.set(false)),
        catchError(() => {
          this.loading.set(false);
          this.error.set('Betöltés sikertelen');
          return of(null);
        })
      );
    })
  );

  readonly result = toSignal(this.result$, { initialValue: null as PagedResult<AuditLogDto> | null });

  applyFilter(): void {
    const next: AuditFilter = { page: 1, pageSize: 20 };
    if (this.filterFrom) next.from = this.filterFrom;
    if (this.filterTo) next.to = this.filterTo;
    if (this.filterAction) next.action = this.filterAction;
    if (this.filterEntityType) next.entityType = this.filterEntityType;
    this.filter.set(next);
  }

  resetFilter(): void {
    this.filterFrom = '';
    this.filterTo = '';
    this.filterAction = '';
    this.filterEntityType = '';
    this.filter.set({ page: 1, pageSize: 20 });
  }

  prevPage(): void {
    const current = this.filter().page ?? 1;
    if (current > 1) {
      this.filter.update(f => ({ ...f, page: current - 1 }));
    }
  }

  nextPage(): void {
    const current = this.filter().page ?? 1;
    const total = this.result()?.totalPages ?? 1;
    if (current < total) {
      this.filter.update(f => ({ ...f, page: current + 1 }));
    }
  }

  toggleRow(id: string): void {
    this.expandedRows.update(set => {
      const next = new Set(set);
      if (next.has(id)) { next.delete(id); } else { next.add(id); }
      return next;
    });
  }

  exportCsv(): void {
    const url = this.api.exportUrl(this.filterFrom || undefined, this.filterTo || undefined, 'csv');
    window.open(url, '_blank');
  }

  entityLink(entityType: string | null | undefined, entityId: string | null | undefined): string[] | null {
    return entityRouterLink(entityType, entityId);
  }
}
