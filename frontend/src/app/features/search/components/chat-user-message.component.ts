import {
  Component,
  ChangeDetectionStrategy,
  input,
  computed,
} from '@angular/core';
import type { SearchRequest, SearchMode } from '../models/search.dto';

const MODE_LABELS: Record<SearchMode, string> = {
  Auto: 'Auto',
  Filter: 'Szuro',
  Text: 'Szoveges',
  Semantic: 'Szemantikus',
  Qa: 'Q&A',
};

@Component({
  selector: 'app-chat-user-message',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex justify-end mb-3">
      <div class="max-w-[80%]">
        <div class="bg-primary-600 text-white rounded-2xl rounded-tr-sm px-4 py-3 shadow-sm">
          <p class="text-sm leading-relaxed whitespace-pre-wrap">{{ query().query }}</p>
        </div>
        <p class="text-xs text-[var(--color-text-muted)] text-right mt-1 pr-1">{{ modeLabel() }}</p>
      </div>
    </div>
  `,
})
export class ChatUserMessageComponent {
  query = input.required<SearchRequest>();

  modeLabel = computed(() => MODE_LABELS[this.query().mode] ?? this.query().mode);
}
