# Studio web extension guide

Use this guide for Nuxt pages, components, Pinia state, browser contracts,
services, storage routes, and rendering consumers.

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
Studio API calls use `versionPath`, which reads the selected game version and
builds `/api/game-versions/{version}/...`.

```ts
export function getThingDirectory(
  request: DirectoryRequest = {}
): Promise<DirectoryPage<ThingRecord>> {
  return $fetch<DirectoryPage<ThingRecord>>(versionPath('/content/things'), {
    query: directoryQuery(request)
  })
}
```

- Trim optional search/filter strings at the service boundary and omit empty
  values.
- Encode path identifiers or individual path segments. Do not encode `/`
  separators when the route intentionally accepts a relative resource path.
- Specify the response type, HTTP method, body, and query explicitly.
- Keep models, requests, and responses in their corresponding `types`
  directory. Reuse generic `DirectoryPage<T>` and shared job types.
- Mirror C# property optionality and nullability exactly. ASP.NET Core JSON uses
  camelCase browser properties.
- Resolve generated asset URLs through published-asset utilities; do not
  concatenate the public asset origin in page components.
- Use `storage-api.ts` only for Nuxt-owned file operations. Mutations must remain
  limited to original-resource storage; generated assets are read-only.

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
- Component or Nuxt tests cover behavior that cannot be expressed through a
  store or utility test.
- Navigation tests cover groups, canonical destinations, aliases that must not
  return, and route titles.
- Playwright covers critical user journeys; keep unit tests as the primary
  coverage for new state and services.

Run the web `validate` Docker target after any web change. It runs Vitest, Nuxt
type checking, and the production build.
