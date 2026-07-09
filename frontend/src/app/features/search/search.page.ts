import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
  AfterViewInit,
  ViewChild,
  ElementRef,
  effect,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { SearchFacade } from './services/search.facade';
import { ChatUserMessageComponent } from './components/chat-user-message.component';
import { ChatAnswerMessageComponent } from './components/chat-answer-message.component';
import type { SearchMode, SearchRequest } from './models/search.dto';

@Component({
  selector: 'app-search-page',
  standalone: true,
  imports: [FormsModule, ChatUserMessageComponent, ChatAnswerMessageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col h-full max-h-[calc(100vh-4rem)]">

      <!-- Header -->
      <div class="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border)] bg-[var(--color-bg)] shrink-0">
        <h1 class="text-xl font-semibold">Keresés</h1>
        <div class="flex items-center gap-2">
          @if (facade.history().length > 0) {
            <button
              data-testid="search-clear-history"
              class="px-3 py-1.5 text-xs rounded-lg border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-surface)] transition-colors"
              (click)="facade.clearHistory()"
            >Előzmények törlése</button>
          }
          <button
            class="px-3 py-1.5 text-xs rounded-lg border border-[var(--color-border)] hover:bg-[var(--color-surface)] transition-colors"
            [class.bg-primary-50]="showSaved()"
            [class.text-primary-700]="showSaved()"
            (click)="toggleSaved()"
          >Mentett keresések {{ facade.savedSearches().length > 0 ? '(' + facade.savedSearches().length + ')' : '' }}</button>
        </div>
      </div>

      <!-- Main content -->
      <div class="flex flex-1 overflow-hidden">

        <!-- Chat area -->
        <div class="flex-1 flex flex-col overflow-hidden">

          <!-- Scrollable messages -->
          <div
            #chatContainer
            class="flex-1 overflow-y-auto px-4 py-4 space-y-1"
          >
            @if (facade.history().length === 0 && !facade.loading()) {
              <!-- Empty state -->
              <div class="flex flex-col items-center justify-center h-full text-center py-16">
                <svg class="w-12 h-12 text-[var(--color-text-muted)] mb-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                  <path stroke-linecap="round" stroke-linejoin="round"
                    d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 15.803 7.5 7.5 0 0015.803 15.803z" />
                </svg>
                <p class="text-lg font-medium text-[var(--color-text-muted)]">Kérdezz bármit a dokumentumaidról</p>
                <p class="text-sm text-[var(--color-text-muted)] mt-1">Keresés szöveg, szemantika vagy Q&A módban</p>
              </div>
            } @else {
              @for (entry of facade.history(); track entry.timestamp) {
                <app-chat-user-message [query]="entry.query" />
                <app-chat-answer-message [response]="entry.response" />
              }

              <!-- Loading indicator (after last user message) -->
              @if (facade.loading()) {
                <div class="flex justify-start mb-3">
                  <div class="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl rounded-tl-sm px-4 py-3">
                    <div class="flex items-center gap-2">
                      <div class="w-2 h-2 bg-primary-400 rounded-full animate-bounce [animation-delay:-0.3s]"></div>
                      <div class="w-2 h-2 bg-primary-400 rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                      <div class="w-2 h-2 bg-primary-400 rounded-full animate-bounce"></div>
                    </div>
                  </div>
                </div>
              }
            }
          </div>

          <!-- Input area -->
          <div class="shrink-0 border-t border-[var(--color-border)] bg-[var(--color-bg)] px-4 py-3">
            <div class="max-w-3xl mx-auto">
              <!-- Mode selector -->
              <div class="flex items-center gap-2 mb-2">
                <select
                  data-testid="search-mode-select"
                  [(ngModel)]="selectedMode"
                  class="text-xs border border-[var(--color-border)] rounded-lg px-2 py-1 bg-[var(--color-bg)] text-[var(--color-text-muted)]"
                >
                  <option value="Auto">Auto</option>
                  <option value="Text">Szöveges</option>
                  <option value="Semantic">Szemantikus</option>
                  <option value="Qa">Q&A</option>
                  <option value="Filter">Szűrő</option>
                </select>
              </div>

              <!-- Textarea + send -->
              <div class="flex items-end gap-2">
                <textarea
                  #searchInput
                  data-testid="search-input"
                  [ngModel]="queryText()" (ngModelChange)="queryText.set($event)"
                  rows="1"
                  placeholder="Kérdezz vagy keress..."
                  class="flex-1 resize-none rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent transition-all"
                  [style.max-height]="'120px'"
                  (input)="autoGrow($event)"
                  (keydown.enter)="onEnter($event)"
                  (keydown.shift.enter)="$event.stopPropagation()"
                ></textarea>

                <button
                  data-testid="search-submit"
                  class="shrink-0 px-4 py-2 rounded-xl bg-primary-600 text-white text-sm font-medium hover:bg-primary-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  [disabled]="queryText().trim().length === 0 || facade.loading()"
                  (click)="submit()"
                >
                  <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2.5">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Saved searches sidebar -->
        @if (showSaved()) {
          <div class="w-72 shrink-0 border-l border-[var(--color-border)] bg-[var(--color-surface)] flex flex-col overflow-hidden">
            <div class="px-3 py-3 border-b border-[var(--color-border)]">
              <p class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wide">Mentett keresések</p>
            </div>
            <div class="flex-1 overflow-y-auto p-3 space-y-2">
              @if (facade.savedSearches().length === 0) {
                <p class="text-xs text-[var(--color-text-muted)] text-center py-6">Még nincs mentett keresés</p>
              } @else {
                @for (saved of facade.savedSearches(); track saved.id) {
                  <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-bg)] p-3">
                    <p class="text-sm font-medium leading-snug mb-2 truncate">{{ saved.name }}</p>
                    <div class="flex items-center gap-1">
                      <button
                        class="flex-1 px-2 py-1 text-xs rounded-lg bg-primary-50 text-primary-700 hover:bg-primary-100 transition-colors"
                        (click)="loadSaved(saved.queryJson)"
                      >Betöltés</button>
                      <button
                        class="px-2 py-1 text-xs rounded-lg text-[var(--color-text-muted)] hover:text-danger-600 hover:bg-danger-50 transition-colors"
                        (click)="facade.deleteSaved(saved.id)"
                      >Törlés</button>
                    </div>
                  </div>
                }
              }
            </div>

            <!-- Save current search -->
            @if (queryText().trim()) {
              <div class="p-3 border-t border-[var(--color-border)]">
                <button
                  class="w-full px-3 py-2 text-xs rounded-xl border border-dashed border-[var(--color-border)] text-[var(--color-text-muted)] hover:border-primary-400 hover:text-primary-600 transition-colors"
                  (click)="promptSave()"
                >+ Aktuális keresés mentése</button>
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
})
export class SearchPage implements OnInit, AfterViewInit, OnDestroy {
  facade = inject(SearchFacade);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  @ViewChild('chatContainer') chatContainer!: ElementRef<HTMLDivElement>;

  queryText = signal('');
  selectedMode: SearchMode = 'Auto';
  showSaved = signal(false);
  private paramSub?: Subscription;

  canSubmit = computed(() => this.queryText().trim().length > 0 && !this.facade.loading());

  private scrollEffect = effect(() => {
    // Re-run whenever history or loading changes
    const _h = this.facade.history();
    const _l = this.facade.loading();
    // Scroll after paint
    requestAnimationFrame(() => this.scrollToBottom());
  });

  ngOnInit(): void {
    this.facade.loadSaved();
    this.paramSub = this.route.queryParamMap.subscribe(params => {
      const q = params.get('q')?.trim();
      if (q) {
        this.queryText.set(q);
        void this.submit();
        void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
      }
    });
  }

  ngOnDestroy(): void {
    this.paramSub?.unsubscribe();
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }

  toggleSaved(): void {
    this.showSaved.update(v => !v);
  }

  async submit(): Promise<void> {
    const text = this.queryText().trim();
    if (!text || this.facade.loading()) return;

    const req: SearchRequest = {
      query: text,
      mode: this.selectedMode,
      page: 1,
      pageSize: 20,
    };

    this.queryText.set('');
    this.resetTextareaHeight();
    await this.facade.ask(req);
  }

  onEnter(event: Event): void {
    const kbEvent = event as KeyboardEvent;
    if (kbEvent.shiftKey) return;
    kbEvent.preventDefault();
    void this.submit();
  }

  autoGrow(event: Event): void {
    const el = event.target as HTMLTextAreaElement;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 120) + 'px';
  }

  loadSaved(queryJson: string): void {
    try {
      const req = JSON.parse(queryJson) as SearchRequest;
      this.queryText.set(req.query);
      if (req.mode) this.selectedMode = req.mode;
    } catch {
      // ignore malformed JSON
    }
  }

  promptSave(): void {
    const name = prompt('Keresés neve:');
    if (!name?.trim()) return;
    const req: SearchRequest = {
      query: this.queryText().trim(),
      mode: this.selectedMode,
    };
    void this.facade.saveCurrentSearch(name.trim(), req);
  }

  private scrollToBottom(): void {
    const el = this.chatContainer?.nativeElement;
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  }

  private resetTextareaHeight(): void {
    // Reset happens naturally as value clears; optional explicit resize
    const ta = document.querySelector<HTMLTextAreaElement>('[data-testid="search-input"]');
    if (ta) {
      ta.style.height = 'auto';
    }
  }
}
