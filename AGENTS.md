# Repository Guidelines

## Scope

This repository owns only the Studio Nuxt frontend. Studio API, worker, source-file access, conversion logic, and database migrations remain in the backend repository.

## Commands

```sh
npm ci
npm test
npm run typecheck
npm run build
```

Use `NODE_AUTH_TOKEN` for private `@l2` package installation. Run `docker compose up --build` for a containerized development server.

## Conventions

Use UTF-8, LF endings, two-space indentation, single quotes, no semicolons, and no trailing commas. Keep the API endpoint configurable through `NUXT_PUBLIC_STUDIO_API_BASE`. Do not commit original game sources or ignored generated assets.
