# L2 Studio architecture

L2 Studio owns game-content authoring, original-resource management, asset
conversion, generated artifacts, release publication, and the persistence that
supports those workflows. It does not own accounts, characters, sessions, or
authoritative gameplay outcomes.

## Product composition

- The Nuxt application owns authoring and operational interfaces. Browser API
  calls go through its same-origin `/api` proxy, while `/storage-api` owns
  version-scoped original-resource mutations.
- The ASP.NET Core API owns browser contracts and request orchestration.
- The Worker owns durable imports, conversion, preview generation, artifact
  publication, integrity reconciliation, and release output.
- `GameContentDbContext` and Studio migrations own authored content, import
  state, catalogs, artifacts, and releases in Studio's PostgreSQL database.
- The asset server exposes only generated public output. Original resources and
  Worker staging remain private.

## Import jobs and content directories

Content and asset imports share the `import_jobs` lifecycle, status vocabulary,
timestamps, abandonment handling, and query API. Category-specific columns and
asset work items remain on the same EF hierarchy, while the browser consumes a
single paged history contract.

Every content directory imports one explicit target. `add_missing` preserves
all existing values; `restore_defaults` overwrites source-backed values for the
target but preserves custom-only rows. Handlers may insert missing dependency
lookups, but never restore a dependency as a side effect. The Nuxt directory
layout owns the standard header, actions, confirmation modal, dismissible import
progress drawer, refresh behavior, and table region. Individual pages own columns,
filters, editing, and whether pagination is enabled.

## Cross-product boundaries

Every authored and generated record is scoped by a stable game-version key.
Studio publishes immutable release manifests beneath
`versions/{version}/releases/{release-id}/` and atomically activates one through
`versions/{version}/current.json`.
Individual generated artifacts retain their source hierarchy immediately below
`versions/{version}/`, rather than adding an import-kind directory.

Server discovers that pointer but does not query Studio persistence. Any future
game-content ingestion uses an explicit receiving contract owned by Server.
Web owns a local browser representation of published manifests and resolves
their resources through the configured asset origin. Studio owns a separate
Three.js renderer for static-mesh material inspection, skeletal-animation
playback and notify-timeline inspection, authored terrain layers, and generated
map previews. It validates the same published formats without
sharing rendering code or fidelity requirements with Web's Babylon runtime.

See [asset-pipeline.md](asset-pipeline.md) for import, artifact, release, and
media compatibility contracts.
