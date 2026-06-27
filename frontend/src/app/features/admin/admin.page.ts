import { Component, ChangeDetectionStrategy } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-page',
  standalone: true,
  imports: [TranslateModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center h-64 text-center">
      <h1 class="text-xl font-semibold mb-2">{{ 'nav.admin' | translate }}</h1>
      <p class="text-[var(--color-text-muted)] text-sm">{{ 'common.comingSoon' | translate }}</p>
    </div>
  `,
})
export class AdminPage {}
