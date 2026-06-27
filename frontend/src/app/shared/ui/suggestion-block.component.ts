import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

export interface SuggestionItem {
  id: string;
  label: string;
  type: string;
}

@Component({
  selector: 'ui-suggestion-block',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (suggestions().length > 0) {
      <div class="border border-warn-300 bg-warn-50 rounded-xl p-4 mb-4">
        <div class="flex items-center justify-between mb-3">
          <span class="text-sm font-semibold text-warn-800">AI javasolta</span>
          <div class="flex gap-2">
            <button data-testid="suggestion-approve-all"
              (click)="approveAll.emit(suggestions())"
              class="text-xs text-success-700 hover:underline">Elfogadom mindet</button>
            <button data-testid="suggestion-reject-all"
              (click)="rejectAll.emit(suggestions())"
              class="text-xs text-danger-600 hover:underline">Elvetem mindet</button>
          </div>
        </div>
        @for (s of suggestions(); track s.id) {
          <div class="flex items-center justify-between py-1 border-t border-warn-200">
            <span class="text-sm">{{ s.label }}</span>
            <div class="flex gap-2">
              <button [attr.data-testid]="'suggestion-approve-' + s.id" (click)="approve.emit(s)" class="text-xs text-success-700 hover:underline">Elfogadom</button>
              <button [attr.data-testid]="'suggestion-reject-' + s.id" (click)="reject.emit(s)" class="text-xs text-danger-600 hover:underline">Elvetem</button>
            </div>
          </div>
        }
      </div>
    }
  `,
})
export class SuggestionBlockComponent {
  suggestions = input.required<SuggestionItem[]>();
  approve = output<SuggestionItem>();
  reject = output<SuggestionItem>();
  approveAll = output<SuggestionItem[]>();
  rejectAll = output<SuggestionItem[]>();
}
