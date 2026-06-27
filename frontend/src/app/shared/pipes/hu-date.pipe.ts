import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'huDate', standalone: true, pure: true })
export class HuDatePipe implements PipeTransform {
  private fmt = new Intl.DateTimeFormat('hu-HU', {
    year: 'numeric', month: 'long', day: 'numeric',
  });

  transform(value: string | Date | null | undefined): string {
    if (!value) return '';
    return this.fmt.format(typeof value === 'string' ? new Date(value) : value);
  }
}
