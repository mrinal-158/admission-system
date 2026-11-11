import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './client';

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // 4xx answers will not change on retry; network blips and 5xx might.
        retry: (failureCount, error) =>
          !(error instanceof ApiError && error.status !== null && error.status < 500) && failureCount < 2,
      },
      mutations: { retry: false },
    },
  });
}
