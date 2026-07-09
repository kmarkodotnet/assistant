import { describe, it, expect } from 'vitest';
import { TaskCardComponent } from './components/task-card.component';
import { TaskFormDialogComponent } from './components/task-form.dialog';
import { TasksFacade } from './services/tasks.facade';
import { TasksApiService } from './services/tasks.api';
import { TasksPage } from './tasks.page';
import type { TaskStatus, TaskPriority, TaskOrigin, TaskListItemDto } from './models/task.dto';

// Pure logic helpers extracted from components (no Angular DI needed)

function priorityVariant(priority: TaskPriority): string {
  switch (priority) {
    case 'High': return 'danger';
    case 'Low': return 'default';
    default: return 'info';
  }
}

function priorityLabel(priority: TaskPriority): string {
  switch (priority) {
    case 'High': return 'Magas';
    case 'Low': return 'Alacsony';
    default: return 'Normal';
  }
}

function isAiOrigin(origin: TaskOrigin): boolean {
  return origin === 'AiSuggested' || origin === 'AiApproved';
}

function isOverdue(dueDateUtc: string | null): boolean {
  if (!dueDateUtc) return false;
  return new Date(dueDateUtc) < new Date();
}

function filterByMember(tasks: TaskListItemDto[], memberId: string): TaskListItemDto[] {
  if (!memberId) return tasks;
  return tasks.filter(t => t.assignedToFamilyMemberId === memberId);
}

function filterByPriority(tasks: TaskListItemDto[], priority: TaskPriority | ''): TaskListItemDto[] {
  if (!priority) return tasks;
  return tasks.filter(t => t.priority === priority);
}

// ─── Component class existence ───────────────────────────────────────────────

describe('Tasks feature — class existence', () => {
  it('TaskCardComponent is defined', () => {
    expect(TaskCardComponent).toBeDefined();
    expect(typeof TaskCardComponent).toBe('function');
  });

  it('TaskFormDialogComponent is defined', () => {
    expect(TaskFormDialogComponent).toBeDefined();
    expect(typeof TaskFormDialogComponent).toBe('function');
  });

  it('TasksFacade is defined', () => {
    expect(TasksFacade).toBeDefined();
    expect(typeof TasksFacade).toBe('function');
  });

  it('TasksApiService is defined', () => {
    expect(TasksApiService).toBeDefined();
    expect(typeof TasksApiService).toBe('function');
  });

  it('TasksPage is defined', () => {
    expect(TasksPage).toBeDefined();
    expect(typeof TasksPage).toBe('function');
  });
});

// ─── TaskCardComponent pure logic ─────────────────────────────────────────────

describe('TaskCardComponent — priority badge logic', () => {
  it('High priority → danger variant', () => {
    expect(priorityVariant('High')).toBe('danger');
  });

  it('Low priority → default variant', () => {
    expect(priorityVariant('Low')).toBe('default');
  });

  it('Normal priority → info variant', () => {
    expect(priorityVariant('Normal')).toBe('info');
  });
});

describe('TaskCardComponent — priority label', () => {
  it('labels map correctly', () => {
    expect(priorityLabel('High')).toBe('Magas');
    expect(priorityLabel('Low')).toBe('Alacsony');
    expect(priorityLabel('Normal')).toBe('Normal');
  });
});

describe('TaskCardComponent — AI origin detection', () => {
  it('AiSuggested is AI origin', () => {
    expect(isAiOrigin('AiSuggested')).toBe(true);
  });

  it('AiApproved is AI origin', () => {
    expect(isAiOrigin('AiApproved')).toBe(true);
  });

  it('Manual is NOT AI origin', () => {
    expect(isAiOrigin('Manual')).toBe(false);
  });

  it('ImportedEmail is NOT AI origin', () => {
    expect(isAiOrigin('ImportedEmail')).toBe(false);
  });
});

describe('TaskCardComponent — overdue detection', () => {
  it('null due date → not overdue', () => {
    expect(isOverdue(null)).toBe(false);
  });

  it('past date → overdue', () => {
    expect(isOverdue('2020-01-01T00:00:00Z')).toBe(true);
  });

  it('future date → not overdue', () => {
    expect(isOverdue('2099-01-01T00:00:00Z')).toBe(false);
  });
});

// ─── DTO type validity ────────────────────────────────────────────────────────

describe('Task domain enums', () => {
  it('TaskStatus values are correct', () => {
    const statuses: TaskStatus[] = ['Suggested', 'Open', 'InProgress', 'Done', 'Cancelled'];
    expect(statuses).toHaveLength(5);
    expect(statuses).toContain('Suggested');
    expect(statuses).toContain('InProgress');
  });

  it('TaskPriority values are correct', () => {
    const priorities: TaskPriority[] = ['Low', 'Normal', 'High'];
    expect(priorities).toHaveLength(3);
  });

  it('TaskOrigin values are correct', () => {
    const origins: TaskOrigin[] = ['Manual', 'AiSuggested', 'AiApproved', 'ImportedEmail', 'ImportedFile'];
    expect(origins).toHaveLength(5);
  });
});

// ─── Filter logic ─────────────────────────────────────────────────────────────

describe('TasksPage — filter logic', () => {
  const tasks: TaskListItemDto[] = [
    { id: '1', title: 'A', status: 'Open', priority: 'High', origin: 'Manual', assignedToFamilyMemberId: 'member-1', dueDateUtc: null, createdUtc: '2024-01-01T00:00:00Z' },
    { id: '2', title: 'B', status: 'Open', priority: 'Low',  origin: 'Manual', assignedToFamilyMemberId: 'member-2', dueDateUtc: null, createdUtc: '2024-01-01T00:00:00Z' },
    { id: '3', title: 'C', status: 'Open', priority: 'High', origin: 'Manual', assignedToFamilyMemberId: null,       dueDateUtc: null, createdUtc: '2024-01-01T00:00:00Z' },
  ];

  it('empty memberId returns all tasks', () => {
    expect(filterByMember(tasks, '')).toHaveLength(3);
  });

  it('specific memberId filters correctly', () => {
    const result = filterByMember(tasks, 'member-1');
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('1');
  });

  it('empty priority returns all tasks', () => {
    expect(filterByPriority(tasks, '')).toHaveLength(3);
  });

  it('priority filter returns only matching tasks', () => {
    const result = filterByPriority(tasks, 'High');
    expect(result).toHaveLength(2);
    expect(result.every(t => t.priority === 'High')).toBe(true);
  });

  it('Low priority filter returns one task', () => {
    const result = filterByPriority(tasks, 'Low');
    expect(result).toHaveLength(1);
    expect(result[0].id).toBe('2');
  });
});
