import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-settings-system',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="text-[var(--color-text-muted)] text-sm">A rendszerbeállítások hamarosan elérhetők lesznek.</div>`,
})
export class SettingsSystemPage {}
