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

Asset runs participate in Studio's universal import-job history together with
content reconciliation runs. Asset-specific work items and diagnostics remain
available when a universal job is opened; the common job record owns category,
target, operation, status, lifecycle timestamps, aggregate progress, and errors.

Supported asset families currently include textures, static meshes, Chronicle
1 skeletal animations and NPC appearances, sounds, music, maps, scenes, and generated map previews. Conversion failures are
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
  without preventing geometry inspection. UE2 brightness-based translucent
  shaders publish their diffuse texture as a luminance opacity channel,
  including the same flipbook timeline; explicit opacity maps use decoded
  alpha when present and luminance otherwise. A normal Shader that reuses its
  diffuse texture as a specularity mask remains opaque: decoded alpha is kept
  for the mask and is not promoted to surface opacity.
  Maps and scenes publish complete render manifests whose terrain texture and
  control-map contracts are consumed independently by both renderers. Map
  manifests also retain UE2 `LevelSummary` browser metadata (including the raw
  screenshot material reference) without converting it into a preview image,
  plus `PlayerStart` transforms for Studio inspection. Authored BSP water
  surfaces use their published material graph and live texture or UV animation;
  gameplay water-volume meshes remain optional diagnostics, default to hidden
  in the map inspector, and are omitted from generated map previews.
  PlayerStart data is not authoritative Game Server spawn configuration.
- Chronicle 1 `.ukx` packages publish skeletal meshes, skin weights, skeletons,
  resolved per-section default materials, and reusable compatible animation
  sets as browser-playable GLB. Animation
  sets bind by exact runtime bone name and are linked when at least 95 percent
  of animation bones exist on the mesh; lower-coverage rigs remain unlinked and
  report their matched-bone counts as warnings. Schema 2 animation manifests
  retain ordered default-material references, resolution diagnostics, sequence
  groups, and typed notify timelines for inspection. Studio
  visualizes notify timing and metadata but does not execute notify sounds,
  effects, or functions. The GLB conversion changes UE2's left-handed Z-up
  coordinates to glTF's right-handed Y-up coordinates, converts centimeters to
  meters, and applies the UE2 root-bone quaternion convention consistently to
  bind poses and clips. Unsupported `VertMesh` exports and malformed skeletal
  objects are isolated as warnings without invalidating the rest of a package.
- Chronicle 1 `system/npcgrp.txt` publishes one immutable, normalized NPC
  appearance manifest per Mobius NPC at `npcs/{npcId}/manifest.json`. Studio
  resolves each Mobius `displayId` through `CT0_to_C4_ids.txt` before matching
  it to the client appearance ID, so NPC aliases receive separate manifests
  while retaining their shared `appearanceId`. Mesh and sound values retain
  their source references and include URLs when exactly one active asset
  resolves them. Schema 6 mesh
  references additionally retain the compatible animation-set URL used by the
  Studio NPC preview. Raw texture slots retain the ordered `npcgrp` overrides,
  while material slots record each skeletal section's default, override,
  effective material, provenance, and fallback warning. Resolved overrides
  replace their matching section; absent or unresolved overrides preserve the
  skeletal default. Texture lookup
  prefers an exact Unreal object path,
  then accepts a unique package-local final object segment so grouped C1 exports
  such as `LineageNpcsTex.Box.coffer_a_t00` resolve the shorter `npcgrp`
  reference. Missing, ambiguous, or incomplete material graphs remain visible
  with diagnostics and do not block publication. Effect references remain
  unresolved until an effect asset importer exists. The catalog stores only
  manifest summary metadata, match counts, and a compact Mobius NPC ID index,
  not one row per NPC.
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
