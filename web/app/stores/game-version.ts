import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { getGameVersions } from '../services/studio-api'
import type { GameVersionSummary } from '../types/models/game-version'
import {
  gameVersionStorageKey,
  resolveSelectedGameVersionKey,
  selectedGameVersionKey
} from '../utils/game-version'

export const useGameVersionStore = defineStore('game-version', () => {
  const versions = ref<GameVersionSummary[]>([])
  const selected = ref(selectedGameVersionKey())
  const loading = ref(true)
  const error = ref(false)
  const options = computed(() =>
    versions.value.map(version => ({
      label: version.displayName,
      value: version.key
    }))
  )

  async function load() {
    loading.value = true
    error.value = false
    try {
      versions.value = await getGameVersions()
      selected.value = resolveSelectedGameVersionKey(
        versions.value,
        selectedGameVersionKey()
      )
      persist()
    } catch (cause) {
      error.value = true
      throw cause
    } finally {
      loading.value = false
    }
  }

  function select(value: string) {
    if (value === selected.value) return
    selected.value = value
    persist()
    window.location.reload()
  }

  function persist() {
    if (import.meta.client)
      window.localStorage.setItem(gameVersionStorageKey, selected.value)
  }

  return { versions, selected, options, loading, error, load, select }
})
