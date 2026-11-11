declare global {
  interface Window {
    __APP_CONFIG__?: { apiBaseUrl?: string };
  }
}

/**
 * Backend URL, resolved in this order:
 * 1. window.__APP_CONFIG__.apiBaseUrl: written by the Docker entrypoint from API_BASE_URL at
 *    container start, so one image works against any backend without rebuilding;
 * 2. VITE_API_BASE_URL: build-time env, handy for `npm run dev`;
 * 3. http://localhost:8080: the brief's default.
 */
export const apiBaseUrl: string = (
  window.__APP_CONFIG__?.apiBaseUrl ||
  import.meta.env.VITE_API_BASE_URL ||
  'http://localhost:8080'
).replace(/\/+$/, '');

/** How often seat counts refresh without a page reload. */
export const CLASS_REFRESH_MS = 3_000;
/** How often an open application re-syncs its status with the server. */
export const HOLD_REFRESH_MS = 5_000;
