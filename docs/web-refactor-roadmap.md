# Studio web and API refactor roadmap

This is a target-state roadmap. A checkbox becomes complete only when the code,
contracts, focused tests, and relevant Docker validation are shipped together.

## Current implementation

- One `services/studio-api.ts` façade contains system, content, catalog, import,
  and release calls. Version-scoped URLs obtain their key via a hidden
  local-storage helper.
- `ContentDirectoryController` owns items, NPCs, skills, player authoring, and
  all related lookups under one version-scoped route root.
- Some directories already use Pinia stores with stale-response counters, but
  several page sections own route parsing, loading, filtering, polling, and
  selection locally.
- Map and scene inspection behavior is present, with large page feature
  components that mix feature lifecycle and rendering coordination.
- Nuxt storage already is a dedicated server seam and already restricts
  mutation to original resources.

## Target architecture

- Capability clients: system/game versions, content authoring, asset catalog,
  imports, releases, and storage. `createStudioVersionClient(gameVersion)`
  receives context explicitly; no API client reads local storage.
- TypeScript browser requests and responses mirror named C# contracts. Mutation
  bodies are named request types, never inline structures or `Record` payloads.
  Published-manifest types remain distinct.
- Aggregate-owned API controllers: items, NPCs, skills, player authoring, and
  lookups, all beneath the existing version-scoped API root.
- Route pages own query parsing/synchronization and feature composition;
  focused stores/composables own shared async state and stale-response
  protection. Presentation components use typed props/events.
- Map and scene inspector modules own their loading, selection, filtering,
  paging, preview polling, and cleanup. Three.js remains isolated runtime
  adapters. Manifest formats do not change.

## Migration slices

| Slice | Scope | Status |
| --- | --- | --- |
| 0 | Preserve storage isolation and published formats; document the actual baseline | Complete — existing storage checks and published contracts retained; documentation refreshed in this change. |
| 1 | Named C#/TypeScript API contracts, explicit version client factory, item-condition array response, and aggregate controller split | In progress — the item persistence slice now has an `IItemRepository`/`ItemRepository`; the legacy directory repository delegates item operations while its controller is still unsplit. NPC, skills, players, and shared lookup ownership remain planned within this slice. |
| 2 | Content and pipeline directories: route-owned query/loading and focused feature state | Planned. |
| 3 | Asset-library directories and import/release operational views | Planned. |
| 4 | Map and scene inspector feature modules and runtime-adapter seams | In progress — map, scene, and NPC spawn-map route pages lazy-load their heavy features; renderer adapters dynamically load Three.js implementations. Further feature-module decomposition remains planned. |
| 5 | Behavioral testing migration: client, state, route, Nuxt-runtime, and browser journeys | In progress — Node and Nuxt-runtime runners plus focused mocked-API browser journeys are tracked. Every page family still needs its owned behavioral test before this slice is complete. |

For every slice, run focused tests first. Web work requires the web Docker
validation target; server work requires unit tests plus API and Worker production
targets. Coordinated API/web or storage work also requires Compose config and
build validation.
