import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { ButtonComponent } from '../../../shared/ui/button.component';
import { NotificationService } from '../../../core/notifications/notification.service';
import { PreferencesApiService } from '../services/preferences.api';
import type { CurrentUserDto } from '../../../core/auth/current-user.dto';

@Component({
  selector: 'app-preferences-page',
  standalone: true,
  imports: [ReactiveFormsModule, TranslateModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="save()" class="flex flex-col gap-6">
      <div>
        <label class="flex items-center gap-3 cursor-pointer">
          <input data-testid="prefs-email-enabled" formControlName="emailEnabled" type="checkbox"
            class="w-4 h-4 rounded text-primary-600">
          <span class="text-sm font-medium">Email értesítések engedélyezése</span>
        </label>
        <p class="text-xs text-[var(--color-text-muted)] mt-1">Az email értesítés a háztartási SMTP konfigurálása után válik aktívvá.</p>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="text-sm font-medium block mb-1">Csendes óra kezdete</label>
          <input data-testid="prefs-quiet-start" formControlName="quietHoursStart" type="time"
            class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm">
        </div>
        <div>
          <label class="text-sm font-medium block mb-1">Csendes óra vége</label>
          <input data-testid="prefs-quiet-end" formControlName="quietHoursEnd" type="time"
            class="w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm">
        </div>
      </div>

      <div>
        <ui-button type="submit" data-testid="prefs-save">Mentés</ui-button>
      </div>
    </form>
  `,
})
export class PreferencesPage implements OnInit {
  private api = inject(PreferencesApiService);
  private notify = inject(NotificationService);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    emailEnabled: [false],
    quietHoursStart: ['22:00'],
    quietHoursEnd: ['07:00'],
  });

  async ngOnInit(): Promise<void> {
    try {
      const resp = await firstValueFrom(this.api.get());
      const user = resp as unknown as CurrentUserDto;
      if (user.preferences) {
        const p = user.preferences;
        this.form.patchValue({
          emailEnabled: p.emailEnabled,
          quietHoursStart: p.quietHoursStart ?? '22:00',
          quietHoursEnd: p.quietHoursEnd ?? '07:00',
        });
      }
    } catch { /* silent */ }
  }

  async save(): Promise<void> {
    try {
      const v = this.form.getRawValue();
      await firstValueFrom(this.api.patch({
        emailEnabled: v.emailEnabled ?? false,
        quietHoursStart: v.quietHoursStart,
        quietHoursEnd: v.quietHoursEnd,
      }));
      this.notify.success('Beállítások mentve.');
    } catch {
      this.notify.error('Nem sikerült menteni a beállításokat.');
    }
  }
}
