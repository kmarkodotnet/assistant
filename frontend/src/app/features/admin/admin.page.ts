import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

const ACTIVE = 'border-b-2 border-primary-600 text-primary-600 font-medium';
const BASE = 'px-4 py-2 text-sm text-[var(--color-text-muted)] hover:text-[var(--color-text)] transition-colors whitespace-nowrap';

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-5xl mx-auto">
      <h1 class="text-xl font-bold mb-6">Adminisztráció</h1>

      <div class="flex gap-1 mb-6 border-b border-[var(--color-border)] overflow-x-auto">
        <a routerLink="audit"
          routerLinkActive="${ACTIVE}"
          [routerLinkActiveOptions]="{ exact: false }"
          data-testid="admin-tab-audit"
          class="${BASE}">Audit napló</a>

        <a routerLink="security-events"
          routerLinkActive="${ACTIVE}"
          [routerLinkActiveOptions]="{ exact: false }"
          data-testid="admin-tab-security"
          class="${BASE}">Biztonsági események</a>

        <a routerLink="jobs"
          routerLinkActive="${ACTIVE}"
          [routerLinkActiveOptions]="{ exact: false }"
          data-testid="admin-tab-jobs"
          class="${BASE}">AI feladatok</a>

        <a routerLink="providers"
          routerLinkActive="${ACTIVE}"
          [routerLinkActiveOptions]="{ exact: false }"
          data-testid="admin-tab-providers"
          class="${BASE}">AI providerek</a>
      </div>

      <router-outlet />
    </div>
  `,
})
export class AdminPage {}
