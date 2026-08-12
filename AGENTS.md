# Repository Guidelines

## Scope

This repository owns the Studio Nuxt frontend plus Studio API, Worker, content authoring, conversion tooling, and Studio content persistence. Authoritative Game Server runtime persistence remains outside this repository.

## Commands

Run development, validation, and builds through Docker from the repository root:

```sh
docker compose up --build
docker build --target validate --build-arg APP_ENV=production web
docker build --target unit-tests --file server/Dockerfile .
docker compose config
docker compose build
```

`compose.yaml` owns the standalone Studio stack. Browser code calls the same-origin `/api` proxy, while Nuxt owns `/storage-api` for version-scoped volume management. Keep `NUXT_STUDIO_API_BASE` private to Nuxt and use `NUXT_PUBLIC_ASSET_BASE_URL` only for browser-readable generated assets. Do not run host-installed Node.js, npm, or .NET commands for normal product validation.

## Server Architecture

- Keep .NET production projects in `server/src`, project-owned unit-test projects in `server/tests`, and the solution, shared build properties, package versions, and Dockerfile in `server/`.
- `L2.Studio.Api` owns controllers, request filters, and HTTP composition. `L2.Studio.Worker` remains a thin process host.
- `L2.Studio.Configurations` owns service registration, CORS, health checks, service identity, and host composition.
- `L2.Studio.Contracts` owns browser-facing DTOs; `L2.Studio.Context` owns EF Core entities and model mapping; `L2.Studio.Migrations` owns migrations and seed data.
- `L2.Studio.Repositories.Interfaces` owns persistence abstractions and shared import models. `L2.Studio.Repositories` owns runtime persistence implementations and path validation.
- `L2.Studio.Services` owns import orchestration, manifests, preview generation, and asset processing. `L2.Tools.*` conversion libraries are Studio solution projects; preserve their public names and namespaces.
- Name test projects after the production project they verify: `{ProjectName}.Tests`. Keep each test in its owning project’s test assembly; repository tests must not require a database connection.

## Web Architecture

- Keep Nuxt source in `web/app`, browser API calls in `web/app/services`, and state in Pinia setup stores.
- Pages own route synchronization, loading, and store composition; reusable shell components live under `components/app`, while substantial page sections live under `components/pages`.
- Browser code calls only the Nuxt `/api` proxy through the service layer. Do not call the upstream Studio API from pages or components.
- Keep file storage routes in Nuxt `/storage-api`; permit mutations only in the original-resource volume and keep generated assets read-only so catalog state remains authoritative.
- Organize tests under `web/test/unit`, `web/test/nuxt`, and `web/test/e2e`. Keep store, service, and pure utility tests in `unit`.

## Configuration

`NUXT_STUDIO_API_BASE` is required whenever Nuxt configuration loads. Docker Compose selects `APP_ENV=development`; image validation selects `APP_ENV=production`. `NUXT_PUBLIC_ASSET_BASE_URL` is the browser-visible generated-asset origin, while the Studio API base remains server-only.

Environment-specific API settings belong in `server/src/L2.Studio.Api/appsettings.<Environment>.json`; environment variables may override them through standard ASP.NET Core configuration. Never commit tokens, non-development credentials, original game sources, or generated private assets.

## Conventions

Use UTF-8 and LF endings. TypeScript and Vue use two-space indentation, single quotes, no semicolons, and no trailing commas. Preserve established C# formatting and nullable-reference-type safety.
