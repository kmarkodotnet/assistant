import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-backup-info',
  standalone: true,
  imports: [TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-white border border-[var(--color-border)] rounded-xl p-5">
      <div class="flex items-center gap-3 mb-4">
        <svg class="w-5 h-5 text-success-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M4 7v10c0 2.21 3.582 4 8 4s8-1.79 8-4V7M4 7c0 2.21 3.582 4 8 4s8-1.79 8-4M4 7c0-2.21 3.582-4 8-4s8 1.79 8 4" />
        </svg>
        <h2 class="text-base font-semibold">{{ 'settings.backup.title' | translate }}</h2>
      </div>

      <div class="flex flex-col gap-3 text-sm">
        <div class="flex items-start gap-3 p-3 bg-success-50 rounded-lg border border-success-200">
          <svg class="w-4 h-4 text-success-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd"
              d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z"
              clip-rule="evenodd" />
          </svg>
          <div>
            <p class="font-medium text-success-800">{{ 'settings.backup.info' | translate }}</p>
            <p class="text-success-700 text-xs mt-0.5">Mentési szkript: <code class="bg-success-100 px-1 rounded">scripts/backup.sh</code></p>
          </div>
        </div>

        <div class="p-3 bg-gray-50 rounded-lg border border-gray-200">
          <p class="text-[var(--color-text-muted)]">
            <span class="font-medium text-[var(--color-text)]">Utolsó mentés ellenőrzése:</span>
            Tekintse meg a
            <code class="bg-gray-200 px-1 rounded text-xs">/data/backups/manifest.txt</code>
            fájlt a szerveren.
          </p>
        </div>

        <div class="p-3 bg-gray-50 rounded-lg border border-gray-200">
          <p class="text-[var(--color-text-muted)]">
            <span class="font-medium text-[var(--color-text)]">Visszaállítási útmutató:</span>
            Részletes leírás a
            <code class="bg-gray-200 px-1 rounded text-xs">docs/DELIVERY.md</code>
            dokumentációban.
          </p>
        </div>
      </div>
    </div>
  `,
})
export class BackupInfoComponent {}
