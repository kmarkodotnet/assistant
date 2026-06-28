import { Component, ChangeDetectionStrategy, signal, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, of } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';
import { AiProvidersApiService, AiProviderDto } from '../../admin/services/ai-providers.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { SkeletonComponent } from '../../../shared/ui/skeleton.component';
import { NotificationService } from '../../../core/notifications/notification.service';

interface EditableProvider {
  name: string;
  enabled: boolean;
  model: string;
  lastHealth: string | null;
}

@Component({
  selector: 'app-ai-providers-settings-page',
  standalone: true,
  imports: [FormsModule, TranslateModule, ButtonComponent, BadgeComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto">
      <h2 class="text-lg font-semibold mb-4">{{ 'admin.aiProviders.title' | translate }}</h2>

      <!-- Privacy banner (prominent in settings context) -->
      <div class="flex items-start gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-3 mb-5">
        <svg class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd"
            d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z"
            clip-rule="evenodd" />
        </svg>
        <div>
          <p class="text-sm font-medium text-amber-800">{{ 'admin.aiProviders.privacyLocked' | translate }}</p>
          <p class="text-xs text-amber-700 mt-1">Az adatvédelmi mód a rendszer konfigurációjában van rögzítve. Módosításhoz fordulj a rendszergazdához.</p>
        </div>
      </div>

      @if (!rawProviders()) {
        <div class="flex flex-col gap-3">
          @for (i of [1,2]; track i) { <ui-skeleton height="80px" /> }
        </div>
      } @else {
        <div class="flex flex-col gap-3">
          @for (provider of editableProviders(); track provider.name; let idx = $index) {
            <div class="bg-white border border-[var(--color-border)] rounded-xl px-5 py-4">
              <div class="flex items-center justify-between gap-4">
                <div class="flex items-center gap-2">
                  <span class="font-semibold text-sm">{{ provider.name }}</span>
                  @if (provider.lastHealth) {
                    <ui-badge [variant]="healthBadge(provider.lastHealth)">{{ provider.lastHealth }}</ui-badge>
                  }
                </div>
                <label class="flex items-center gap-2 cursor-pointer text-xs text-[var(--color-text-muted)]">
                  Engedélyezett
                  <input type="checkbox" [checked]="provider.enabled"
                    (change)="toggleEnabled(idx, $event)"
                    class="w-4 h-4 rounded text-primary-600 cursor-pointer" />
                </label>
              </div>
              <div class="mt-3">
                <label class="text-xs font-medium text-[var(--color-text-muted)] block mb-1">Modell azonosító</label>
                <input type="text" [value]="provider.model"
                  (input)="updateModel(idx, $event)"
                  placeholder="pl. gpt-4o, claude-3-5-sonnet"
                  class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm" />
              </div>
              <div class="mt-3 flex justify-end">
                <ui-button [disabled]="saving()" (click)="save(provider)">Mentés</ui-button>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class AiProvidersSettingsPage {
  private readonly api = inject(AiProvidersApiService);
  private readonly notify = inject(NotificationService);

  readonly saving = signal(false);
  readonly editableProviders = signal<EditableProvider[]>([]);

  readonly rawProviders = toSignal(
    this.api.list().pipe(catchError(() => of([] as AiProviderDto[]))),
    { initialValue: null as AiProviderDto[] | null }
  );

  constructor() {
    effect(() => {
      const raw = this.rawProviders();
      if (raw) {
        this.editableProviders.set(
          raw.map(p => ({ name: p.name, enabled: p.enabled, model: p.model ?? '', lastHealth: p.lastHealth }))
        );
      }
    });
  }

  toggleEnabled(idx: number, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.editableProviders.update(list => {
      const next = [...list];
      next[idx] = { ...next[idx], enabled: checked } as EditableProvider;
      return next;
    });
  }

  updateModel(idx: number, event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.editableProviders.update(list => {
      const next = [...list];
      next[idx] = { ...next[idx], model: value } as EditableProvider;
      return next;
    });
  }

  healthBadge(health: string): 'success' | 'danger' | 'warn' | 'default' {
    const h = health.toLowerCase();
    if (h.includes('ok') || h.includes('healthy')) return 'success';
    if (h.includes('error') || h.includes('fail')) return 'danger';
    return 'warn';
  }

  async save(provider: EditableProvider): Promise<void> {
    this.saving.set(true);
    try {
      await new Promise<void>((resolve, reject) =>
        this.api.patch(provider.name, { enabled: provider.enabled, model: provider.model || undefined })
          .subscribe({ complete: resolve, error: reject })
      );
      this.notify.success(`${provider.name} beállításai mentve.`);
    } catch {
      this.notify.error('Mentés sikertelen.');
    } finally {
      this.saving.set(false);
    }
  }
}
