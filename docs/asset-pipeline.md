# Asset pipeline

Studio converts lawful local Lineage II resources into version-scoped browser
assets. Original packages and generated private assets are never committed.

## Sources and processing

The Studio file manager writes original resources beneath the selected
version's source root in the private resources volume. Import requests identify
a supported kind, version, and contained source; callers cannot submit arbitrary
host paths.

Wolverine uses PostgreSQL durable queues plus separate API and Worker
inbox/outbox schemas. A run discovers source files and creates independently
tracked work items. Delivery is at least once, handlers are idempotent, and one
Worker replica processes heavy file work sequentially for the current
milestone. Diagnostics and discovered, completed, succeeded, warning, and failed
counts are stored as queryable rows.

Supported asset families currently include textures, static meshes, sounds,
music, maps, scenes, and generated map previews. Conversion failures are
isolated to their source and remain visible in the catalog and diagnostics.

## Artifacts and releases

Each successful source produces an immutable artifact identified by its game
version, kind, normalized source key, build fingerprint, output files, and
dependencies. Publication updates the active catalog slice in a short database
transaction; superseded artifacts remain registered and may remain on disk.
Integrity checks compare registered files with generated output.

Generated outputs retain the source hierarchy directly below their version
root: `versions/{version}/{source-directory}/{source-file}/{build-fingerprint}`.
For example, texture packages from `System Textures/` and `Textures/` publish
directly beneath those folders; map, scene, and preview sources publish beneath
`Maps/`.

A release selects a coherent version-scoped artifact graph and its client entry
points. Publishing writes an immutable `client-manifest.json`; activation
atomically replaces only `versions/{version}/current.json`. Rolling back means
activating an earlier published release. Consumers must not infer live content
from mutable catalog state or convention paths when the release manifest
provides an explicit file.

## Stable media rules

- Texture imports retain source identity and material relationships. Browser
  fallback images are lossless WebP; supported native DXT mip chains may also
  be published in KTX containers for capable GPUs. Texture manifests preserve
  authored masking, alpha, two-sided, detail-map, and clamp behavior, and record
  transparency found in decoded pixels for direct-texture mesh sections.
- Static meshes are published as GLB with standard geometry plus material
  metadata consumed by Web and Studio. Studio resolves published WebP texture
  channels independently so an unavailable material falls back per section
  without preventing geometry inspection.
  Maps and scenes publish complete render manifests whose terrain texture and
  control-map contracts are consumed independently by both renderers. Map
  manifests also retain UE2 `LevelSummary` browser metadata (including the raw
  screenshot material reference) without converting it into a preview image.
- Embedded UAX PCM payloads are published as RIFF/WAVE without lossy
  transcoding.
- Music accepts either the proprietary `L2SD` first-page signature or standard
  `OggS`. `L2SD` is restored to `OggS`, every Ogg page and the Vorbis identity
  are validated, and a present 20-byte Lineage trailer is removed. Standard
  trailer-free Ogg remains unchanged.
- Unsupported objects and material or scene features produce explicit
  diagnostics and fall back locally instead of invalidating unrelated assets.

Format changes advance the owning recipe or manifest version and require
consumer compatibility tests. Historical fixture counts and one-time schema
upgrade instructions belong in Git history, not this living contract.
