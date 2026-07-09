import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { NotificationService } from '../../../core/notifications/notification.service';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { ButtonComponent } from '../../../shared/ui/button.component';

interface SystemSettings {
  privacyMode: string;
  auditRetentionDays: number | null;
  notificationFeedRetentionDays: number | null;
  smtp: { host: string | null; port: number | null; from: string | null } | null;
}

@Component({
  selector: 'app-settings-system',
  standalone: true,
  imports: [FormsModule, SkeletonComponent, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl space-y-5">
      @if (loading()) {
        <ui-skeleton height="80px" cssClass="mb-3" />
        <ui-skeleton height="120px" cssClass="mb-3" />
        <ui-skeleton height="160px" />
      } @else if (!settings()) {
        <p class="text-sm text-danger-600">Nem sikerült betölteni a rendszerbeállításokat.</p>
      } @else {
        <!-- Privacy mode (read-only) -->
        <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4 flex items-center justify-between gap-4">
          <div>
            <p class="text-sm font-medium">Adatvédelmi mód</p>
            <p class="text-xs text-[var(--color-text-muted)] mt-0.5">Rendszer szintű konfiguráció, nem módosítható innen.</p>
          </div>
          <span class="shrink-0 text-xs font-mono bg-[var(--color-bg)] border border-[var(--color-border)] rounded px-2 py-1">
            {{ settings()!.privacyMode }}
          </span>
        </div>

        <!-- Retention -->
        <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4 space-y-4">
          <p class="text-sm font-semibold">Megőrzési idők</p>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Audit napló (nap)</label>
              <input
                data-testid="system-audit-retention"
                type="number" min="1" max="3650"
                placeholder="pl. 365"
                [value]="auditRetentionDays()"
                (input)="auditRetentionDays.set(toNum($any($event.target).value))"
                class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Értesítési feed (nap)</label>
              <input
                type="number"
                [value]="settings()!.notificationFeedRetentionDays ?? ''"
                disabled
                class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm opacity-50 cursor-not-allowed"
              />
            </div>
          </div>
        </div>

        <!-- SMTP -->
        <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl px-5 py-4 space-y-4">
          <p class="text-sm font-semibold">SMTP beállítások</p>
          <div>
            <label class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Host</label>
            <input
              data-testid="system-smtp-host"
              type="text"
              placeholder="pl. smtp.gmail.com"
              [value]="smtpHost()"
              (input)="smtpHost.set($any($event.target).value)"
              class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm"
            />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Port</label>
              <input
                data-testid="system-smtp-port"
                type="number" min="1" max="65535"
                placeholder="587"
                [value]="smtpPort()"
                (input)="smtpPort.set(toNum($any($event.target).value))"
                class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-[var(--color-text-muted)] mb-1">Feladó cím</label>
              <input
                data-testid="system-smtp-from"
                type="email"
                placeholder="noreply@example.com"
                [value]="smtpFrom()"
                (input)="smtpFrom.set($any($event.target).value)"
                class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm"
              />
            </div>
          </div>
        </div>

        <div class="flex justify-end">
          <ui-button data-testid="system-save" [disabled]="saving()" (click)="save()">
            {{ saving() ? 'Mentés...' : 'Mentés' }}
          </ui-button>
        </div>
      }
    </div>
  `,
})
export class SettingsSystemPage implements OnInit {
  private http = inject(HttpClient);
  private notify = inject(NotificationService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly settings = signal<SystemSettings | null>(null);

  readonly auditRetentionDays = signal<number | null>(null);
  readonly smtpHost = signal('');
  readonly smtpPort = signal<number | null>(null);
  readonly smtpFrom = signal('');

  async ngOnInit(): Promise<void> {
    try {
      const s = await firstValueFrom(this.http.get<SystemSettings>('/api/v1/settings/system'));
      this.settings.set(s);
      this.auditRetentionDays.set(s.auditRetentionDays);
      this.smtpHost.set(s.smtp?.host ?? '');
      this.smtpPort.set(s.smtp?.port ?? null);
      this.smtpFrom.set(s.smtp?.from ?? '');
    } catch {
      this.settings.set(null);
    } finally {
      this.loading.set(false);
    }
  }

  async save(): Promise<void> {
    this.saving.set(true);
    try {
      await firstValueFrom(this.http.patch<void>('/api/v1/settings/system', {
        auditRetentionDays: this.auditRetentionDays() ?? null,
        smtp: {
          host: this.smtpHost() || null,
          port: this.smtpPort() ?? null,
          from: this.smtpFrom() || null,
        },
      }));
      this.notify.success('Rendszerbeállítások mentve.');
    } catch {
      this.notify.error('Nem sikerült menteni a beállításokat.');
    } finally {
      this.saving.set(false);
    }
  }

  toNum(val: string): number | null {
    const n = parseInt(val, 10);
    return isNaN(n) ? null : n;
  }
}
