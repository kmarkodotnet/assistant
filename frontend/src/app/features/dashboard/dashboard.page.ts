import { Component, ChangeDetectionStrategy, inject, signal, computed, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardService, DashboardDto } from '../../core/api/dashboard.service';
import { CardComponent } from '../../shared/ui/card.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { HuDatePipe } from '../../shared/pipes/hu-date.pipe';
import { HuRelativeDatePipe } from '../../shared/pipes/hu-relative-date.pipe';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [RouterLink, CardComponent, SkeletonComponent, HuDatePipe, HuRelativeDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto p-4 space-y-4">
      <h1 class="text-2xl font-bold">Áttekintés</h1>

      @if (loading()) {
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          @for (_ of [1,2,3,4]; track $index) {
            <ui-card>
              <ui-skeleton height="1.5rem" cssClass="mb-3 w-1/2" />
              <ui-skeleton height="1rem" cssClass="mb-2" />
              <ui-skeleton height="1rem" cssClass="mb-2 w-3/4" />
              <ui-skeleton height="1rem" cssClass="w-1/2" />
            </ui-card>
          }
        </div>
      } @else if (error()) {
        <div class="text-danger-600 p-4">Nem sikerült betölteni az adatokat.</div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

          <!-- Upcoming Deadlines -->
          <ui-card>
            <div class="flex items-center justify-between mb-3">
              <h2 class="font-semibold text-base">Közelgő határidők</h2>
              <a routerLink="/deadlines" class="text-xs text-primary-600 hover:underline">Összes</a>
            </div>
            @if (data()!.upcomingDeadlines.length === 0) {
              <p class="text-sm text-[var(--color-text-muted)]">Nincs közelgő határidő.</p>
            }
            @for (d of data()!.upcomingDeadlines; track d.id) {
              <div class="flex items-center justify-between py-1.5 border-b border-[var(--color-border)] last:border-0">
                <span class="text-sm truncate">{{ d.title }}</span>
                <span class="text-xs text-[var(--color-text-muted)] ml-2 shrink-0">{{ d.dueDateUtc | huRelativeDate }}</span>
              </div>
            }
          </ui-card>

          <!-- Overdue Reminders -->
          <ui-card>
            <div class="flex items-center justify-between mb-3">
              <h2 class="font-semibold text-base">Lecsúszott emlékeztetők</h2>
              <a routerLink="/deadlines" class="text-xs text-danger-600 hover:underline">Összes</a>
            </div>
            @if (data()!.overdueReminders.length === 0) {
              <p class="text-sm text-[var(--color-text-muted)]">Nincs lejárt határidő.</p>
            }
            @for (d of data()!.overdueReminders; track d.id) {
              <div class="flex items-center justify-between py-1.5 border-b border-[var(--color-border)] last:border-0">
                <span class="text-sm truncate text-danger-700">{{ d.title }}</span>
                <span class="text-xs text-danger-500 ml-2 shrink-0">{{ d.dueDateUtc | huDate }}</span>
              </div>
            }
          </ui-card>

          <!-- Pending Suggestions -->
          <ui-card>
            <div class="flex items-center justify-between mb-3">
              <h2 class="font-semibold text-base">AI javaslatok</h2>
              <a routerLink="/suggestions" class="text-xs text-primary-600 hover:underline">Kezelés</a>
            </div>
            @if (data()!.pendingSuggestions.total === 0) {
              <p class="text-sm text-[var(--color-text-muted)]">Nincs jóváhagyásra váró javaslat.</p>
            } @else {
              <div class="space-y-1">
                @if (data()!.pendingSuggestions.tasks > 0) {
                  <div class="flex justify-between text-sm">
                    <span>Feladatok</span>
                    <span class="font-medium">{{ data()!.pendingSuggestions.tasks }}</span>
                  </div>
                }
                @if (data()!.pendingSuggestions.deadlines > 0) {
                  <div class="flex justify-between text-sm">
                    <span>Határidők</span>
                    <span class="font-medium">{{ data()!.pendingSuggestions.deadlines }}</span>
                  </div>
                }
                @if (data()!.pendingSuggestions.tags > 0) {
                  <div class="flex justify-between text-sm">
                    <span>Tagek</span>
                    <span class="font-medium">{{ data()!.pendingSuggestions.tags }}</span>
                  </div>
                }
                @if (data()!.pendingSuggestions.topics > 0) {
                  <div class="flex justify-between text-sm">
                    <span>Témák</span>
                    <span class="font-medium">{{ data()!.pendingSuggestions.topics }}</span>
                  </div>
                }
                <div class="flex justify-between text-sm font-semibold pt-1 border-t border-[var(--color-border)]">
                  <span>Összesen</span>
                  <span>{{ data()!.pendingSuggestions.total }}</span>
                </div>
              </div>
            }
          </ui-card>

          <!-- Recent Documents -->
          <ui-card>
            <div class="flex items-center justify-between mb-3">
              <h2 class="font-semibold text-base">Legutóbbi dokumentumok</h2>
              <a routerLink="/documents" class="text-xs text-primary-600 hover:underline">Összes</a>
            </div>
            @if (data()!.recentDocuments.length === 0) {
              <p class="text-sm text-[var(--color-text-muted)]">Nincs dokumentum.</p>
            }
            @for (doc of data()!.recentDocuments; track doc.id) {
              <a [routerLink]="['/documents', doc.id]"
                 class="flex items-center justify-between py-1.5 border-b border-[var(--color-border)] last:border-0 hover:bg-[var(--color-surface-hover)] -mx-1 px-1 rounded">
                <span class="text-sm truncate">{{ doc.title || doc.originalFileName }}</span>
                <span class="text-xs text-[var(--color-text-muted)] ml-2 shrink-0">{{ doc.createdUtc | huRelativeDate }}</span>
              </a>
            }
          </ui-card>

          <!-- Saved Searches -->
          @if (data()!.savedSearches.length > 0) {
            <ui-card>
              <div class="flex items-center justify-between mb-3">
                <h2 class="font-semibold text-base">Mentett keresések</h2>
              </div>
              @for (s of data()!.savedSearches; track s.id) {
                <a [routerLink]="['/search']" [queryParams]="{ q: s.queryJson }"
                   class="flex items-center py-1.5 border-b border-[var(--color-border)] last:border-0 hover:underline">
                  <span class="text-sm">{{ s.name }}</span>
                </a>
              }
            </ui-card>
          }

        </div>
      }
    </div>
  `,
})
export class DashboardPage implements OnInit {
  private dashboardService = inject(DashboardService);
  private notificationService = inject(NotificationService);

  loading = signal(true);
  error = signal(false);
  data = signal<DashboardDto | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const result = await this.dashboardService.get();
      this.data.set(result);
    } catch {
      this.error.set(true);
      this.notificationService.error('Nem sikerült betölteni az áttekintést.');
    } finally {
      this.loading.set(false);
    }
  }
}
