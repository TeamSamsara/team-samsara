# Team Samsara

Production website for Team Samsara — React frontend, C# (.NET) modular monolith backend, Payload CMS, and Firebase infrastructure.

## Architecture

### Applications

- `apps/web` — React + Vite frontend (public site + CMS admin UI)
- `apps/api` — C# modular monolith backend
- `apps/cms` — Payload CMS (API-only, admin panel disabled), backed by PostgreSQL

### Infrastructure

- Firebase — Authentication, Storage, Hosting, and Firestore
- PostgreSQL — Payload CMS database

### Documentation

- `docs/architecture` — Architecture decisions, specifications, and planning documents

## Local Development

### Prerequisites

- .NET SDK `9.0.314` — see `global.json`
- Node.js `22.22.3` — see `.nvmrc`
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

Create feature branches from an up-to-date `main`:

```bash
git checkout main
git pull
git checkout -b feature/<slug>
```

Branch naming:

- `feature/<slug>` — new functionality
- `fix/<slug>` — corrections to already-merged functionality

### Commits

Use Conventional Commits:

- `feat:` — new functionality
- `fix:` — bug fixes
- `chore:` — maintenance
- `refactor:` — code restructuring without behavior changes
- `test:` — tests

### Merge Requirements

Before merging:

```bash
dotnet build apps/api/TeamSamsara.sln
```

The build must be clean. No branch is merged with a failing build.

Once test projects exist, `dotnet test` is also required.

Merge feature branches using squash merging so each completed feature becomes one clean commit on `main`:

```bash
git merge --squash <branch>
```

After merging, rebuild immediately. The combined result must also pass the build.

Deferred branches may remain unmerged while other work continues. When merging multiple deferred branches, merge them one at a time and rebuild after each merge.

Once CI and branch protection are enabled, these requirements move to GitHub pull requests and are enforced automatically.

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

## Deferred Implementations

Some interface methods are intentionally stubbed rather than implemented, because building them now would mean guessing at requirements with no real caller driving their shape yet.

Any method that throws `NotImplementedException` must be documented here, alongside a `// TODO` comment at the call site explaining why. One without the other means either the code has no context for a reader, or this list silently drifts out of date.

- `FirebaseStorageService.GetSignedUrlAsync` — throws `NotImplementedException`. Requires a service account configured with explicit signing credentials, which isn't set up yet. Implement once a real caller needs time-limited access to a non-public file.

- `Alerting & differentiated log retention` — not implemented. Requires a real pattern of what
  counts as critical vs. routine, which doesn't exist until Identity (login attempts, anomaly
  detection) and other modules are generating real events to observe. Logging already emits
  everything a future system would need to filter on (level, exception, correlation ID, source
  context) — no changes needed in Shared.Logging when this is built; it will be a separate
  consumer, not a coupled one.
