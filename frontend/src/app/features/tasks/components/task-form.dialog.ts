import {
  Component,
  ChangeDetectionStrategy,
  inject,
  input,
  output,
  computed,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { FamilyApiService } from '../../family/services/family.api';
import { ButtonComponent } from '../../../shared/ui/button.component';
import type { FamilyMemberDto } from '../../family/models/family-member.dto';
import type { TaskListItemDto, CreateTaskRequest, PatchTaskRequest } from '../models/task.dto';

@Component({
  selector: 'app-task-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/50"
      (click)="cancel.emit()"
    >
      <div
        class="bg-[var(--color-bg)] rounded-xl p-6 max-w-lg w-full shadow-xl mx-4 max-h-[90vh] overflow-y-auto"
        (click)="$event.stopPropagation()"
      >
        <h3 class="font-semibold text-lg mb-4">
          {{ readonly() ? 'Feladat megtekintése' : (task() ? 'Feladat szerkesztése' : 'Új feladat') }}
        </h3>

        @if (task()?.sourceDocumentId) {
          <div class="text-sm mb-4">
            <a
              data-testid="task-source-link"
              [href]="'/documents/' + task()!.sourceDocumentId"
              target="_blank"
              rel="noopener noreferrer"
              class="text-primary-600 hover:underline inline-flex items-center gap-1"
            >
              {{ task()!.sourceDocumentTitle || 'Forrás dokumentum megnyitása' }}
              <span aria-hidden="true">↗</span>
            </a>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <!-- Title -->
          <div>
            <label class="text-sm font-medium">Cím *</label>
            <input
              data-testid="task-title-input"
              formControlName="title"
              type="text"
              placeholder="Feladat neve..."
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)]"
            />
            @if (form.get('title')?.hasError('required') && form.get('title')?.touched) {
              <p class="text-danger-600 text-xs mt-1">A cím megadása kötelező.</p>
            }
          </div>

          <!-- Description -->
          <div>
            <label class="text-sm font-medium">Leírás</label>
            <textarea
              data-testid="task-form-description"
              formControlName="description"
              rows="3"
              placeholder="Részletek..."
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)] resize-none"
            ></textarea>
          </div>

          <!-- Due date -->
          <div>
            <label class="text-sm font-medium">Határidő</label>
            <input
              data-testid="task-form-dueDate"
              formControlName="dueDate"
              type="date"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500 bg-[var(--color-bg)]"
            />
          </div>

          <!-- Priority -->
          <div>
            <label class="text-sm font-medium">Prioritás</label>
            <select
              data-testid="task-form-priority"
              formControlName="priority"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm bg-[var(--color-bg)] focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="Low">Alacsony</option>
              <option value="Normal">Normal</option>
              <option value="High">Magas</option>
            </select>
          </div>

          <!-- Assigned to -->
          <div>
            <label class="text-sm font-medium">Hozzárendelt tag</label>
            <select
              data-testid="task-form-assignedTo"
              formControlName="assignedToFamilyMemberId"
              class="mt-1 w-full border border-[var(--color-border)] rounded-lg px-3 py-2 text-sm bg-[var(--color-bg)] focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="">— Nincs hozzárendelve —</option>
              @for (m of familyMembers(); track m.id) {
                <option [value]="m.id">{{ m.displayName }}</option>
              }
            </select>
          </div>

          <!-- isPrivate -->
          <div class="flex items-center gap-2">
            <input
              data-testid="task-form-isPrivate"
              formControlName="isPrivate"
              type="checkbox"
              id="task-isPrivate"
              class="rounded border-[var(--color-border)]"
            />
            <label for="task-isPrivate" class="text-sm">Privát feladat</label>
          </div>

          <!-- Buttons -->
          <div class="flex gap-3 justify-end pt-2">
            @if (readonly()) {
              <!-- View mode: closing must never mutate the task — Bezár only closes. -->
              <ui-button
                data-testid="task-view-close"
                variant="ghost"
                type="button"
                (click)="cancel.emit()"
              >
                Bezár
              </ui-button>
            } @else if (isSuggested()) {
              <!--
                Editing a Suggested (AI-proposed) task has no plain "save" — the only
                meaningful actions are approve/reject (same as the card buttons). Mégse
                only closes, same as above; it must NOT fall through to onSubmit()/save.
              -->
              <ui-button data-testid="task-cancel" variant="ghost" type="button" (click)="cancel.emit()">
                Mégse
              </ui-button>
              <ui-button
                data-testid="task-form-reject"
                variant="ghost"
                type="button"
                [disabled]="acting()"
                (click)="reject.emit(task()!.id)"
              >
                Elutasít
              </ui-button>
              <ui-button
                data-testid="task-form-approve"
                variant="primary"
                type="button"
                [disabled]="acting()"
                (click)="approve.emit(task()!.id)"
              >
                Elfogad
              </ui-button>
            } @else {
              <ui-button data-testid="task-cancel" variant="ghost" type="button" (click)="cancel.emit()">
                Mégse
              </ui-button>
              <ui-button
                data-testid="task-form-submit"
                variant="primary"
                type="submit"
                [disabled]="form.invalid"
              >
                {{ task() ? 'Mentés' : 'Létrehozás' }}
              </ui-button>
            }
          </div>
        </form>
      </div>
    </div>
  `,
})
export class TaskFormDialogComponent implements OnInit {
  task = input<TaskListItemDto | null>(null);
  readonly = input(false);
  acting = input(false);

  save = output<CreateTaskRequest | PatchTaskRequest>();
  approve = output<string>();
  reject = output<string>();
  cancel = output<void>();

  isSuggested = computed(() => this.task()?.status === 'Suggested');

  private fb = inject(FormBuilder);
  private familyApi = inject(FamilyApiService);

  familyMembers = signal<FamilyMemberDto[]>([]);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(500)]],
    description: [''],
    dueDate: [''],
    priority: ['Normal'],
    assignedToFamilyMemberId: [''],
    isPrivate: [false],
  });

  ngOnInit(): void {
    this.familyApi.list().subscribe({
      next: members => this.familyMembers.set(members),
    });

    const t = this.task();
    if (t) {
      this.form.patchValue({
        title: t.title,
        description: t.description ?? '',
        priority: t.priority,
        assignedToFamilyMemberId: t.assignedToFamilyMemberId ?? '',
        dueDate: t.dueDateUtc ? t.dueDateUtc.substring(0, 10) : '',
      });
    }

    if (this.readonly()) {
      this.form.disable();
    }
  }

  onSubmit(): void {
    // Defensive: a Suggested-task dialog has no submit button (approve/reject instead), and a
    // readonly dialog has none either — but if a stray implicit-submit (e.g. Enter key) ever
    // reaches here, it must never fall through to a save/patch call.
    if (this.readonly() || this.isSuggested()) return;
    if (this.form.invalid) return;
    const v = this.form.getRawValue();

    const existing = this.task();
    if (existing) {
      const req: PatchTaskRequest = {};
      if (v.title) req.title = v.title;
      if (v.description) req.description = v.description;
      if (v.dueDate) req.dueDateUtc = new Date(v.dueDate).toISOString();
      if (v.priority) req.priority = v.priority as 'Low' | 'Normal' | 'High';
      if (v.assignedToFamilyMemberId) req.assignedToFamilyMemberId = v.assignedToFamilyMemberId;
      req.isPrivate = v.isPrivate ?? false;
      this.save.emit(req);
    } else {
      const req: CreateTaskRequest = {
        title: v.title ?? '',
        priority: (v.priority as 'Low' | 'Normal' | 'High') ?? 'Normal',
        isPrivate: v.isPrivate ?? false,
      };
      if (v.description) req.description = v.description;
      if (v.dueDate) req.dueDateUtc = new Date(v.dueDate).toISOString();
      if (v.assignedToFamilyMemberId) req.assignedToFamilyMemberId = v.assignedToFamilyMemberId;
      this.save.emit(req);
    }
  }
}
