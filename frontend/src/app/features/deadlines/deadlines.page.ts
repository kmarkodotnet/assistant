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
import { DeadlinesFacade } from './services/deadlines.facade';
import { DeadlineCardComponent } from './components/deadline-card.component';
import { DeadlineFormDialogComponent } from './components/deadline-form.dialog';
import type {
  DeadlineListItemDto,
  DeadlineListParams,
  CreateDeadlineRequest,
  PatchDeadlineRequest,
  DeadlineCategory,
} from './models/deadline.dto';

type StatusFilter = 'all' | 'upcoming' | 'passed' | 'resolved';

@Component({
  selector: 'app-deadlines-page',
  standalone: true,
  imports: [CommonModule, FormsModule, DeadlineCardComponent, DeadlineFormDialogComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-4xl mx-auto space-y-4">

      <!-- Header -->
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-semibold">Határidők</h1>
        <button
          data-testid="deadline-create-btn"
          class="px-4 py-2 rounded-lg bg-primary-600 text-white text-sm font-medium hover:bg-primary-700 transition-colors"
          (click)="openCreate()"
        >+ Új határidő</button>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-3">
        <select
          data-testid="deadlines-filter-category"
          [(ngModel)]="filterCategory"
          (ngModelChange)="applyFilters()"
          class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm bg-[var(--color-bg)]"
        >
          <option value="">Minden kategória</option>
          <option value="Insurance">Biztosítás</option>
          <option value="Invoice">Számla</option>
          <option value="Inspection">Szemle</option>
          <option value="School">Iskola</option>
          <option value="Medical">Orvosi</option>
          <option value="Subscription">Előfizetés</option>
          <option value="Personal">Személyes</option>
          <option value="Other">Egyéb</option>
        </select>

        <select
          data-testid="deadlines-filter-status"
          [(ngModel)]="filterStatus"
          (ngModelChange)="applyFilters()"
          class="border border-[var(--color-border)] rounded-lg px-3 py-1.5 text-sm bg-[var(--color-bg)]"
        >
          <option value="all">Összes</option>
          <option value="upcoming">Közelgő</option>
          <option value="passed">Lejárt</option>
          <option value="resolved">Megoldva</option>
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

        <!-- Upcoming section -->
        @if (filterStatus === 'all' || filterStatus === 'upcoming') {
          <section>
            <h2 class="text-sm font-semibold uppercase tracking-wider mb-2 flex items-center gap-2">
              <span class="w-2 h-2 rounded-full bg-primary-400 inline-block"></span>
              Közelgő
              <span
                class="ml-auto text-xs font-bold px-1.5 py-0.5 rounded-full"
                [class.bg-danger-100]="hasUrgent()"
                [class.text-danger-700]="hasUrgent()"
                [class.bg-gray-100]="!hasUrgent()"
                [class.text-gray-500]="!hasUrgent()"
              >{{ filteredUpcoming().length }}</span>
            </h2>
            <div class="space-y-2">
              @for (d of filteredUpcoming(); track d.id) {
                <app-deadline-card
                  [deadline]="d"
                  [acting]="facade.actingId() === d.id"
                  (approve)="facade.approve($event)"
                  (resolve)="facade.resolve($event)"
                  (dismiss)="facade.dismiss($event)"
                  (edit)="openEdit($event)"
                />
              }
              @if (!filteredUpcoming().length) {
                <p class="text-xs text-[var(--color-text-muted)] py-4 text-center">Nincs közelgő határidő</p>
              }
            </div>
          </section>
        }

        <!-- Passed section -->
        @if (filterStatus === 'all' || filterStatus === 'passed') {
          <section>
            <h2 class="text-sm font-semibold uppercase tracking-wider mb-2 flex items-center gap-2 text-danger-600">
              <span class="w-2 h-2 rounded-full bg-danger-500 inline-block"></span>
              Lejárt
              <span class="ml-auto text-xs font-bold px-1.5 py-0.5 rounded-full bg-danger-100 text-danger-700">
                {{ filteredPassed().length }}
              </span>
            </h2>
            <div class="space-y-2">
              @for (d of filteredPassed(); track d.id) {
                <app-deadline-card
                  [deadline]="d"
                  [acting]="facade.actingId() === d.id"
                  (approve)="facade.approve($event)"
                  (resolve)="facade.resolve($event)"
                  (dismiss)="facade.dismiss($event)"
                  (edit)="openEdit($event)"
                />
              }
              @if (!filteredPassed().length) {
                <p class="text-xs text-[var(--color-text-muted)] py-4 text-center">Nincs lejárt határidő</p>
              }
            </div>
          </section>
        }

        <!-- Resolved / Dismissed section -->
        @if (filterStatus === 'all' || filterStatus === 'resolved') {
          <section>
            <button
              class="w-full text-left text-sm font-semibold uppercase tracking-wider mb-2 flex items-center gap-2 text-[var(--color-text-muted)]"
              (click)="toggleResolved()"
            >
              <span class="w-2 h-2 rounded-full bg-gray-400 inline-block"></span>
              Megoldva / Mellőzve
              <span class="ml-auto text-xs font-normal px-1.5 py-0.5 rounded-full bg-[var(--color-surface)] text-[var(--color-text-muted)]">
                {{ filteredResolved().length }}
              </span>
              <span class="text-xs">{{ resolvedOpen() ? '▲' : '▼' }}</span>
            </button>
            @if (resolvedOpen()) {
              <div class="space-y-2">
                @for (d of filteredResolved(); track d.id) {
                  <app-deadline-card
                    [deadline]="d"
                    [acting]="facade.actingId() === d.id"
                    (approve)="facade.approve($event)"
                    (resolve)="facade.resolve($event)"
                    (dismiss)="facade.dismiss($event)"
                    (edit)="openEdit($event)"
                  />
                }
                @if (!filteredResolved().length) {
                  <p class="text-xs text-[var(--color-text-muted)] py-4 text-center">Nincs megoldott vagy mellőzött határidő</p>
                }
              </div>
            }
          </section>
        }

        <!-- Empty state overall -->
        @if (isEmpty()) {
          <div class="text-center py-16 text-[var(--color-text-muted)]">
            <p class="text-4xl mb-3">&#128197;</p>
            <p class="font-medium">Nincsenek határidők</p>
            <p class="text-sm mt-1">Kattints az "+ Új határidő" gombra az első határidő létrehozásához.</p>
          </div>
        }

      }
    </div>

    <!-- Deadline form dialog -->
    @if (showDialog()) {
      <app-deadline-form-dialog
        [deadline]="editingDeadline()"
        (save)="onDialogSave($event)"
        (cancel)="closeDialog()"
      />
    }
  `,
})
export class DeadlinesPage implements OnInit {
  facade = inject(DeadlinesFacade);

  showDialog = signal(false);
  editingDeadline = signal<DeadlineListItemDto | null>(null);
  resolvedOpen = signal(true);

  filterCategory: DeadlineCategory | '' = '';
  filterStatus: StatusFilter = 'all';

  filteredUpcoming = computed(() =>
    this.applyLocalFilter(this.facade.upcoming())
  );
  filteredPassed = computed(() =>
    this.applyLocalFilter(this.facade.passed())
  );
  filteredResolved = computed(() =>
    this.applyLocalFilter(this.facade.resolved())
  );

  hasUrgent = computed(() =>
    this.filteredUpcoming().some(d => d.status === 'Due') ||
    this.filteredPassed().length > 0
  );

  isEmpty = computed(() =>
    !this.filteredUpcoming().length &&
    !this.filteredPassed().length &&
    !this.filteredResolved().length
  );

  ngOnInit(): void {
    this.facade.load();
  }

  applyFilters(): void {
    const params: DeadlineListParams = {};
    if (this.filterCategory) params.category = this.filterCategory;
    if (this.filterStatus === 'upcoming') params.status = 'Upcoming';
    else if (this.filterStatus === 'passed') params.status = 'Passed';
    else if (this.filterStatus === 'resolved') params.status = 'Resolved';
    this.facade.load(params);
  }

  private applyLocalFilter(items: DeadlineListItemDto[]): DeadlineListItemDto[] {
    if (!this.filterCategory) return items;
    return items.filter(d => d.category === this.filterCategory);
  }

  toggleResolved(): void {
    this.resolvedOpen.update(v => !v);
  }

  openCreate(): void {
    this.editingDeadline.set(null);
    this.showDialog.set(true);
  }

  openEdit(deadline: DeadlineListItemDto): void {
    this.editingDeadline.set(deadline);
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.editingDeadline.set(null);
  }

  onDialogSave(req: CreateDeadlineRequest | PatchDeadlineRequest): void {
    const editing = this.editingDeadline();
    if (editing) {
      this.facade.patch(editing.id, req as PatchDeadlineRequest, () => this.closeDialog());
    } else {
      this.facade.create(req as CreateDeadlineRequest, () => this.closeDialog());
    }
  }
}
