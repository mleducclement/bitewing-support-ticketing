# Bitewing web

React + TypeScript + Vite frontend for the Bitewing support ticketing system.
UI built with Tailwind CSS v4 and shadcn/ui.

## Development

```
npm install
npm run dev
```

The dev server runs on `http://localhost:5173` and proxies `/api` to the API on
`http://localhost:5073` (start it with `dotnet run --project ../bitewing`).

## Build

```
npm run build
```

Output goes to `../bitewing/wwwroot`, which ASP.NET Core serves in production
(single deployable, see `docs/decision-log.md`).

## Layout

- `src/components/ui/` — shadcn/ui primitives (generated, not hand-edited)
- `src/components/` — shared app components
- `src/features/<feature>/` — feature UI, grouped like the API's DTOs
- `src/types/` — shared types, mirroring the API DTOs