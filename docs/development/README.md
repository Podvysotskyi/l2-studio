# Extending L2 Studio

This is the canonical implementation guide for new Studio functionality. Use
it with [architecture.md](../architecture.md), [web-architecture.md](../web-architecture.md),
and [asset-pipeline.md](../asset-pipeline.md). The current implementation has
older seams; [the roadmap](../web-refactor-roadmap.md) identifies those seams
and the target pattern. Do not document or copy a planned pattern as shipped
behavior until its slice is complete.

## Contents

- [Choose the owning surface](#choose-the-owning-surface)
- [Rules shared by every change](#rules-shared-by-every-change)
- [Preferred existing building blocks](#preferred-existing-building-blocks)
- [Build a vertical slice](#build-a-vertical-slice)
- [Validation matrix](#validation-matrix)
- [Definition of done](#definition-of-done)

## Choose the owning surface

| Change | Read next | Primary owner |
| --- | --- | --- |
| Page, component, browser state, browser contract, or browser request | [web.md](web.md) | `web/app` |
| HTTP endpoint, entity, repository, configuration, or migration | [server.md](server.md) | `server/src/L2.Studio.*` |
| Durable work, imports, conversion, reconciliation, or startup work | [jobs.md](jobs.md) | `L2.Studio.Messages`, API, Worker, repositories, and services |
| Published asset shape or conversion behavior | [jobs.md](jobs.md) and [asset-pipeline.md](../asset-pipeline.md) | Worker, `L2.Studio.Services`, and `L2.Tools.*` |
| Cross-product runtime behavior | The owning Server or Web repository guide | Not Studio unless the contract is authored or published here |

Use a synchronous endpoint when work is short, atomic, and meaningful to the
caller immediately. Use a durable job when work needs retry, progress,
concurrency control, audit history, or can outlive one HTTP request. Use a
hosted service only for bounded process-lifecycle work such as startup
reconciliation or abandoned-job recovery.

## Rules shared by every change

- Scope authored records, jobs, catalogs, and generated output by the stable
  game-version key. Keep global endpoints limited to genuinely global data,
  such as game-version discovery and service information.
- Keep browser calls behind the web service layer and Nuxt's same-origin
  `/api` proxy. Never expose the upstream Studio API base to browser code.
- Keep C# browser contracts and their TypeScript representations structurally
  aligned. Contract generation is not used, so update and test both sides in
  the same change.
- Keep API and Worker hosts thin. Put host composition, persistence,
  observability, messaging, health checks, and registrations in
  `L2.Studio.Configurations`.
- Keep database reads and writes behind repository abstractions. Put
  orchestration, conversion, manifests, previews, and asset processing in
  services or Worker handlers rather than controllers.
- Keep one same-named top-level C# declaration per file. Generated migrations,
  host metadata, private nested helpers, and intentional partial fragments are
  the documented exceptions.
- Preserve original game resources, generated private assets, credentials, and
  production endpoints outside version control.
- Run product checks through Docker. Do not use host-installed Node.js, npm,
  .NET, or EF tooling for normal development.

Do not treat every older implementation as a template. Some existing reusable
page components own local search and paging state, while the canonical pattern
for new searchable directories is URL synchronization plus a Pinia Setup
Store. Large existing controllers and repositories may use intentional partial
files; that does not justify unrelated types or multiple top-level declarations
in one file.

## Preferred existing building blocks

| Need | Reuse |
| --- | --- |
| Standard title, description, icon, and actions | `StudioPageHeader` |
| Content directory header, import controls, progress, errors, and refresh | `StudioContentDirectoryLayout` |
| Search, filters, table, responsive slots, and pagination | `StudioDataTable` |
| View, edit, and delete controls | `StudioTableRowActions` |
| Destructive confirmation or small string edit | `useStudioDialogs` |
| Success and failure feedback | `useStudioToasts` |
| Shareable directory search, filters, and paging | `useDirectoryRouteSync` |
| Loading all values from a paged lookup | `loadDirectoryOptions` |
| Universal content/asset job progress | `StudioImportProgressDrawer` and import-job utilities |

Use the item directory as the preferred end-to-end example of a paged,
filterable content directory. Use the NPC detail route as the example for a
route-owned detail view with nested sections. Use the universal import-job flow
as the example for user-visible durable work.

## Build a vertical slice

Implement a new cross-surface feature in this order:

1. Confirm Studio ownership and game-version behavior.
2. Define the public C# request, response, and model shapes.
3. Add or change annotated EF entities, Fluent relationship/index mapping, and
   an additive migration when persistence changes.
4. Extend the owning repository abstraction and implementation, including
   projection, paging, conflict behavior, and cancellation.
5. Add a thin controller action with normalization, validation, and explicit
   HTTP result mapping.
6. For durable work, add the job record, message, outbox publication, routing,
   handler, lifecycle transitions, recovery, and progress contract.
7. Mirror browser contracts, add the service function, and test its exact
   same-origin URL, method, query, and body.
8. Add the Pinia Setup Store, route synchronization, page component, thin route
   page, navigation entry, and route title as applicable.
9. Add tests in every owning test project instead of concentrating the entire
   slice in one integration-style test.
10. Update architecture or asset-pipeline documentation only when a boundary or
    published contract changed.

For server-only or web-only work, follow the same order but omit unaffected
layers. Do not add placeholder layers that have no responsibility.

## Validation matrix

| Changed surface | Required checks |
| --- | --- |
| Web page, component, store, service, contract, utility, or Nuxt route | Web `validate` Docker target |
| API, Worker, entity, repository, service, tool, migration, or server configuration | Server `unit-tests`, `api-production`, and `worker-production` Docker targets |
| Compose, storage, host, asset path, or end-to-end configuration | Relevant web/server checks plus `docker compose config` and `docker compose build` |
| Studio skill | Skill `quick_validate.py` |

The exact commands are maintained in the repository `AGENTS.md`. Add focused
tests before running the aggregate targets:

- Web service tests for exact URLs and serialized inputs.
- Store and utility tests for state, stale-response protection, and route
  conversions.
- API tests for validation, normalization, delegation, and HTTP status mapping.
- Context tests for table, key, relationship, index, default, and provider type
  metadata.
- Repository tests for filters, projections, updates, conflicts, and paging.
- Worker tests for dispatch, reconciliation, lifecycle transitions, retry-safe
  behavior, and generated catalog integrity.
- Configuration tests for every new registration or option.

## Definition of done

- The feature has one clear owner and follows the selected synchronous, durable
  job, or hosted-service model.
- Every query and mutation is correctly game-version scoped.
- Public C# and TypeScript contracts agree.
- Pages use existing shells, feedback, table, and route-state primitives where
  their responsibilities match.
- Endpoint validation and failure responses are explicit and tested.
- Persistence changes include model tests and an additive migration.
- Durable jobs are observable, concurrency-safe, recoverable, and idempotent.
- Registrations, navigation, route titles, documentation, and owning tests are
  updated.
- Docker validation passes and no private source material or secrets are
  tracked.
