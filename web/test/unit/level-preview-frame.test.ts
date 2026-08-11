import { describe, expect, it } from 'vitest'
import { calculateLevelPreviewFrame } from '../../app/utils/level-preview-frame'

describe('level preview framing', () => {
  it('frames the longest terrain dimension with two percent padding per side', () => {
    const frame = calculateLevelPreviewFrame({
      minimum: { x: -100, y: -10, z: -50 },
      maximum: { x: 100, y: 30, z: 50 }
    })

    expect(frame.center).toEqual({ x: 0, y: 10, z: 0 })
    expect(frame.extent).toBe(208)
    expect(frame.camera.y).toBe(238)
  })

  it('uses negative Babylon Z as image-up for decreasing Unreal Y', () => {
    const frame = calculateLevelPreviewFrame({
      minimum: { x: 10, y: 0, z: 20 },
      maximum: { x: 20, y: 5, z: 60 }
    })

    expect(frame.up).toEqual({ x: 0, y: 0, z: -1 })
    expect(frame.camera.x).toBe(15)
    expect(frame.camera.z).toBe(40)
  })
})
