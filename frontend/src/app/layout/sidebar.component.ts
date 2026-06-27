import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';

const NAV_ITEMS = [
  { path: '/',           key: 'nav.dashboard',   testId: 'sidebar-dashboard' },
  { path: '/documents',  key: 'nav.documents',   testId: 'sidebar-documents' },
  { path: '/search',     key: 'nav.search',      testId: 'sidebar-search' },
  { path: '/tasks',      key: 'nav.tasks',       testId: 'sidebar-tasks' },
  { path: '/deadlines',  key: 'nav.deadlines',   testId: 'sidebar-deadlines' },
  { path: '/reminders',  key: 'nav.reminders',   testId: 'sidebar-reminders' },
  { path: '/notes',      key: 'nav.notes',       testId: 'sidebar-notes' },
  { path: '/topics',     key: 'nav.topics',      testId: 'sidebar-topics' },
  { path: '/suggestions',key: 'nav.suggestions', testId: 'sidebar-suggestions' },
  { path: '/settings',   key: 'nav.settings',    testId: 'sidebar-settings' },
];

const ADMIN_ITEMS = [
  { path: '/family', key: 'nav.family', testId: 'sidebar-family' },
  { path: '/admin',  key: 'nav.admin',  testId: 'sidebar-admin' },
];

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="w-56 border-r border-[var(--color-border)] bg-[var(--color-surface)] flex flex-col py-4 overflow-y-auto">
      @for (item of navItems; track item.path) {
        <a
          [routerLink]="item.path"
          [attr.data-testid]="item.testId"
          routerLinkActive="bg-primary-50 text-primary-700 font-medium"
          [routerLinkActiveOptions]="{ exact: item.path === '/' }"
          class="px-4 py-2 text-sm text-[var(--color-text)] hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md mx-2"
        >{{ item.key | translate }}</a>
      }
      @if (auth.isAdmin()) {
        <hr class="my-2 border-[var(--color-border)] mx-4">
        @for (item of adminItems; track item.path) {
          <a
            [routerLink]="item.path"
            [attr.data-testid]="item.testId"
            routerLinkActive="bg-primary-50 text-primary-700 font-medium"
            class="px-4 py-2 text-sm text-[var(--color-text)] hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md mx-2"
          >{{ item.key | translate }}</a>
        }
      }
    </nav>
  `,
})
export class SidebarComponent {
  auth = inject(AuthService);
  navItems = NAV_ITEMS;
  adminItems = ADMIN_ITEMS;
}
