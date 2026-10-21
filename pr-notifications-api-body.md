## Summary

The member inbox API, and the first thing that sends into it: a notice when someone signs in from a device the account does not know.

### API

- **Inbox endpoints** (`/notifications`, verified members only, each call touches only the caller's own notifications):
  - `GET /notifications`: newest first, `?limit` (default 20, clamped to 50) and an opaque versioned `?cursor`; answers `{items, nextCursor}`. A malformed cursor answers 400.
  - `GET /notifications/unread-count`: answers `{"unreadCount": n}`.
  - `POST /notifications/{id}/read`: idempotent, runs in a transaction; an unknown, malformed or someone else's id answers 404.
  - `POST /notifications/read-all`: marks the caller's unread notifications read in batches of 500, up to 10,000 per call.
- **Storage:** the inbox store can now list, count unread, mark one and mark a batch as read. Expired notifications the TTL has not deleted yet are hidden from the list.
- **Sign-in notice:** when a sign-in from an unrecognized client sends its code, `AuthenticationService` also puts a `security.new-login` notice in the member's inbox (IP address and user agent). One notice per device per UTC day. A failure is logged and never blocks the sign-in.
- **Index:** composite index on `notifications` (`RecipientId` ascending, `CreatedAt` descending, `__name__` descending) in `firestore.indexes.json`.
- README: new "Notifications" endpoint section, the keys the API sends, and the updated deployment and planned-work notes.

## Testing

- `dotnet test apps/api`: 870 passed, 0 failed (110 new tests: cursor, ids, inbox service, endpoints, module registration, and the sign-in notice).
- The in-memory tests cannot prove the Firestore queries. Manual checks on Staging, after the index shows Ready:
  - [ ] `firebase deploy --only firestore --project staging` (read its prompt about live indexes before answering) and wait for the index to be Ready
  - [ ] Sign in from an unknown device: a `security.new-login` notice appears in the inbox, and a second attempt the same day adds none
  - [ ] `GET /notifications?limit=1`, then follow `nextCursor` to the end: no repeats, no gaps
  - [ ] `GET /notifications/unread-count` matches what the list shows
  - [ ] `POST /notifications/{id}/read`: the count drops by one, a second call still answers 204
  - [ ] `POST /notifications/read-all`: the count drops to 0
  - [ ] Another member's id answers 404

## Notes

- The index must be deployed and Ready on Staging before this is tested, and on production before it is promoted; without it the list answers an error.
- The web side (bell, list page, sign-in banner) comes in the next PR.
