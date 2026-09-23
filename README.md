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

## Local Development

### Prerequisites

- .NET SDK `9.0.314` - see `global.json`
- Node.js `22.22.3` - see `.nvmrc`
- PostgreSQL `17`

### Setup

> Local setup instructions are not yet documented.

The required setup sequence still needs to be documented, including:

- PostgreSQL installation and database creation
- Required `.env` values for each application
- Firebase emulator configuration
- Application startup order

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
