import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  computed,
} from '@angular/core';
import type { ToolCallProposal, ToolCallResult, ToolCallStatus } from '../models/search.dto';

/**
 * Megerősítő kártya egy whitelistelt tool-hívási javaslathoz (api-design.md §16.1/§16.3).
 * A kártya sosem hajt végre semmit önmagában — csak a facade metódusait hívja
 * a confirm/reject outputokon keresztül, majd a `status` input alapján vált
 * megjelenítést (pending → executing → executed/rejected). Kétszeri elküldés
 * ellen a gombok a `pending` állapoton kívül el vannak rejtve.
 */
@Component({
  selector: 'app-tool-call-proposal-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex justify-start mb-3">
      <div class="max-w-[90%] w-full">
        <div class="border border-warn-300 bg-warn-50 rounded-2xl rounded-tl-sm px-4 py-3 shadow-sm">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-semibold text-warn-800 uppercase tracking-wide">AI parancs-javaslat</span>
          </div>

          <p class="text-sm leading-relaxed mb-3">{{ proposal().summary }}</p>

          @if (proposal().parameters.length > 0) {
            <div class="rounded-xl border border-warn-200 bg-[var(--color-bg)] divide-y divide-warn-100 mb-3">
              @for (param of proposal().parameters; track param.label) {
                <div class="flex items-center justify-between px-3 py-1.5 text-xs">
                  <span class="text-[var(--color-text-muted)]">{{ param.label }}</span>
                  <span class="font-medium text-right">{{ param.value }}</span>
                </div>
              }
            </div>
          }

          @if (proposal().warnings.length > 0) {
            <ul class="mb-3 space-y-1">
              @for (warning of proposal().warnings; track warning) {
                <li class="text-xs text-warn-800">⚠ {{ warning }}</li>
              }
            </ul>
          }

          @if (isPending()) {
            <div class="flex items-center gap-2">
              <button
                data-testid="toolcall-confirm"
                [disabled]="isExecuting()"
                (click)="confirm.emit()"
                class="px-3 py-1.5 text-xs font-medium rounded-lg bg-success-600 text-white hover:bg-success-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >Jóváhagyás</button>
              <button
                data-testid="toolcall-reject"
                [disabled]="isExecuting()"
                (click)="reject.emit()"
                class="px-3 py-1.5 text-xs font-medium rounded-lg border border-danger-300 text-danger-700 hover:bg-danger-50 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >Elutasítás</button>
            </div>
          } @else if (status() === 'executed') {
            <p data-testid="toolcall-result" class="text-xs font-medium text-success-700">
              ✓ {{ result()?.summary ?? 'Végrehajtva.' }}
            </p>
          } @else if (status() === 'rejected') {
            <p data-testid="toolcall-result" class="text-xs font-medium text-[var(--color-text-muted)]">
              Elutasítva — nem történt módosítás.
            </p>
          }
        </div>
      </div>
    </div>
  `,
})
export class ToolCallProposalCardComponent {
  proposal = input.required<ToolCallProposal>();
  status = input<ToolCallStatus>('pending');
  result = input<ToolCallResult | null>(null);

  confirm = output<void>();
  reject = output<void>();

  isPending = computed(() => this.status() === 'pending' || this.status() === 'executing');
  isExecuting = computed(() => this.status() === 'executing');
}
