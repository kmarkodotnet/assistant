export type ProcessingStatus = 'Pending' | 'Extracting' | 'Analyzing' | 'Done' | 'Failed';
export type SourceType = 'Upload' | 'Email' | 'Manual';
export type Origin = 'Manual' | 'AiSuggested' | 'AiApproved' | 'ImportedEmail' | 'ImportedFile';
export type ExtractionMethod = 'PdfTextLayer' | 'TesseractOcr' | 'ManualPaste' | 'EmailBody';

export interface DocumentDto {
  id: string;
  title: string;
  originalFileName: string;
  mimeType: string;
  sizeBytes: number;
  sha256: string;
  sourceType: SourceType;
  isPrivate: boolean;
  processingStatus: ProcessingStatus;
  documentDate: string | null;
  relatedFamilyMemberId: string | null;
  createdByUserAccountId: string;
  createdUtc: string;
  updatedUtc: string;
}

export interface DocumentDetailDto extends DocumentDto {
  textSummary: {
    charCount: number;
    languageDetected: string | null;
    isManuallyEdited: boolean;
    extractionMethod: ExtractionMethod;
  } | null;
  aiSummary: string | null;
}

export interface DocumentTextDto {
  content: string;
  extractionMethod: ExtractionMethod;
  languageDetected: string | null;
  charCount: number;
  isManuallyEdited: boolean;
}

export interface DocumentListResponse {
  items: DocumentDto[];
  page: number;
  pageSize: number;
  totalCount: number;
}
