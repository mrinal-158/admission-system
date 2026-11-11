import type { HoldStatus } from '../api/types';

const statusLabels: Record<HoldStatus, string> = {
  HELD: 'Seat held, fee due',
  CONFIRMED: 'Admission confirmed',
  EXPIRED: 'Expired',
  CANCELLED: 'Cancelled',
};

const stampLabels: Record<HoldStatus, string> = {
  HELD: 'Seat held',
  CONFIRMED: 'Admitted',
  EXPIRED: 'Expired',
  CANCELLED: 'Cancelled',
};

export function StatusBadge({ status }: { status: HoldStatus }) {
  return (
    <span className={`badge badge-${status.toLowerCase()}`} data-status={status}>
      {statusLabels[status]}
    </span>
  );
}

/** The ink stamp on the admission slip. Purely visual: the badge carries the status for screen readers. */
export function StatusStamp({ status }: { status: HoldStatus }) {
  return <span className="stamp" data-status={status} data-label={stampLabels[status]} aria-hidden="true" />;
}
