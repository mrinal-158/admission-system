import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { StudentContext, type StudentContextValue } from './context';
import { isValidStudentId, normalizeStudentId } from './studentId';

const STORAGE_KEY = 'school-admission.studentId';

function readStored(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? '';
  } catch {
    return ''; // storage blocked (private mode etc.): just start empty
  }
}

/**
 * The student identity typed at the top of the page. Persisted in localStorage so that
 * after a refresh or reopening the browser the parent sees their ongoing applications.
 */
export function StudentProvider({ children }: { children: ReactNode }) {
  const [input, setRawInput] = useState(readStored);

  const setInput = useCallback((raw: string) => {
    const value = normalizeStudentId(raw);
    setRawInput(value);
    try {
      localStorage.setItem(STORAGE_KEY, value);
    } catch {
      /* not fatal */
    }
  }, []);

  const value = useMemo<StudentContextValue>(
    () => ({ input, setInput, studentId: isValidStudentId(input) ? input : null }),
    [input, setInput],
  );

  return <StudentContext value={value}>{children}</StudentContext>;
}
