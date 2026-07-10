import {
  Component,
  ChangeDetectionStrategy,
  input,
  computed,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { BadgeComponent } from '../../../shared/ui/badge.component';
import { AnswerSourcesComponent } from './answer-sources.component';
import type { SearchResponse, SearchHit } from '../models/search.dto';
import type { BadgeComponent as BadgeType } from '../../../shared/ui/badge.component';

type BadgeVariant = 'default' | 'success' | 'warn' | 'danger' | 'info';

function entityBadgeVariant(entityType: string): BadgeVariant {
  switch (entityType.toLowerCase()) {
    case 'document': return 'info';
    case 'task': return 'warn';
    case 'deadline': return 'danger';
    case 'note': return 'success';
    case 'reminder': return 'info';
    case 'suggestion': return 'default';
    default: return 'default';
  }
}

function entityLabel(entityType: string): string {
  switch (entityType.toLowerCase()) {
    case 'document': return 'Dokumentum';
    case 'task': return 'Feladat';
    case 'deadline': return 'Határidő';
    case 'note': return 'Feljegyzés';
    case 'reminder': return 'Emlékeztető';
    case 'suggestion': return 'Javaslat';
    default: return entityType;
  }
}

function truncate(text: string, max: number): string {
  return text.length > max ? text.slice(0, max) + '...' : text;
}

@Component({
  selector: 'app-chat-answer-message',
  standalone: true,
  imports: [BadgeComponent, AnswerSourcesComponent, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex justify-start mb-3">
      <div class="max-w-[90%] w-full">
        <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl rounded-tl-sm px-4 py-3 shadow-sm">

          <!-- Q&A answer block -->
          @if (response().answer) {
            <div class="mb-3 pb-3 border-b border-[var(--color-border)]">
              <p class="text-xs font-semibold text-primary-600 uppercase tracking-wide mb-1.5">Válasz</p>
              <p class="text-sm leading-relaxed">{{ response().answer }}</p>

              <!-- Confidence bar -->
              @if (response().confidence != null) {
                <div class="mt-2">
                  <div class="flex items-center justify-between text-xs text-[var(--color-text-muted)] mb-1">
                    <span>Bizalom</span>
                    <span>{{ confidencePct() }}%</span>
                  </div>
                  <div class="h-1.5 bg-[var(--color-border)] rounded-full overflow-hidden">
                    <div
                      class="h-full rounded-full transition-all"
                      [class]="confidenceBarClass()"
                      [style.width]="confidencePct() + '%'"
                    ></div>
                  </div>
                </div>
              }

              <!-- Sources -->
              @if (response().answerSources?.length) {
                <div class="mt-2">
                  <app-answer-sources
                    [sources]="response().answerSources!"
                    [count]="response().answerSources!.length"
                  />
                </div>
              }
            </div>
          }

          <!-- Hits list -->
          @if (visibleHits().length > 0) {
            <div class="space-y-2">
              @for (hit of visibleHits(); track hit.entityId) {
                <a
                  [routerLink]="entityRouterLink(hit)"
                  class="block rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-3 hover:border-primary-400 hover:bg-primary-50/30 transition-colors cursor-pointer"
                >
                  <div class="flex items-center gap-2 mb-1 flex-wrap">
                    <ui-badge [variant]="badgeVariant(hit)">{{ entityTypeLabel(hit) }}</ui-badge>
                    <span class="text-sm font-medium leading-snug flex-1">{{ hit.title }}</span>
                    <svg class="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 003 8.25v10.5A2.25 2.25 0 005.25 21h10.5A2.25 2.25 0 0018 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                    </svg>
                  </div>
                  @if (hit.snippet) {
                    <p class="text-xs text-[var(--color-text-muted)] leading-relaxed">
                      {{ snippetText(hit) }}
                    </p>
                  }
                </a>
              }
            </div>

            <!-- More results indicator -->
            @if (extraCount() > 0) {
              <p class="text-xs text-[var(--color-text-muted)] text-center mt-3">
                + {{ extraCount() }} további találat
              </p>
            }
          } @else {
            <p class="text-sm text-[var(--color-text-muted)] text-center py-2">
              Nem találtam eredményt
            </p>
          }
        </div>
      </div>
    </div>
  `,
})
export class ChatAnswerMessageComponent {
  response = input.required<SearchResponse>();

  visibleHits = computed(() => this.response().hits.slice(0, 10));

  extraCount = computed(() => {
    const total = this.response().totalCount;
    const visible = this.visibleHits().length;
    return total > visible ? total - visible : 0;
  });

  confidencePct = computed(() => {
    const c = this.response().confidence;
    return c != null ? Math.round(c * 100) : 0;
  });

  confidenceBarClass = computed(() => {
    const pct = this.confidencePct();
    if (pct >= 75) return 'bg-success-500';
    if (pct >= 40) return 'bg-warn-400';
    return 'bg-danger-500';
  });

  badgeVariant(hit: SearchHit): BadgeVariant {
    return entityBadgeVariant(hit.entityType);
  }

  entityTypeLabel(hit: SearchHit): string {
    return entityLabel(hit.entityType);
  }

  snippetText(hit: SearchHit): string {
    return hit.snippet ? truncate(hit.snippet, 150) : '';
  }

  entityRouterLink(hit: SearchHit): string[] {
    switch (hit.entityType.toLowerCase()) {
      case 'document':   return ['/documents', hit.entityId];
      case 'note':       return ['/notes'];
      case 'task':       return ['/tasks'];
      case 'deadline':   return ['/deadlines'];
      case 'reminder':   return ['/reminders'];
      case 'suggestion': return ['/suggestions'];
      default:           return ['/'];
    }
  }
}
