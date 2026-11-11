import { createContext, useContext } from 'react';

export interface StudentContextValue {
  /** What the user typed (normalised). */
  input: string;
  setInput: (raw: string) => void;
  /** The input when it is a valid 17-digit number, otherwise null. */
  studentId: string | null;
}

export const StudentContext = createContext<StudentContextValue | null>(null);

export function useStudent(): StudentContextValue {
  const context = useContext(StudentContext);
  if (!context) throw new Error('useStudent must be used inside <StudentProvider>');
  return context;
}
