## What

The API backlog for accounts and notifications, plus the web changes that make it usable. One branch, one commit.

## API

- **Handle cooldown.** A member can change their handle once every 30 days. Picking a handle during onboarding costs nothing, and the first rename after onboarding is free. The claim rules now live in one place (`HandleClaimRules`) shared by the Firestore store and the in-memory fake, and `ProfileService` is split into smaller pieces (`HandleFormat`, `HandleCandidates`, `ProfileText`).
- **Email change.** The new address is checked up front: an invalid, unchanged or already-used address is refused before anything is sent, and a refusal starts no cooldown. Logs never contain codes or full addresses (masked). Requests are limited per member (10 per hour by default) on top of the existing per-IP limit, via an endpoint filter because the rate limiter runs before authentication.
- **Notification clearing.** `DELETE /notifications/{id}` (204, 404, or 409 for an unanswered sign-in) and `DELETE /notifications/read` (clears the read ones). Each member's inbox is capped (`MaxPerMember`, 50 by default, 10 to 500): after a new notification is stored, the oldest read ones go first, then the oldest unread ones. An unanswered sign-in notice and the notification just stored are never removed, and a failed trim is only logged. Deletes are decided inside a Firestore transaction.
- **Auth service split.** `AuthenticationService` now depends on `INewLoginNotifier` (email alert and inbox notice, each failing on its own without breaking the login) and `AccountRecovery` (the recovery-window and purge rules).
- The Firestore inbox store was refactored into small helpers. The notification key `security.new-login` has one shared definition.

## Web

- Email change handles the two new statuses (address already used, address unchanged), and both code steps get a help line about spam and spelling plus a "use a different address" action that returns to step 1 with the address kept.
- Notification panel: icon buttons to remove a notification and to mark all as read or clear all read, with an error toast and rollback on failure. An unanswered sign-in has no remove button. The cache code moved into `useNotificationCache`.

## Testing

- `dotnet test apps/api`: 1,012 passed, 0 failed.
- `tsc` and `eslint` are clean on `apps/web`.
- Tried by hand: email change in the running API, and the notification bell and panel against the Firestore emulator.

## Not in this PR

Figma sync, French strings, small cleanups, README note about polling, public profile and socials.
