import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'documentIcon', standalone: true, pure: true })
export class DocumentIconPipe implements PipeTransform {
  transform(mime: string | null | undefined): string {
    if (!mime) return '📄';
    if (mime === 'application/pdf') return '📕';
    if (mime.startsWith('image/')) return '🖼️';
    if (mime.includes('word') || mime.includes('document')) return '📝';
    if (mime.includes('sheet') || mime.includes('excel')) return '📊';
    return '📄';
  }
}
