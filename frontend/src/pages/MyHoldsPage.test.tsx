import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Hold } from '../api/types';
import { jsonResponse, renderWithProviders } from '../test/render';
import { MyHoldsPage } from './MyHoldsPage';

const STUDENT = '20152691234567890';

function hold(overrides: Partial<Hold>): Hold {
  return {
    holdId: 'h_1',
    classId: 'c_1',
    className: 'Class 6 - Morning Shift',
    studentId: STUDENT,
    status: 'HELD',
    expiresAt: new Date(Date.now() + 300_000).toISOString(),
    createdAt: new Date().toISOString(),
    confirmedAt: null,
    ...overrides,
  };
}

describe('MyHoldsPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it("lists the student's applications with their status", async () => {
    const fetchMock = vi.fn(async () => jsonResponse([
      hold({ holdId: 'h_1' }),
      hold({ holdId: 'h_2', className: 'Class 7 - Day Shift', status: 'CONFIRMED' }),
    ]));
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<MyHoldsPage />, { studentId: STUDENT });

    expect(await screen.findByText('Class 7 - Day Shift')).toBeInTheDocument();
    expect(screen.getByText('Seat held, fee due')).toHaveAttribute('data-status', 'HELD');
    expect(screen.getByText('Admission confirmed')).toHaveAttribute('data-status', 'CONFIRMED');
    expect(screen.getByText(/^\d\d:\d\d left$/)).toBeInTheDocument(); // countdown on the live one
    const [, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect((init.headers as Record<string, string>)['X-Student-Id']).toBe(STUDENT);
  });

  it('asks for the student id instead of calling the API', () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<MyHoldsPage />);

    expect(screen.getByText(/to see your applications/)).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
