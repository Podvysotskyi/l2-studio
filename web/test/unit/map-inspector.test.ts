import type {
  MapLightManifestEntry,
  MapManifest,
  MapTerrainManifestEntry,
  MapWaterVolumeManifestEntry
} from '~/types/studio'
import { describe, expect, it } from 'vitest'
import {
  createTerrainLayerStates,
  enableAllTerrainLayers,
  filterMapLights,
  filterMapWaterVolumes,
  hasMapLevelSummaryData,
  mapSkyZonePreviewManifest,
  mapIdealPlayerCount,
  mapEnvironmentColor,
  previewableMapSkyZones,
  setTerrainLayerEnabled,
  toggleSoloTerrainLayer
} from '../../app/utils/map-inspector'

const terrain = {
  name: 'TerrainInfo0',
  layers: [{ index: 0 }, { index: 1 }, { index: 2 }]
} as MapTerrainManifestEntry

describe('map inspector', () => {
  it('formats normalized environment colors for display', () => {
    expect(
      mapEnvironmentColor({ r: 0.35686275, g: 0.4, b: 0.4509804 })
    ).toEqual({
      css: 'rgb(91 102 115)',
      label: 'RGB 91, 102, 115'
    })
    expect(mapEnvironmentColor({ r: -1, g: 0.5, b: 2 })).toEqual({
      css: 'rgb(0 128 255)',
      label: 'RGB 0, 128, 255'
    })
  })

  it('formats authored level-summary player ranges and detects empty summaries', () => {
    const empty = {
      title: null,
      author: null,
      description: null,
      levelEnterText: null,
      extraInfo: null,
      decoTextName: null,
      hideFromMenus: null,
      idealPlayerCountMin: null,
      idealPlayerCountMax: null,
      singlePlayerTeamSize: null,
      screenshot: null
    }

    expect(mapIdealPlayerCount(empty)).toBeNull()
    expect(hasMapLevelSummaryData(empty)).toBe(false)
    expect(mapIdealPlayerCount({ ...empty, idealPlayerCountMin: 2 })).toBe('2+')
    expect(
      mapIdealPlayerCount({
        ...empty,
        idealPlayerCountMin: 2,
        idealPlayerCountMax: 8
      })
    ).toBe('2–8')
    expect(hasMapLevelSummaryData({ ...empty, hideFromMenus: false })).toBe(
      true
    )
  })

  it('initializes every imported terrain layer as enabled', () => {
    expect(createTerrainLayerStates([terrain])).toEqual({
      TerrainInfo0: { enabled: [true, true, true] }
    })
  })

  it('toggles layers and resets all layers', () => {
    const disabled = setTerrainLayerEnabled(
      { enabled: [true, true, true] },
      1,
      false
    )

    expect(disabled.enabled).toEqual([true, false, true])
    expect(enableAllTerrainLayers(disabled).enabled).toEqual([true, true, true])
  })

  it('solos a layer and restores the previous configuration', () => {
    const original = { enabled: [true, false, true] }
    const firstSolo = toggleSoloTerrainLayer(original, 2)
    const otherSolo = toggleSoloTerrainLayer(firstSolo, 0)

    expect(firstSolo.enabled).toEqual([false, false, true])
    expect(otherSolo.enabled).toEqual([true, false, false])
    expect(toggleSoloTerrainLayer(otherSolo, 0)).toEqual(original)
  })

  it('filters lights by name and class', () => {
    const lights = [
      { name: 'Light8', className: 'Light' },
      { name: 'NMovableSunLight0', className: 'NMovableSunLight' }
    ] as MapLightManifestEntry[]

    expect(filterMapLights(lights, 'sun')).toEqual([lights[1]])
    expect(filterMapLights(lights, 'light8')).toEqual([lights[0]])
  })

  it('filters water volumes by actor, class, and brush name', () => {
    const volumes = [
      { name: 'WaterVolume0', className: 'WaterVolume', brushName: 'Model269' },
      { name: 'WaterVolume1', className: 'WaterVolume', brushName: null }
    ] as MapWaterVolumeManifestEntry[]

    expect(filterMapWaterVolumes(volumes, 'model269')).toEqual([volumes[0]])
    expect(filterMapWaterVolumes(volumes, 'watervolume1')).toEqual([
      volumes[1]
    ])
  })

  it('selects published Sky Zones by priority and isolates their preview manifest', () => {
    const manifest = {
      skyZones: [
        { name: 'Lower', order: 1 },
        { name: 'Higher', order: 2 },
        { name: 'Unavailable', order: 3 }
      ],
      bspMeshes: [
        {
          name: 'Lower0',
          role: 'sky-zone',
          skyZone: 'Lower',
          meshUrl: '/lower.glb'
        },
        {
          name: 'Higher0',
          role: 'sky-zone',
          skyZone: 'Higher',
          meshUrl: '/higher.glb'
        },
        {
          name: 'Higher1',
          role: 'sky-zone',
          skyZone: 'Higher',
          meshUrl: null
        },
        {
          name: 'World0',
          role: 'geometry',
          skyZone: null,
          meshUrl: '/world.glb'
        }
      ],
      terrains: [{}],
      actors: [{}],
      lights: [{}],
      waterVolumes: [{}]
    } as MapManifest

    expect(previewableMapSkyZones(manifest).map((zone) => zone.name)).toEqual([
      'Higher',
      'Lower'
    ])

    expect(mapSkyZonePreviewManifest(manifest, 'Higher')).toMatchObject({
      skyZones: [{ name: 'Higher' }],
      bspMeshes: [{ name: 'Higher0' }],
      terrains: [],
      actors: [],
      lights: [],
      waterVolumes: []
    })
    expect(mapSkyZonePreviewManifest(manifest, 'Unavailable')).toBeUndefined()
  })
})
