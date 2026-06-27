import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="border-t border-[var(--color-border)] bg-[var(--color-bg)] flex justify-around py-2">
      <a routerLink="/" routerLinkActive="text-primary-600" [routerLinkActiveOptions]="{ exact: true }" data-testid="bottom-dashboard" class="flex flex-col items-center text-xs px-3">
        <span>🏠</span><span>{{ 'nav.dashboard' | translate }}</span>
      </a>
      <a routerLink="/search" routerLinkActive="text-primary-600" data-testid="bottom-search" class="flex flex-col items-center text-xs px-3">
        <span>🔍</span><span>{{ 'nav.search' | translate }}</span>
      </a>
      <a routerLink="/suggestions" routerLinkActive="text-primary-600" data-testid="bottom-suggestions" class="flex flex-col items-center text-xs px-3">
        <span>⭐</span><span>{{ 'nav.suggestions' | translate }}</span>
      </a>
      <a routerLink="/reminders" routerLinkActive="text-primary-600" data-testid="bottom-reminders" class="flex flex-col items-center text-xs px-3">
        <span>🔔</span><span>{{ 'nav.reminders' | translate }}</span>
      </a>
      <a routerLink="/documents" routerLinkActive="text-primary-600" data-testid="bottom-documents" class="flex flex-col items-center text-xs px-3">
        <span>📄</span><span>{{ 'nav.documents' | translate }}</span>
      </a>
    </nav>
  `,
})
export class BottomNavComponent {}
