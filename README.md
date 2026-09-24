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

## Git Workflow

### Branches

Two long-lived branches:

- `main` - production. Only ever updated by promoting `dev` into it. Nothing branches directly from `main`.
- `dev` - staging. Every feature branch merges here first; this is the active integration branch.

Create feature branches from an up-to-date `dev`, never from `main`:

```bash
git checkout dev
git pull
git checkout -b feature/<slug>
```

Branch naming:

- `feature/<slug>` - new functionality
- `fix/<slug>` - corrections to already-merged functionality

### Commits

Use Conventional Commits:

- `feat:` - new functionality
- `fix:` - bug fixes
- `chore:` - maintenance
- `refactor:` - code restructuring without behavior changes
- `test:` - tests

### Promoting a feature to staging (`dev`)

Before merging:

```bash
dotnet build apps/api/TeamSamsara.sln
dotnet test apps/api/TeamSamsara.sln
```

The build and tests must be clean. No branch is merged with a failing build.

Merge using squash merging, so each completed feature becomes one clean commit on `dev`:

```bash
git checkout dev
git merge --squash feature/<slug>
git commit -m "..."
```

After merging, rebuild and retest immediately. The combined result must also pass.

Deferred branches may remain unmerged while other work continues. When merging multiple deferred branches, merge them one at a time and rebuild after each merge.

Once CI and branch protection are enabled, these requirements move to GitHub pull requests and are enforced automatically.

### Promoting staging to production (`main`)

Once `dev` is verified stable, promote it to `main` using a regular merge, **not** a squash:

```bash
git checkout main
git pull
git merge dev
git push
```

Squashing here would collapse every feature already squashed into `dev` into one undifferentiated commit, destroying the ability to see what actually shipped in each release. A regular merge preserves that history on `main` exactly as it happened on `dev`. Rebuild and retest after this merge too, same as any other.

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

File headers keep the required format, but `Purpose` is one short sentence describing what the
file does - not the reasoning behind it, not how other files interact with it, not what would
happen if something were misused.

Inline comments are the exception, not the default. Add one only when the code cannot reasonably
communicate the intent on its own; keep it to a short sentence or phrase. Don't restate what a
name, type, or structure already makes clear.

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
