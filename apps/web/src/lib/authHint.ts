// File : /team-samsara/apps/web/src/lib/authHint.ts
// Version : 1.0.0
// Latest commit: task/web-notifications
// Author : Gerrah
// Purpose : A small note kept in the browser about whether the last visit was
// a signed-in member and what their avatar looked like. The navbar draws from
// it while the session is still being checked, so it does not flash the guest
// buttons on a refresh. It only changes how the bar looks for a moment: access
// is always decided by the real account status, never by this note.

const storageKey = "teamsamara.authHint.v1";
const maxInitialLength = 2;

export interface AuthHint {
  readonly initial: string;
  readonly pictureAssetId: string | null;
}

const emptyHint: AuthHint = { initial: "", pictureAssetId: null };

function isTextOrNull(value: unknown): value is string | null {
  return value === null || typeof value === "string";
}

function parseHint(raw: string): AuthHint | null {
  const value: unknown = JSON.parse(raw);

  if (
    typeof value !== "object" ||
    value === null ||
    !("initial" in value) ||
    !("pictureAssetId" in value) ||
    typeof value.initial !== "string" ||
    !isTextOrNull(value.pictureAssetId)
  ) {
    return null;
  }

  return {
    initial: value.initial.slice(0, maxInitialLength),
    pictureAssetId: value.pictureAssetId,
  };
}

// Browser storage can be blocked or full, and the note is only a convenience,
// so every failure simply means "no note".
export function readAuthHint(): AuthHint | null {
  try {
    const raw = window.localStorage.getItem(storageKey);

    return raw === null ? null : parseHint(raw);
  } catch {
    return null;
  }
}

export function saveAuthHint(hint: AuthHint): void {
  try {
    window.localStorage.setItem(storageKey, JSON.stringify(hint));
  } catch {
    // Nothing to do: the navbar just waits for the real answer.
  }
}

// Saved when a member is confirmed, without overwriting a fuller note.
export function ensureAuthHint(): void {
  if (readAuthHint() === null) {
    saveAuthHint(emptyHint);
  }
}

export function clearAuthHint(): void {
  try {
    window.localStorage.removeItem(storageKey);
  } catch {
    // Nothing to do.
  }
}
