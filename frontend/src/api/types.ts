export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export interface SchoolClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface CreateClassInput {
  name: string;
  totalSeats: number;
}

export interface Hold {
  holdId: string;
  classId: string;
  className: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
  createdAt: string;
  confirmedAt: string | null;
}

/** RFC 7807 body returned by the API for every error. */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}
