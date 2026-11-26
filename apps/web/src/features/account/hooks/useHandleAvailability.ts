// File : /team-samsara/apps/web/src/features/account/hooks/useHandleAvailability.ts
// Version : 1.0.0
// Latest commit: task/web-auth-onboarding-navbar
// Author : Gerrah
// Purpose : Tells whether a handle being typed is free. Waits for a pause in
// typing before asking the API, and ignores an answer that arrives after the
// handle has changed again. A handle that fails the local rules is not sent.

import { useEffect, useState } from "react";
import { checkHandle } from "../service/profileService";
import {
  normalizeHandle,
  validateHandle,
} from "../validation/profileValidation";

export type HandleAvailability =
  /** Nothing to report: empty, or not yet valid. */
  | "idle"
  | "checking"
  | "available"
  | "taken"
  /** The API refused the format, which the local rules did not catch. */
  | "invalid"
  /** The check itself could not be completed. */
  | "unknown";

const checkDelayMs = 400;

interface CheckedHandle {
  readonly handle: string;
  readonly availability: HandleAvailability;
}

async function askApi(handle: string): Promise<HandleAvailability> {
  const outcome = await checkHandle(handle);

  if (outcome.kind !== "answer") {
    return "unknown";
  }

  switch (outcome.value.status) {
    case "success":
      return "available";
    case "handleTaken":
      return "taken";
    case "invalidHandle":
      return "invalid";
    default:
      return "unknown";
  }
}

/**
 * @param handle What is typed in the field.
 * @param ownHandle The handle the member already holds, which is always theirs to keep.
 */
export function useHandleAvailability(
  handle: string,
  ownHandle: string | null,
): HandleAvailability {
  const normalized = normalizeHandle(handle);
  const isValid = validateHandle(normalized) === null;
  const isOwn = normalized === ownHandle;
  const needsCheck = isValid && !isOwn;

  const [checked, setChecked] = useState<CheckedHandle | null>(null);

  useEffect(() => {
    if (!needsCheck) {
      return;
    }

    let cancelled = false;

    const timer = setTimeout(() => {
      void askApi(normalized).then((availability) => {
        if (!cancelled) {
          setChecked({ handle: normalized, availability });
        }
      });
    }, checkDelayMs);

    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [needsCheck, normalized]);

  if (!isValid) {
    return "idle";
  }

  if (isOwn) {
    return "available";
  }

  return checked?.handle === normalized ? checked.availability : "checking";
}
