import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getLookupDirectory } from '../services/studio-api'
import type {
  LookupKind,
  LookupRecord
} from '../types/models/content-directory'

export const useLookupDirectoryStore = defineStore('lookup-directory', () => {
  const records = ref<Partial<Record<LookupKind, LookupRecord[]>>>({})
  const loadingKinds = ref<LookupKind[]>([])
  const errors = ref<Partial<Record<LookupKind, string>>>({})

  async function load(kind: LookupKind, label = 'lookup') {
    loadingKinds.value = [...new Set([...loadingKinds.value, kind])]
    errors.value[kind] = undefined
    try {
      records.value[kind] = (await getLookupDirectory(kind, { page: 1, pageSize: 100 })).items
    } catch {
      errors.value[kind] = `The ${label.toLowerCase()} catalog could not be loaded.`
    } finally {
      loadingKinds.value = loadingKinds.value.filter(item => item !== kind)
    }
  }

  function isLoading(kind: LookupKind) {
    return loadingKinds.value.includes(kind)
  }
  function reset() { records.value = {}; loadingKinds.value = []; errors.value = {} }

  return { records, errors, load, isLoading, reset }
})
