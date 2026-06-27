export interface DocumentFilter {
  page: number;
  pageSize: number;
  relatedFamilyMemberId?: string;
  processingStatus?: string;
}

export function emptyFilter(): DocumentFilter {
  return { page: 1, pageSize: 50 };
}
