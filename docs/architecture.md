# L2 Studio architecture

L2 Studio owns version-scoped authoring, original-resource management,
conversion, generated-asset catalogs, release publication, and its Studio
database. It does not own accounts, characters, sessions, live gameplay rules,
or authoritative Game Server outcomes.

## Shipped composition

- The Nuxt application provides Studio's authoring and operational UI. Browser
  requests use the same-origin `/api` proxy; Nuxt separately owns
  `/storage-api`.
- `L2.Studio.Api` exposes the private Studio HTTP API. Controllers orchestrate
  version-scoped repositories and durable-job requests.
- `L2.Studio.Worker` consumes Wolverine work for content imports, resource
  discovery and conversion, preview generation, release work, and
  reconciliation.
- `GameContentDbContext` and `L2.Studio.Migrations` own Studio's PostgreSQL
  schema for authored content, job history, catalogs, artifacts, and releases.
- Item definitions, item-owned lookups, sets, and recipes use the focused
  `IItemRepository`/`ItemRepository` persistence boundary. The remaining
  content-directory repository is a compatibility seam while its NPC, skill,
  player, and lookup controllers are split; it must not receive new features.
- The asset server exposes generated public output only. Original resources,
  import snapshots, and output staging are private volumes.

All authored records, jobs, catalogs, artifacts, releases, and generated paths
belong to exactly one game version. The API receives that version in its route;
repositories apply it before any identifier, filter, or paging operation.

## Ownership boundaries

| Concern | Owner | Notes |
| --- | --- | --- |
| Editable definitions and lookups | Studio API, repositories, database | Authoring data is scoped by game version. |
| Original client resources | Nuxt storage API and private resources volume | Browser mutations are restricted to these resources. |
| Durable conversion and import work | Worker and services | API queues work; the Worker owns execution and lifecycle progress. |
| Generated output, catalogs, artifacts, releases | Worker, services, repositories | Generated files are read-only to Nuxt storage endpoints. |
| Published manifest consumption | Studio inspection UI and player Web | Consumers use the published files, not Studio persistence. |
| Runtime gameplay | L2 Server and player Web | Studio does not become a game authority. |

## Import, catalog, and release lifecycle

Content reconciliation and asset conversion use the shared `import_jobs`
history, status vocabulary, timestamps, abandonment handling, and browser
query API. Asset jobs additionally retain per-file work items and diagnostics.
`add_missing` preserves existing authored values; `restore_defaults` restores
source-backed values for the selected target while preserving custom-only rows.

An artifact is immutable for its game version, normalized source key, build
fingerprint, outputs, and dependencies. Catalog state selects the current
artifact for a source identity. A release captures a coherent artifact graph and
entry points. Publishing writes an immutable release manifest and activation
atomically changes only `versions/{version}/current.json`. Server discovers the
published pointer; it does not query Studio's database.

## Cross-product contracts

Published map, scene, texture, mesh, animation, NPC-appearance, and release
manifests are independent consumer contracts. Studio's Three.js inspection
adapters and player Web's Babylon runtime may interpret the same published
format, but do not share runtime code or rendering-fidelity requirements.
Changing a published format requires versioning the format and updating every
consumer; the Studio API refactor must not change those formats.

See [web architecture](web-architecture.md) for browser ownership and
[asset pipeline](asset-pipeline.md) for the detailed generated-output contract.
