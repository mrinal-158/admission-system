import { ApiError } from './client';

/** Plain-language messages for every error code the API returns. */
const messagesByCode: Record<string, string> = {
  NO_SEATS_AVAILABLE: 'This class has no free seats right now.',
  ALREADY_APPLIED: 'This student already has an application in this class.',
  HOLD_EXPIRED: 'Time to pay ran out, so the seat was released.',
  HOLD_CANCELLED: 'This application was cancelled.',
  HOLD_ALREADY_CONFIRMED: 'This admission is already confirmed.',
  HOLD_NOT_OWNED: 'This application belongs to another student.',
  HOLD_NOT_FOUND: 'Application not found.',
  CLASS_NOT_FOUND: 'Class not found.',
  INVALID_STUDENT_ID: 'Enter the 17-digit birth registration number.',
  VALIDATION_FAILED: 'Check the details and try again.',
};

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isNetworkError) {
      return 'Cannot reach the server. Check your connection and try again.';
    }
    if (error.code && messagesByCode[error.code]) {
      return messagesByCode[error.code];
    }
    if (error.status !== null && error.status >= 500) {
      return 'The server had a problem. Try again in a moment.';
    }
    if (error.status === 404) {
      return 'Not found.';
    }
  }
  return 'Something unexpected went wrong. Reload the page.';
}
