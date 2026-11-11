import { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useClasses, usePlaceHold } from '../api/hooks';
import type { SchoolClass } from '../api/types';
import { SchoolScene } from '../components/Artwork';
import { ErrorAlert, Loading } from '../components/Feedback';
import { useStudent } from '../student/context';

export function ClassListPage() {
  const classes = useClasses();
  const placeHold = usePlaceHold();
  const { studentId } = useStudent();
  const navigate = useNavigate();
  const [applyingTo, setApplyingTo] = useState<string | null>(null);

  const apply = (schoolClass: SchoolClass) => {
    if (!studentId) return;
    setApplyingTo(schoolClass.id);
    placeHold.mutate(
      { classId: schoolClass.id, studentId },
      {
        onSuccess: (hold) => hold && navigate(`/holds/${hold.holdId}`),
        onSettled: () => setApplyingTo(null),
      },
    );
  };

  return (
    <section className="stack">
      <div className="notice">
        <div className="stack">
          <h1>Apply for a school seat</h1>
          <p className="lead">First come, first served. Pay the fee before your hold runs out.</p>
        </div>
        <SchoolScene />
      </div>

      {!studentId && <p className="alert alert-info">Enter the birth registration number above to apply.</p>}
      {placeHold.isError && <ErrorAlert error={placeHold.error} />}

      {classes.isPending && <Loading />}
      {/* Keep showing the last good data if a background refresh fails. */}
      {classes.isError && <ErrorAlert error={classes.error} onRetry={() => void classes.refetch()} />}

      {classes.data?.length === 0 && (
        <p className="muted">
          No classes yet. <Link to="/admin/classes/new">Create a class</Link>.
        </p>
      )}

      {classes.data && classes.data.length > 0 && (
        <div className="stack" style={{ gap: 14 }}>
          <div className="ledger-head">
            <h2>Classes and seats</h2>
            <div className="legend" aria-hidden="true">
              <span><i className="swatch-confirmed" />Confirmed</span>
              <span><i className="swatch-held" />Held, fee due</span>
              <span><i className="swatch-free" />Free</span>
            </div>
          </div>
          <div className="table-wrap">
            <table className="class-table">
              <thead>
                <tr>
                  <th scope="col">Class</th>
                  <th scope="col" className="num">Total</th>
                  <th scope="col" className="num">Held</th>
                  <th scope="col" className="num">Confirmed</th>
                  <th scope="col" className="num">Available</th>
                  <th scope="col"><span className="sr-only">Action</span></th>
                </tr>
              </thead>
              <tbody>
                {classes.data.map((c) => (
                  <tr key={c.id}>
                    <th scope="row" data-label="Class">
                      {c.name}
                      <SeatMeter schoolClass={c} />
                    </th>
                    <td className="num" data-label="Total">{c.totalSeats}</td>
                    <td className="num" data-label="Held">{c.heldSeats}</td>
                    <td className="num" data-label="Confirmed">{c.confirmedSeats}</td>
                    <td className="num free" data-label="Available">
                      <strong className={c.availableSeats === 0 ? 'text-danger' : 'text-success'}>{c.availableSeats}</strong>
                    </td>
                    <td>
                      <button
                        type="button"
                        className="btn btn-primary"
                        disabled={!studentId || c.availableSeats === 0 || applyingTo !== null}
                        onClick={() => apply(c)}
                      >
                        {applyingTo === c.id ? 'Applying...' : c.availableSeats === 0 ? 'Full' : 'Apply'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}

function SeatMeter({ schoolClass: c }: { schoolClass: SchoolClass }) {
  const pct = (n: number) => `${c.totalSeats > 0 ? (n / c.totalSeats) * 100 : 0}%`;
  return (
    <div className="seat-meter" aria-hidden="true">
      <span className="m-confirmed" style={{ width: pct(c.confirmedSeats) }} />
      <span className="m-held" style={{ width: pct(c.heldSeats) }} />
    </div>
  );
}
