export interface AppError {
  code: string;
  message: string;
  traceId: string;
  fieldErrors: Record<string, string[]> | null;
}
