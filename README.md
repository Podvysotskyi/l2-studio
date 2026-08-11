# L2 Studio

The L2 Studio product: a Nuxt web interface and .NET services for asset conversion, content inspection, validation, and publishing workflows.

## Architecture

Studio is organized into focused .NET projects:

- `server/src` — production projects
- `server/tests` — project-owned, database-free unit-test projects
- `L2.Studio.Api` — controllers, request filters, and HTTP endpoints
- `L2.Studio.Worker` — background import process host
- `L2.Studio.Configurations` — dependency registration, CORS, health checks, and host composition
- `L2.Studio.Contracts` — browser-facing models, requests, and responses
- `L2.Studio.Context` — EF Core entities and content model mapping
- `L2.Studio.Migrations` — database migrations and content seed data
- `L2.Studio.Repositories.Interfaces` — persistence abstractions and shared import models
- `L2.Studio.Repositories` — runtime persistence, catalog access, and source-path validation
- `L2.Studio.Services` — import orchestration, manifests, preview generation, and asset processing
- `L2.Tools.*` — package-reading and audio, texture, and static-mesh conversion libraries
- `*.Tests` — unit tests for their correspondingly named Studio project

The Nuxt application follows Nuxt 4 conventions under `web/app`:

- `components/app` contains shared application-shell components.
- `components/pages` contains substantial page-specific sections.
- `pages` contains route composition, loading, and synchronization.
- `services` contains browser calls to the same-origin Nuxt `/api` proxy.
- `stores` contains Pinia Setup Stores.
- `types` groups browser contracts by models, requests, and responses.
- `runtime`, `composables`, and `utils` contain rendering behavior and reusable helpers.

Web tests are organized under `web/test/unit`, `web/test/nuxt`, and `web/test/e2e`. Server test projects are organized by the production project they verify, such as `L2.Studio.Api.Tests` and `L2.Studio.Services.Tests`.

## Prerequisites

- Docker Engine with Docker Compose
- An L2 game-source directory, mounted through `L2_SOURCE_PATH` when it is not adjacent to this repository

## Local development

```sh
docker compose up --build
```

The Studio UI runs at <http://localhost:3001>; generated assets are served at <http://localhost:5300>. Compose starts PostgreSQL, the API, Worker, web application, nginx asset server, and preview browser. Nuxt proxies all browser `/api` requests to the internal Studio API.

`NUXT_STUDIO_API_BASE` is required whenever Nuxt configuration loads and remains private to the Nuxt server. Use `NUXT_PUBLIC_ASSET_BASE_URL` only for the browser-visible asset origin. Compose loads `web/.env.development`; image validation loads `web/.env.production`.

## Docker Compose

Studio has a standalone development stack containing PostgreSQL, the Studio API and worker, the Studio web application, nginx asset serving, and the preview browser:

```sh
docker compose up --build
```

Set `L2_SOURCE_PATH` when the game source directory is not adjacent to this repository. Generated assets persist in the shared `l2-studio_assets-data` Docker volume; the worker writes to it while the API and asset server read from it.

Asset imports use Wolverine 6.25.2 with PostgreSQL only. API envelopes live in
`l2_messaging_api`, Worker envelopes in `l2_messaging_worker`, and the durable
control and sequential file queues share the `l2_messaging` transport schema.
Development and the isolated test profile create these resources
automatically.

Run exactly one Studio Worker replica for this milestone. Each durable endpoint
is sequential within a Worker process; scaling the Worker horizontally would
allow different heavy files to convert concurrently.

Do not commit original source packages or generated private assets.

## Checks

Run validation through Docker from the repository root:

```sh
docker build --target validate --build-arg APP_ENV=production web
docker build --target unit-tests --file server/Dockerfile .
docker compose config
docker compose build
```

The web `validate` target runs Vitest, Nuxt type checking, and the production build. The server `unit-tests` target builds the solution and runs every project-owned server test assembly. The Compose checks validate the standalone stack and its images. The web workflow additionally runs Playwright end-to-end tests in CI.

Do not run `npm test`, `npm run typecheck`, `npm run build`, `dotnet test`, `dotnet build`, or `dotnet publish` directly on the host for normal validation.

Studio owns its database and migrations. Reset a development database after the August 2026 server reorganization and `l2-studio` database rename because Studio uses the consolidated `InitialStudioContent` migration baseline.

The per-file import migration is also a clean baseline: reset existing Studio
development databases instead of attempting to retain the retired polling-job
rows. Generated URLs now use immutable `{kind}/{source}/{sha256}` locations.

## Configuration and safety

The API uses `server/src/L2.Studio.Api/appsettings.<Environment>.json`; deployment environment variables may override settings through standard ASP.NET Core configuration. The standalone Compose model keeps Studio’s PostgreSQL and generated-asset volume local to this product.

Do not commit production credentials, tokens, original game packages, or generated private assets.
