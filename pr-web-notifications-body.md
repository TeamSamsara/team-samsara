## Summary

The web side of notifications: a bell in the navbar with an unread badge, a dropdown panel with the member's notifications, and an amber banner for an unanswered new sign-in. Built on the notifications API from #62 and matched to the Figma "04 · Account security" frames.

## What it does

- **Bell and badge:** green slanted bell with an amber count badge ("9+" from ten). It polls the unread count every 15 s while the tab is visible, and refreshes the list when the count changes.
- **Panel:** opens under the bell, lists notifications newest first with "load more", "mark as read" per row and "mark all as read". It only fetches the list while open.
- **Sign-in banner:** shown on every page while the newest new-sign-in notice is unread. "That was me" marks it read. "Secure my account" opens a modal that marks it read, ends all sessions and shows the signed-out message.
- **Navbar without flicker:** a small local note (`teamsamara.authHint.v1`) lets the navbar draw the member layout while the session is still loading. It is never used for access decisions.

## Motion

- Badge pops in with a halo ring, the digit rolls when the count changes, the bell swings on a new arrival.
- Panel fades and drops in from the bell and fades out; rows stagger in.
- A notice arriving while the panel is open grows in at the top with an amber flash.
- Marking read shrinks the amber square and dims the row.
- The banner grows from nothing and the page slides with it; it collapses when answered.
- `prefers-reduced-motion` keeps only quick fades.
- Easing and durations live in `shared/styling/tokens/motion.css`.

## Shared changes

- `DropdownPanel`: a `keepMounted` panel is now `inert` instead of `hidden`, so it can animate out. No other dropdown uses `keepMounted`.
- `SiteLayout`: always renders the banner and animates the page padding with it.
- New tokens: `--color-alert`, `--color-text-on-alert`, motion tokens.

## Testing

No web test runner exists yet, so this was checked with `npm run lint`, `npm run build` and manual runs against the emulator: arrival without refresh, mark read, mark all, secure account, refresh with unread notices, reduced motion.

## Known follow-ups (separate PRs)

- Clearing notifications (delete one, clear all read, cap per member).
- Deleting an account should also remove the member's notifications.
