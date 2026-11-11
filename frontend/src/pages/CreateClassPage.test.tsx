import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { jsonResponse, renderWithProviders } from '../test/render';
import { CreateClassPage } from './CreateClassPage';

describe('CreateClassPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  const posts = (fetchMock: ReturnType<typeof vi.fn>) =>
    fetchMock.mock.calls.filter(([, init]) => (init as RequestInit | undefined)?.method === 'POST');

  it('validates the form before calling the API', async () => {
    const fetchMock = vi.fn(async () => jsonResponse([]));
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<CreateClassPage />);

    await userEvent.type(screen.getByLabelText('Total seats'), '0');
    await userEvent.click(screen.getByRole('button', { name: 'Create class' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Enter a class name');

    await userEvent.type(screen.getByLabelText('Class name'), 'Class 6 - Morning Shift');
    await userEvent.click(screen.getByRole('button', { name: 'Create class' }));
    expect(screen.getByRole('alert')).toHaveTextContent('Seats must be');

    expect(posts(fetchMock)).toHaveLength(0);
  });

  it('creates the class with a trimmed name and numeric seat count', async () => {
    const fetchMock = vi.fn(async (_url: string, init?: RequestInit) =>
      init?.method === 'POST'
        ? jsonResponse({ id: 'c_1', name: 'Class 6 - Morning Shift', totalSeats: 120 }, 201)
        : jsonResponse([]));
    vi.stubGlobal('fetch', fetchMock);
    renderWithProviders(<CreateClassPage />);

    await userEvent.type(screen.getByLabelText('Class name'), '  Class 6 - Morning Shift ');
    await userEvent.type(screen.getByLabelText('Total seats'), '120');
    await userEvent.click(screen.getByRole('button', { name: 'Create class' }));

    expect(await screen.findByRole('status')).toHaveTextContent('"Class 6 - Morning Shift" created (120 seats)');
    const [url, init] = posts(fetchMock)[0] as unknown as [string, RequestInit];
    expect(url).toMatch(/\/classes$/);
    expect(JSON.parse(init.body as string)).toEqual({ name: 'Class 6 - Morning Shift', totalSeats: 120 });
    expect(screen.getByLabelText('Class name')).toHaveValue(''); // form reset for the next class
  });

  it('lists the classes that already exist', async () => {
    vi.stubGlobal('fetch', vi.fn(async () =>
      jsonResponse([{ id: 'c_1', name: 'Class 6 - Day Shift', totalSeats: 120, heldSeats: 0, confirmedSeats: 0, availableSeats: 120 }])));
    renderWithProviders(<CreateClassPage />);

    expect(await screen.findByText('Class 6 - Day Shift')).toBeInTheDocument();
  });
});
