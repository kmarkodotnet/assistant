import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'ui-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="'animate-pulse bg-gray-200 dark:bg-gray-700 rounded ' + cssClass()"
      [style.height]="height()"
      [style.width]="width()"
    ></div>
  `,
})
export class SkeletonComponent {
  cssClass = input('');
  height = input('1rem');
  width = input('100%');
}
