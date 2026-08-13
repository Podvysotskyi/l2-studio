import { describe, expect, it } from 'vitest'
import {
  normalizePublishedGltfResourceUrl,
  resolvePublishedGltfMaterialUrl,
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
    )).toBe(
      'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw=='
    )
    expect(resolveStudioGltfResourceUrl(
      'https://assets.test/versions/c1/Meshes/terrain.glb'
    )).toBe('https://assets.test/versions/c1/Meshes/terrain.glb')
  })

  it('resolves versioned material paths against the GLB asset origin', () => {
    expect(resolvePublishedGltfMaterialUrl(
      '/versions/c1/Textures/town/hash/town/roof.webp',
      'http://localhost:5300/versions/c1/StaticMeshes/town/hash/town/house.glb'
    )).toBe('http://localhost:5300/versions/c1/Textures/town/hash/town/roof.webp')
  })

  it('resolves relative material paths beside the GLB', () => {
    expect(resolvePublishedGltfMaterialUrl(
      'textures/roof.webp',
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )).toBe('https://assets.test/versions/c1/Meshes/town/textures/roof.webp')
  })

  it('preserves absolute and embedded material URLs', () => {
    expect(resolvePublishedGltfMaterialUrl(
      'https://cdn.test/roof.webp?v=2',
      'https://assets.test/house.glb'
    )).toBe('https://cdn.test/roof.webp?v=2')
    expect(resolvePublishedGltfMaterialUrl(
      'data:image/png;base64,AA==',
      'https://assets.test/house.glb'
    )).toBe('data:image/png;base64,AA==')
    expect(resolvePublishedGltfMaterialUrl(
      'blob:https://assets.test/9c50a74f',
      'https://assets.test/house.glb'
    )).toBe('blob:https://assets.test/9c50a74f')
  })

  it('normalizes legacy duplicated material paths and retains queries', () => {
    expect(resolvePublishedGltfMaterialUrl(
      'https://assets.test/versions/c1/Meshes/town/versions/c1/Textures/roof.webp?gpu=none',
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )).toBe('https://assets.test/versions/c1/Textures/roof.webp?gpu=none')
  })

  it('falls back to the normalized input when the GLB URL cannot be parsed', () => {
    expect(resolvePublishedGltfMaterialUrl(
      '/versions/c1/Textures/roof.webp',
      'not an absolute URL'
    )).toBe('/versions/c1/Textures/roof.webp')
  })
})
