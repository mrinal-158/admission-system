import type { Hold, HoldStatus } from './types';

/** HELD past its deadline is EXPIRED, even before the next server poll says so. */
export function displayStatus(hold: Hold, remainingMs: number): HoldStatus {
  return hold.status === 'HELD' && remainingMs <= 0 ? 'EXPIRED' : hold.status;
}
