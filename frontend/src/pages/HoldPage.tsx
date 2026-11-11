import { useEffect } from 'react';
import { Link, useParams } from 'react-router';
import { useQueryClient } from '@tanstack/react-query';
import { useCancelHold, useConfirmHold, useHold, queryKeys } from '../api/hooks';
import { displayStatus } from '../api/holdStatus';
import type { Hold } from '../api/types';
import { Crest } from '../components/Artwork';
import { ErrorAlert, Loading } from '../components/Feedback';
import { StatusBadge, StatusStamp } from '../components/StatusBadge';
import { formatDuration, useCountdown } from '../components/useCountdown';
import { useStudent } from '../student/context';

export function HoldPage() {
  const { holdId = '' } = useParams();
  const hold = useHold(holdId);

  return (
    <section className="narrow stack">
      <Link className="back-link" to="/me">My applications</Link>
      <h1>Your application</h1>
      {hold.isPending && <Loading />}
      {/* Also shown over cached data: a parent watching the countdown must learn the server is unreachable. */}
      {hold.isError && <ErrorAlert error={hold.error} onRetry={() => void hold.refetch()} />}
      {hold.data && <HoldDetails hold={hold.data} />}
    </section>
  );
}

export function HoldDetails({ hold }: { hold: Hold }) {
  const { studentId } = useStudent();
  const queryClient = useQueryClient();
  const confirm = useConfirmHold();
  const cancel = useCancelHold();
  const remaining = useCountdown(hold.status === 'HELD' ? hold.expiresAt : null);
  const status = displayStatus(hold, remaining);
  const isOwner = studentId === hold.studentId;
  const busy = confirm.isPending || cancel.isPending;

  // When the countdown runs out, ask the server so seat counts and status are authoritative.
  useEffect(() => {
    if (hold.status === 'HELD' && remaining === 0) {
      void queryClient.invalidateQueries({ queryKey: queryKeys.hold(hold.holdId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.classes });
    }
  }, [hold.status, hold.holdId, remaining, queryClient]);

  return (
    <article className="slip">
      <div className="slip-head">
        <Crest />
        <span className="slip-kind">Admission slip</span>
        <h2>{hold.className}</h2>
        <span className="sr-only"><StatusBadge status={status} /></span>
      </div>
      <StatusStamp status={status} />

      {status === 'HELD' && (
        <div className="slip-timer">
          <span className="label">Time left to pay</span>
          <span className={`countdown ${remaining < 60_000 ? 'text-danger' : ''}`} aria-live="off">
            {formatDuration(remaining)}
          </span>
          <div className={`drain ${remaining < 60_000 ? 'urgent' : ''}`} aria-hidden="true">
            <span style={{ width: `${timeLeftPercent(hold, remaining)}%` }} />
          </div>
        </div>
      )}

      <div className="slip-section">
        <dl className="details">
          <dt>{status === 'CONFIRMED' ? 'Paid on' : 'Pay by'}</dt>
          <dd>{new Date(status === 'CONFIRMED' && hold.confirmedAt ? hold.confirmedAt : hold.expiresAt).toLocaleString('en-GB')}</dd>
          <dt>Application ID</dt>
          <dd className="mono">{hold.holdId}</dd>
        </dl>
      </div>

      <div className="perforation" aria-hidden="true" />

      <div className="slip-section">
        {status === 'EXPIRED' && (
          <p className="warning-text">
            <span>Time ran out and the seat was released. <Link to="/">Apply again</Link></span>
          </p>
        )}
        {status === 'CANCELLED' && <p className="muted">Cancelled. The seat was released.</p>}

        {!isOwner && status === 'HELD' && (
          <p className="alert alert-info">To pay or cancel, enter this applicant's birth registration number above.</p>
        )}

        {confirm.isError && <ErrorAlert error={confirm.error} />}
        {cancel.isError && <ErrorAlert error={cancel.error} />}

        {isOwner && status !== 'CONFIRMED' && (
          <div className="actions">
            <button
              type="button"
              className="btn btn-primary"
              disabled={status !== 'HELD' || busy}
              onClick={() => confirm.mutate({ holdId: hold.holdId, studentId: hold.studentId })}
            >
              {confirm.isPending ? 'Paying...' : 'Pay admission fee'}
            </button>
            <button
              type="button"
              className="btn"
              disabled={status !== 'HELD' || busy}
              onClick={() => {
                if (window.confirm('Cancel this application? The seat will open up for others.')) {
                  cancel.mutate({ holdId: hold.holdId, studentId: hold.studentId });
                }
              }}
            >
              {cancel.isPending ? 'Cancelling...' : 'Cancel'}
            </button>
          </div>
        )}
      </div>
    </article>
  );
}

/** Share of the hold period still left, for the draining bar. */
function timeLeftPercent(hold: Hold, remainingMs: number): number {
  const total = Date.parse(hold.expiresAt) - Date.parse(hold.createdAt);
  return total > 0 ? Math.max(0, Math.min(100, (remainingMs / total) * 100)) : 0;
}
