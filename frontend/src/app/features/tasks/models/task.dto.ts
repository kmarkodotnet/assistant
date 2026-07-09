export type TaskStatus = 'Suggested' | 'Open' | 'InProgress' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Normal' | 'High';
export type TaskOrigin = 'Manual' | 'AiSuggested' | 'AiApproved' | 'ImportedEmail' | 'ImportedFile';

export interface TaskListItemDto {
  id: string;
  title: string;
  dueDateUtc: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  origin: TaskOrigin;
  assignedToFamilyMemberId: string | null;
  createdUtc: string;
}

export interface TaskDto extends TaskListItemDto {
  description: string | null;
  sourceDocumentId: string | null;
  createdByUserAccountId: string;
  isPrivate: boolean;
  updatedUtc: string;
  approvedByUserAccountId: string | null;
  approvedUtc: string | null;
  completedUtc: string | null;
}

export interface TaskListParams {
  status?: TaskStatus | undefined;
  assignedToFamilyMemberId?: string | undefined;
  priority?: TaskPriority | undefined;
  origin?: TaskOrigin | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
}

export interface CreateTaskRequest {
  title: string;
  description?: string | undefined;
  dueDateUtc?: string | undefined;
  priority: TaskPriority;
  assignedToFamilyMemberId?: string | undefined;
  isPrivate: boolean;
}

export interface PatchTaskRequest {
  title?: string | undefined;
  description?: string | undefined;
  dueDateUtc?: string | undefined;
  priority?: TaskPriority | undefined;
  assignedToFamilyMemberId?: string | undefined;
  isPrivate?: boolean | undefined;
}
