# Team Samsara

Production website for Team Samsara - React frontend, C# (.NET) modular monolith backend, Payload CMS, and Firebase infrastructure.

## Architecture

### Applications

- `apps/web` - React + Vite frontend (public site + CMS admin UI)
- `apps/api` - C# modular monolith backend
- `apps/cms` - Payload CMS (API-only, admin panel disabled), backed by PostgreSQL

### Infrastructure

- Firebase - Authentication, Storage, Hosting, and Firestore
- PostgreSQL - Payload CMS database

### Documentation

- `docs/architecture` - Architecture decisions, specifications, and planning documents
- `docs/new-module-checklist.md` - Steps for adding a new business module

## Local Development

### Prerequisites

- .NET SDK `9.0.314` - see `global.json`
- Node.js `22.22.3` - see `.nvmrc`
- PostgreSQL `17`
- Firebase CLI (`npm install -g firebase-tools`), logged in (`firebase login`)

### Setup

**1. Install dependencies**

```bash
npm install --prefix apps/web
npm install --prefix apps/cms
dotnet restore apps/api/TeamSamsara.sln
```

**2. PostgreSQL**

Create a local database for Payload:

```sql
CREATE DATABASE samsara_cms;
```

**3. Environment files**

`apps/api` needs no `.env` file for local development - `appsettings.Development.json` and the
`Development` launch profile in `launchSettings.json` already point at the emulator suite.

`apps/cms/.env` (copy from `apps/cms/.env.example`):

```
DATABASE_URL=postgres://postgres:<your-postgres-password>@127.0.0.1:5432/samsara_cms
PAYLOAD_SECRET=<generate a random string>
```

`apps/web/.env.development` (copy from `apps/web/.env.example`):

```
VITE_FIREBASE_API_KEY=<your local/dev Firebase web app config>
VITE_FIREBASE_AUTH_DOMAIN=<project-id>.firebaseapp.com
VITE_FIREBASE_PROJECT_ID=<project-id>
VITE_FIREBASE_APP_ID=<app-id>
VITE_API_BASE_URL=/api
VITE_FIREBASE_AUTH_EMULATOR_HOST=http://localhost:9099
```

**4. Firebase emulators**

First run downloads the emulator binaries:

```bash
firebase emulators:start --project=demo-samsara-dev
```

Emulator ports: Auth `9099`, Firestore `8080`, Storage `9199`, Hosting `5000`, Functions `5001`,
Realtime Database `9000`.

### Startup order

Each of the following runs in its own terminal. Start the emulators and PostgreSQL first - the
other three depend on them being reachable, though nothing enforces this at process-start time,
so a wrong order fails at request time rather than immediately.

1. PostgreSQL (usually already running as a Windows service)
2. `firebase emulators:start --project=demo-samsara-dev`
3. `dotnet run --project apps/api/src/TeamSamsara.Api`
4. `npm run dev --prefix apps/cms`
5. `npm run dev --prefix apps/web`

Confirm the API is up: `http://localhost:5055/health/ready` should return `Healthy`. Confirm the
frontend can reach it: `http://localhost:5173/ping` should render `pong`.

### Bootstrap

A `scripts/bootstrap.ps1` / `.sh` script may be added once the manual setup process is stable and documented.

### Staging smoke tests

Two PowerShell scripts in `scripts/` check the live Staging API (`teamsamsara-api-staging.onrender.com`)
end to end, with the real Firebase, Firestore, Storage and email behind it. Unit tests cannot cover
that. Each step prints `PASS` or `FAIL`, and the script ends with a count.

| Script                              | Account it needs                                            | Covers                                                                                                                                       |
| ----------------------------------- | ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `scripts/staging-profile-smoke.ps1` | A verified member (it restores the name and bio afterwards) | Reading and editing the profile, picture and banner upload, replace and delete, size and type limits, and the guest and anonymous refusals   |
| `scripts/staging-account-smoke.ps1` | None: it creates its own throwaway account                  | Registration, `logout/all`, account deletion with its immediate block and revoked sessions, the deletion notice, and restoring by signing in |

Run either one from the repository root:

```powershell
.\scripts\staging-profile-smoke.ps1
.\scripts\staging-account-smoke.ps1
```

Both read the Firebase web API key from `apps/web/.env.staging`; `-Api` and `-EnvFile` override the API address and
that file. If PowerShell blocks the script, run
`Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` in the same window first.

- **The account script deletes the account it creates.** Use a throwaway mailbox that really receives mail,
  such as a plus-address (`you+smoke1@example.com`), and a new one every run. It asks for each emailed code in turn
  (registration, login if asked, deletion, a second deletion) and for whether the deletion notice arrived.
  Check the spam folder too.
- **Passwords never touch a file.** The profile script prompts for the member's password. The account script
  generates a random one and does not print it.
- **Never paste a token, password or emailed code into a chat or an issue.**

**Testing the purge by hand.** The account script leaves its account deleted, and prints that account's user id.
The purge needs an account deleted more than `RecoveryWindowDays` (30) ago, so:

1. In the Staging Firestore, open `users/<that id>` and set `DeletedAt` to a date more than 30 days in the past.
   Keep it a timestamp, and check the date is in the past.
2. Restart the Staging service on Render once, and wait a minute. The purge runs at startup.
3. The logs show `Account purge finished: 1 purged, 0 skipped`. `users/<id>` and `profiles/<id>` are gone, and so
   is the Firebase Authentication user.

## API Endpoints

This is the human summary. The machine-readable contract is the OpenAPI document at
`/openapi/v1.json`, which is also what `npm run generate-types` reads.

### Conventions

- **Authentication:** a Firebase ID token in `Authorization: Bearer <token>`.
- **JSON:** camelCase property names, and enums as camelCase strings (`"accessLevel": "member"`).
- **Codes are strings:** a verification code is sent as a JSON string (`"code": "123456"`), never a
  number - a number fails binding and answers `400` with an empty body.
- **Outcome in the body:** the flow endpoints answer with a `status` in the body as well as an HTTP
  status code, so a client reacts to the exact reason rather than to the code alone.

### Response contract

Every failure reaches the client in one of these shapes, so a frontend can handle them in order:

1. **Flow outcome:** an HTTP status plus a `status` string in the body (and, when relevant,
   `codeLength`, `retryAfterSeconds` or `profile`). The `status` is the precise reason; the HTTP
   code is derived from it in one place (`IdentityStatusCodes`).
2. **Authentication and access:** `401` with an empty body for an unknown caller, and `403` with
   an `authState` body on verified-member endpoints (see below). Login checks answer `403` with a
   `status` body instead.
3. **Rate limiting:** `429` with an empty body and a `Retry-After` header, or `429` with
   `cooldownActive` in the body when a code was requested too soon.
4. **Unexpected failures:** `application/problem+json`, where `title` is a snake_case code and
   `detail` is an English message for developers, not for display.
5. **Malformed requests:** `400` with an empty body, for example a code sent as a number.

The API never sends display text. The frontend decides what the member reads, from the `status`
first, then the problem `title`, then the HTTP code alone. Browsers can read the `Retry-After` and
`X-Correlation-Id` headers because the CORS policy exposes them.

### Who can call what

| Access          | Meaning                                                                |
| --------------- | ---------------------------------------------------------------------- |
| Public          | No token needed                                                        |
| API key         | `X-Api-Key` header, for CMS pipelines                                  |
| Signed in       | A valid token, even if registration or the sign-in is not finished yet |
| Verified member | A valid token whose sign-in is verified                                |

A caller is always in one of four states, and `401` versus `403` tells them apart:

| `authState`              | Who they are                                                                                    | On a verified-member endpoint |
| ------------------------ | ----------------------------------------------------------------------------------------------- | ----------------------------- |
| `anonymous`              | No token, or an invalid one - we do not know who they are                                       | `401`                         |
| `registrationIncomplete` | Known account, email never confirmed                                                            | `403`                         |
| `verificationRequired`   | Known account, but this sign-in is not verified (for example a new-device challenge is pending) | `403`                         |
| `member`                 | Known account, verified sign-in                                                                 | allowed                       |

`401` means _we do not know who you are_. `403` means _we know who you are, but you cannot do this
yet_, and its body says why: `{"authState": "verificationRequired"}`. `GET /account/me` reports the
same field, so a client can route the person to the right screen (confirm the registration code,
or the new-device code) instead of guessing. An admin is reported as `member` here, since the field
only describes whether the sign-in is verified; `accessLevel` carries `member` or `admin`.

### Account (`/account`) - signed in

| Method | Path                        | Does                                                                  | Answers                                     |
| ------ | --------------------------- | --------------------------------------------------------------------- | ------------------------------------------- |
| `POST` | `/account/register`         | Records the account as a Guest and emails the first confirmation code | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/register/resend`  | Emails a fresh confirmation code                                      | same                                        |
| `POST` | `/account/register/confirm` | Checks `{"code"}` and completes registration                          | `status`                                    |
| `POST` | `/account/login/check`      | Verifies a recognized sign-in, or emails the new-device code          | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/login/confirm`    | Checks `{"code"}` and verifies the sign-in                            | `status`                                    |
| `POST` | `/account/login/resend`     | Emails a fresh new-device code                                        | `status`, `codeLength`, `retryAfterSeconds` |
| `GET`  | `/account/me`               | Who the API considers the caller to be                                | `userId`, `accessLevel`, `authState`        |

HTTP status per outcome:

| Outcome (`status`)                              | HTTP  |
| ----------------------------------------------- | ----- |
| `success`, `authenticated`, `challengeRequired` | `200` |
| `invalidCode`, `codeExpired`, `noPendingCode`   | `400` |
| `registrationIncomplete`                        | `403` |
| `accountNotFound`                               | `404` |
| `alreadyRegistered`                             | `409` |
| `cooldownActive`, `tooManyAttempts`             | `429` |

### Profile (`/account/profile`) - verified member

A member can only reach their own profile. Every call answers
`{"status": "...", "profile": {...}}`, with `profile` set only on success, so the client never needs
a second request after an edit.

| Method   | Path                       | Does                                                       |
| -------- | -------------------------- | ---------------------------------------------------------- |
| `GET`    | `/account/profile`         | Returns the profile                                        |
| `PUT`    | `/account/profile`         | Replaces `{"displayName", "bio"}`; a blank `bio` clears it |
| `PUT`    | `/account/profile/picture` | Sets the profile picture (multipart, field `file`)         |
| `DELETE` | `/account/profile/picture` | Removes the profile picture                                |
| `PUT`    | `/account/profile/banner`  | Sets the banner (multipart, field `file`)                  |
| `DELETE` | `/account/profile/banner`  | Removes the banner                                         |

The profile holds `displayName`, `bio`, `profilePictureAssetId` and `bannerAssetId`. Images are
asset ids - the client builds the address from `GET /assets/{id}/file`. It also carries
`handleChangeableAt`: the moment the handle can be changed again, or `null` when it can be
changed now.

Rules (the limits are configurable under `Identity:Profile` and `MemberImages`):

| Rule                      | Default                                                                     | Refused with                 |
| ------------------------- | --------------------------------------------------------------------------- | ---------------------------- |
| Display name              | 1-50 visible characters after trimming, no control characters               | `400` `invalidDisplayName`   |
| Bio                       | at most 300 visible characters, line breaks allowed                         | `400` `invalidBio`           |
| Image format              | PNG, JPEG or WebP, checked on the file's real bytes, not its `Content-Type` | `415` `imageUnsupportedType` |
| Picture size              | at most 2 MB                                                                | `413` `imageTooLarge`        |
| Banner size               | at most 5 MB                                                                | `413` `imageTooLarge`        |
| No profile for the member | -                                                                           | `404` `profileNotFound`      |

Text is stored exactly as typed. It is not HTML-cleaned, so the frontend must always render it as
text, never as raw HTML. Replacing an image deletes the one it replaces.

### Password (`/account/password`) - verified member

Changing a password takes two calls. The new password travels with the code, so the server never
stores it between steps.

| Method | Path                             | Does                                                                                        | Answers                                     |
| ------ | -------------------------------- | ------------------------------------------------------------------------------------------- | ------------------------------------------- |
| `POST` | `/account/password/request-code` | Checks `{"newPassword"}` against the rules and emails a confirmation code                   | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/password/change`       | Checks `{"newPassword", "code"}`, replaces the password and signs the member out everywhere | `status`                                    |

The password must be 8-128 characters (`Identity:Password`). An unacceptable password is refused
with `invalidPassword` before the code is checked, so it does not use the code up. A change ends
every session: refresh tokens are revoked, verified sign-ins are forgotten, and the member gets a
password-changed email.

### Password reset (`/account/password/reset`) - signed out

For a member who forgot their password. The three calls run in order, and none needs a token.

| Method | Path                              | Does                                                                     | Answers                                     |
| ------ | --------------------------------- | ------------------------------------------------------------------------ | ------------------------------------------- |
| `POST` | `/account/password/reset/request` | Emails a reset code to `{"email"}` if it belongs to a member             | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/password/reset/verify`  | Checks `{"email", "code"}` and returns a one-time reset token            | `status`, `resetToken`                      |
| `POST` | `/account/password/reset/confirm` | Sets `{"resetToken", "newPassword"}` and signs the member out everywhere | `status`                                    |

- **Same answer for every address:** `request` answers identically whether or not the email has an
  account. The lookup and the email happen in the background, so the response time reveals nothing
  either.
- **Reset token:** single use, valid for 10 minutes (`Identity:Password:ResetTokenMinutes`). A weak
  password does not use it up, so the member can retry.
- **Rate limit:** every call is limited per client IP (default 10 per 60 seconds, `RateLimit`
  section) and answers `429` with a `Retry-After` header when exceeded.
- **Session end:** a successful reset ends every session and emails a password-changed notice.

Password outcomes (change and reset) map to HTTP like this:

| Outcome (`status`)                                                                    | HTTP  |
| ------------------------------------------------------------------------------------- | ----- |
| `success`                                                                             | `200` |
| `invalidPassword`, `invalidCode`, `codeExpired`, `noPendingCode`, `invalidResetToken` | `400` |
| `accountNotFound`                                                                     | `404` |
| `cooldownActive`, `tooManyAttempts`                                                   | `429` |

### Email change (`/account/email`) - verified member

Changing the sign-in address needs proof of both mailboxes, so a stolen session alone cannot take
over the account.

| Method | Path                     | Does                                                                                    | Answers                                     |
| ------ | ------------------------ | --------------------------------------------------------------------------------------- | ------------------------------------------- |
| `POST` | `/account/email/request` | Takes `{"newEmail"}`, emails one code to the current address and one to the new address | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/email/confirm` | Checks `{"oldCode"?, "newCode"?}` and, once both are accepted, switches the address       | `status`                                    |

1. `request` saves the pending change and queues two 8-digit codes: one to the **current** address
   and one to the **new** address. Codes expire after 10 minutes. Asking again within 60 seconds
   answers `cooldownActive`.
2. `confirm` takes either code or both; leave a code out to send it later. A right code is
   remembered, and while the other is missing the answer is `codeAccepted` (`200`). After a typo
   the client only has to resend the other one. Sending no code at all answers `invalidCode`.
3. Once both codes are accepted (`success`) the address changes, every session ends, and the **old** address gets an
   email-changed notice naming the new address in masked form (`j***@example.com`). The client must
   sign the member in again with the new address.

Account existence is never revealed:

- `request` answers identically whether the new address is free, taken or the member's own. A taken
  address receives no email; a decoy code is stored instead, and the check runs in the background.
- Every wrong, missing or expired code, and an address taken while the change was pending, answers
  `invalidCode`. The response never says which code failed.
- Five wrong guesses cancel the change (`tooManyAttempts`), a decoy code included.

| Outcome (`status`)                                | HTTP  |
| ------------------------------------------------- | ----- |
| `success`                                         | `200` |
| `invalidEmail`, `invalidCode`, `noPendingRequest` | `400` |
| `accountNotFound`                                 | `404` |
| `cooldownActive`, `tooManyAttempts`               | `429` |

### Account deletion (`/account/delete`) - verified member

A member deletes their own account in two steps. The account is not erased at once: it is blocked
immediately and can be restored for 30 days (`RecoveryWindowDays`), then a scheduled job removes
it for good.

| Method | Path                           | Does                                                       | Answers                                     |
| ------ | ------------------------------ | ---------------------------------------------------------- | ------------------------------------------- |
| `POST` | `/account/delete/request-code` | Emails a confirmation code to the member's current address | `status`, `codeLength`, `retryAfterSeconds` |
| `POST` | `/account/delete`              | Checks `{"code"}` and deletes the account                  | `status`                                    |

| Outcome (`status`)                            | HTTP  |
| --------------------------------------------- | ----- |
| `success`                                     | `200` |
| `invalidCode`, `codeExpired`, `noPendingCode` | `400` |
| `accountNotFound`                             | `404` |
| `cooldownActive`, `tooManyAttempts`           | `429` |

**Lifecycle**

1. **Confirm.** `request-code` emails a code; asking again too soon answers `cooldownActive`.
   `delete` accepts the code once. A second request with the same code, or a parallel request that
   lost the race, answers `noPendingCode`.
2. **Blocked at once.** One write marks the account deleted and forgets every verified sign-in.
   From that moment every token the member holds is answered as `403` `verificationRequired`, even
   though Firebase still considers the token valid until it expires. Nothing after this write is
   needed for the block to hold.
3. **Cleanup after the commit.** The cached verified sign-ins are dropped and the member's Firebase
   refresh tokens are revoked, so no device can get a new ID token. These steps are best effort:
   a failure is logged as an error and does not undo the deletion or fail the request, because the
   account is already blocked.
4. **Notice.** Once the deletion is committed, the `AccountDeleted` email goes to the member's
   address and states the recovery window. A failed send is logged and ignored. No email is sent
   for a deletion that did not commit.
5. **Recovery window.** For 30 days the member can restore the account by signing in again and
   completing the normal login check (the new-device code if the device is not recognized). There
   is no cancel endpoint. The restore is one write: it clears the deletion and records the verified
   sign-in together, so an account is never half restored. The deadline is strict: exactly 30 days
   after the deletion still restores, one instant later does not. Past the deadline, or while the
   purge is already working on the account, the sign-in answers `accountNotFound`.
6. **Purge.** After the window the scheduled job (see Background Services) removes the profile
   images, the profile, every verification code, password reset token and email-change request
   the account still has, what other modules hold about the member (the notification inbox, for
   example), the Firebase account, and finally the user record.

- **The client must call Firebase `signOut()` after a successful delete.** The API already refuses
  the old token as a member, but the Firebase session is still open on the device until the client
  ends it. Without `signOut()` the next request answers `403` instead of returning to the login
  screen.
- A restored account does not bring old tokens back: they keep their old sign-in time, so they stay
  unverified. The member signs in again.

### Logout (`/account/logout`) - verified member

| Method | Path                  | Does                                                                                   |
| ------ | --------------------- | -------------------------------------------------------------------------------------- |
| `POST` | `/account/logout`     | Ends this device's sign-in; the member's other devices stay signed in                  |
| `POST` | `/account/logout/all` | Revokes every refresh token and forgets every verified sign-in, like a password change |

Both answer `204 No Content` with no body, and repeating them is harmless.

- **The client must also call Firebase `signOut()`.** `logout` only stops this API accepting the
  sign-in as a member. The ID token itself stays valid with Firebase until it expires (up to an
  hour), and until then it is answered as `403` `verificationRequired`.
- **`logout/all` takes effect on every device:** refresh tokens are revoked, so no device can get a
  new ID token, and the verified sign-ins are forgotten at once. Each device has to sign in again.
- **Verified members only.** A caller still waiting on the new-device code, or on registration, has
  no verified sign-in to end and gets `403`. To abandon the device challenge the client calls
  Firebase `signOut()` and returns to the login screen; nothing was recorded on the server.

### Notifications (`/notifications`) - verified member

Every call reads or changes the caller's own notifications; nobody can see or touch another
member's.

| Method | Path                          | Does                                                                                    |
| ------ | ----------------------------- | --------------------------------------------------------------------------------------- |
| `GET`  | `/notifications`              | Lists the caller's notifications, newest first, with `?limit` and `?cursor`             |
| `GET`  | `/notifications/unread-count` | Answers `{"unreadCount": n}`                                                            |
| `POST` | `/notifications/{id}/read`    | Marks one notification read                                                             |
| `POST` | `/notifications/read-all`     | Marks every unread notification read, up to 10,000 in one call                          |

- **Items.** The list answers `{"items": [...], "nextCursor": "..." | null}`. Each item is
  `{id, key, data, createdAt, readAt}`; `readAt` is `null` while unread. The category is not sent;
  the frontend derives it from `key`, and turns `key` and `data` into text.
- **Order and paging.** Newest first by `createdAt`, then by id. `limit` defaults to 20 and is
  clamped to 1-50. `nextCursor` is opaque and versioned: send it back unchanged as `?cursor=` for
  the next page, and `null` means there is no next page. A malformed cursor answers `400` with an
  empty body, and so does a `limit` that is not a number. A page can hold fewer items than `limit`
  when some have expired but the TTL has not deleted them yet; keep following `nextCursor`.
- **Unread count.** Counts the caller's notifications that have no `readAt`.
- **Mark read.** Idempotent: a second call keeps the first `readAt` and answers `204` as well. The
  id must look like a notification id (32 or 64 lowercase hex characters); an unknown id, a
  malformed one and someone else's all answer `404` with an empty body, so ids cannot be probed.
- **Mark all read.** Answers `204` with no body, including when nothing was unread. It works in
  batches of 500 and stops after 20, so a member with more than 10,000 unread notifications calls
  it again.
- **Storage failures** are unexpected failures (`application/problem+json`), like everywhere else.

### Assets (`/assets`)

| Method   | Path                | Access  | Does                                                                  |
| -------- | ------------------- | ------- | --------------------------------------------------------------------- |
| `GET`    | `/assets`           | Public  | Lists assets, optionally `?type=Image\|Video\|File`                   |
| `GET`    | `/assets/{id}`      | Public  | Returns an asset's metadata                                           |
| `GET`    | `/assets/{id}/file` | Public  | Returns the file: streamed for images and files, redirected for video |
| `POST`   | `/assets`           | API key | Uploads a file (multipart: `file`, `alt`)                             |
| `DELETE` | `/assets/{id}`      | API key | Deletes an asset and its stored file                                  |

Member profile images are stored as assets too, so they appear in `GET /assets` and can be fetched
by id like any other image. They are meant to be shown publicly. This must change before any image
that has to stay private is stored as an asset.

Deleting an asset removes the stored file first and the metadata second, so a failed delete leaves
the asset in place and can simply be repeated. A file that is already gone counts as deleted, so a
repeat after a partial failure succeeds. If the file cannot be deleted the request answers an
error. A file can still be left without metadata only if the cleanup after a failed upload fails
twice in a row; that is logged.

### Contact (`/contact`) - public

The website's contact form. One call emails the team; nothing is stored.

| Method | Path       | Access                 | Does                                                |
| ------ | ---------- | ---------------------- | --------------------------------------------------- |
| `POST` | `/contact` | Public, limited per IP | Emails the form to the team for the chosen category |

The body is `multipart/form-data`:

| Field                   | Required | Rule                                                              |
| ----------------------- | -------- | ----------------------------------------------------------------- |
| `firstName`, `lastName` | yes      | Up to 100 characters each, no control characters                  |
| `email`                 | yes      | One plain address (no display name), up to 254 characters         |
| `phone`                 | no       | Up to 30 characters: digits, spaces and `+ - ( ) .`               |
| `category`              | yes      | `sponsorship`, `general-inquiries`, `media` or `customer-service` |
| `message`               | yes      | Up to 5000 characters; line breaks allowed                        |
| `files`                 | no       | Up to 5 files; repeat the field once per file                     |
| `website`               | no       | Hidden honeypot field: people leave it empty (see below)          |

- **Attachments:** PNG, JPEG, WebP, PDF, DOCX, PPTX and XLSX only. The extension and the file's
  first bytes must both match, so a renamed file is refused; anything else (zip, macro-enabled
  Office files, executables) is refused. At most 5 files, 5 MiB each and 8 MiB together; the whole
  request is capped at 10 MiB. File names are cleaned (any path and control characters removed,
  at most 100 characters). Files are attached to the email directly and never stored.
- **Routing:** each category goes to its own list of recipients (see Contact configuration).
  One email is sent to everyone on the list, with the visitor's address as `Reply-To`, so a staff
  member answers by replying. The sender is the shared `Email:FromAddress`.
- **Honeypot:** a filled `website` field answers `sent` exactly like a real submission but sends
  nothing, so a bot cannot tell it was dropped. The form must keep the field hidden and empty.
- **Rate limit:** at most 5 submissions per 10 minutes per IP address. More answers `429` with a
  `Retry-After` header and an empty body. A honeypot hit counts toward the limit.
- **No confirmation email:** the visitor is not emailed, so the form cannot be used to send mail
  to other people. The frontend shows its own thank-you screen on `sent`.
- **Failure keeps the form:** `deliveryFailed` (`503`) means the email could not be sent and
  nothing was recorded, so the frontend keeps the visitor's text and files and lets them retry.

Every answer except `429` has the body `{"status": "..."}`, and the HTTP code follows the status:

| Outcome (`status`)                                                      | HTTP  |
| ----------------------------------------------------------------------- | ----- |
| `sent`                                                                  | `200` |
| `invalidRequest`, `invalidField`, `tooManyFiles`, `unsupportedFileType` | `400` |
| `fileTooLarge`                                                          | `413` |
| `deliveryFailed`                                                        | `503` |

### Contact configuration

| Setting (`Contact`)          | Default           | Range        | Meaning                                                |
| ---------------------------- | ----------------- | ------------ | ------------------------------------------------------ |
| `SponsorshipRecipients`      | required          | -            | Comma-separated addresses for the Sponsorship category |
| `GeneralInquiriesRecipients` | required          | -            | Comma-separated addresses for General inquiries        |
| `MediaRecipients`            | required          | -            | Comma-separated addresses for Media                    |
| `CustomerServiceRecipients`  | required          | -            | Comma-separated addresses for Customer service         |
| `MaxFiles`                   | `5`               | 1-10         | Most files one submission may attach                   |
| `MaxFileBytes`               | `5242880` (5 MiB) | up to 10 MiB | Largest single attachment                              |
| `MaxTotalBytes`              | `8388608` (8 MiB) | up to 10 MiB | Largest combined size of all attachments               |
| `RateLimitPermitLimit`       | `5`               | 1-1000       | Submissions one IP address may make per window         |
| `RateLimitWindowSeconds`     | `600`             | 1-86400      | Length of that window                                  |

- **Recipients:** comma-separated addresses, at most 50 per category. They are checked at startup:
  the API does not start if a category has no valid address or if `MaxFileBytes` is larger than
  `MaxTotalBytes`.
- **Where they live:** Staging reads them from `appsettings.Staging.json`. Production has no file
  for them, so they are set as Render environment variables at cutover
  (`Contact__SponsorshipRecipients`, `Contact__GeneralInquiriesRecipients`,
  `Contact__MediaRecipients`, `Contact__CustomerServiceRecipients`); the API will not start
  without them. A change needs a restart, and so does a new rate limit value.
- **Local development** uses fake `@example.com` addresses from `appsettings.Development.json`, so
  nothing reaches a real inbox.
- **Staging test.** Real addresses receive this, so tell them first:

```bash
curl -F firstName=Ada -F lastName=Lovelace -F email=you@example.com \
  -F category=media -F message=Hello -F files=@file.pdf \
  https://<staging-host>/contact
```

It should answer `{"status":"sent"}` and deliver to the Media recipients.

### Health and utility

| Method | Path            | Access    | Does                                                       |
| ------ | --------------- | --------- | ---------------------------------------------------------- |
| `GET`  | `/health/live`  | Public    | The process is running                                     |
| `GET`  | `/health/ready` | Public    | Dependencies are reachable (`Healthy`)                     |
| `GET`  | `/ping`         | Public    | Answers `pong`; used to check the frontend reaches the API |
| `GET`  | `/ping/secure`  | Signed in | `401` without a token                                      |

## Emails

Code and alert emails are sent through `IAlertSender` with an `AlertType`. Every template shares one
layout (`AlertEmailLayout`) styled like the contact-form emails: dark header, green code card, grey footer.

| Kind   | Alert types                                                                                                                                  |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| Codes  | `RegistrationCode`, `LoginCode`, `PasswordChangeCode`, `PasswordResetCode`, `EmailChangeCurrentCode`, `EmailChangeNewCode`, `AccountDeletionCode` (`StepUpCode` is the generic wording) |
| Alerts | `NewLoginAttempt`, `PasswordResetRequested`, `PasswordChanged`, `EmailChanged`, `AccountDeleted`                                                |

Templates use `{{token}}` placeholders filled from `AlertTemplateKeys` (`code`, `expiresInMinutes`,
`newEmail`, `recoveryDays`, `ipAddress`, `userAgent`). Values are HTML-encoded and a token that is not
supplied stays visible, which the "no `{{` left" tests catch. `VerificationCodeService.IssueAsync` picks the
wording from the purpose and accepts an override and extra data (password change and account deletion
use it, because they share the step-up purpose).

## Notifications

The Notification module keeps a member's inbox. Other modules send into it through
`INotificationSender` from `TeamSamsara.Shared`; a member reads it through the inbox endpoints
(see Notifications under API Endpoints).

```csharp
NotificationResult result = await sender.SendNotificationAsync(
    new NotificationRequest(recipientId, "order.shipped", data, dedupeKey), cancellationToken);
```

| Field         | Rule                                                                                                        |
| ------------- | ----------------------------------------------------------------------------------------------------------- |
| `RecipientId` | The member's Firebase uid (`A-Z a-z 0-9 _ -`, up to 128 characters)                                         |
| `Key`         | `category.event`: 2-4 lowercase tokens joined by dots, each `[a-z][a-z0-9-]{0,31}`                          |
| `Data`        | Optional flat map of strings: up to 20 entries, names `[a-z][A-Za-z0-9]{0,39}`, values up to 500 characters |
| `DedupeKey`   | Optional, `A-Z a-z 0-9 . _ : -`, up to 100 characters                                                       |

A malformed request throws `ArgumentException` (a bug in the caller, not a runtime condition).
The category is the text before the first dot of `Key`; it is derived and never stored.

| Status      | Meaning                                                                                                      |
| ----------- | ------------------------------------------------------------------------------------------------------------ |
| `Stored`    | The notification was written; `Id` is its document id                                                        |
| `Duplicate` | The same `DedupeKey` was already stored with the same `Key` and `Data`; nothing was written                  |
| `Conflict`  | The `DedupeKey` was already used for a different `Key` or `Data`; nothing was written and an error is logged |
| `Failed`    | The write could not be confirmed; it may or may not have happened                                            |

- **Dedupe.** A `DedupeKey` is scoped to one recipient, and the document id is a hash of the two,
  so a repeat lands on the same document. The window lasts as long as the document does: until
  the retention period ends and the Firestore TTL deletes it. Without a `DedupeKey` every call
  makes a new notification.
- **Best effort.** Sending is at most once per call and a notification can be lost. The sender
  gives up after `WriteTimeoutSeconds` and reports `Failed`; it rethrows only the caller's own
  cancellation, and lets unexpected exceptions (bugs) through. A producer whose business
  operation has already committed must guard the call itself so a failure cannot undo it.
- **Frontend compatibility.** The frontend turns `Key` and `Data` into text, so: an unknown key
  gets a generic localized fallback; unknown `Data` fields are ignored and missing ones tolerated;
  `Data` values are untrusted text, never HTML; dates are ISO-8601 strings the frontend formats;
  where a notification leads is decided by the frontend; translation keys are
  `notifications.<key>`. A key never changes meaning (a new meaning is a new key), `Data` only
  gains optional fields, and a renderer stays until its producer is gone and the retention period
  has passed.

| Setting (`Notification`) | Default | Range | Meaning                                                    |
| ------------------------ | ------- | ----- | ---------------------------------------------------------- |
| `RetentionDays`          | `90`    | 1-365 | How long a notification lives before the TTL may delete it |
| `WriteTimeoutSeconds`    | `5`     | 1-30  | How long one storage call may take before `Failed`         |

`RetentionDays` is a policy choice, not a confirmed requirement. `ExpiresAt` is stored with each
notification and the Firestore TTL policy on `notifications.ExpiresAt` does the deleting, usually
within a day of expiry. Expired documents still appear in queries until then.

**Account purge.** When an account is purged, the Notification module erases the member's whole
inbox (`NotificationMemberDataEraser`, in batches of up to 500) before the Firebase account is
deleted; if that fails, the account is retried on the next purge run. A notification written for
the member while their purge is running can be missed by that run and then stays until the TTL
deletes it, at most `RetentionDays` later.

### Notifications the API sends

| Key                  | When                                                           | `Data`                                                     |
| -------------------- | -------------------------------------------------------------- | ---------------------------------------------------------- |
| `security.new-login` | A sign-in from an unrecognized client, when its code goes out | `ipAddress` and `userAgent` (each cut to 500 characters) |

- **`security.new-login`** is sent by `AuthenticationService` next to the new-login email. The
  `DedupeKey` is `new-login:<device>:<yyyyMMdd>`, where `<device>` is 32 hex characters hashed from
  the client fingerprint and the day is the UTC day, so the same client on the same day leaves one
  notice however many times it tries. It is not sent for a recognized client, during the resend
  cooldown, or when a code is resent.
- **Best effort.** If the notification cannot be stored, a warning is logged and the sign-in
  carries on exactly as before: the code email has already gone out and the notice is a courtesy.

### Notifications deployment

Nothing in CI deploys Firestore configuration; it is applied by hand with
`firebase deploy --only firestore --project staging` (or `production`). Before any module sends a
notification:

1. **TTL policy.** `firestore.indexes.json` declares it; confirm in the Firebase console that the
   policy on `notifications.ExpiresAt` is active. Without it, notifications are never deleted.
2. **Security rules.** `firestore.rules` denies all client access to `notifications`; the API
   reads and writes through the Admin SDK. Deploying rules overwrites the live ones, so compare
   them with the console first.
3. **Indexes.** The inbox list needs a composite index on `RecipientId` ascending, `CreatedAt`
   descending and `__name__` descending, declared in `firestore.indexes.json`; it must be deployed
   and Ready before the inbox endpoints are merged, or the list answers an error. The first deploy of
   `firestore.indexes.json` may offer to delete live indexes that are not in the file; read that
   prompt before answering.

### Notifications planned work

**Inbox API: check against real Firestore.** The in-memory fakes cannot reproduce cursors, the
unread count, equality on an empty `ReadAt` or index requirements, so after the index is Ready on
Staging, call the endpoints once with a real member's token: list with `?limit=1` and follow
`nextCursor` to the end, read the unread count, mark one notification read and check the count
drops, then mark all read. A pass against the Firestore emulator would also do.

**Not planned yet.** Mark unread, delete, per-member preferences, other delivery channels (email,
push) and sending one notification to many members.

**Before the first producer.** The first module that calls `INotificationSender` in code that
reaches `dev` does these, in this order:

1. Deploy the TTL policy and rules to Staging with the commands in Notifications deployment, and
   confirm the TTL policy is active in the Firebase console.
2. Run the batch delete once against the Firestore emulator: store more than 500 notifications for
   one member, run `NotificationMemberDataEraser`, and check they are all gone. The unit tests use
   a fake store, so this is the only check against real Firestore behavior.
3. Decide how a failed send is handled in the producer (guard the call so it cannot undo a
   business operation that already committed) and what its `DedupeKey` is, if any.
4. Repeat step 1 for the production project at cutover.

A producer's notifications reach the member through the inbox API; the frontend turns each key
into text, so a new key needs a renderer there. A feature that only emails someone, such as the
contact form, needs none of this.

## Git Workflow

Replace every `<...>` placeholder before running a command.

### Prerequisites

Pull requests are created and merged from the terminal with the GitHub CLI:

```bash
winget install --id GitHub.cli -e
gh auth login
```

### Branches

Two long-lived branches:

- `main` - production. Only ever updated by promoting `dev` into it. Nothing branches directly from `main`.
- `dev` - staging and the next release. Everything merges here first, and only after it is tested.

Short-lived branches, always created from an up-to-date `dev` (or from an integration branch, see below):

- `feature/<slug>` - a feature, or an integration branch for a multi-part implementation
- `task/<implementation>-<piece>` - one piece of an implementation, merged into its integration branch
- `fix/<slug>` - corrections to already-merged functionality

Branch names cannot also be folders: `feature/identity` and `feature/identity/alerts` cannot exist together. That is why pieces use `task/`.

### Choosing a path

Before creating a branch, ask:

> Is this an implementation made of two or more pieces that must be verified together before any of it reaches `dev`?

- **No** - use a direct branch (below). This is the default for a single feature, a fix, or any piece that can land on `dev` alone and leave `dev` working.
- **Yes** - use an integration branch (below).

Write dependencies down in the sprint list before starting ("Item 2 depends on Item 1"). Work is merged in that order, so nobody has to guess what must land first.

### Direct branch

```bash
git checkout dev
git pull
git checkout -b feature/<slug>
# build and commit
git push -u origin feature/<slug>

gh pr create --base dev --title "feat(<scope>): <summary>" --body-file pr.md
gh pr merge --squash --auto

# once it has merged
git checkout dev
git pull
git branch -D feature/<slug>
dotnet build apps/api/TeamSamsara.sln
dotnet test apps/api/TeamSamsara.sln
```

If one direct branch depends on another, merge the first and start the second from the updated `dev`.

### Integration branch

The integration branch proves the pieces work together before any of them reaches `dev`.

```bash
# create it and open a draft PR so its state is visible from the start
git checkout dev
git pull
git checkout -b feature/<implementation>
git push -u origin feature/<implementation>
gh pr create --draft --base dev --title "feat(<scope>): <summary>" --body-file pr.md

# for each piece
git checkout feature/<implementation>
git pull
git checkout -b task/<implementation>-<piece>
# build and commit
git push -u origin task/<implementation>-<piece>
gh pr create --base feature/<implementation> --title "feat(<scope>): <piece>" --body "<what this piece does>"
gh pr merge --squash --auto

# once the piece has merged
git checkout feature/<implementation>
git pull
git branch -D task/<implementation>-<piece>

# keep it current whenever dev changes
git fetch origin
git merge origin/dev
git push

# finish, once every piece is in
dotnet build apps/api/TeamSamsara.sln
dotnet test apps/api/TeamSamsara.sln
gh pr ready
gh pr merge --squash --auto

# once it has merged
git checkout dev
git pull
git branch -D feature/<implementation>
```

Rules:

- If piece B needs piece A, A merges into the integration branch first, then B branches from the updated integration branch.
- Merge `dev` into the integration branch (never rebase, the branch is shared) whenever `dev` changes and before finishing.
- Keep it short-lived: days, not months.
- `dev` receives one commit for the whole implementation.

### Commits

Use Conventional Commits, with the module or area as the scope:

- `feat(<scope>):` - new functionality
- `fix(<scope>):` - bug fixes
- `chore(<scope>):` - maintenance
- `refactor(<scope>):` - code restructuring without behavior changes
- `test(<scope>):` - tests
- `docs(<scope>):` - documentation
- `release:` - promotes `dev` into `main` (no scope, the summary names the version or date)

Examples of scopes: `identity`, `alerts`, `assets`, `api`, `shared`, `readme`.

Commits inside a branch are working history and are squashed on merge. The pull request title is what stays on `dev`, so it must be a well-formed Conventional Commit.

### Before opening a pull request

```bash
dotnet build apps/api/TeamSamsara.sln
dotnet test apps/api/TeamSamsara.sln
dotnet format apps/api/TeamSamsara.sln --verify-no-changes
```

All three must be clean, and CI must pass on the pull request. Nothing is merged with a failing build. After merging, update `dev`, rebuild and retest: the combined result must also pass. New and changed code also follows [Code Comments](#code-comments).

### Merging and cleanup

- Every merge goes through a pull request. `main` and `dev` are protected: no direct pushes, no force pushes, no deletion, and the `backend` and `frontend` CI checks must pass before anything merges. The rules apply to admins too.
- Merge from the CLI with `gh pr merge --squash --auto`, right after `gh pr create` and from the same branch: GitHub waits for CI to pass and then merges, so there is no need to watch the checks.
- Features, tasks and fixes use **squash**: each completed piece becomes one clean commit.
- The pull request title becomes the commit on `dev`, so it must be a well-formed Conventional Commit. The repository builds the squash commit from the PR title, adds the PR number, and leaves the body empty. History on `dev` cannot be rewritten, so a wrong title stays.
- Merged branches are deleted on GitHub automatically. After the merge, update `dev` and delete the local branch with `git checkout dev`, `git pull` and `git branch -D <branch>`.

### Promoting staging to production (`main`)

Staging is `dev` deployed by Render. Once the whole of `dev` is verified on Staging, promote it with a regular merge, **not** a squash:

```bash
gh pr create --base main --head dev --title "release: <summary>" --body-file pr.md
gh pr merge --merge --auto --subject "release: <summary>" --body " "
```

A regular merge does not use the PR title by default, so the subject is set explicitly. In Windows PowerShell an empty argument is dropped, so the body is a single space.

`dev` is a long-lived branch and is protected from deletion. A squash would collapse every feature already squashed into `dev` into one undifferentiated commit and destroy the history of what shipped in each release. Rebuild and retest after this merge too.

`dev` is promoted as a unit. If one feature on `dev` fails verification, `main` waits until it is fixed (with a `fix/<slug>` branch) or reverted.

Deferred branches may remain unmerged while other work continues. When merging multiple deferred branches, merge them one at a time and rebuild after each merge.

### Deployment triggers

- **Render** (`TeamSamsara.Api`, `apps/cms`): the staging service watches `dev`, the production service watches `main`. Both auto-deploy on every push to their respective branch. The `dev` → `main` merge itself is the deliberate promotion gate - there is no separate manual deploy step.
- **Firebase Hosting** (`apps/web`): deployed manually via `firebase deploy --project staging` or `--project production`. Not yet automated; unaffected by the branching model above.

## Architecture Principles

### Database Portability

The application currently uses Firestore. `IRepository<T>` and module-specific repository interfaces such as a future `IRoleRepository` define the boundary between business logic and persistence.

We do **not** attempt to build a database-agnostic abstraction layer. Firestore and relational databases differ fundamentally in their capabilities, query models, and transaction semantics. Forcing them behind an identical abstraction would either limit the application to the lowest common denominator or leak backend-specific behavior through the abstraction.

The rule is simple:

> Repository interfaces must never expose database-specific types such as `Query`, `DocumentReference`, or `CollectionReference`. Interfaces speak only in domain terms.

For example:

```csharp
Task<Role?> GetByNameAsync(string name);
```

Not:

```csharp
Task<DocumentSnapshot?> GetByNameAsync(string name);
```

Database-specific types and behavior belong exclusively inside concrete repository implementations such as `FirestoreRepository<T>`.

This keeps business logic independent of Firestore without introducing a portability abstraction we do not currently need.

### Database Migrations

Payload's Postgres schema is managed differently in dev versus staging/production.

**Development:** Payload's dev-mode auto-push applies schema changes directly, for iteration
speed. No migration files are involved locally.

**Staging and production:** schema changes are applied only through Payload's migration system -
never auto-push. Workflow:

- `npm run migrate:create --prefix apps/cms` generates a migration file from the current schema,
  committed to the repo like any other code change and reviewed the same way
- `npm run migrate --prefix apps/cms` applies any pending migrations; this is run as an explicit
  step before the app starts in staging/production, never implicitly

This means the database schema is always the result of a reviewable, ordered set of changes in
staging/production, never something inferred silently at runtime.

## Coding Conventions

### String & Magic-Value Constants

Application-defined semantic values must not be scattered as inline literals. Use the appropriate
named representation - a `const string`, an enum or union type, a typed value, a configuration
entry, or another suitable abstraction.

The test: if the value represents something our code depends on matching exactly - a permission
string, a claim key, a header name, an error code, a collection name, a route, a log property
name, a status - it gets a named definition. Whether it is used once or ten times is irrelevant;
usage count does not determine whether something is a magic value.

The following may stay inline:

- Incidental text with no application-defined meaning
- External identifiers we don't own (framework namespace strings, third-party API values)
- Values already represented through an appropriate typed API (e.g. ASP.NET Core's
  `Headers.XContentTypeOptions` property)
- Serilog message templates at the logging call site (`_logger.LogWarning("Handled application
error: {Code}", ...)`) - kept inline deliberately, so Serilog's analyzer and IDE tooling can
  validate named placeholders against supplied properties directly at the call site. Log
  _property names_ (`"CorrelationId"`, `"Environment"`) still follow the named-definition
  convention; only the complete message template is exempt.

This is not a rule to eliminate string literals from the language - only to eliminate
_unnamed semantic values_. A one-off, genuinely arbitrary piece of text has nothing to gain from
being wrapped in a constant.

**Ownership:** constants are owned by the area they belong to, not collected into one global
file. `Shared`-level values (used across every module) live in small, purpose-named files close
to what they describe - `Shared/Authorization/Permissions.cs`, `Shared/Authentication/
ClaimNames.cs`, `Shared/Http/HttpConstants.cs`, and similarly-scoped files under `Logging`,
`Results`, and `Persistence`. Module-specific values live inside that module, not in `Shared` -
e.g. `Modules.PingPong/PingPongConstants.cs`. The frontend mirrors this: `app/ClaimNames.ts`,
`lib/httpConstants.ts`, `lib/uiMessages.ts`, `lib/devInvariants.ts`, plus feature-owned files such
as `features/ping/constants/pingApiRoutes.ts`.

A value that must match across the C#/TypeScript boundary (e.g. the `accessLevel` claim key) gets
its own definition on each side - a literal can't be shared across languages, but each side
should reference its own named constant, not an inline string.

### Code Comments

Comments are short and state intent. Code that reads well needs few of them.

**File header.** Keep the required format (`File`, `Version`, `Latest commit`, `Author`,
`Purpose`). `Purpose` is one sentence saying what the file does - not the reasoning behind it, not
how other files use it, not what would happen if it were misused. Version, commit and author lines
are never dropped when a file is rewritten.

**Methods.** Every method, public or private, has a one-line comment above it stating its primary
responsibility or an important contract the caller must know (single-use, expiry, security,
ordering). Constructors, fields and trivial properties need none.

**Inline comments.** The exception, not the default. Add one only when the code cannot
communicate the intent on its own, and keep it to a short sentence or phrase.

**Do not write:**

- Narration of what the code does, line by line.
- Conversational justification ("we do this because we considered...").
- Text that restates a name, type or structure that is already clear.
- A paragraph where one line says it.

**Readability.** Prefer flat code to comments that explain nesting: early returns, and small named
helper methods, so a method reads top to bottom without deeply nested `if` blocks.

**Existing code.** Bring a file in line when it is next touched for another reason. Do not change
behavior just to shorten a comment, and do not remove documentation that carries real meaning.

```csharp
// Consumes the code so it can never be used twice.
private async Task<VerificationResult> ConsumeAsync(...)
```

## Background Services

Work that must not hold up a request, or that runs on a schedule, goes through one of two
mechanisms. Both live in `TeamSamsara.Shared.BackgroundTasks`.

- **Task queue** (`IBackgroundTaskQueue`) - request-triggered work. A handler calls
  `TryEnqueue` and returns; one worker runs the items in order, each in its own DI scope.
  The queue is in memory and bounded (1000 items), so queued work is lost if the app restarts
  and `TryEnqueue` returns `false` when it is full. Only enqueue work that is safe to lose
  (a user can tap resend) or that is re-derivable.
- **Scheduled job** - time-triggered work, built as its own `BackgroundService` on a
  `PeriodicTimer`. It runs once at startup, then every interval, each run in its own DI scope. A
  failed run is logged and the job waits for the next tick; it never stops.

### Implemented

| Service                | Where    | What it does                                                                                                                |
| ---------------------- | -------- | --------------------------------------------------------------------------------------------------------------------------- |
| `BackgroundTaskWorker` | Shared   | Runs queued work one item at a time, logs failures, never stops on one. Runs password reset and email change code delivery. |
| `AccountPurgeWorker`   | Identity | Permanently removes accounts whose recovery window has ended. See below.                                                    |

### Account purge

`AccountPurgeWorker` calls `IAccountPurgeService` at startup and then every `IntervalHours`. It
removes every account deleted more than `RecoveryWindowDays` (30) ago. See Account deletion for
the member's side.

| Setting (`Identity:AccountPurge`) | Default | Range | Meaning                                                 |
| --------------------------------- | ------- | ----- | ------------------------------------------------------- |
| `IntervalHours`                   | `6`     | 1-168 | How often the job looks for accounts to remove          |
| `LeaseMinutes`                    | `10`    | 1-120 | How long one instance holds an account while purging it |

- **Several instances.** Before touching an account an instance claims it in a transaction that
  checks the account is still deleted, past the deadline and not leased by another instance. Only
  one claim succeeds. If the instance crashes, the lease lapses after `LeaseMinutes` and a later
  run takes the account over. A member who signs in during a lease is not restored.
- **Retry safety.** Each step can be repeated, and something already gone counts as success. The
  order is images, profile, verification codes, password reset tokens, email-change request,
  other modules' data, Firebase account, and the user record last, so an account that stopped
  halfway is still listed and the next run finishes it.
- **Other modules' data.** A module that holds member data registers an `IMemberDataEraser`
  (`TeamSamsara.Shared.Members`). The purge runs them one after another, after the leftover
  documents and before the Firebase account is deleted. An eraser that fails stops the purge of
  that account, which stays listed and is retried on the next run.
- **Failures.** A failure on one account is logged and counted, and the rest of the batch goes on.
  The account stays listed and is retried on every later run. A run that logs failed accounts is
  a warning.
- **Leftover documents.** The purge deletes verification codes, password reset tokens and the
  email-change request directly, so a purged account leaves none behind. The Firestore TTL
  policies stay as the cleanup for everyone else.

### Planned

Nothing is planned at the moment.

### Audit before starting the frontend

Run this audit once every backend module is built. Findings so far, from the code that exists:

- `AuthenticationService` - the new-login alert and the new-login inbox notice are awaited inside
  the sign-in request and their failures are swallowed. Candidate for the queue.
- `PasswordService` - the password-changed notice is awaited inside the request and its failure
  is swallowed. Candidate for the queue.
- `VerificationCodeService` - the code email stays inline. `IssueAsync` discards the stored code
  and reports failure if the send throws, so the caller has to know the outcome. Not a candidate
  as written.
- `ResendEmailSender` - no retry. A queued send could retry a few times before giving up.
- Assets - a deleted asset no longer leaves its file behind. Two cases remain: the file left by
  a failed upload whose cleanup also failed (`TryDeleteFileAsync`), and metadata with no owner.
  Candidate for a scheduled sweep of orphaned files.
- Expired `verificationCodes`, `passwordResetTokens` and `emailChangeRequests` - cleaned up by a
  Firestore TTL policy on `ExpiresAt`, not by a service. The account purge also deletes them
  directly for the accounts it removes.
- Catalog, Content, Media, Orders - stubs. Audit each when built (image processing, order and
  receipt emails, stock and price sync).

For each module, check: anything awaited in a request that the caller does not need the result
of; anything that fails quietly and leaves leftovers; anything that should expire or be swept on
a schedule; and whether it can tolerate being lost on restart. If not, it needs a durable outbox
rather than the in-memory queue.

## Deferred Implementations

Some interface methods are intentionally stubbed rather than implemented, because building them now would mean guessing at requirements with no real caller driving their shape yet.

Any method that throws `NotImplementedException` must be documented here, alongside a `// TODO` comment at the call site explaining why. One without the other means either the code has no context for a reader, or this list silently drifts out of date.

- `FirebaseStorageService.GetSignedUrlAsync` - throws `NotImplementedException`. Requires a service account configured with explicit signing credentials, which isn't set up yet. Implement once a real caller needs time-limited access to a non-public file.

- `Alerting & differentiated log retention` - not implemented. Requires a real pattern of what
  counts as critical vs. routine, which doesn't exist until Identity (login attempts, anomaly
  detection) and other modules are generating real events to observe. Logging already emits
  everything a future system would need to filter on (level, exception, correlation ID, source
  context) - no changes needed in Shared.Logging when this is built; it will be a separate
  consumer, not a coupled one.

- `npm run generate-types` requires the API running locally - it fetches the OpenAPI spec live
  over HTTP rather than reading a static file, so it can't run in an environment without the API
  also running (e.g. a future CI job, item 15, would need to stand up the API first).

- **Firebase service account key rotation** - the staging and production service account keys
  were shared in full outside their intended storage location during setup. A deliberate decision
  was made to defer rotating them rather than block on it immediately. Both keys should be rotated
  (Firebase Console → Project Settings → Service Accounts → delete the old key, generate a new
  one) before this project is treated as production-hardened.

- `GoogleCredentialProvider.TryGetFromEnvironment` uses `GoogleCredential.FromJson(string)`,
  which the SDK marks obsolete (`CS0618`) in favor of `CredentialFactory`. Left as-is for now
  rather than guessing at the replacement API's exact shape without a concrete reason to change
  it; the current method still works correctly and is exercised by real staging traffic.

- **Verbose file-header comments** - several files predating the code-comment convention above
  still have multi-sentence `Purpose` lines explaining implementation reasoning rather than
  stating what the file does. Not yet trimmed; revisit as those files are next touched, or as a
  dedicated pass.

# Blocked word lists

`BlockedWords.en.txt` and `BlockedWords.fr.txt` come from the LDNOOBW project,
"List of Dirty, Naughty, Obscene, and Otherwise Bad Words"
(https://github.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words),
by Shutterstock, Inc. and its contributors, under the CC BY 4.0 license.
The full license text is in `LDNOOBW-LICENSE.txt`.

The files are unchanged copies of the project's `en` and `fr` lists, only renamed.
No endorsement by the authors is claimed or implied.
