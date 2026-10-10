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
asset ids - the client builds the address from `GET /assets/{id}/file`.

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

### Health and utility

| Method | Path            | Access    | Does                                                       |
| ------ | --------------- | --------- | ---------------------------------------------------------- |
| `GET`  | `/health/live`  | Public    | The process is running                                     |
| `GET`  | `/health/ready` | Public    | Dependencies are reachable (`Healthy`)                     |
| `GET`  | `/ping`         | Public    | Answers `pong`; used to check the frontend reaches the API |
| `GET`  | `/ping/secure`  | Signed in | `401` without a token                                      |

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
  `PeriodicTimer`. Not built yet.

### Implemented

| Service                | Where  | What it does                                                                                       |
| ---------------------- | ------ | -------------------------------------------------------------------------------------------------- |
| `BackgroundTaskWorker` | Shared | Runs queued work one item at a time, logs failures, never stops on one. Nothing enqueues work yet. |

### Planned

| Work                                         | Mechanism     | Notes                                                                                                                                                                                               |
| -------------------------------------------- | ------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Forgot-password lookup, code issue and email | Queue         | Every request gets the same answer; the work happens behind it so timing cannot reveal whether an email has an account.                                                                             |
| Account purge                                | Scheduled job | Deletes accounts past `RecoveryWindowDays` (30). `IUserStore.ListDeletedBeforeAsync` and `DeleteAsync` already exist; nothing calls them yet. Needs the gateway, profile and asset deletes as well. |

### Audit before starting the frontend

Run this audit once every backend module is built. Findings so far, from the code that exists:

- `AuthenticationService` - the new-login alert is awaited inside the sign-in request and its
  failure is swallowed. Candidate for the queue.
- `PasswordService` - the password-changed notice is awaited inside the request and its failure
  is swallowed. Candidate for the queue.
- `VerificationCodeService` - the code email stays inline. `IssueAsync` discards the stored code
  and reports failure if the send throws, so the caller has to know the outcome. Not a candidate
  as written.
- `ResendEmailSender` - no retry. A queued send could retry a few times before giving up.
- Assets - a failed file delete is logged and the file is left behind (`TryDeleteFileAsync`).
  Candidate for a scheduled sweep of orphaned files.
- Expired `verificationCodes` and `passwordResetTokens` - cleaned up by a Firestore TTL policy on
  `ExpiresAt`, not by a service.
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
