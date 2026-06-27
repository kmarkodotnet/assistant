import { Component, ChangeDetectionStrategy, output, signal } from '@angular/core';

@Component({
  selector: 'app-dropzone',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      data-testid="documents-dropzone"
      (dragover)="onDragOver($event)"
      (dragleave)="dragActive.set(false)"
      (drop)="onDrop($event)"
      [class.border-primary-500]="dragActive()"
      class="border-2 border-dashed border-[var(--color-border)] rounded-xl p-12 text-center cursor-pointer transition-colors hover:border-primary-400"
      (click)="fileInput.click()"
    >
      <span class="text-4xl">📁</span>
      <p class="mt-3 font-medium">Húzd ide a fájlokat, vagy kattints a böngészéshez</p>
      <p class="text-sm text-[var(--color-text-muted)] mt-1">PDF, JPEG, PNG, HEIC, TXT, DOCX · max. 50 MB</p>
      <input #fileInput type="file" multiple accept=".pdf,.jpg,.jpeg,.png,.heic,.txt,.docx"
        class="hidden" (change)="onFileChange($event)" data-testid="documents-file-input" />
    </div>
  `,
})
export class DropzoneComponent {
  filesSelected = output<File[]>();
  dragActive = signal(false);

  onDragOver(e: DragEvent): void { e.preventDefault(); this.dragActive.set(true); }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.dragActive.set(false);
    const files = Array.from(e.dataTransfer?.files ?? []);
    if (files.length) this.filesSelected.emit(files);
  }

  onFileChange(e: Event): void {
    const input = e.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (files.length) this.filesSelected.emit(files);
    input.value = '';
  }
}
