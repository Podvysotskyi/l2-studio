import { describe, expect, it } from 'vitest'
import {
  normalizePublishedGltfResourceUrl,
  resolveStudioGltfResourceUrl
} from '../../app/runtime/core/published-gltf'

describe('published glTF resource URLs', () => {
  it('removes a duplicated GLB directory from versioned resources', () => {
    expect(normalizePublishedGltfResourceUrl(
      'http://localhost:5300/versions/c1/Meshes/D_wash_st/hash/D_wash_st//versions/c1/Textures/D_wash_tx/hash/D_wash_tx/worldtree.vine01.webp'
    )).toBe(
      'http://localhost:5300/versions/c1/Textures/D_wash_tx/hash/D_wash_tx/worldtree.vine01.webp'
    )
  })

  it('supports legacy trimmed paths and preserves texture queries', () => {
    expect(normalizePublishedGltfResourceUrl(
      'https://assets.test/versions/c1/Meshes/package/versions/c1/Textures/texture.webp?gpu=none'
    )).toBe(
      'https://assets.test/versions/c1/Textures/texture.webp?gpu=none'
    )
  })

  it('preserves valid and unrelated resources', () => {
    expect(normalizePublishedGltfResourceUrl(
      'https://assets.test/versions/c1/Textures/texture.webp'
    )).toBe('https://assets.test/versions/c1/Textures/texture.webp')
    expect(normalizePublishedGltfResourceUrl('textures/local.webp'))
      .toBe('textures/local.webp')
    expect(normalizePublishedGltfResourceUrl('data:image/png;base64,AA=='))
      .toBe('data:image/png;base64,AA==')
  })

  it('replaces external material images while retaining GLB geometry URLs', () => {
    expect(resolveStudioGltfResourceUrl(
      'https://assets.test/versions/c1/Textures/terrain.webp?v=1'
    )).toMatch(/^data:image\/png;base64,/)
    expect(resolveStudioGltfResourceUrl(
      'https://assets.test/versions/c1/Meshes/terrain.glb'
    )).toBe('https://assets.test/versions/c1/Meshes/terrain.glb')
  })
})
