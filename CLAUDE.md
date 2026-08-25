# Bitewing

Support ticketing system. ASP.NET Core API + React frontend, single deployable.
Solo portfolio project with a fictional client.

## Read first

- `docs/spec.md` — states, transitions, entities, v1 scope
- `docs/decision-log.md` — why things are the way they are

## Layout

- `src/bitewing/` — ASP.NET Core API
- `docs/` — spec and decision log
- `src/bitewing/Data/` — AppDbContext, ApplicationUser, Entities
- `src/bitewing/Extensions` - Seeding
- `src/bitewing/Controllers/` — API controllers
- `src/bitewing/Dtos/` — request and response records, grouped by feature
- `src/bitewing/Options/` — configuration-bound classes
- `src/web/` — React frontend (Vite, TypeScript)

## Stack

ASP.NET Core, EF Core, PostgreSQL, React + shadcn/ui + Tailwind,
ASP.NET Core Identity, Claude API for classification. Hosted on Render.

## Rules

- Do not add features outside v1. See §8 of the spec for what's excluded.
- Follow the state machine in §3 exactly. Don't invent transitions.
- Classification is asynchronous via a work table. Never call the LLM
  during a request.
- API keys and connection strings come from environment variables. Never
  commit secrets or put them in appsettings.json.
- Ask before adding a NuGet or npm package.
- If the spec doesn't cover a case, stop and ask rather than picking a reasonable-looking default
- Data access is EF Core with Npgsql. Don't introduce Dapper or raw SQL
- Migrations run automatically at startup via `MigrateAsync`
- Connection string: `DATABASE_URL` env var in production, appsettings locally. NEVER HARDCODE IT
- Auth is cookie-based Identity. Don't add JWT.
- Authorization uses policies, not role attributes: `AgentAccess` (Agent or
  TeamLead) and `TeamLeadOnly`. Don't write `[Authorize(Roles = "...")]`.
- No self-registration. Accounts come from configuration seeding.
- Never return entities from controllers. Define a DTO record.
- Secrets come from environment variables. Never commit a password or
  connection string.
- Controllers stay thin: validate, call a service, return. Business logic and
  state transitions belong in services, not controllers.
- After running the app in the background to test an endpoint manually,
  stop that process once done (e.g. `taskkill //F //IM bitewing.exe` /
  `dotnet.exe`). A leftover instance locks the build output and breaks the
  next `dotnet build`/`dotnet test` run.

## Commands

- `dotnet run --project src/bitewing` — run the API
- `docker compose up -d` — start Postgres
- `dotnet ef migrations add <Name> --project src/bitewing`
- `docker build -f src/bitewing/Dockerfile -t bitewing src` — verify the
  container builds before pushing