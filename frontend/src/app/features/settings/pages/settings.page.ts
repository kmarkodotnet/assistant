import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [TranslateModule, RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto">
      <h1 class="text-xl font-bold mb-6">{{ 'nav.settings' | translate }}</h1>
      <div class="flex gap-2 mb-6 border-b border-[var(--color-border)]">
        <a routerLink="preferences" routerLinkActive="border-b-2 border-primary-600 text-primary-600"
          data-testid="settings-tab-preferences"
          class="px-4 py-2 text-sm font-medium text-[var(--color-text-muted)]">Saját</a>
        <a routerLink="system" routerLinkActive="border-b-2 border-primary-600 text-primary-600"
          data-testid="settings-tab-system"
          class="px-4 py-2 text-sm font-medium text-[var(--color-text-muted)]">Rendszer</a>
      </div>
      <router-outlet />
    </div>
  `,
})
export class SettingsPage {}
