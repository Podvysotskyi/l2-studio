# L2 Studio Web

The L2 Studio product: a Nuxt web interface and .NET services for asset conversion, content inspection, validation, and publishing workflows.

## Prerequisites

- Node.js 22.13 or newer
- npm
- A GitHub Packages token with `read:packages` access, set as `NODE_AUTH_TOKEN`

## Local development

```sh
export NODE_AUTH_TOKEN="$(gh auth token)"
docker compose up --build
```

The Studio UI runs at <http://localhost:3001>; generated assets are served at <http://localhost:5300>. Nuxt proxies all browser `/api` requests to the internal Studio API. Set `NUXT_STUDIO_API_BASE` only for the Nuxt server and use `NUXT_PUBLIC_ASSET_BASE_URL` for the browser asset origin.

## Docker Compose

Studio has a standalone development stack containing PostgreSQL, the Studio API and worker, the Studio web application, nginx asset serving, and the preview browser:

```sh
docker compose up --build
```

Set `L2_SOURCE_PATH` when the game source directory is not adjacent to this repository. The root `l2-infra` Compose model remains available for integration testing.

Do not commit original source packages or generated private assets.

## Checks

```sh
docker compose --profile test run --rm server-tests
docker build --target validate --build-arg APP_ENV=production --secret id=npm_token,env=NODE_AUTH_TOKEN web
docker compose config
```

The .NET solution is self-contained. Production projects are organized by responsibility under `server/src` (API and Worker hosts, configuration, contracts, context and migrations, repositories, import services, and conversion libraries); matching test projects live under `server/tests`. Studio does not reference Server implementation projects.

Resetting a development database is required after the August 2026 server reorganization because Studio now uses a consolidated `InitialStudioContent` migration baseline.

## Dependencies

Studio consumes explicitly pinned GitHub Packages releases of `@l2/ui` and `@l2/babylon-runtime`. Update package versions intentionally and commit the generated lockfile.
