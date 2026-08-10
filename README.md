# L2 Studio Web

The Nuxt web interface for L2 asset conversion, content inspection, validation, and publishing workflows.

## Prerequisites

- Node.js 22.13 or newer
- npm
- A GitHub Packages token with `read:packages` access, set as `NODE_AUTH_TOKEN`
- Studio API running at the configured endpoint

## Local development

```sh
export NODE_AUTH_TOKEN="$(gh auth token)"
npm ci
npm run dev
```

The Studio UI runs at <http://localhost:3001>. Override its API endpoint when necessary:

```sh
NUXT_PUBLIC_STUDIO_API_BASE=http://localhost:5101 npm run dev
```

## Docker Compose

```sh
export NODE_AUTH_TOKEN="$(gh auth token)"
docker compose up --build
```

Compose mounts derived browser assets from the ignored `assets/` directory. Do not commit original source packages or generated private assets.

## Checks

```sh
npm test
npm run typecheck
npm run build
```

## Dependencies

Studio consumes explicitly pinned GitHub Packages releases of `@l2/ui` and `@l2/babylon-runtime`. Update package versions intentionally and commit the generated lockfile.
