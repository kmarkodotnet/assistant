import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'huRelativeDate', standalone: true, pure: true })
export class HuRelativeDatePipe implements PipeTransform {
  private rtf = new Intl.RelativeTimeFormat('hu-HU', { numeric: 'auto' });

  transform(value: string | Date | null | undefined): string {
    if (!value) return '';
    const date = typeof value === 'string' ? new Date(value) : value;
    const diffMs = date.getTime() - Date.now();
    const diffDays = Math.round(diffMs / 86_400_000);

    if (Math.abs(diffDays) < 1) return 'ma';
    if (Math.abs(diffDays) < 7) return this.rtf.format(diffDays, 'day');
    if (Math.abs(diffDays) < 30) return this.rtf.format(Math.round(diffDays / 7), 'week');
    return this.rtf.format(Math.round(diffDays / 30), 'month');
  }
}
