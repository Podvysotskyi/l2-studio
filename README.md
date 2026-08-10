# L2 Studio Web

The L2 Studio product: a Nuxt web interface and .NET services for asset conversion, content inspection, validation, and publishing workflows.

## Prerequisites

- Node.js 22.13 or newer
- npm
- A GitHub Packages token with `read:packages` access, set as `NODE_AUTH_TOKEN`
- Studio API running at the configured endpoint

## Local development

```sh
export NODE_AUTH_TOKEN="$(gh auth token)"
cd web
npm ci
npm run dev
```

The Studio UI runs at <http://localhost:3001>. Override its API endpoint when necessary:

```sh
NUXT_PUBLIC_STUDIO_API_BASE=http://localhost:5101 npm run dev
```

## Docker Compose

Run the repository-local Studio and nginx asset-server stack:

```sh
docker compose up --build
```

Generated client manifests and resources live in ignored `assets/` and are served at http://localhost:5300.

Do not commit original source packages or generated private assets.

## Checks

```sh
cd web && npm test
cd web && npm run typecheck
cd web && npm run build
dotnet build server/L2.Studio.slnx
```

## Dependencies

Studio consumes explicitly pinned GitHub Packages releases of `@l2/ui` and `@l2/babylon-runtime`. Update package versions intentionally and commit the generated lockfile.
