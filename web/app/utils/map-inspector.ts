import type {
  MapEnvironmentManifestEntry,
  MapLevelSummaryManifestEntry,
  MapLightManifestEntry,
  MapManifest,
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

export function mapIdealPlayerCount(
  summary: MapLevelSummaryManifestEntry | null | undefined
) {
  if (!summary) return null
  const { idealPlayerCountMin: min, idealPlayerCountMax: max } = summary
  if (min === null && max === null) return null
  if (min === null) return `Up to ${max}`
  if (max === null) return `${min}+`
  return min === max ? `${min}` : `${min}–${max}`
}

export function hasMapLevelSummaryData(
  summary: MapLevelSummaryManifestEntry
) {
  return Boolean(
    summary.title ||
      summary.author ||
      summary.description ||
      summary.levelEnterText ||
      summary.extraInfo ||
      summary.decoTextName ||
      summary.hideFromMenus !== null ||
      mapIdealPlayerCount(summary) !== null ||
      summary.singlePlayerTeamSize !== null ||
      summary.screenshot
  )
}

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

export function previewableMapSkyZones(manifest: MapManifest) {
  return [...manifest.skyZones]
    .filter((zone) =>
      manifest.bspMeshes.some(
        (mesh) =>
          mesh.role === 'sky-zone' &&
          mesh.skyZone === zone.name &&
          Boolean(mesh.meshUrl)
      )
    )
    .sort((left, right) => right.order - left.order)
}

export function mapSkyZonePreviewManifest(
  manifest: MapManifest,
  skyZoneName: string | undefined
) {
  const skyZone = previewableMapSkyZones(manifest).find(
    (zone) => zone.name === skyZoneName
  )
  if (!skyZone) return undefined

  return {
    ...manifest,
    terrains: [],
    actors: [],
    lights: [],
    waterVolumes: [],
    skyZones: [skyZone],
    bspMeshes: manifest.bspMeshes.filter(
      (mesh) =>
        mesh.role === 'sky-zone' &&
        mesh.skyZone === skyZone.name &&
        Boolean(mesh.meshUrl)
    )
  }
}
