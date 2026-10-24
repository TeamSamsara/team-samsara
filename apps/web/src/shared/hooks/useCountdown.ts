// File : /team-samsara/apps/web/src/shared/hooks/useCountdown.ts
// Version : 1.0.0
// Latest commit: task/web-auth-codes-reset
// Author : Gerrah
// Purpose : A countdown in whole seconds, for waits such as the pause before a
// new code can be asked for. It measures against the clock, so a background
// tab or a slow frame never makes it drift.

import { useCallback, useEffect, useState } from "react";

const TICK_MILLISECONDS = 250;

export interface Countdown {
  /** Whole seconds still to wait; 0 when nothing is running. */
  secondsLeft: number;
  /** Starts, or restarts, the countdown from the given number of seconds. */
  start: (seconds: number) => void;
}

/** Pass the seconds to count from the first render, or leave out for none. */
export function useCountdown(initialSeconds = 0): Countdown {
  const [endsAt, setEndsAt] = useState<number | null>(() =>
    initialSeconds > 0 ? Date.now() + initialSeconds * 1000 : null,
  );
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (endsAt === null) {
      return;
    }
    const timer = window.setInterval(() => {
      const current = Date.now();
      setNow(current);
      if (current >= endsAt) {
        window.clearInterval(timer);
      }
    }, TICK_MILLISECONDS);
    return () => {
      window.clearInterval(timer);
    };
  }, [endsAt]);

  const start = useCallback((seconds: number) => {
    const current = Date.now();
    setNow(current);
    setEndsAt(seconds > 0 ? current + seconds * 1000 : null);
  }, []);

  const secondsLeft =
    endsAt === null ? 0 : Math.max(0, Math.ceil((endsAt - now) / 1000));

  return { secondsLeft, start };
}
