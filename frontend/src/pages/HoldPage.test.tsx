import { act, screen } from '@testing-library/react';
import { Route, Routes } from 'react-router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Hold } from '../api/types';
import { jsonResponse, renderWithProviders } from '../test/render';
import { HoldDetails, HoldPage } from './HoldPage';

const STUDENT = '20152691234567890';
const NOW = new Date('2026-11-01T04:00:00Z');

function hold(overrides: Partial<Hold> = {}): Hold {
  return {
    holdId: 'h_1',
    classId: 'c_1',
    className: 'Class 6 - Morning Shift',
    studentId: STUDENT,
    status: 'HELD',
    expiresAt: new Date(NOW.getTime() + 3_000).toISOString(),
    createdAt: NOW.toISOString(),
    confirmedAt: null,
    ...overrides,
  };
}

describe('HoldDetails', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
    // The expiry triggers a background refresh; keep it off the network.
    vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})));
  });
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('counts down, then shows EXPIRED and disables payment without a reload', () => {
    renderWithProviders(<HoldDetails hold={hold()} />, { studentId: STUDENT });

    expect(screen.getByText('00:03')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pay admission fee' })).toBeEnabled();

    act(() => vi.advanceTimersByTime(3_000));

    expect(screen.getByText('Expired')).toHaveAttribute('data-status', 'EXPIRED');
    expect(screen.getByRole('button', { name: 'Pay admission fee' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Cancel' })).toBeDisabled();
  });

  it('hides the actions from a different student', () => {
    renderWithProviders(<HoldDetails hold={hold()} />, { studentId: '20152691234567891' });

    expect(screen.queryByRole('button', { name: 'Pay admission fee' })).not.toBeInTheDocument();
    expect(screen.getByText(/enter this applicant's birth registration number/)).toBeInTheDocument();
  });

  it('shows a confirmed admission without a countdown', () => {
    renderWithProviders(<HoldDetails hold={hold({ status: 'CONFIRMED' })} />, { studentId: STUDENT });

    expect(screen.getByText('Admission confirmed')).toBeInTheDocument();
    expect(screen.queryByText(/^\d\d:\d\d$/)).not.toBeInTheDocument();
  });
});

describe('HoldPage', () => {
  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('warns the parent when the server goes down while the application is open', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW);
    const live = hold({ expiresAt: new Date(NOW.getTime() + 600_000).toISOString() });
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(jsonResponse(live))
      .mockRejectedValue(new TypeError('Failed to fetch'));
    vi.stubGlobal('fetch', fetchMock);

    renderWithProviders(
      <Routes><Route path="/holds/:holdId" element={<HoldPage />} /></Routes>,
      { studentId: STUDENT, route: '/holds/h_1' },
    );
    await act(() => vi.advanceTimersByTimeAsync(100));
    expect(screen.getByText('Class 6 - Morning Shift')).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    // Next poll fails, retries fail too: the cached details stay, with a warning above them.
    await act(() => vi.advanceTimersByTimeAsync(15_000));

    expect(screen.getByRole('alert')).toHaveTextContent('Cannot reach the server');
    expect(screen.getByText('Class 6 - Morning Shift')).toBeInTheDocument();
  });
});
