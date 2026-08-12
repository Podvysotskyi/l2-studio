import type {
  MapEnvironmentManifestEntry,
  MapLightManifestEntry,
  MapTerrainManifestEntry,
  MapWaterVolumeManifestEntry
} from '~/types/studio'

export interface TerrainLayerState {
  enabled: boolean[]
  soloIndex?: number
  restore?: boolean[]
}

export type TerrainLayerStates = Record<string, TerrainLayerState>

type MapEnvironmentColor = MapEnvironmentManifestEntry['ambientColor']

export function mapEnvironmentColor(color: MapEnvironmentColor) {
  const channels = [color.r, color.g, color.b].map((channel) =>
    Math.round(Math.min(Math.max(channel, 0), 1) * 255)
  )
  return {
    css: `rgb(${channels.join(' ')})`,
    label: `RGB ${channels.join(', ')}`
  }
}

export function createTerrainLayerStates(
  terrains: MapTerrainManifestEntry[]
): TerrainLayerStates {
  return Object.fromEntries(
    terrains.map((terrain) => [
      terrain.name,
      { enabled: terrain.layers.map(() => true) }
    ])
  )
}

export function setTerrainLayerEnabled(
  state: TerrainLayerState,
  index: number,
  enabled: boolean
): TerrainLayerState {
  const next = [...state.enabled]
  next[index] = enabled
  return { enabled: next }
}

export function enableAllTerrainLayers(
  state: TerrainLayerState
): TerrainLayerState {
  return { enabled: state.enabled.map(() => true) }
}

export function toggleSoloTerrainLayer(
  state: TerrainLayerState,
  index: number
): TerrainLayerState {
  if (state.soloIndex === index && state.restore) {
    return { enabled: [...state.restore] }
  }

  const restore = state.restore ? [...state.restore] : [...state.enabled]
  return {
    enabled: state.enabled.map((_, layerIndex) => layerIndex === index),
    soloIndex: index,
    restore
  }
}

export function filterMapLights(
  lights: MapLightManifestEntry[],
  query: string
) {
  const normalized = query.trim().toLowerCase()
  if (!normalized) return lights
  return lights.filter(
    (light) =>
      light.name.toLowerCase().includes(normalized) ||
      light.className.toLowerCase().includes(normalized)
  )
}

export function mapLightColor(light: MapLightManifestEntry) {
  const hue = (light.hue / 255) * 360
  const saturation = (1 - light.saturation / 255) * 100
  return `hsl(${hue.toFixed(1)} ${saturation.toFixed(1)}% 55%)`
}

export function filterMapWaterVolumes(
  volumes: MapWaterVolumeManifestEntry[],
  query: string
) {
  const normalized = query.trim().toLowerCase()
  if (!normalized) return volumes
  return volumes.filter(
    (volume) =>
      volume.name.toLowerCase().includes(normalized) ||
      volume.className.toLowerCase().includes(normalized) ||
      volume.brushName?.toLowerCase().includes(normalized)
  )
}
