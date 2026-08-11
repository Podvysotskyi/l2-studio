import type { LevelTerrainManifestEntry } from '~/types/studio'
import { describe, expect, it } from 'vitest'
import {
  blendShader,
  terrainSamplerCount,
  unpackTerrainControlPixels
} from '../../app/utils/terrain-material'

const terrain: LevelTerrainManifestEntry = {
  name: 'TerrainInfo0',
  location: { x: 0, y: 0, z: 0 },
  rotation: { pitch: 0, yaw: 0, roll: 0 },
  scale: { x: 128, y: 128, z: 76 },
  heightmap: 'T_17_25.Height.17_25',
  heightmapWidth: 256,
  heightmapHeight: 256,
  meshUrl: '/levels/17_25/TerrainInfo0.glb',
  materialStatus: 'resolved',
  materialError: null,
  controlMapUrls: ['/levels/17_25/control-0.webp'],
  controlMapWidth: 512,
  controlMapHeight: 512,
  controlMapEncoding: 'webp-rgb-a-horizontal',
  controlMapArrayGroup: 1,
  layers: [
    {
      index: 0,
      texturePackage: 'T_texture',
      textureObject: 'Texture.Base',
      textureUrl: '/textures/t_texture/Texture.Base.webp',
      textureWidth: 256,
      textureHeight: 256,
      textureArrayGroup: 0,
      textureArrayLayer: 0,
      alphaPackage: 'T_texture',
      alphaObject: 'Texture.layer0',
      controlMapIndex: 0,
      controlMapChannel: 0,
      uScale: 1,
      vScale: 1,
      uPan: 0,
      vPan: 0,
      textureMapAxis: 'xy',
      textureRotation: 0,
      layerRotation: { pitch: 0, yaw: 0, roll: 0 },
      uvTransform: {
        u: { x: 1, y: 0, z: 0, offset: 0 },
        v: { x: 0, y: 0, z: 1, offset: 0 }
      }
    },
    {
      index: 1,
      texturePackage: 'T_sland',
      textureObject: 'SL_G',
      textureUrl: '/textures/t_sland/SL_G.webp',
      textureWidth: 256,
      textureHeight: 256,
      textureArrayGroup: 0,
      textureArrayLayer: 1,
      alphaPackage: 'T_17_25',
      alphaObject: 'Height.17_25_G1',
      controlMapIndex: 0,
      controlMapChannel: 1,
      uScale: 2,
      vScale: 2,
      uPan: 0,
      vPan: 0,
      textureMapAxis: 'yz',
      textureRotation: 0,
      layerRotation: { pitch: 4096, yaw: 8192, roll: 2048 },
      uvTransform: {
        u: { x: 0, y: 0.5, z: 0, offset: 3 },
        v: { x: 0, y: 0, z: 0.25, offset: -2 }
      }
    }
  ]
}

describe('terrain material', () => {
  it('counts diffuse and packed control samplers', () => {
    expect(terrainSamplerCount(terrain)).toBe(2)
  })

  it('projects every layer from terrain-local position while keeping control maps on normalized UVs', () => {
    const shader = blendShader(terrain)

    expect(shader).toContain('dot(vTerrainPosition, vec3(1.0, 0.0, 0.0)) + 0.0')
    expect(shader).toContain('dot(vTerrainPosition, vec3(0.0, 0.5, 0.0)) + 3.0')
    expect(shader).toContain('terrainControlArray, vec3(vTerrainUV, 0.0)).r')
    expect(shader).toContain('terrainControlArray, vec3(vTerrainUV, 0.0)).g')
    expect(shader).toContain('terrainDiffuseArray0')
    expect(shader).toContain('* terrainLayerEnabled0')
    expect(shader).toContain('* terrainLayerEnabled1')
    expect(shader.indexOf('terrainLayerColor0')).toBeLessThan(
      shader.indexOf('terrainLayerColor1')
    )
    expect(shader).toContain('terrainAnyLayerEnabled > 0.5')
  })

  it('reconstructs four independent weights from opaque transport pixels', () => {
    const encoded = new Uint8ClampedArray([
      10, 20, 30, 255, 40, 50, 60, 255, 0, 0, 0, 255, 128, 0, 0, 255
    ])

    expect([...unpackTerrainControlPixels(encoded, 2, 1)]).toEqual([
      10, 20, 30, 0, 40, 50, 60, 128
    ])
  })

  it('rejects transparent or incorrectly sized transport pixels', () => {
    expect(() => unpackTerrainControlPixels(new Uint8Array(15), 1, 2)).toThrow(
      /dimensions/
    )
    expect(() =>
      unpackTerrainControlPixels(
        new Uint8Array([10, 20, 30, 0, 40, 0, 0, 255]),
        1,
        1
      )
    ).toThrow(/opaque/)
  })
})
