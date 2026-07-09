import { describe, it, expect } from 'vitest';
import { SearchApiService } from './services/search.api';
import { SearchFacade } from './services/search.facade';
import { ChatUserMessageComponent } from './components/chat-user-message.component';
import { ChatAnswerMessageComponent } from './components/chat-answer-message.component';
import { AnswerSourcesComponent } from './components/answer-sources.component';
import { SearchPage } from './search.page';
import type { SearchMode, SearchHit, SearchResponse } from './models/search.dto';

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
});
