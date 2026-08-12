import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { getGameVersions } from '../services/studio-api'
import type { GameVersionSummary } from '../types/models/game-version'
import {
  gameVersionStorageKey,
  selectedGameVersionKey
} from '../utils/game-version'

export const useGameVersionStore = defineStore('game-version', () => {
  const versions = ref<GameVersionSummary[]>([])
  const selected = ref(selectedGameVersionKey())
  const loading = ref(false)
  const options = computed(() =>
    versions.value.map(version => ({
      label: version.displayName,
      value: version.key
    }))
  )

  async function load() {
    loading.value = true
    try {
      versions.value = await getGameVersions()
      if (!versions.value.some(version => version.key === selected.value)) {
        selected.value =
          versions.value.find(version => version.isDefault)?.key ??
          versions.value[0]?.key ??
          'interlude'
        persist()
      }
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

  return { versions, selected, options, loading, load, select }
})
