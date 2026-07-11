import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TasksFacade } from './services/tasks.facade';
import { FamilyApiService } from '../family/services/family.api';
import { TaskCardComponent } from './components/task-card.component';
import { TaskFormDialogComponent } from './components/task-form.dialog';
import type { FamilyMemberDto } from '../family/models/family-member.dto';
import type { TaskListItemDto, TaskListParams, CreateTaskRequest, PatchTaskRequest, TaskPriority } from './models/task.dto';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [CommonModule, FormsModule, TaskCardComponent, TaskFormDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto space-y-4">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Feladatok</h1>
        <button
          data-testid="task-create-btn"
          class="px-4 py-2 rounded-lg bg-primary-600 text-white text-sm font-medium hover:bg-primary-700 transition-colors"
          (click)="openCreate()"
        >+ Új feladat</button>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-3">
        <select
          data-testid="tasks-filter-member"
          [(ngModel)]="filterMemberId"
          (ngModelChange)="applyFilters()"
          class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm bg-[var(--color-bg)]"
        >
          <option value="">Összes tag</option>
          @for (m of familyMembers(); track m.id) {
            <option [value]="m.id">{{ m.displayName }}</option>
          }
        </select>

        <select
          data-testid="tasks-filter-priority"
          [(ngModel)]="filterPriority"
          (ngModelChange)="applyFilters()"
          class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm bg-[var(--color-bg)]"
        >
          <option value="">Minden prioritás</option>
          <option value="High">Magas</option>
          <option value="Normal">Normal</option>
          <option value="Low">Alacsony</option>
        </select>
      </div>

      <!-- Loading -->
      @if (facade.loading()) {
        <div class="text-center py-12 text-[var(--color-text-muted)]">Betöltés...</div>
      } @else if (facade.error()) {
        <div class="text-center py-12">
          <p class="text-danger-600 mb-3">{{ facade.error() }}</p>
          <button
            class="px-4 py-2 rounded-lg border border-[var(--color-border)] text-sm hover:bg-[var(--color-surface)]"
            (click)="facade.load()"
          >Újrapróbál</button>
        </div>
      } @else {

        <!-- Kanban columns -->
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">

          <!-- Suggested -->
          <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-warn-400 inline-block"></span>
              Javasolt
              <span class="ml-auto text-[var(--color-text-muted)]">{{ filteredSuggested().length }}</span>
            </h2>
            <div class="space-y-2">
              @for (task of filteredSuggested(); track task.id) {
                <app-task-card
                  [task]="task"
                  [acting]="facade.actingId() === task.id"
                  (approve)="facade.approve($event)"
                  (reject)="facade.reject($event)"
                  (start)="facade.start($event)"
                  (complete)="facade.complete($event)"
                  (cancel)="facade.cancel($event)"
                  (edit)="openEdit($event)"
                  (view)="openView($event)"
                />
              }
              @if (!filteredSuggested().length) {
                <p class="text-xs text-[var(--color-text-muted)] text-center py-4">Nincs javasolt feladat</p>
              }
            </div>
          </div>

          <!-- Open -->
          <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-primary-400 inline-block"></span>
              Nyitott
              <span class="ml-auto text-[var(--color-text-muted)]">{{ filteredOpen().length }}</span>
            </h2>
            <div class="space-y-2">
              @for (task of filteredOpen(); track task.id) {
                <app-task-card
                  [task]="task"
                  [acting]="facade.actingId() === task.id"
                  (approve)="facade.approve($event)"
                  (reject)="facade.reject($event)"
                  (start)="facade.start($event)"
                  (complete)="facade.complete($event)"
                  (cancel)="facade.cancel($event)"
                  (edit)="openEdit($event)"
                  (view)="openView($event)"
                />
              }
              @if (!filteredOpen().length) {
                <p class="text-xs text-[var(--color-text-muted)] text-center py-4">Nincs nyitott feladat</p>
              }
            </div>
          </div>

          <!-- InProgress -->
          <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-success-400 inline-block"></span>
              Folyamatban
              <span class="ml-auto text-[var(--color-text-muted)]">{{ filteredInProgress().length }}</span>
            </h2>
            <div class="space-y-2">
              @for (task of filteredInProgress(); track task.id) {
                <app-task-card
                  [task]="task"
                  [acting]="facade.actingId() === task.id"
                  (approve)="facade.approve($event)"
                  (reject)="facade.reject($event)"
                  (start)="facade.start($event)"
                  (complete)="facade.complete($event)"
                  (cancel)="facade.cancel($event)"
                  (edit)="openEdit($event)"
                  (view)="openView($event)"
                />
              }
              @if (!filteredInProgress().length) {
                <p class="text-xs text-[var(--color-text-muted)] text-center py-4">Nincs folyamatban lévő feladat</p>
              }
            </div>
          </div>

          <!-- Done -->
          <div class="rounded-xl border border-[var(--color-border)] bg-[var(--color-surface)] p-3">
            <h2 class="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-3 flex items-center gap-1.5">
              <span class="w-2 h-2 rounded-full bg-gray-400 inline-block"></span>
              Kész
              <span class="ml-auto text-[var(--color-text-muted)]">{{ filteredDone().length }}</span>
            </h2>
            <div class="space-y-2">
              @for (task of filteredDone(); track task.id) {
                <app-task-card
                  [task]="task"
                  [acting]="facade.actingId() === task.id"
                  (approve)="facade.approve($event)"
                  (reject)="facade.reject($event)"
                  (start)="facade.start($event)"
                  (complete)="facade.complete($event)"
                  (cancel)="facade.cancel($event)"
                  (edit)="openEdit($event)"
                  (view)="openView($event)"
                />
              }
              @if (!filteredDone().length) {
                <p class="text-xs text-[var(--color-text-muted)] text-center py-4">Nincs befejezett feladat</p>
              }
            </div>
          </div>

        </div>

        <!-- Empty state overall -->
        @if (isEmpty()) {
          <div class="text-center py-16 text-[var(--color-text-muted)]">
            <p class="text-4xl mb-3">&#x2713;</p>
            <p class="font-medium">Nincsenek feladatok</p>
            <p class="text-sm mt-1">Kattints az "+ Új feladat" gombra az első feladat létrehozásához.</p>
          </div>
        }

      }
    </div>

    <!-- Task form dialog -->
    @if (showDialog()) {
      <app-task-form-dialog
        [task]="editingTask()"
        [readonly]="dialogReadonly()"
        [acting]="facade.actingId() === editingTask()?.id"
        (save)="onDialogSave($event)"
        (approve)="onDialogApprove($event)"
        (reject)="onDialogReject($event)"
        (cancel)="closeDialog()"
      />
    }
  `,
})
export class TasksPage implements OnInit {
  facade = inject(TasksFacade);
  private familyApi = inject(FamilyApiService);

  familyMembers = signal<FamilyMemberDto[]>([]);
  showDialog = signal(false);
  editingTask = signal<TaskListItemDto | null>(null);
  dialogReadonly = signal(false);

  filterMemberId = '';
  filterPriority = '';

  filteredSuggested = computed(() =>
    this.applyLocalFilters(this.facade.suggested())
  );
  filteredOpen = computed(() =>
    this.applyLocalFilters(this.facade.open())
  );
  filteredInProgress = computed(() =>
    this.applyLocalFilters(this.facade.inProgress())
  );
  filteredDone = computed(() =>
    this.applyLocalFilters(this.facade.done())
  );

  isEmpty = computed(() =>
    !this.filteredSuggested().length &&
    !this.filteredOpen().length &&
    !this.filteredInProgress().length &&
    !this.filteredDone().length
  );

  ngOnInit(): void {
    this.facade.load();
    this.familyApi.list().subscribe({
      next: members => this.familyMembers.set(members),
    });
  }

  applyFilters(): void {
    const params: TaskListParams = {};
    if (this.filterMemberId) params.assignedToFamilyMemberId = this.filterMemberId;
    if (this.filterPriority) params.priority = this.filterPriority as TaskPriority;
    this.facade.load(params);
  }

  private applyLocalFilters(tasks: TaskListItemDto[]): TaskListItemDto[] {
    return tasks.filter(t => {
      if (this.filterMemberId && t.assignedToFamilyMemberId !== this.filterMemberId) return false;
      if (this.filterPriority && t.priority !== this.filterPriority) return false;
      return true;
    });
  }

  openCreate(): void {
    this.editingTask.set(null);
    this.dialogReadonly.set(false);
    this.showDialog.set(true);
  }

  openEdit(task: TaskListItemDto): void {
    this.editingTask.set(task);
    this.dialogReadonly.set(false);
    this.showDialog.set(true);
  }

  openView(task: TaskListItemDto): void {
    this.editingTask.set(task);
    this.dialogReadonly.set(true);
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.editingTask.set(null);
    this.dialogReadonly.set(false);
  }

  onDialogSave(req: CreateTaskRequest | PatchTaskRequest): void {
    const editing = this.editingTask();
    if (editing) {
      this.facade.patch(editing.id, req as PatchTaskRequest, () => this.closeDialog());
    } else {
      this.facade.create(req as CreateTaskRequest, () => this.closeDialog());
    }
  }

  async onDialogApprove(id: string): Promise<void> {
    await this.facade.approve(id);
    this.closeDialog();
  }

  async onDialogReject(id: string): Promise<void> {
    await this.facade.reject(id);
    this.closeDialog();
  }
}
