\# Team Samsara



Production website for Team Samsara — React frontend, C# (.NET) modular monolith backend,

Payload CMS, Firebase (Auth, Storage, Hosting).



\## Repo structure



\- `apps/web` — React + Vite frontend (public site + CMS admin UI)

\- `apps/api` — C# modular monolith

\- `apps/cms` — Payload CMS (API-only, admin panel disabled), Postgres-backed

\- `docs/architecture` — architecture and planning docs



\## Local development



Prerequisites: .NET SDK 9.0.314 (see `global.json`), Node 22.22.3 (see `.nvmrc`), PostgreSQL 17.



\_(Exact setup steps not yet written down — see TODO below.)\_



\## TODO



\- \[ ] Write up the actual local setup sequence (Postgres install + DB creation, `.env` values

&#x20;     needed per app, Firebase emulator setup) — currently only exists as chat history from the

&#x20;     session that first set this up

\- \[ ] `scripts/bootstrap.ps1` / `.sh` — automate the above once the manual sequence is confirmed

&#x20;     stable; deferred for now since this is solo dev and the setup already works on this machine

