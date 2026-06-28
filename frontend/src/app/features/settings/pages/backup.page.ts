import { Component, ChangeDetectionStrategy } from '@angular/core';
import { BackupInfoComponent } from '../components/backup-info.component';

@Component({
  selector: 'app-backup-page',
  standalone: true,
  imports: [BackupInfoComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<app-backup-info />`,
})
export class BackupPage {}
