import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { isAdmin } from '../../../core/auth/auth.store';

const TAB_ACTIVE = 'border-b-2 border-primary-600 text-primary-600 font-medium';
const TAB_BASE = 'px-4 py-2 text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [TranslateModule, RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-2xl mx-auto">
      <h1 class="text-xl font-bold mb-6">{{ 'nav.settings' | translate }}</h1>

      <!-- Tab navigation -->
      <div class="flex gap-1 mb-6 border-b border-[var(--color-border)] overflow-x-auto">
        <a routerLink="preferences"
          routerLinkActive="${TAB_ACTIVE}"
          [routerLinkActiveOptions]="{ exact: false }"
          data-testid="settings-tab-preferences"
          class="${TAB_BASE}">
          Személyes
        </a>

        @if (isAdmin()) {
          <a routerLink="system"
            routerLinkActive="${TAB_ACTIVE}"
            [routerLinkActiveOptions]="{ exact: false }"
            data-testid="settings-tab-system"
            class="${TAB_BASE}">
            Rendszer
          </a>
          <a routerLink="integrations"
            routerLinkActive="${TAB_ACTIVE}"
            [routerLinkActiveOptions]="{ exact: false }"
            data-testid="settings-tab-integrations"
            class="${TAB_BASE}">
            Integrációk
          </a>
          <a routerLink="ai-providers"
            routerLinkActive="${TAB_ACTIVE}"
            [routerLinkActiveOptions]="{ exact: false }"
            data-testid="settings-tab-ai-providers"
            class="${TAB_BASE}">
            AI providerek
          </a>
          <a routerLink="backup"
            routerLinkActive="${TAB_ACTIVE}"
            [routerLinkActiveOptions]="{ exact: false }"
            data-testid="settings-tab-backup"
            class="${TAB_BASE}">
            Mentések
          </a>
        }
      </div>

      <router-outlet />
    </div>
  `,
})
export class SettingsPage {
  readonly isAdmin = isAdmin;
}
