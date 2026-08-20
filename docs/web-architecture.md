# Studio web architecture

## Shipped request and version flow

```
Browser ──same-origin /api──> Nuxt proxy ──private NUXT_STUDIO_API_BASE──> Studio API
Browser ──same-origin /storage-api──> Nuxt server storage seam
Browser ──public asset URL──> asset server (generated output only)
```

`NUXT_STUDIO_API_BASE` is server-only. Browser code calls `/api` through web
services; it does not receive an upstream API URL. `NUXT_PUBLIC_ASSET_BASE_URL`
is only for resolving published browser-readable assets.

Today `services/studio-api.ts` is a monolithic façade. Its version-scoped calls
read the selected game-version key from browser local storage. The global
game-version Pinia store owns discovery, validation, persistence, and the
selector. This is the current behavior, not the target API boundary.

## Version and storage isolation

The server API path is `/api/game-versions/{gameVersion}/…`; all Studio data
operations are version scoped. Nuxt storage resolves the selected version to
the matching private source folder or public generated-asset folder. Mutation
routes call `requireResourceStorage`, so only original resources may be
written, moved, created, or deleted. Generated assets remain read-only.

## UI and runtime ownership

`pages` contain route-level composition, although some existing page sections
still parse routes, load data, poll jobs, and keep directory state locally.
`components/app` contains shared shells; `components/pages` contains feature
sections; Pinia stores hold several reusable directories and operational state;
`services` make HTTP and published-asset calls. This mixed ownership is the
reason for the planned migration in the roadmap.

Map and scene inspection currently combine feature state and Three.js runtime
work in large page sections. Three.js renderers are Studio-only runtime
adapters. They consume unchanged published manifests and must not be coupled to
the player Web Babylon runtime.

## Contracts and dependencies

The C# `L2.Studio.Contracts` project owns the private browser API shapes.
TypeScript mirrors those shapes manually under `web/app/types`; contract
generation is not in use. Published manifest types remain separate because
they are cross-product asset contracts, not Studio API contracts.

Dependencies point inward: pages compose state; feature state calls capability
clients; UI components receive typed props and emit user intent; runtime
adapters receive prepared published data. Components must not call the upstream
API or perform implicit version lookup. The completed target replaces the
monolith with explicit version-scoped capability clients for content, catalogs,
imports, releases, and storage.

`useStudioApiError` is the shared browser error boundary for Studio API calls.
It translates ASP.NET Core `ProblemDetails` and `ValidationProblemDetails` into
a page message and named field messages. Authoring forms clear it before a
request, capture a rejected request with operation-specific fallback text, and
bind named messages to their form fields. Unknown network failures retain the
fallback rather than exposing transport details.

## Testing status and target

The tracked web suite uses three execution seams: Node Vitest tests under
`web/test/unit`, Nuxt-runtime tests under `web/test/nuxt`, and focused
mocked-API Playwright journeys under `web/test/e2e`. The web Docker `validate`
target runs the policy guard, Node/Nuxt suites, type checking, and a production
build; the `browser-tests` target runs Playwright separately.

Target coverage adds Node unit tests for pure utilities and clients,
Nuxt-runtime tests for route pages/components, and focused browser journeys
using the [Nuxt testing guide](https://nuxt.com/docs/4.x/getting-started/testing).
Each feature family needs request URL/query/body, stale-response, error,
route-state, and relevant inspector/runtime behavior coverage. Tests use module
interfaces; only the dedicated storage-handler test may read the filesystem.
