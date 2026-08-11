# Repository Guidelines

## Scope

This repository owns the Studio Nuxt frontend plus Studio API, Worker, content authoring, and conversion tooling. Server runtime persistence remains outside this repository.

## Commands

```sh
docker compose up --build
docker build --target validate --build-arg APP_ENV=production web
```

`compose.yaml` owns the standalone Studio stack. Browser code calls the same-origin `/api` proxy. Keep `NUXT_STUDIO_API_BASE` private to Nuxt and use `NUXT_PUBLIC_ASSET_BASE_URL` only for browser-readable generated assets.

## Architecture

- Keep .NET production projects in `server/src` and build configuration and Dockerfile in `server/`.
- Keep Nuxt source in `web/app`, browser API calls in `web/app/services`, state in Pinia setup stores, and tests under `web/test`.
- `L2.Tools.*` conversion libraries are Studio solution projects under `server/src`; preserve their public names and namespaces.
- Keep migration and seed data in `L2.Studio.Migrations`, repositories limited to runtime persistence, and Worker/API hosts thin.

## Conventions

Use UTF-8, LF endings, two-space indentation, single quotes, no semicolons, and no trailing commas. Do not commit original game sources or ignored generated assets.
