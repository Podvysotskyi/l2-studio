import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  getNpcLookupDirectory,
  updateNpcLookupDisplayName
} from '../services/studio-api'
import type {
  NpcLookupKind,
  NpcLookupRecord
} from '../types/models/content-directory'

export const useNpcLookupDirectoryStore = defineStore('npc-lookup-directory', () => {
  const records = ref<Partial<Record<NpcLookupKind, NpcLookupRecord[]>>>({})
  const loadingKinds = ref<NpcLookupKind[]>([])
  const errors = ref<Partial<Record<NpcLookupKind, string>>>({})

  async function load(kind: NpcLookupKind, label = 'lookup') {
    loadingKinds.value = [...new Set([...loadingKinds.value, kind])]
    errors.value[kind] = undefined
    try {
      records.value[kind] = await getNpcLookupDirectory(kind)
    } catch {
      errors.value[kind] = `The ${label.toLowerCase()} catalog could not be loaded.`
    } finally {
      loadingKinds.value = loadingKinds.value.filter(item => item !== kind)
    }
  }

  async function updateDisplayName(kind: NpcLookupKind, name: string, displayName: string) {
    const updated = await updateNpcLookupDisplayName(kind, name, displayName)
    records.value[kind] = (records.value[kind] ?? []).map(record =>
      record.name === name ? updated : record
    )
  }

  function isLoading(kind: NpcLookupKind) {
    return loadingKinds.value.includes(kind)
  }

  return { records, errors, load, updateDisplayName, isLoading }
})
