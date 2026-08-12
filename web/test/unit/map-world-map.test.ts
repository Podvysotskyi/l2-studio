import { describe, expect, it } from 'vitest'
import {
  centerMapWorldMap,
  clampMapWorldMapScale,
  constrainMapWorldMapPan,
  fitMapWorldMap,
  mapWorldMapMaximumScale,
  mapWorldMapMinimumScale,
  mapWorldMapTileSize,
  zoomMapWorldMapAt
} from '../../app/utils/map-world-map'

describe('map world map viewport', () => {
  it('centers the world at the readable 128 pixel tile scale', () => {
    const transform = centerMapWorldMap(
      { width: 1000, height: 700 },
      { width: mapWorldMapTileSize * 10, height: mapWorldMapTileSize * 4 }
    )

    expect(transform).toEqual({ x: -140, y: 94, scale: 1 })
  })

  it('fits the complete world within the padded viewport', () => {
    const transform = fitMapWorldMap(
      { width: 1000, height: 700 },
      { width: 1280, height: 512 }
    )

    expect(transform.scale).toBe(0.73125)
    expect(transform.x).toBe(32)
    expect(transform.y).toBeCloseTo(162.8)
  })

  it('clamps zoom to the supported range', () => {
    expect(clampMapWorldMapScale(0.01)).toBe(mapWorldMapMinimumScale)
    expect(clampMapWorldMapScale(8)).toBe(mapWorldMapMaximumScale)
  })

  it('keeps the world point under the cursor fixed while zooming', () => {
    const transform = zoomMapWorldMapAt({ x: -100, y: 20, scale: 1 }, 2, {
      x: 300,
      y: 220
    })

    expect(transform).toEqual({ x: -500, y: -180, scale: 2 })
  })

  it('keeps an edge of an oversized map within the viewport', () => {
    expect(
      constrainMapWorldMapPan(
        { x: -5000, y: 5000, scale: 1 },
        { width: 800, height: 600 },
        { width: 1600, height: 1200 }
      )
    ).toEqual({ x: -864, y: 64, scale: 1 })
  })

  it('centers a map that is smaller than its viewport', () => {
    expect(
      constrainMapWorldMapPan(
        { x: 400, y: -100, scale: 0.5 },
        { width: 800, height: 600 },
        { width: 1000, height: 600 }
      )
    ).toEqual({ x: 150, y: 150, scale: 0.5 })
  })
})
