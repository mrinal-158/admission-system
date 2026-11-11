import { QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter } from 'react-router';
import { createQueryClient } from '../api/queryClient';
import { StudentContext } from '../student/context';

/** Renders UI with the app's providers and a fixed student identity. */
export function renderWithProviders(
  ui: ReactElement,
  { studentId = null as string | null, route = '/' } = {},
) {
  const queryClient = createQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <StudentContext value={{ input: studentId ?? '', setInput: () => {}, studentId }}>
        <MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>
      </StudentContext>
    </QueryClientProvider>,
  );
}

export function jsonResponse(body: unknown, status = 200, contentType = 'application/json') {
  return new Response(status === 204 ? null : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': contentType },
  });
}
