import { Link } from 'react-router';
import { displayStatus } from '../api/holdStatus';
import { useMyHolds } from '../api/hooks';
import type { Hold } from '../api/types';
import { EmptyTray } from '../components/Artwork';
import { ErrorAlert, Loading } from '../components/Feedback';
import { StatusBadge } from '../components/StatusBadge';
import { formatDuration, useCountdown } from '../components/useCountdown';
import { useStudent } from '../student/context';

export function MyHoldsPage() {
  const { studentId } = useStudent();
  const holds = useMyHolds(studentId);

  return (
    <section className="stack">
      <h1>My applications</h1>
      {!studentId && <p className="alert alert-info">Enter the birth registration number above to see your applications.</p>}
      {studentId && holds.isPending && <Loading />}
      {holds.isError && <ErrorAlert error={holds.error} onRetry={() => void holds.refetch()} />}
      {holds.data?.length === 0 && (
        <div className="empty">
          <EmptyTray />
          <p>No applications yet. <Link to="/">Choose a class</Link></p>
        </div>
      )}
      {holds.data && holds.data.length > 0 && (
        <ul className="hold-list">
          {holds.data.map((h) => <HoldRow key={h.holdId} hold={h} />)}
        </ul>
      )}
    </section>
  );
}

function HoldRow({ hold }: { hold: Hold }) {
  const remaining = useCountdown(hold.status === 'HELD' ? hold.expiresAt : null);
  const status = displayStatus(hold, remaining);

  return (
    <li className="hold-row">
      <div>
        <strong>{hold.className}</strong>
        <div className="muted small">{new Date(hold.createdAt).toLocaleString('en-GB')}</div>
      </div>
      <StatusBadge status={status} />
      {status === 'HELD' && <span className="countdown">{formatDuration(remaining)} left</span>}
      <Link className="btn btn-small" to={`/holds/${hold.holdId}`}>
        {status === 'HELD' ? 'Pay / Cancel' : 'Details'}
      </Link>
    </li>
  );
}
