import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { getStudioServiceInfo } from '../services/studio-api'
import type { ServiceState } from '../types/models/service-state'
import type { StudioServiceInfo } from '../types/responses/studio-service-info'

export const useSystemStore = defineStore('system', () => {
  const info = ref<StudioServiceInfo>()
  const serviceState = ref<ServiceState>('connecting')
  const loading = ref(false)
  const error = ref<string>()
  let pendingRequest: Promise<StudioServiceInfo> | undefined

  const description = computed(() =>
    info.value
      ? `${info.value.environment} · ${info.value.buildVersion}`
      : '/api via Nuxt'
  )

  async function load(force = false): Promise<StudioServiceInfo> {
    if (!force && info.value) return info.value
    if (!force && pendingRequest) return pendingRequest

    loading.value = true
    serviceState.value = 'connecting'
    error.value = undefined
    const request = getStudioServiceInfo()
    pendingRequest = request
    try {
      info.value = await request
      serviceState.value = 'connected'
      return info.value
    } catch (cause) {
      serviceState.value = 'error'
      error.value = 'Studio API service information could not be loaded.'
      throw cause
    } finally {
      if (pendingRequest === request) pendingRequest = undefined
      loading.value = false
    }
  }

  return { info, serviceState, loading, error, description, load }
})
