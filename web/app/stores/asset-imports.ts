import type { AssetImportKind } from '~/types/studio'
import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getAssetImportJobs, startAssetImport } from '../services/studio-api'
import type { AssetImportJob } from '../types/models/asset-import-job'

export const useAssetImportsStore = defineStore('asset-imports', () => {
  const jobs = ref<Partial<Record<AssetImportKind, AssetImportJob[]>>>({})
  const loading = ref(false)
  const error = ref<string>()

  async function load(kinds: AssetImportKind[]) {
    loading.value = true
    error.value = undefined
    try {
      const responses = await Promise.all(
        kinds.map((kind) => getAssetImportJobs(kind, 100))
      )
      jobs.value = Object.fromEntries(
        kinds.map((kind, index) => [kind, responses[index] ?? []])
      )
    } catch (cause) {
      error.value = 'Asset import jobs could not be loaded.'
      throw cause
    } finally {
      loading.value = false
    }
  }

  async function start(kind: AssetImportKind) {
    const job = await startAssetImport(kind)
    jobs.value[kind] = [job, ...(jobs.value[kind] ?? [])]
    return job
  }

  function reset() { jobs.value = {}; loading.value = false; error.value = undefined }

  return { jobs, loading, error, load, start, reset }
})
