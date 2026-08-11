import { nextTick, onBeforeUnmount, onMounted, watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { directoryRouteQuery, directoryRouteState } from '../utils/directory'

export function useDirectoryRouteSync(
  path: string,
  state: { query: Ref<string>; page: Ref<number>; pageSize: Ref<number> },
  load: () => Promise<void>
) {
  const route = useRoute()
  const router = useRouter()
  let searchTimer: ReturnType<typeof setTimeout> | undefined
  let mutatingViewState = false
  let replacingRoute = false

  const initialState = directoryRouteState(route.query)
  state.query.value = initialState.query
  state.page.value = initialState.page
  state.pageSize.value = initialState.pageSize

  async function replaceRouteAndLoad() {
    replacingRoute = true
    try {
      await router.replace({
        path,
        query: directoryRouteQuery(
          state.query.value,
          state.page.value,
          state.pageSize.value
        )
      })
    } finally {
      replacingRoute = false
    }
    await load()
  }

  watch(
    () => route.query,
    async (query) => {
      if (replacingRoute) return
      clearTimeout(searchTimer)
      const routeState = directoryRouteState(query)
      mutatingViewState = true
      state.query.value = routeState.query
      state.page.value = routeState.page
      state.pageSize.value = routeState.pageSize
      await nextTick()
      mutatingViewState = false
      await load()
    },
    { deep: true }
  )

  watch(state.query, () => {
    if (mutatingViewState) return
    clearTimeout(searchTimer)
    searchTimer = setTimeout(() => {
      void (async () => {
        mutatingViewState = true
        state.page.value = 1
        await nextTick()
        mutatingViewState = false
        await replaceRouteAndLoad()
      })()
    }, 300)
  })

  watch(state.page, () => {
    if (mutatingViewState) return
    void replaceRouteAndLoad()
  })

  watch(state.pageSize, () => {
    if (mutatingViewState) return
    void (async () => {
      mutatingViewState = true
      state.page.value = 1
      await nextTick()
      mutatingViewState = false
      await replaceRouteAndLoad()
    })()
  })

  onMounted(() => void load())
  onBeforeUnmount(() => clearTimeout(searchTimer))
}
