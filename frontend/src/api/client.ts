import { apiBaseUrl } from '../config';
import type { CreateClassInput, Hold, ProblemDetails, SchoolClass } from './types';

export const STUDENT_ID_HEADER = 'X-Student-Id';

/** Every failure the UI can see, normalised: HTTP errors carry the API's problem body. */
export class ApiError extends Error {
  readonly status: number | null;
  readonly code: string | null;
  readonly problem: ProblemDetails | null;

  constructor(message: string, status: number | null, problem: ProblemDetails | null = null) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = problem?.code ?? null;
    this.problem = problem;
  }

  /** The server could not be reached at all (down, offline, CORS, DNS). */
  get isNetworkError(): boolean {
    return this.status === null;
  }
}

// ---- Server clock -------------------------------------------------------

let serverOffsetMs = 0;

/**
 * Current time on the server's clock. Countdowns use this so a wrong clock on the
 * parent's phone cannot make the "pay fee" button look enabled after the hold expired.
 * The HTTP Date header has 1 s resolution, so small offsets are ignored as noise.
 */
export function serverNow(): number {
  return Date.now() + serverOffsetMs;
}

function syncClock(response: Response): void {
  const header = response.headers.get('Date');
  const serverTime = header ? Date.parse(header) : NaN;
  if (Number.isNaN(serverTime)) return;
  const offset = serverTime - Date.now();
  serverOffsetMs = Math.abs(offset) > 1_500 ? offset : 0;
}

// ---- Transport ----------------------------------------------------------

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE';
  body?: unknown;
  studentId?: string;
}

async function request<T>(path: string, { method = 'GET', body, studentId }: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (studentId) headers[STUDENT_ID_HEADER] = studentId;

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch (cause) {
    throw new ApiError(`Network error: ${String(cause)}`, null);
  }

  syncClock(response);

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as ProblemDetails | null;
    throw new ApiError(problem?.title ?? `HTTP ${response.status}`, response.status, problem);
  }

  return (response.status === 204 ? undefined : await response.json()) as T;
}

// ---- Endpoints (1:1 with the API contract) ------------------------------

export const api = {
  createClass: (input: CreateClassInput) =>
    request<SchoolClass>('/classes', { method: 'POST', body: input }),
  listClasses: () => request<SchoolClass[]>('/classes'),
  getClass: (classId: string) => request<SchoolClass>(`/classes/${encodeURIComponent(classId)}`),
  placeHold: (classId: string, studentId: string) =>
    request<Hold>(`/classes/${encodeURIComponent(classId)}/holds`, { method: 'POST', studentId }),
  getHold: (holdId: string) => request<Hold>(`/holds/${encodeURIComponent(holdId)}`),
  confirmHold: (holdId: string, studentId: string) =>
    request<Hold>(`/holds/${encodeURIComponent(holdId)}/confirm`, { method: 'POST', studentId }),
  cancelHold: (holdId: string, studentId: string) =>
    request<void>(`/holds/${encodeURIComponent(holdId)}`, { method: 'DELETE', studentId }),
  myHolds: (studentId: string) => request<Hold[]>('/me/holds', { studentId }),
};
