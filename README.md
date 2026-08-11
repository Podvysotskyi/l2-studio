# L2 Studio Web

The L2 Studio product: a Nuxt web interface and .NET services for asset conversion, content inspection, validation, and publishing workflows.

## Prerequisites

- Node.js 22.13 or newer
- npm

## Local development

```sh
docker compose up --build
```

The Studio UI runs at <http://localhost:3001>; generated assets are served at <http://localhost:5300>. Nuxt proxies all browser `/api` requests to the internal Studio API. Set `NUXT_STUDIO_API_BASE` only for the Nuxt server and use `NUXT_PUBLIC_ASSET_BASE_URL` for the browser asset origin.

## Docker Compose

Studio has a standalone development stack containing PostgreSQL, the Studio API and worker, the Studio web application, nginx asset serving, and the preview browser:

```sh
docker compose up --build
```

Set `L2_SOURCE_PATH` when the game source directory is not adjacent to this repository. Generated assets persist in the shared `l2-studio_assets-data` Docker volume; the worker writes to it while the API and asset server read from it.

Do not commit original source packages or generated private assets.

## Checks

```sh
docker build --target validate --build-arg APP_ENV=production web
docker compose config
```

The .NET solution is self-contained. Production projects are organized by responsibility under `server/src` (API and Worker hosts, configuration, contracts, context and migrations, repositories, import services, and conversion libraries). Studio does not reference Server implementation projects.

Resetting a development database is required after the August 2026 server reorganization and the `l2-studio` database rename because Studio now uses a consolidated `InitialStudioContent` migration baseline.

## Dependencies

Studio owns its browser contracts and Babylon.js rendering helpers under `web/app/types` and `web/app/runtime`, so its web build does not require the shared L2 UI or runtime packages.

The Nuxt application keeps route files thin. Feature UI lives under
`web/app/components/pages`, browser requests are centralized in
`web/app/services`, server-backed state lives in Pinia stores, and Babylon.js
runtime code is grouped into `core`, `effects`, `materials`, and `scene`
domains. `web/app/types/studio.ts` remains a compatibility barrel over the
domain contracts in `web/app/types/models`.
