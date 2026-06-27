import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="h-14 border-b border-[var(--color-border)] flex items-center px-4 gap-4 bg-[var(--color-bg)]">
      <span class="font-bold text-primary-600 text-lg">Family OS</span>
      <div class="flex-1"></div>
      <span class="text-sm text-[var(--color-text-muted)]">{{ auth.currentUser()?.displayName }}</span>
      <button
        data-testid="navbar-logout"
        class="text-sm text-danger-600 hover:underline"
        (click)="auth.logout()"
      >{{ 'nav.logout' | translate }}</button>
    </header>
  `,
})
export class NavbarComponent {
  auth = inject(AuthService);
}
