# Team Samsara - Project plan

_Rebuilt on 2026-11-05 from the git history, the README and the Oct 4 status snapshot. The original
plan document was lost, so this is a reconstruction: anything marked **(confirm)** is an
assumption to check against what the team remembers. Keep this file current in the same pull
request that finishes a piece of work, so it never drifts again._

## How the work is organised

| Phase | Goal                                                                                   | State                 |
| ----- | -------------------------------------------------------------------------------------- | --------------------- |
| 1     | Backend API: modules, infrastructure, deployed to staging (the CMS app was scaffolded) | Done                  |
| 2     | Frontend shell: the base website and member accounts, on top of Phase 1                | In progress (see 2.x) |
| 3     | CMS: content managed in Payload, plus roles and admin tools (full RBAC) **(confirm)**  | Not started           |
| 4     | Shop                                                                                   | Not started           |
| 5     | Community features                                                                     | Not started           |

Phases 3 to 5 are the team's recollection of the lost plan and have no detailed breakdown yet.
Each one gets its own section here when it is scoped. The Oct 4 snapshot placed full RBAC and the
admin tools in Phase 3, so they are listed there until the team says otherwise.

Phase 2 was planned in nine stages. The numbering in the Oct 4 snapshot and the numbering used in
the web pull requests differ (the web work called account security "stage 6"), so this file
describes the work by what it delivers and lists the pull request that delivered it.

## Phase 1 - Foundation (done)

Core backend, CMS and infrastructure, deployed to staging. The no-magic-strings convention and the
new-module checklist are documented in this repository.

## Phase 2 - Base website

### 2.1 Backend baseline (done)

| Area            | What exists                                                                                                   | Pull requests         |
| --------------- | ------------------------------------------------------------------------------------------------------------- | --------------------- |
| Assets          | Upload, metadata in Firestore, files in Postgres, video routed to Firebase Storage                            | earlier work          |
| Identity        | Register, log in, single-use codes, password change and reset, email change, logout, account delete and purge | #17 to #40            |
| Profiles        | Name, bio, picture, banner, handle with a 30-day cooldown, members-only lookup by handle                      | #13, #65, in progress |
| Alerts and mail | Email sending, security alerts (new login, password changes), several recipients and attachments              | #16, #18, #47         |
| Notifications   | Send path, inbox, mark read, remove one, remove all read, inbox cap, erase on account purge                   | #43 to #46, #65       |
| Contact         | Public contact endpoint with per-category routing and attachments                                             | #48                   |
| Quality         | Over 1,000 automated tests, architecture tests, staging smoke-test scripts, API contract baseline             | #39, #42              |

### 2.2 Web foundation (done)

Environment configuration (#50), account API client, auth state and route guard (#51), shared icons
(#58), 404, 403 and 500 pages (#56), toasts and inline messages (#64), modal styles (#61).

### 2.3 Member journey on the web (done, to be re-checked in the browser at the end)

| Piece                                                         | Pull request                            |
| ------------------------------------------------------------- | --------------------------------------- |
| Home page and footer                                          | #50                                     |
| Register and log in                                           | #52                                     |
| Code verification and forgot password                         | #53                                     |
| Onboarding, post-login landing and account menu               | #55                                     |
| Edit profile (now folded into the profile page)               | #59                                     |
| Account security: password, email, delete, log out everywhere | #60                                     |
| Notification bell, panel and sign-in banner                   | #63                                     |
| Notification clearing, handle cooldown, email checks          | #65                                     |
| Cleanups, shared slanted button hover, navbar fade            | #67                                     |
| Members-only profile page with in-place editing               | In progress (`task/web-member-profile`) |

### 2.4 Still to build for Phase 2

1. **Commit and merge the profile page** (the branch above), then re-check it in the browser.
2. **Teams, About and Contact pages.** Their routes are reserved in `routePaths`, but no feature
   exists yet. The Contact API is done; the web form is not.
3. **Wire the real design into every remaining page** and finish the shared components the new
   pages need.
4. **Real assets and a polish pass** (images, copy, spacing, motion, mobile).
5. **Prove the whole site on staging** with the frontend pointed at it **(confirm how much was
   already done in #49 and #50)**.

## Parked on purpose

| Item                                                  | Why it waits                                                                                                                                             |
| ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| French strings                                        | Only the language switch exists. Add translations once the English pages are settled.                                                                    |
| Socials                                               | The profile shows a "coming soon" box. The API and linking flow come later.                                                                              |
| Figma sync                                            | Update the Figma file to match the code: badge sizes (18 px, 13 px digit), the sign-in and password rows of the notification frame, and the panel icons. |
| Mark unread, preferences, other notification channels | Not needed yet (README, Notifications planned work).                                                                                                     |

## Open technical items

From the README, kept here so they are not forgotten:

- **Check the inbox API against real Firestore** once the index is Ready on staging.
- **Before the first notification producer** reaches `dev`: deploy the TTL policy, run the batch
  delete against the emulator with more than 500 notifications, decide how a failed send is
  handled and its `DedupeKey`, and repeat for production at cutover.
- **Audit the backend before the frontend is finished**: the list in the README ("Audit before
  starting the frontend") covers retries on email sends, orphaned asset files and which calls
  belong on the queue.
- **Deferred items**: Firebase service account key rotation, the obsolete
  `GoogleCredential.FromJson` call, `FirebaseStorageService.GetSignedUrlAsync`, alerting and log
  retention, and trimming the verbose file-header comments.
- **Production**: the production Postgres database and its Render environment variables do not
  exist yet. Staging has the same asset routing; production is a separate rollout.
- **Video streaming**: adaptive bitrate was researched and deliberately deferred. One rendition
  with native range requests covers 3 to 5 videos and 100 to 1,000 visitors a month. Revisit if
  traffic or video count grows much beyond that. The write-up was never saved here.
- **Catalog, Content, Media and Orders** are empty module scaffolds, waiting for their phase.

## Decisions worth remembering

- Profiles are visible to **signed-in, verified members only**.
- Members **edit their profile in place** on their own profile page; there is no separate settings
  page. `/account/profile` redirects to it.
- The handle changes at most once every 30 days.
- Notification inbox: an unanswered sign-in notice cannot be cleared; each member keeps at most
  50 notifications by default.
- The slanted green button (send code, save changes) rests in a deeper green and lightens on hover
  with a light wave. No shadows or scaling.

## Working agreements

- `main` and `dev` are protected: changes arrive by pull request, with the `backend` and
  `frontend` checks green, squash-merged.
- Always give `gh pr checks` and `gh pr merge` the pull request **number**. Without it, `gh` picks
  the pull request of the branch you are on and can delete the wrong branch.
- No Claude attribution lines in commits or pull request descriptions.
- Do not commit pull request body files.
