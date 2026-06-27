import { Component, ChangeDetectionStrategy, inject, computed } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';

interface NavItem { path: string; key: string; testId: string; roles?: string[] }

const ALL_NAV: NavItem[] = [
  { path: '/',            key: 'nav.dashboard',   testId: 'sidebar-dashboard' },
  { path: '/documents',   key: 'nav.documents',   testId: 'sidebar-documents', roles: ['Admin', 'Adult'] },
  { path: '/notes',       key: 'nav.notes',       testId: 'sidebar-notes',     roles: ['Admin', 'Adult'] },
  { path: '/search',      key: 'nav.search',      testId: 'sidebar-search' },
  { path: '/tasks',       key: 'nav.tasks',       testId: 'sidebar-tasks',     roles: ['Admin', 'Adult'] },
  { path: '/deadlines',   key: 'nav.deadlines',   testId: 'sidebar-deadlines', roles: ['Admin', 'Adult'] },
  { path: '/reminders',   key: 'nav.reminders',   testId: 'sidebar-reminders' },
  { path: '/topics',      key: 'nav.topics',      testId: 'sidebar-topics',    roles: ['Admin', 'Adult'] },
  { path: '/suggestions', key: 'nav.suggestions', testId: 'sidebar-suggestions', roles: ['Admin', 'Adult'] },
  { path: '/settings',    key: 'nav.settings',    testId: 'sidebar-settings' },
  { path: '/family',      key: 'nav.family',      testId: 'sidebar-family',    roles: ['Admin'] },
  { path: '/admin',       key: 'nav.admin',        testId: 'sidebar-admin',     roles: ['Admin'] },
];

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <nav class="w-56 border-r border-[var(--color-border)] bg-[var(--color-surface)] flex flex-col py-4 overflow-y-auto">
      @for (item of visibleItems(); track item.path) {
        <a
          [routerLink]="item.path"
          [attr.data-testid]="item.testId"
          routerLinkActive="bg-primary-50 text-primary-700 font-medium"
          [routerLinkActiveOptions]="{ exact: item.path === '/' }"
          class="px-4 py-2 text-sm text-[var(--color-text)] hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md mx-2"
        >{{ item.key | translate }}</a>
      }
    </nav>
  `,
})
export class SidebarComponent {
  auth = inject(AuthService);

  visibleItems = computed(() => {
    const role = this.auth.currentUser()?.role;
    return ALL_NAV.filter(item => {
      if (!item.roles) return true;
      return role ? item.roles.includes(role) : false;
    });
  });
}
