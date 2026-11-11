import { describe, expect, it } from 'vitest';
import { ApiError } from './client';
import { errorMessage } from './errors';

describe('errorMessage', () => {
  it('maps API error codes to a plain-language message', () => {
    const error = new ApiError('No seats', 409, { code: 'NO_SEATS_AVAILABLE' });
    expect(errorMessage(error)).toContain('no free seats');
  });

  it('explains an unreachable server instead of showing a raw error', () => {
    expect(errorMessage(new ApiError('Failed to fetch', null))).toContain('Cannot reach the server');
  });

  it('has a generic fallback for 5xx and unknown errors', () => {
    expect(errorMessage(new ApiError('boom', 503))).toContain('The server had a problem');
    expect(errorMessage(new Error('?'))).toContain('Something unexpected');
  });
});
