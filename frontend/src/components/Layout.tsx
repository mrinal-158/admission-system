import { Link, NavLink, Outlet } from 'react-router';
import { useStudent } from '../student/context';
import { STUDENT_ID_LENGTH } from '../student/studentId';
import { Crest, InkFilter } from './Artwork';

function StudentIdBox() {
  const { input, setInput, studentId } = useStudent();
  const showError = input.length > 0 && studentId === null;

  return (
    <div className={`student-box ${showError ? 'form-group-error' : ''}`}>
      <label htmlFor="student-id">Student's birth registration number</label>
      <div className="student-row">
        <input
          id="student-id"
          inputMode="numeric"
          autoComplete="off"
          placeholder="2015XXXXXXXXXXXXX"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          aria-invalid={showError}
          aria-describedby="student-id-status"
        />
        <span id="student-id-status" className={`id-status ${showError ? 'bad' : studentId ? 'ok' : ''}`}>
          {showError
            ? `${input.length} of ${STUDENT_ID_LENGTH} digits`
            : studentId
              ? '✓'
              : ''}
        </span>
      </div>
    </div>
  );
}

export function Layout() {
  return (
    <>
      <InkFilter />
      <header className="app-header">
        <div className="container header-inner">
          <Link to="/" className="brand">
            <Crest />
            <span className="brand-name">School Admission</span>
          </Link>
          <nav aria-label="Main">
            <NavLink to="/" end>Classes</NavLink>
            <NavLink to="/me">My applications</NavLink>
            <NavLink to="/admin/classes/new">Create class</NavLink>
          </nav>
        </div>
      </header>
      <div className="applicant-band">
        <div className="container">
          <StudentIdBox />
        </div>
      </div>
      <main className="container">
        <Outlet />
      </main>
    </>
  );
}
