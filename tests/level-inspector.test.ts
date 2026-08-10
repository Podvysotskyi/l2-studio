import type {
  LevelLightManifestEntry,
  LevelTerrainManifestEntry,
  LevelWaterVolumeManifestEntry
} from '@podvysotskyi/l2-ui'
import { describe, expect, it } from 'vitest'
import {
  createTerrainLayerStates,
  enableAllTerrainLayers,
  filterLevelLights,
  filterLevelWaterVolumes,
  levelEnvironmentColor,
  setTerrainLayerEnabled,
  toggleSoloTerrainLayer
} from '../lib/level-inspector'

const terrain = {
  name: 'TerrainInfo0',
  layers: [{ index: 0 }, { index: 1 }, { index: 2 }]
} as LevelTerrainManifestEntry

describe('level inspector', () => {
  it('formats normalized environment colors for display', () => {
    expect(
      levelEnvironmentColor({ r: 0.35686275, g: 0.4, b: 0.4509804 })
    ).toEqual({
      css: 'rgb(91 102 115)',
      label: 'RGB 91, 102, 115'
    })
    expect(levelEnvironmentColor({ r: -1, g: 0.5, b: 2 })).toEqual({
      css: 'rgb(0 128 255)',
      label: 'RGB 0, 128, 255'
    })
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
    ] as LevelLightManifestEntry[]

    expect(filterLevelLights(lights, 'sun')).toEqual([lights[1]])
    expect(filterLevelLights(lights, 'light8')).toEqual([lights[0]])
  })

  it('filters water volumes by actor, class, and brush name', () => {
    const volumes = [
      { name: 'WaterVolume0', className: 'WaterVolume', brushName: 'Model269' },
      { name: 'WaterVolume1', className: 'WaterVolume', brushName: null }
    ] as LevelWaterVolumeManifestEntry[]

    expect(filterLevelWaterVolumes(volumes, 'model269')).toEqual([volumes[0]])
    expect(filterLevelWaterVolumes(volumes, 'watervolume1')).toEqual([
      volumes[1]
    ])
  })
})
