export type DeadlineStatus = 'Upcoming' | 'Due' | 'Passed' | 'Resolved' | 'Dismissed';
export type DeadlineCategory = 'Insurance' | 'Invoice' | 'Inspection' | 'School' | 'Medical' | 'Subscription' | 'Personal' | 'Other';
export type DeadlineOrigin = 'Manual' | 'AiSuggested' | 'AiApproved' | 'ImportedEmail' | 'ImportedFile';

export interface DeadlineListItemDto {
  id: string;
  title: string;
  dueDateUtc: string;
  status: DeadlineStatus;
  category: DeadlineCategory;
  origin: DeadlineOrigin;
  relatedFamilyMemberId: string | null;
  createdUtc: string;
}

export interface DeadlineDto extends DeadlineListItemDto {
  description: string | null;
  sourceDocumentId: string | null;
  createdByUserAccountId: string;
  isPrivate: boolean;
  updatedUtc: string;
  approvedByUserAccountId: string | null;
  approvedUtc: string | null;
}

export interface DeadlineListParams {
  from?: string | undefined;
  to?: string | undefined;
  category?: DeadlineCategory | undefined;
  status?: DeadlineStatus | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
}

export interface CreateDeadlineRequest {
  title: string;
  description?: string | undefined;
  dueDateUtc: string;
  category: DeadlineCategory;
  relatedFamilyMemberId?: string | undefined;
  isPrivate: boolean;
}

export interface PatchDeadlineRequest {
  title?: string | undefined;
  description?: string | undefined;
  dueDateUtc?: string | undefined;
  category?: DeadlineCategory | undefined;
  relatedFamilyMemberId?: string | undefined;
  isPrivate?: boolean | undefined;
}
