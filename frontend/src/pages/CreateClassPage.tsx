import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { ApiError } from '../api/client';
import { useClasses, useCreateClass } from '../api/hooks';
import type { SchoolClass } from '../api/types';
import { ErrorAlert, SuccessAlert } from '../components/Feedback';

const MAX_NAME = 100;
const MAX_SEATS = 10_000;

function validate(name: string, seats: string): string | null {
  if (!name.trim()) return 'Enter a class name.';
  if (name.trim().length > MAX_NAME) return `The name can be at most ${MAX_NAME} characters.`;
  const n = Number(seats);
  if (!Number.isInteger(n) || n < 1 || n > MAX_SEATS) return `Seats must be a whole number from 1 to ${MAX_SEATS}.`;
  return null;
}

export function CreateClassPage() {
  const createClass = useCreateClass();
  const [name, setName] = useState('');
  const [seats, setSeats] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [created, setCreated] = useState<SchoolClass | null>(null);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    const problem = validate(name, seats);
    setFormError(problem);
    setCreated(null);
    if (problem) return;

    createClass.mutate(
      { name: name.trim(), totalSeats: Number(seats) },
      {
        onSuccess: (c) => {
          setCreated(c);
          setName('');
          setSeats('');
        },
      },
    );
  };

  // Server-side field errors (should be rare: the form validates the same rules first).
  const serverFieldErrors =
    createClass.error instanceof ApiError ? Object.values(createClass.error.problem?.errors ?? {}).flat() : [];

  return (
    <section className="office">
      <div className="stack">
        <h1>Create a class</h1>

        <form onSubmit={submit} className="form" noValidate>
          <div className="form-group">
            <label htmlFor="class-name">Class name</label>
            <span className="hint" id="class-name-hint">For example, Class 6 - Morning Shift</span>
            <input
              aria-describedby="class-name-hint"
              id="class-name"
              value={name}
              maxLength={MAX_NAME}
              onChange={(e) => setName(e.target.value)}
            />
          </div>

          <div className="form-group">
            <label htmlFor="class-seats">Total seats</label>
            <input
              className="input-short"
              id="class-seats"
              type="number"
              inputMode="numeric"
              min={1}
              max={MAX_SEATS}
              value={seats}
              onChange={(e) => setSeats(e.target.value)}
            />
          </div>

          {formError && <p className="field-error" role="alert">{formError}</p>}
          {createClass.isError && <ErrorAlert error={createClass.error} />}
          {serverFieldErrors.length > 0 && (
            <ul className="field-error">{serverFieldErrors.map((m) => <li key={m}>{m}</li>)}</ul>
          )}

          <div className="actions">
            <button type="submit" className="btn btn-primary" disabled={createClass.isPending}>
              {createClass.isPending ? 'Creating...' : 'Create class'}
            </button>
          </div>
        </form>

        {created && (
          <SuccessAlert>
            "{created.name}" created ({created.totalSeats} seats). <Link to="/">See it in the class list</Link>
          </SuccessAlert>
        )}
      </div>
      <ExistingClasses />
    </section>
  );
}

/** What the office has already set up, so the same shift isn't created twice. */
function ExistingClasses() {
  const classes = useClasses();
  return (
    <aside className="register" aria-labelledby="existing-classes">
      <h2 id="existing-classes">Existing classes</h2>
      {classes.data && classes.data.length === 0 && <p className="muted">None yet.</p>}
      {classes.data && classes.data.length > 0 && (
        <ol>
          {classes.data.map((c) => (
            <li key={c.id}>
              <span>{c.name}</span>
              <span className="seats">{c.totalSeats} seats</span>
            </li>
          ))}
        </ol>
      )}
    </aside>
  );
}
