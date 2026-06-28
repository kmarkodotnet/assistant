import { Component, ChangeDetectionStrategy, inject, signal, OnInit } from '@angular/core';
import {
  SuggestionsService,
  SuggestionsAggregateDto,
  DocumentTagSuggestionDto,
  DocumentTopicSuggestionDto,
  SuggestedTaskDto,
  SuggestedDeadlineDto,
} from '../../core/api/suggestions.service';
import { SuggestionBlockComponent, SuggestionItem } from '../../shared/ui/suggestion-block.component';
import { CardComponent } from '../../shared/ui/card.component';
import { SkeletonComponent } from '../../shared/ui/skeleton.component';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-suggestions-page',
  standalone: true,
  imports: [SuggestionBlockComponent, CardComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-3xl mx-auto p-4 space-y-4">
      <div class="flex items-center justify-between">
        <h1 class="text-2xl font-bold">AI javaslatok</h1>
        @if (!loading() && data()) {
          <span class="text-sm text-[var(--color-text-muted)]">
            {{ data()!.totalCount }} javaslat vár jóváhagyásra
          </span>
        }
      </div>

      @if (loading()) {
        <ui-card>
          <div class="space-y-2">
            @for (_ of [1,2,3]; track $index) {
              <ui-skeleton height="3rem" />
            }
          </div>
        </ui-card>
      } @else if (error()) {
        <div class="text-danger-600 p-4">Nem sikerült betölteni a javaslatokat.</div>
      } @else if (data()!.totalCount === 0) {
        <ui-card>
          <div class="flex flex-col items-center py-8 text-center">
            <span class="text-4xl mb-3">✓</span>
            <p class="font-medium">Nincs jóváhagyásra váró javaslat.</p>
            <p class="text-sm text-[var(--color-text-muted)] mt-1">Az AI feldolgozás után itt jelennek meg az ajánlások.</p>
          </div>
        </ui-card>
      } @else {

        <!-- Tasks -->
        @if (data()!.tasks.length > 0) {
          <div>
            <h2 class="text-base font-semibold mb-2">Feladatok ({{ data()!.tasks.length }})</h2>
            <ui-suggestion-block
              [suggestions]="taskSuggestions()"
              (approve)="approveTask($event)"
              (reject)="rejectTask($event)"
              (approveAll)="approveAllTasks()"
              (rejectAll)="rejectAllTasks()" />
          </div>
        }

        <!-- Deadlines -->
        @if (data()!.deadlines.length > 0) {
          <div>
            <h2 class="text-base font-semibold mb-2">Határidők ({{ data()!.deadlines.length }})</h2>
            <ui-suggestion-block
              [suggestions]="deadlineSuggestions()"
              (approve)="approveDeadline($event)"
              (reject)="rejectDeadline($event)"
              (approveAll)="approveAllDeadlines()"
              (rejectAll)="rejectAllDeadlines()" />
          </div>
        }

        <!-- Tags -->
        @if (data()!.tags.length > 0) {
          <div>
            <h2 class="text-base font-semibold mb-2">Tagek dokumentumokhoz ({{ data()!.tags.length }})</h2>
            <ui-suggestion-block
              [suggestions]="tagSuggestions()"
              (approve)="approveTag($event)"
              (reject)="rejectTag($event)"
              (approveAll)="approveAllTags()"
              (rejectAll)="rejectAllTags()" />
          </div>
        }

        <!-- Topics -->
        @if (data()!.topics.length > 0) {
          <div>
            <h2 class="text-base font-semibold mb-2">Témák dokumentumokhoz ({{ data()!.topics.length }})</h2>
            <ui-suggestion-block
              [suggestions]="topicSuggestions()"
              (approve)="approveTopic($event)"
              (reject)="rejectTopic($event)"
              (approveAll)="approveAllTopics()"
              (rejectAll)="rejectAllTopics()" />
          </div>
        }

      }
    </div>
  `,
})
export class SuggestionsPage implements OnInit {
  private suggestionsService = inject(SuggestionsService);
  private notificationService = inject(NotificationService);

  loading = signal(true);
  error = signal(false);
  data = signal<SuggestionsAggregateDto | null>(null);

  taskSuggestions = signal<SuggestionItem[]>([]);
  deadlineSuggestions = signal<SuggestionItem[]>([]);
  tagSuggestions = signal<SuggestionItem[]>([]);
  topicSuggestions = signal<SuggestionItem[]>([]);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(false);
    try {
      const result = await this.suggestionsService.getAll();
      this.data.set(result);
      this.taskSuggestions.set(result.tasks.map(t => ({ id: t.id, label: t.title, type: 'task' })));
      this.deadlineSuggestions.set(result.deadlines.map(d => ({ id: d.id, label: d.title, type: 'deadline' })));
      this.tagSuggestions.set(result.tags.map(t => ({
        id: `${t.documentId}::${t.tagId}`,
        label: `${t.tagName}`,
        type: 'tag',
      })));
      this.topicSuggestions.set(result.topics.map(t => ({
        id: `${t.documentId}::${t.topicId}`,
        label: `${t.topicName} (/${t.topicSlug})`,
        type: 'topic',
      })));
    } catch {
      this.error.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  async approveTask(item: SuggestionItem): Promise<void> {
    await this.batch({ approve: { tasks: [item.id] } });
  }

  async rejectTask(item: SuggestionItem): Promise<void> {
    await this.batch({ reject: { tasks: [item.id] } });
  }

  async approveAllTasks(): Promise<void> {
    await this.batch({ approve: { tasks: this.data()!.tasks.map(t => t.id) } });
  }

  async rejectAllTasks(): Promise<void> {
    await this.batch({ reject: { tasks: this.data()!.tasks.map(t => t.id) } });
  }

  async approveDeadline(item: SuggestionItem): Promise<void> {
    await this.batch({ approve: { deadlines: [item.id] } });
  }

  async rejectDeadline(item: SuggestionItem): Promise<void> {
    await this.batch({ reject: { deadlines: [item.id] } });
  }

  async approveAllDeadlines(): Promise<void> {
    await this.batch({ approve: { deadlines: this.data()!.deadlines.map(d => d.id) } });
  }

  async rejectAllDeadlines(): Promise<void> {
    await this.batch({ reject: { deadlines: this.data()!.deadlines.map(d => d.id) } });
  }

  async approveTag(item: SuggestionItem): Promise<void> {
    const [documentId, tagId] = item.id.split('::') as [string, string];
    await this.batch({ approve: { tags: [{ documentId, tagId }] } });
  }

  async rejectTag(item: SuggestionItem): Promise<void> {
    const [documentId, tagId] = item.id.split('::') as [string, string];
    await this.batch({ reject: { tags: [{ documentId, tagId }] } });
  }

  async approveAllTags(): Promise<void> {
    const tags = this.data()!.tags.map(t => ({ documentId: t.documentId, tagId: t.tagId }));
    await this.batch({ approve: { tags } });
  }

  async rejectAllTags(): Promise<void> {
    const tags = this.data()!.tags.map(t => ({ documentId: t.documentId, tagId: t.tagId }));
    await this.batch({ reject: { tags } });
  }

  async approveTopic(item: SuggestionItem): Promise<void> {
    const [documentId, topicId] = item.id.split('::') as [string, string];
    await this.batch({ approve: { topics: [{ documentId, topicId }] } });
  }

  async rejectTopic(item: SuggestionItem): Promise<void> {
    const [documentId, topicId] = item.id.split('::') as [string, string];
    await this.batch({ reject: { topics: [{ documentId, topicId }] } });
  }

  async approveAllTopics(): Promise<void> {
    const topics = this.data()!.topics.map(t => ({ documentId: t.documentId, topicId: t.topicId }));
    await this.batch({ approve: { topics } });
  }

  async rejectAllTopics(): Promise<void> {
    const topics = this.data()!.topics.map(t => ({ documentId: t.documentId, topicId: t.topicId }));
    await this.batch({ reject: { topics } });
  }

  private async batch(body: Parameters<SuggestionsService['batch']>[0]): Promise<void> {
    try {
      const result = await this.suggestionsService.batch(body);
      this.notificationService.success(`${result.approved} elfogadva, ${result.rejected} elutasítva.`);
      await this.load();
    } catch {
      this.notificationService.error('Nem sikerült feldolgozni a javaslatokat.');
    }
  }
}
