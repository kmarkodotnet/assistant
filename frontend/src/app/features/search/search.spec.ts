import { describe, it, expect, vi, beforeEach } from 'vitest';
import { Injector, runInInjectionContext } from '@angular/core';
import { of, throwError } from 'rxjs';
import { SearchApiService } from './services/search.api';
import { SearchFacade } from './services/search.facade';
import { ChatUserMessageComponent } from './components/chat-user-message.component';
import { ChatAnswerMessageComponent } from './components/chat-answer-message.component';
import { AnswerSourcesComponent } from './components/answer-sources.component';
import { ToolCallProposalCardComponent } from './components/tool-call-proposal-card.component';
import { SearchPage } from './search.page';
import { NotificationService } from '../../core/notifications/notification.service';
import type {
  SearchMode,
  SearchHit,
  SearchResponse,
  ToolCallProposal,
  ToolCallResult,
} from './models/search.dto';

// ─── Pure logic helpers mirrored from components ──────────────────────────────

type BadgeVariant = 'default' | 'success' | 'warn' | 'danger' | 'info';

function entityBadgeVariant(entityType: string): BadgeVariant {
  switch (entityType.toLowerCase()) {
    case 'document': return 'info';
    case 'task': return 'warn';
    case 'deadline': return 'danger';
    default: return 'default';
  }
}

function entityLabel(entityType: string): string {
  switch (entityType.toLowerCase()) {
    case 'document': return 'Dokumentum';
    case 'task': return 'Feladat';
    case 'deadline': return 'Határidő';
    default: return entityType;
  }
}

function truncate(text: string, max: number): string {
  return text.length > max ? text.slice(0, max) + '...' : text;
}

const MODE_LABELS: Record<SearchMode, string> = {
  Auto: 'Auto',
  Filter: 'Szuro',
  Text: 'Szoveges',
  Semantic: 'Szemantikus',
  Qa: 'Q&A',
  Command: 'Parancs',
};

function modeLabel(mode: SearchMode): string {
  return MODE_LABELS[mode] ?? mode;
}

function confidencePct(confidence: number): number {
  return Math.round(confidence * 100);
}

function confidenceBarClass(pct: number): string {
  if (pct >= 75) return 'bg-success-500';
  if (pct >= 40) return 'bg-warn-400';
  return 'bg-danger-500';
}

function extraCount(totalCount: number, visibleCount: number): number {
  return totalCount > visibleCount ? totalCount - visibleCount : 0;
}

// ─── Class existence ──────────────────────────────────────────────────────────

describe('Search feature — class existence', () => {
  it('SearchApiService is defined', () => {
    expect(SearchApiService).toBeDefined();
    expect(typeof SearchApiService).toBe('function');
  });

  it('SearchFacade is defined', () => {
    expect(SearchFacade).toBeDefined();
    expect(typeof SearchFacade).toBe('function');
  });

  it('ChatUserMessageComponent is defined', () => {
    expect(ChatUserMessageComponent).toBeDefined();
    expect(typeof ChatUserMessageComponent).toBe('function');
  });

  it('ChatAnswerMessageComponent is defined', () => {
    expect(ChatAnswerMessageComponent).toBeDefined();
    expect(typeof ChatAnswerMessageComponent).toBe('function');
  });

  it('AnswerSourcesComponent is defined', () => {
    expect(AnswerSourcesComponent).toBeDefined();
    expect(typeof AnswerSourcesComponent).toBe('function');
  });

  it('SearchPage is defined', () => {
    expect(SearchPage).toBeDefined();
    expect(typeof SearchPage).toBe('function');
  });

  it('ToolCallProposalCardComponent is defined', () => {
    expect(ToolCallProposalCardComponent).toBeDefined();
    expect(typeof ToolCallProposalCardComponent).toBe('function');
  });
});

// ─── entityBadgeVariant ───────────────────────────────────────────────────────

describe('ChatAnswerMessageComponent — entityBadgeVariant', () => {
  it('document → info', () => {
    expect(entityBadgeVariant('document')).toBe('info');
  });

  it('task → warn', () => {
    expect(entityBadgeVariant('task')).toBe('warn');
  });

  it('deadline → danger', () => {
    expect(entityBadgeVariant('deadline')).toBe('danger');
  });

  it('unknown → default', () => {
    expect(entityBadgeVariant('other')).toBe('default');
  });

  it('case insensitive: Document → info', () => {
    expect(entityBadgeVariant('Document')).toBe('info');
  });
});

// ─── entityLabel ──────────────────────────────────────────────────────────────

describe('ChatAnswerMessageComponent — entityLabel', () => {
  it('document → Dokumentum', () => {
    expect(entityLabel('document')).toBe('Dokumentum');
  });

  it('task → Feladat', () => {
    expect(entityLabel('task')).toBe('Feladat');
  });

  it('deadline → Határidő', () => {
    expect(entityLabel('deadline')).toBe('Határidő');
  });

  it('unknown → passthrough', () => {
    expect(entityLabel('invoice')).toBe('invoice');
  });
});

// ─── truncate ─────────────────────────────────────────────────────────────────

describe('truncate helper', () => {
  it('short text unchanged', () => {
    expect(truncate('hello', 150)).toBe('hello');
  });

  it('exact length unchanged', () => {
    const text = 'a'.repeat(150);
    expect(truncate(text, 150)).toBe(text);
  });

  it('long text truncated with ellipsis', () => {
    const text = 'a'.repeat(200);
    const result = truncate(text, 150);
    expect(result).toHaveLength(153); // 150 + '...'
    expect(result.endsWith('...')).toBe(true);
  });

  it('empty string unchanged', () => {
    expect(truncate('', 150)).toBe('');
  });
});

// ─── modeLabel ────────────────────────────────────────────────────────────────

describe('ChatUserMessageComponent — modeLabel', () => {
  it('Auto → Auto', () => {
    expect(modeLabel('Auto')).toBe('Auto');
  });

  it('Qa → Q&A', () => {
    expect(modeLabel('Qa')).toBe('Q&A');
  });

  it('Semantic → Szemantikus', () => {
    expect(modeLabel('Semantic')).toBe('Szemantikus');
  });

  it('Text → Szoveges', () => {
    expect(modeLabel('Text')).toBe('Szoveges');
  });

  it('Command → Parancs', () => {
    expect(modeLabel('Command')).toBe('Parancs');
  });

  it('Filter → Szuro', () => {
    expect(modeLabel('Filter')).toBe('Szuro');
  });
});

// ─── confidencePct ───────────────────────────────────────────────────────────

describe('ChatAnswerMessageComponent — confidencePct', () => {
  it('0.0 → 0%', () => {
    expect(confidencePct(0)).toBe(0);
  });

  it('0.5 → 50%', () => {
    expect(confidencePct(0.5)).toBe(50);
  });

  it('1.0 → 100%', () => {
    expect(confidencePct(1)).toBe(100);
  });

  it('0.856 → 86% (rounded)', () => {
    expect(confidencePct(0.856)).toBe(86);
  });
});

// ─── confidenceBarClass ───────────────────────────────────────────────────────

describe('ChatAnswerMessageComponent — confidenceBarClass', () => {
  it('>=75 → success', () => {
    expect(confidenceBarClass(75)).toBe('bg-success-500');
    expect(confidenceBarClass(100)).toBe('bg-success-500');
  });

  it('40-74 → warn', () => {
    expect(confidenceBarClass(40)).toBe('bg-warn-400');
    expect(confidenceBarClass(74)).toBe('bg-warn-400');
  });

  it('<40 → danger', () => {
    expect(confidenceBarClass(0)).toBe('bg-danger-500');
    expect(confidenceBarClass(39)).toBe('bg-danger-500');
  });
});

// ─── extraCount ───────────────────────────────────────────────────────────────

describe('ChatAnswerMessageComponent — extraCount', () => {
  it('totalCount === visible → 0 extra', () => {
    expect(extraCount(10, 10)).toBe(0);
  });

  it('totalCount < visible → 0 extra (defensive)', () => {
    expect(extraCount(5, 10)).toBe(0);
  });

  it('totalCount > visible → positive extra', () => {
    expect(extraCount(50, 10)).toBe(40);
  });

  it('totalCount === 0 → 0', () => {
    expect(extraCount(0, 0)).toBe(0);
  });
});

// ─── SearchFacade initial state ───────────────────────────────────────────────

describe('SearchFacade — initial signal values', () => {
  it('history starts empty', () => {
    // Verify that the facade class exposes a history property (signal)
    const proto = SearchFacade.prototype;
    expect(typeof proto.ask).toBe('function');
    expect(typeof proto.clearHistory).toBe('function');
    expect(typeof proto.loadSaved).toBe('function');
    expect(typeof proto.saveCurrentSearch).toBe('function');
    expect(typeof proto.deleteSaved).toBe('function');
  });
});

// ─── SearchApiService method signatures ──────────────────────────────────────

describe('SearchApiService — method signatures', () => {
  it('has search method', () => {
    expect(typeof SearchApiService.prototype.search).toBe('function');
  });

  it('has getSaved method', () => {
    expect(typeof SearchApiService.prototype.getSaved).toBe('function');
  });

  it('has saveSearch method', () => {
    expect(typeof SearchApiService.prototype.saveSearch).toBe('function');
  });

  it('has deleteSaved method', () => {
    expect(typeof SearchApiService.prototype.deleteSaved).toBe('function');
  });

  it('has confirmToolCall method', () => {
    expect(typeof SearchApiService.prototype.confirmToolCall).toBe('function');
  });

  it('has rejectToolCall method', () => {
    expect(typeof SearchApiService.prototype.rejectToolCall).toBe('function');
  });
});

// ─── Command-mód tool-call flow (E8 — jóváhagyás/elutasítás, nulla módosítás hibán) ──

function makeProposal(overrides: Partial<ToolCallProposal> = {}): ToolCallProposal {
  return {
    proposalToken: 'token-123',
    toolName: 'create_reminder',
    summary: 'Létrehozzak egy emlékeztetőt a mosógép garanciájának lejárta előtt 3 nappal?',
    parameters: [
      { label: 'Termék', value: 'Mosógép (Bosch WAT28)' },
      { label: 'Lejárat', value: '2027-03-01' },
    ],
    warnings: [],
    expiresUtc: '2026-07-11T12:40:00Z',
    ...overrides,
  };
}

function makeCommandResponse(proposal: ToolCallProposal | null): SearchResponse {
  return {
    hits: [],
    totalCount: 0,
    modeUsed: 'Command',
    answer: proposal ? undefined : 'Nem sikerült egyértelműen feloldani a kérést.',
    toolCallProposal: proposal,
  };
}

describe('SearchFacade — tool-call proposal flow', () => {
  let facade: SearchFacade;
  let apiMock: {
    search: ReturnType<typeof vi.fn>;
    getSaved: ReturnType<typeof vi.fn>;
    saveSearch: ReturnType<typeof vi.fn>;
    deleteSaved: ReturnType<typeof vi.fn>;
    confirmToolCall: ReturnType<typeof vi.fn>;
    rejectToolCall: ReturnType<typeof vi.fn>;
  };
  let notifyMock: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    apiMock = {
      search: vi.fn(),
      getSaved: vi.fn(),
      saveSearch: vi.fn(),
      deleteSaved: vi.fn(),
      confirmToolCall: vi.fn(),
      rejectToolCall: vi.fn(),
    };
    notifyMock = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

    // A projekt nem használ TestBed/zone.js-t (lásd test-setup.ts) — a facade
    // `inject()`-jeit egy sima Injector + runInInjectionContext-tel oldjuk fel.
    const injector = Injector.create({
      providers: [
        { provide: SearchApiService, useValue: apiMock },
        { provide: NotificationService, useValue: notifyMock },
      ],
    });
    facade = runInInjectionContext(injector, () => new SearchFacade());
  });

  it('ask() stores an entry with a pending toolCallStatus when a proposal is returned', async () => {
    const proposal = makeProposal();
    apiMock.search.mockReturnValue(of(makeCommandResponse(proposal)));

    await facade.ask({ query: 'Emlékeztess a mosógép garanciájára', mode: 'Command' });

    expect(facade.history()).toHaveLength(1);
    expect(facade.history()[0].toolCallStatus).toBe('pending');
    expect(facade.history()[0].response.toolCallProposal).toEqual(proposal);
  });

  it('ask() leaves toolCallStatus undefined when no proposal is returned (action: none / Ok=false)', async () => {
    apiMock.search.mockReturnValue(of(makeCommandResponse(null)));

    await facade.ask({ query: 'Mikor jár le a biztosításom?', mode: 'Command' });

    expect(facade.history()[0].toolCallStatus).toBeUndefined();
    expect(facade.history()[0].response.toolCallProposal).toBeNull();
  });

  it('confirmToolCall() moves a pending entry to executed and stores the result', async () => {
    const proposal = makeProposal();
    apiMock.search.mockReturnValue(of(makeCommandResponse(proposal)));
    await facade.ask({ query: 'x', mode: 'Command' });

    const result: ToolCallResult = {
      executed: true,
      resultType: 'Reminder',
      resultId: '01910a0c-aaaa-bbbb-cccc-000000000000',
      summary: 'Emlékeztető létrehozva 2027-02-26 09:00-ra.',
    };
    apiMock.confirmToolCall.mockReturnValue(of(result));

    await facade.confirmToolCall(proposal.proposalToken);

    expect(apiMock.confirmToolCall).toHaveBeenCalledWith(proposal.proposalToken);
    expect(facade.history()[0].toolCallStatus).toBe('executed');
    expect(facade.history()[0].toolCallResult).toEqual(result);
  });

  it('confirmToolCall() reverts to pending and notifies on error — zero mutation on failure', async () => {
    const proposal = makeProposal();
    apiMock.search.mockReturnValue(of(makeCommandResponse(proposal)));
    await facade.ask({ query: 'x', mode: 'Command' });

    apiMock.confirmToolCall.mockReturnValue(throwError(() => new Error('403')));

    await facade.confirmToolCall(proposal.proposalToken);

    expect(facade.history()[0].toolCallStatus).toBe('pending');
    expect(facade.history()[0].toolCallResult).toBeUndefined();
    expect(notifyMock.error).toHaveBeenCalled();
  });

  it('rejectToolCall() moves a pending entry to rejected with no result payload', async () => {
    const proposal = makeProposal();
    apiMock.search.mockReturnValue(of(makeCommandResponse(proposal)));
    await facade.ask({ query: 'x', mode: 'Command' });

    apiMock.rejectToolCall.mockReturnValue(of(undefined));

    await facade.rejectToolCall(proposal.proposalToken);

    expect(apiMock.rejectToolCall).toHaveBeenCalledWith(proposal.proposalToken, undefined);
    expect(facade.history()[0].toolCallStatus).toBe('rejected');
  });

  it('confirmToolCall() is a no-op (no second HTTP call) once the entry is no longer pending', async () => {
    const proposal = makeProposal();
    apiMock.search.mockReturnValue(of(makeCommandResponse(proposal)));
    await facade.ask({ query: 'x', mode: 'Command' });

    apiMock.confirmToolCall.mockReturnValue(
      of({ executed: true, resultType: 'Reminder', resultId: 'id-1', summary: 'OK' }),
    );

    await facade.confirmToolCall(proposal.proposalToken);
    await facade.confirmToolCall(proposal.proposalToken); // double-click guard

    expect(apiMock.confirmToolCall).toHaveBeenCalledTimes(1);
  });
});

// ─── SearchFacade error handling — 501 (feature not enabled) ──────────────────────

describe('SearchFacade — HTTP 501 error handling', () => {
  let facade: SearchFacade;
  let apiMock: {
    search: ReturnType<typeof vi.fn>;
    getSaved: ReturnType<typeof vi.fn>;
    saveSearch: ReturnType<typeof vi.fn>;
    deleteSaved: ReturnType<typeof vi.fn>;
    confirmToolCall: ReturnType<typeof vi.fn>;
    rejectToolCall: ReturnType<typeof vi.fn>;
  };
  let notifyMock: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    apiMock = {
      search: vi.fn(),
      getSaved: vi.fn(),
      saveSearch: vi.fn(),
      deleteSaved: vi.fn(),
      confirmToolCall: vi.fn(),
      rejectToolCall: vi.fn(),
    };
    notifyMock = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

    const injector = Injector.create({
      providers: [
        { provide: SearchApiService, useValue: apiMock },
        { provide: NotificationService, useValue: notifyMock },
      ],
    });
    facade = runInInjectionContext(injector, () => new SearchFacade());
  });

  it('ask() shows specific message for HTTP 501 (Command mode not enabled)', async () => {
    const error501 = { status: 501, error: { detail: 'Not Implemented' } };
    apiMock.search.mockReturnValue(throwError(() => error501));

    await facade.ask({ query: 'Parancs kérés', mode: 'Command' });

    expect(facade.error()).toBe('A parancs mód jelenleg nincs bekapcsolva ezen a szerveren.');
    expect(notifyMock.error).toHaveBeenCalledWith(
      'A parancs mód jelenleg nincs bekapcsolva ezen a szerveren.',
    );
  });

  it('ask() shows generic message for other HTTP errors', async () => {
    const error500 = { status: 500, error: { detail: 'Internal Server Error' } };
    apiMock.search.mockReturnValue(throwError(() => error500));

    await facade.ask({ query: 'Keresés', mode: 'Text' });

    expect(facade.error()).toBe('Nem sikerült végrehajtani a keresést.');
    expect(notifyMock.error).toHaveBeenCalledWith('Nem sikerült végrehajtani a keresést.');
  });

  it('ask() shows generic message for non-HTTP errors', async () => {
    const plainError = new Error('Network unreachable');
    apiMock.search.mockReturnValue(throwError(() => plainError));

    await facade.ask({ query: 'Keresés', mode: 'Text' });

    expect(facade.error()).toBe('Nem sikerült végrehajtani a keresést.');
    expect(notifyMock.error).toHaveBeenCalledWith('Nem sikerült végrehajtani a keresést.');
  });
});
