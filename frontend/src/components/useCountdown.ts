import { useEffect, useState } from 'react';
import { serverNow } from '../api/client';

/** Milliseconds left until `deadline` (ISO string), ticking every second; never negative. */
export function useCountdown(deadline: string | null): number {
  const target = deadline ? Date.parse(deadline) : NaN;
  const [now, setNow] = useState(serverNow);

  useEffect(() => {
    if (Number.isNaN(target)) return;
    const timer = setInterval(() => {
      const current = serverNow();
      setNow(current);
      if (current >= target) clearInterval(timer);
    }, 1_000);
    return () => clearInterval(timer);
  }, [target]);

  return Number.isNaN(target) ? 0 : Math.max(0, target - now);
}

export function formatDuration(ms: number): string {
  const totalSeconds = Math.ceil(ms / 1_000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}
