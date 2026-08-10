# Repository Guidelines

## Scope

This repository owns the Studio Nuxt frontend plus Studio API, Worker, content authoring, and conversion tooling. Server runtime persistence remains outside this repository.

## Commands

```sh
cd web && npm ci
cd web && npm test
cd web && npm run typecheck
cd web && npm run build
dotnet build server/L2.Studio.slnx
```

Use `NODE_AUTH_TOKEN` for private package installation. Container orchestration lives in the `l2-infra` repository.

## Conventions

Use UTF-8, LF endings, two-space indentation, single quotes, no semicolons, and no trailing commas. Keep the API endpoint configurable through `NUXT_PUBLIC_STUDIO_API_BASE`. Do not commit original game sources or ignored generated assets.
