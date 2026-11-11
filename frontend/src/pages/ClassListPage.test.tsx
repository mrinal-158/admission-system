import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { SchoolClass } from '../api/types';
import { jsonResponse, renderWithProviders } from '../test/render';
import { ClassListPage } from './ClassListPage';

const classes: SchoolClass[] = [
  { id: 'c_1', name: 'Class 6 - Morning Shift', totalSeats: 120, heldSeats: 15, confirmedSeats: 80, availableSeats: 25 },
  { id: 'c_2', name: 'Class 6 - Day Shift', totalSeats: 60, heldSeats: 10, confirmedSeats: 50, availableSeats: 0 },
];

function mockApi(onHold: () => Response) {
  const fetchMock = vi.fn(async (url: string, init?: RequestInit) =>
    init?.method === 'POST' && url.endsWith('/holds') ? onHold() : jsonResponse(classes));
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

describe('ClassListPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('lists seat counts and disables apply for a full class', async () => {
    mockApi(() => jsonResponse({}, 500));
    renderWithProviders(<ClassListPage />, { studentId: '20152691234567890' });

    expect(await screen.findByText('Class 6 - Morning Shift')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Apply' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Full' })).toBeDisabled();
  });

  it('asks for the student id before allowing applications', async () => {
    mockApi(() => jsonResponse({}, 500));
    renderWithProviders(<ClassListPage />);

    expect(screen.getByText(/birth registration number above to apply/)).toBeInTheDocument();
    expect(await screen.findByRole('button', { name: 'Apply' })).toBeDisabled();
  });

  it('sends the student id header and explains a 409 in plain language', async () => {
    const fetchMock = mockApi(() =>
      jsonResponse({ status: 409, code: 'NO_SEATS_AVAILABLE' }, 409, 'application/problem+json'));
    renderWithProviders(<ClassListPage />, { studentId: '20152691234567890' });

    await userEvent.click(await screen.findByRole('button', { name: 'Apply' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('no free seats');
    const [url, init] = fetchMock.mock.calls.find(([, i]) => i?.method === 'POST')!;
    expect(url).toMatch(/\/classes\/c_1\/holds$/);
    expect((init!.headers as Record<string, string>)['X-Student-Id']).toBe('20152691234567890');
  });

  it('shows a friendly message when the server is down', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('Failed to fetch'))));
    renderWithProviders(<ClassListPage />);

    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Cannot reach the server'), {
      timeout: 5_000,
    });
  });
});
