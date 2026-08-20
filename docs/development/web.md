# Studio web extension guide

Use this guide for Nuxt pages, components, Pinia state, browser contracts,
services, storage routes, and rendering consumers. For the shipped baseline and
the deliberately staged target structure, read
[web architecture](../web-architecture.md) and the
[refactor roadmap](../web-refactor-roadmap.md) first.

## Contents

- [Place responsibilities](#place-responsibilities)
- [Choose a page shape](#choose-a-page-shape)
- [Build a directory page](#build-a-directory-page)
- [Use shared UI](#use-shared-ui)
- [Call APIs and model contracts](#call-apis-and-model-contracts)
- [Register routes and navigation](#register-routes-and-navigation)
- [Test web changes](#test-web-changes)

## Place responsibilities

| Location | Responsibility |
| --- | --- |
| `pages` | Route parsing, URL synchronization, initial loading, store composition, and nested-route composition |
| `components/app` | Product-wide shells and reusable interaction primitives |
| `components/pages` | Substantial feature or page sections with explicit props and events |
| `stores` | Shareable asynchronous feature state in Pinia Setup Stores |
| `services` | All HTTP calls and published-asset loading |
| `types/models`, `types/requests`, `types/responses` | Browser representations of public contracts |
| `composables` | Vue lifecycle/state behavior reused across features |
| `utils` | Pure conversions, route/query mapping, and formatting |
| `runtime` | Three.js rendering and published-format consumption |
| `web/server/routes/storage-api` | Version-scoped server-side file storage operations |

Components must not call the upstream Studio API directly. Use a service
function, and let Nuxt proxy `/api/**` using the private
`NUXT_STUDIO_API_BASE`. Use `NUXT_PUBLIC_ASSET_BASE_URL` only for generated
resources intended for browsers.

## Choose a page shape

Use one of these shapes based on behavior:

1. **Searchable directory:** a thin route page composes a Pinia Setup Store,
   binds its refs, and uses `useDirectoryRouteSync`. A page component receives
   rows/state through props and models, emits refresh, and owns row actions.
2. **Detail route:** the page validates the route identifier, owns loading and
   not-found/error state, and composes substantial sections. Nested detail tabs
   use child pages through `NuxtPage` when each tab has a stable URL.
3. **Reusable operational view:** a route page may render a single substantial
   component when that component is already the complete reusable feature.

Use component-local state for modal forms, selection, transient progress, and
details that do not belong in a shareable URL. Do not use component-local state
for the query, filters, page, or page size of a new directory.

## Build a directory page

The canonical route composition is:

```vue
<script setup lang="ts">
import { storeToRefs } from 'pinia'
import { useDirectoryRouteSync } from '~/composables/use-directory-route-sync'
import { useThingDirectoryStore } from '~/stores/thing-directory'

const store = useThingDirectoryStore()
const { things, total, query, page, pageSize, loading, error } = storeToRefs(store)

useDirectoryRouteSync(
  '/authoring/things',
  { query, page, pageSize },
  store.load
)
</script>

<template>
  <ThingDirectory
    v-model:query="query"
    v-model:page="page"
    v-model:page-size="pageSize"
    :things="things"
    :total="total"
    :loading="loading"
    :error="error"
    @refresh="store.load"
  />
</template>
```

Add filter refs through `useDirectoryRouteSync` options. Put route parsing and
serialization in a pure utility when filters require more than direct strings.
Reset the page to one whenever search, filter, or page size changes. Search is
debounced by the route-sync composable.

Use a Setup Store with stale-response protection:

```ts
export const useThingDirectoryStore = defineStore('thing-directory', () => {
  const things = ref<ThingRecord[]>([])
  const total = ref(0)
  const query = ref('')
  const page = ref(1)
  const pageSize = ref(25)
  const loading = ref(true)
  const error = ref<string>()
  let requestVersion = 0

  async function load() {
    const version = ++requestVersion
    loading.value = true
    error.value = undefined
    try {
      const response = await getThingDirectory({
        query: query.value,
        page: page.value,
        pageSize: pageSize.value
      })
      if (version !== requestVersion) return
      things.value = response.items
      total.value = response.total
    } catch {
      if (version === requestVersion)
        error.value = 'The thing directory could not be loaded.'
    } finally {
      if (version === requestVersion) loading.value = false
    }
  }

  return { things, total, query, page, pageSize, loading, error, load }
})
```

The page component should use `StudioContentDirectoryLayout` for content that
supports source imports and `StudioDataTable` for search, filters, responsive
rows, and paging. Configure columns in script, expose specialized cells through
slots, and use `StudioTableRowActions` for consistent row controls.

## Use shared UI

- Use `StudioPageHeader` for every top-level page header. Put primary actions in
  its actions slot.
- Use `StudioContentDirectoryLayout` for standard content imports, refresh,
  import status, errors, confirmation, and the progress drawer.
- Use `useStudioDialogs` instead of duplicating confirmation and prompt modals.
- Use `useStudioToasts` for mutation success and failure. Keep load failures as
  persistent page alerts with a retry path.
- Use `useStudioApiError` for a form or page API failure. Call `clear()` before
  a request, `capture(cause, fallback)` in its catch block, show `pageError` in
  the alert, and bind `fieldError('requestProperty')` to the corresponding
  `UFormField`. Do not parse `$fetch` errors in individual components.
- Confirm destructive actions and explain dependency conflicts or restoration
  behavior. Disable or show loading on the exact action in progress.
- Supply explicit search placeholders, accessible labels, empty states, and
  human-readable fallback values.
- Use the mobile slot on `StudioDataTable` when a wide table cannot remain
  understandable on a small screen.
- Keep TypeScript/Vue formatting at two spaces, single quotes, no semicolons,
  and no trailing commas.

## Call APIs and model contracts

Add browser calls to the appropriate service rather than pages or components.
The legacy `studio-api.ts` façade still uses `versionPath` and reads selected
browser storage; do not add new calls to it. New capability clients receive a
`createStudioVersionClient(gameVersion)` from route or feature state and build
`/api/game-versions/{version}/...` from that explicit context.

```ts
export function createThingApi(client: StudioVersionClient) {
  return {
    getDirectory(request: DirectoryRequest = {}) {
      return $fetch<DirectoryPage<ThingRecord>>(client.path('/content/things'), {
        query: directoryQuery(request)
      })
    }
  }
}
```

- Trim optional search/filter strings at the service boundary and omit empty
  values.
- Encode path identifiers or individual path segments. Do not encode `/`
  separators when the route intentionally accepts a relative resource path.
- Specify the response type, HTTP method, body, and query explicitly.
- Keep models, requests, and responses in their corresponding `types`
  directory. Name every mutation request after its C# contract; do not use
  inline structural bodies or `Record` payloads. Reuse generic
  `DirectoryPage<T>` and shared job types.
- Mirror C# property optionality and nullability exactly. ASP.NET Core JSON uses
  camelCase browser properties.
- Resolve generated asset URLs through published-asset utilities; do not
  concatenate the public asset origin in page components.
- Use `storage-api.ts` only for Nuxt-owned file operations. Mutations must remain
  limited to original-resource storage; generated assets are read-only.
- Keep game-version context outside route parameters unless a cross-version URL
  is an explicit sharing workflow. A selection change persists the selected key,
  calls `resetVersionScopedState()`, and keys the active page subtree by that
  key so local inspector state and version-scoped Pinia state cannot leak into
  the next version.
- Load renderer implementations with dynamic imports from their `.client.vue`
  adapters. Route pages lazy-load map, scene, and spawn-map inspectors; do not
  import the runtime barrel into a browser feature.

## Register routes and navigation

Nuxt derives routes from `web/app/pages`. For a user-visible destination also:

1. Add the item to the correct group in `studio-navigation.ts`.
2. Extend `studioRouteGroup` when introducing a new top-level route family.
3. Add list/detail title handling to `studioRouteTitle`.
4. Extend navigation tests with the canonical route and removed aliases, if
   any.
5. Keep internal rendering/capture routes out of the sidebar.

Prefer workflow-oriented paths: `/authoring` for editable content, `/library`
for generated asset browsing, `/pipeline` for imports and releases, and
`/storage` for resource and artifact storage.

## Test web changes

- Service tests assert the exact same-origin URL, encoded path, method, query,
  request body, and default paging.
- Store tests cover successful loads, failures, page/filter inputs, and stale
  response suppression.
- Pure utility tests cover route parsing/serialization, pagination, labels, and
  published URL resolution.
- Nuxt-runtime tests under `test/nuxt` mount pages, components, and composables
  with `@nuxt/test-utils`; assert visible state, route synchronization, emitted
  interactions, and typed API-error projection rather than component source.
- Navigation tests cover groups, canonical destinations, aliases that must not
  return, and route titles.
- Playwright journeys under `test/e2e` mock same-origin `/api` responses and
  cover version switching, authoring validation, directory URL state, storage
  mutability, and lazy inspection surfaces. Run them with
  `docker build --target browser-tests web`; keep browser mocks deterministic
  and validate the coordinated deployment separately through Compose.
- `npm test` runs the policy guard, Node tests, and Nuxt-runtime tests.
  `npm run test:coverage` writes separate Node and Nuxt V8 reports under
  `coverage/` and rejects a regression from the tracked baseline. Run the
  checked workflow as `docker build --target coverage web`.

### Target feature-family matrix

Use this as the required placement for new coverage and migration slices; it
describes the target suite, not a claim that every cell is already covered.

| Family | Node seam | Nuxt/runtime seam | Browser journey |
| --- | --- | --- | --- |
| Shell, version, dashboard, navigation | Version/state and route utilities | Selector, remount, dashboard errors | Version switch clears old results and loads the replacement version |
| Items, NPCs, players, skills, lookups | Capability calls and directory stores | Detail tabs, forms, query state, typed errors | Item validation/save and directory URL state |
| Library, pipeline, storage | Catalog/import and storage handlers | Loading, paging, failure, and read-only state | Original resources mutate; generated assets do not |
| Maps, scenes, spawn map | Manifest/filter/paging/preview helpers | Selection, polling, and cleanup with renderer adapters mocked | Map and scene inspection surfaces lazy-load |

Run the web `validate` Docker target after any web change. It runs Vitest, Nuxt
type checking, and the production build.
