import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { SourcesApiService, SourceDto } from '../services/sources.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { NotificationService } from '../../../core/notifications/notification.service';

@Component({
  selector: 'app-integrations-page',
  standalone: true,
  imports: [DatePipe, TranslateModule, ButtonComponent, BadgeComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto">
      <h2 class="text-lg font-semibold mb-4">{{ 'settings.integrations.title' | translate }}</h2>

      @if (!sources()) {
        <ui-skeleton height="120px" />
      } @else {
        <!-- Gmail card -->
        <div class="bg-white border border-[var(--color-border)] rounded-xl p-5">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-3">
              <svg class="w-6 h-6 text-red-500" viewBox="0 0 24 24" fill="currentColor">
                <path d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm0 4l-8 5-8-5V6l8 5 8-5v2z"/>
              </svg>
              <span class="font-semibold">Gmail</span>
            </div>
            @if (gmailSource()) {
              <ui-badge variant="success">{{ 'settings.integrations.gmail.connected' | translate }}</ui-badge>
            } @else {
              <ui-badge variant="default">{{ 'settings.integrations.gmail.notConnected' | translate }}</ui-badge>
            }
          </div>

          @if (gmailSource()) {
            <p class="text-xs text-[var(--color-text-muted)] mb-3">
              Utolsó szinkronizálás:
              @if (gmailSource()!.lastSyncUtc) {
                {{ gmailSource()!.lastSyncUtc | date:'yyyy-MM-dd HH:mm' }}
              } @else {
                Még nem történt szinkronizálás
              }
            </p>
            <div class="flex gap-2">
              <ui-button variant="ghost" [disabled]="actionLoading()" (click)="syncGmail()">
                {{ 'settings.integrations.gmail.sync' | translate }}
              </ui-button>
              <ui-button variant="danger" [disabled]="actionLoading()" (click)="disconnectGmail()">
                {{ 'settings.integrations.gmail.disconnect' | translate }}
              </ui-button>
            </div>
          } @else {
            <p class="text-sm text-[var(--color-text-muted)] mb-3">
              Csatlakoztasd Gmail fiókodat az e-mailek automatikus szinkronizálásához.
            </p>
            <ui-button [disabled]="actionLoading()" (click)="connectGmail()">
              {{ 'settings.integrations.gmail.connect' | translate }}
            </ui-button>
          }
        </div>

        <!-- Other sources if any -->
        @for (source of otherSources(); track source.id) {
          <div class="bg-white border border-[var(--color-border)] rounded-xl p-5 mt-3">
            <div class="flex items-center justify-between">
              <div>
                <h3 class="font-semibold text-sm">{{ source.name }}</h3>
                <p class="text-xs text-[var(--color-text-muted)]">{{ source.kind }}</p>
              </div>
              <ui-badge [variant]="source.isActive ? 'success' : 'default'">
                {{ source.isActive ? 'Aktív' : 'Inaktív' }}
              </ui-badge>
            </div>
          </div>
        }
      }
    </div>
  `,
})
export class IntegrationsPage {
  private readonly api = inject(SourcesApiService);
  private readonly notify = inject(NotificationService);

  readonly actionLoading = signal(false);
  readonly sourcesSignal = signal<SourceDto[] | null>(null);

  private readonly rawSources = toSignal<SourceDto[] | null>(
    this.api.list().pipe(catchError(() => of([] as SourceDto[]))),
    { initialValue: null }
  );

  sources() {
    return this.rawSources();
  }

  gmailSource() {
    return this.rawSources()?.find(s => s.kind === 'Gmail') ?? null;
  }

  otherSources() {
    return this.rawSources()?.filter(s => s.kind !== 'Gmail') ?? [];
  }

  async connectGmail(): Promise<void> {
    this.actionLoading.set(true);
    try {
      const result = await new Promise<{ redirectUrl: string }>((resolve, reject) =>
        this.api.connectGmail().subscribe({ next: resolve, error: reject })
      );
      window.location.href = result.redirectUrl;
    } catch {
      this.notify.error('Gmail csatlakoztatás sikertelen.');
      this.actionLoading.set(false);
    }
  }

  async syncGmail(): Promise<void> {
    const source = this.gmailSource();
    if (!source) return;
    this.actionLoading.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.api.sync(source.id).subscribe({ complete: resolve, error: reject })
      );
      this.notify.success('Szinkronizálás elindítva.');
    } catch {
      this.notify.error('Szinkronizálás sikertelen.');
    } finally {
      this.actionLoading.set(false);
    }
  }

  async disconnectGmail(): Promise<void> {
    const source = this.gmailSource();
    if (!source) return;
    if (!confirm('Biztosan lecsatlakoztatod a Gmail fiókot?')) return;
    this.actionLoading.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.api.disconnect(source.id).subscribe({ complete: resolve, error: reject })
      );
      this.notify.success('Gmail lecsatlakoztatva.');
    } catch {
      this.notify.error('Lecsatlakoztatás sikertelen.');
    } finally {
      this.actionLoading.set(false);
    }
  }
}
