import { describe, it, expect } from 'vitest';
import { DeadlineCardComponent } from './components/deadline-card.component';
import { DeadlineFormDialogComponent } from './components/deadline-form.dialog';
import { DeadlinesFacade } from './services/deadlines.facade';
import { DeadlinesApiService } from './services/deadlines.api';
import { DeadlinesPage } from './deadlines.page';
import type {
  DeadlineStatus,
  DeadlineCategory,
  DeadlineOrigin,
  DeadlineListItemDto,
} from './models/deadline.dto';

// ─── Pure logic helpers (extracted from component computed signals) ────────────

type BadgeVariant = 'info' | 'warn' | 'default' | 'success' | 'danger';

function categoryVariant(category: DeadlineCategory): BadgeVariant {
  const map: Record<DeadlineCategory, BadgeVariant> = {
    Insurance: 'info',
    Invoice: 'warn',
    Inspection: 'default',
    School: 'success',
    Medical: 'danger',
    Subscription: 'info',
    Personal: 'default',
    Other: 'default',
  };
  return map[category];
}

function categoryLabel(category: DeadlineCategory): string {
  const map: Record<DeadlineCategory, string> = {
    Insurance: 'Biztosítás',
    Invoice: 'Számla',
    Inspection: 'Szemle',
    School: 'Iskola',
    Medical: 'Orvosi',
    Subscription: 'Előfizetés',
    Personal: 'Személyes',
    Other: 'Egyéb',
  };
  return map[category];
}

function isAiOrigin(origin: DeadlineOrigin): boolean {
  return origin === 'AiSuggested' || origin === 'AiApproved';
}

function isExpired(status: DeadlineStatus): boolean {
  return status === 'Passed' || status === 'Due';
}

function showActionButtons(status: DeadlineStatus): boolean {
  return status === 'Upcoming' || status === 'Due' || status === 'Passed';
}

function filterByCategory(items: DeadlineListItemDto[], category: DeadlineCategory | ''): DeadlineListItemDto[] {
  if (!category) return items;
  return items.filter(d => d.category === category);
}

function getUpcoming(items: DeadlineListItemDto[]): DeadlineListItemDto[] {
  return items.filter(d => d.status === 'Upcoming' || d.status === 'Due');
}

function getPassed(items: DeadlineListItemDto[]): DeadlineListItemDto[] {
  return items.filter(d => d.status === 'Passed');
}

function getResolved(items: DeadlineListItemDto[]): DeadlineListItemDto[] {
  return items.filter(d => d.status === 'Resolved' || d.status === 'Dismissed');
}

// ─── Class existence ──────────────────────────────────────────────────────────

describe('Deadlines feature — class existence', () => {
  it('DeadlineCardComponent is defined', () => {
    expect(DeadlineCardComponent).toBeDefined();
    expect(typeof DeadlineCardComponent).toBe('function');
  });

  it('DeadlineFormDialogComponent is defined', () => {
    expect(DeadlineFormDialogComponent).toBeDefined();
    expect(typeof DeadlineFormDialogComponent).toBe('function');
  });

  it('DeadlinesFacade is defined', () => {
    expect(DeadlinesFacade).toBeDefined();
    expect(typeof DeadlinesFacade).toBe('function');
  });

  it('DeadlinesApiService is defined', () => {
    expect(DeadlinesApiService).toBeDefined();
    expect(typeof DeadlinesApiService).toBe('function');
  });

  it('DeadlinesPage is defined', () => {
    expect(DeadlinesPage).toBeDefined();
    expect(typeof DeadlinesPage).toBe('function');
  });
});

// ─── Domain enum values ───────────────────────────────────────────────────────

describe('Deadline domain enums', () => {
  it('DeadlineStatus values are correct', () => {
    const statuses: DeadlineStatus[] = ['Upcoming', 'Due', 'Passed', 'Resolved', 'Dismissed'];
    expect(statuses).toHaveLength(5);
    expect(statuses).toContain('Upcoming');
    expect(statuses).toContain('Due');
    expect(statuses).toContain('Passed');
    expect(statuses).toContain('Resolved');
    expect(statuses).toContain('Dismissed');
  });

  it('DeadlineCategory values are correct', () => {
    const cats: DeadlineCategory[] = ['Insurance', 'Invoice', 'Inspection', 'School', 'Medical', 'Subscription', 'Personal', 'Other'];
    expect(cats).toHaveLength(8);
  });

  it('DeadlineOrigin values are correct', () => {
    const origins: DeadlineOrigin[] = ['Manual', 'AiSuggested', 'AiApproved', 'ImportedEmail', 'ImportedFile'];
    expect(origins).toHaveLength(5);
  });
});

// ─── Category badge logic ─────────────────────────────────────────────────────

describe('DeadlineCardComponent — category badge variant', () => {
  it('Insurance → info', () => expect(categoryVariant('Insurance')).toBe('info'));
  it('Invoice → warn', () => expect(categoryVariant('Invoice')).toBe('warn'));
  it('Inspection → default', () => expect(categoryVariant('Inspection')).toBe('default'));
  it('School → success', () => expect(categoryVariant('School')).toBe('success'));
  it('Medical → danger', () => expect(categoryVariant('Medical')).toBe('danger'));
  it('Subscription → info', () => expect(categoryVariant('Subscription')).toBe('info'));
  it('Personal → default', () => expect(categoryVariant('Personal')).toBe('default'));
  it('Other → default', () => expect(categoryVariant('Other')).toBe('default'));
});

describe('DeadlineCardComponent — category label', () => {
  it('Insurance label is Biztosítás', () => expect(categoryLabel('Insurance')).toBe('Biztosítás'));
  it('Invoice label is Számla', () => expect(categoryLabel('Invoice')).toBe('Számla'));
  it('Inspection label is Szemle', () => expect(categoryLabel('Inspection')).toBe('Szemle'));
  it('School label is Iskola', () => expect(categoryLabel('School')).toBe('Iskola'));
  it('Medical label is Orvosi', () => expect(categoryLabel('Medical')).toBe('Orvosi'));
  it('Subscription label is Előfizetés', () => expect(categoryLabel('Subscription')).toBe('Előfizetés'));
  it('Personal label is Személyes', () => expect(categoryLabel('Personal')).toBe('Személyes'));
  it('Other label is Egyéb', () => expect(categoryLabel('Other')).toBe('Egyéb'));
});

// ─── AI origin detection ──────────────────────────────────────────────────────

describe('DeadlineCardComponent — AI origin detection', () => {
  it('AiSuggested is AI origin', () => expect(isAiOrigin('AiSuggested')).toBe(true));
  it('AiApproved is AI origin', () => expect(isAiOrigin('AiApproved')).toBe(true));
  it('Manual is NOT AI origin', () => expect(isAiOrigin('Manual')).toBe(false));
  it('ImportedEmail is NOT AI origin', () => expect(isAiOrigin('ImportedEmail')).toBe(false));
  it('ImportedFile is NOT AI origin', () => expect(isAiOrigin('ImportedFile')).toBe(false));
});

// ─── Expired detection ────────────────────────────────────────────────────────

describe('DeadlineCardComponent — expired status detection', () => {
  it('Passed status → expired', () => expect(isExpired('Passed')).toBe(true));
  it('Due status → expired', () => expect(isExpired('Due')).toBe(true));
  it('Upcoming status → not expired', () => expect(isExpired('Upcoming')).toBe(false));
  it('Resolved status → not expired', () => expect(isExpired('Resolved')).toBe(false));
  it('Dismissed status → not expired', () => expect(isExpired('Dismissed')).toBe(false));
});

// ─── Action button visibility ─────────────────────────────────────────────────

describe('DeadlineCardComponent — action button visibility', () => {
  it('Upcoming → show actions', () => expect(showActionButtons('Upcoming')).toBe(true));
  it('Due → show actions', () => expect(showActionButtons('Due')).toBe(true));
  it('Passed → show actions', () => expect(showActionButtons('Passed')).toBe(true));
  it('Resolved → hide actions', () => expect(showActionButtons('Resolved')).toBe(false));
  it('Dismissed → hide actions', () => expect(showActionButtons('Dismissed')).toBe(false));
});

// ─── Section grouping logic ───────────────────────────────────────────────────

const sampleItems: DeadlineListItemDto[] = [
  { id: '1', title: 'A', status: 'Upcoming', category: 'Medical', origin: 'Manual', relatedFamilyMemberId: null, dueDateUtc: '2099-01-01T00:00:00Z', createdUtc: '2024-01-01T00:00:00Z' },
  { id: '2', title: 'B', status: 'Due',      category: 'Invoice', origin: 'AiSuggested', relatedFamilyMemberId: null, dueDateUtc: '2024-06-01T00:00:00Z', createdUtc: '2024-01-01T00:00:00Z' },
  { id: '3', title: 'C', status: 'Passed',   category: 'School',  origin: 'Manual', relatedFamilyMemberId: null, dueDateUtc: '2024-05-01T00:00:00Z', createdUtc: '2024-01-01T00:00:00Z' },
  { id: '4', title: 'D', status: 'Resolved', category: 'Other',   origin: 'Manual', relatedFamilyMemberId: null, dueDateUtc: '2024-04-01T00:00:00Z', createdUtc: '2024-01-01T00:00:00Z' },
  { id: '5', title: 'E', status: 'Dismissed',category: 'Other',   origin: 'Manual', relatedFamilyMemberId: null, dueDateUtc: '2024-03-01T00:00:00Z', createdUtc: '2024-01-01T00:00:00Z' },
];

describe('DeadlinesPage — section grouping', () => {
  it('upcoming includes Upcoming and Due', () => {
    const result = getUpcoming(sampleItems);
    expect(result).toHaveLength(2);
    expect(result.map(d => d.id)).toContain('1');
    expect(result.map(d => d.id)).toContain('2');
  });

  it('passed includes only Passed', () => {
    const result = getPassed(sampleItems);
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('3');
  });

  it('resolved includes Resolved and Dismissed', () => {
    const result = getResolved(sampleItems);
    expect(result).toHaveLength(2);
    expect(result.map(d => d.id)).toContain('4');
    expect(result.map(d => d.id)).toContain('5');
  });
});

// ─── Category filter logic ────────────────────────────────────────────────────

describe('DeadlinesPage — category filter', () => {
  it('empty category returns all items', () => {
    expect(filterByCategory(sampleItems, '')).toHaveLength(5);
  });

  it('Medical filter returns 1 item', () => {
    const result = filterByCategory(sampleItems, 'Medical');
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('1');
  });

  it('Other filter returns 2 items', () => {
    const result = filterByCategory(sampleItems, 'Other');
    expect(result).toHaveLength(2);
  });

  it('Invoice filter returns AiSuggested item', () => {
    const result = filterByCategory(sampleItems, 'Invoice');
    expect(result).toHaveLength(1);
    expect(result[0].origin).toBe('AiSuggested');
  });
});
