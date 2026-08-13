import {
  ClampToEdgeWrapping,
  Mesh,
  MeshStandardMaterial,
  PlaneGeometry,
  Texture,
  TextureLoader
} from 'three'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  prepareStaticMeshMaterials,
  publishedStaticMeshMaterial
} from '../../app/runtime/materials/static-mesh-material'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('published static-mesh material', () => {
  it('reads the published L2 material contract from glTF material extras', () => {
    const material = new MeshStandardMaterial()
    material.userData.l2 = {
      diffuseUrl: '/versions/c1/Textures/a.webp',
      emissiveUrl: '/versions/c1/Textures/e.webp',
      blendMode: 'masked',
      windMode: 'foliage'
    }

    expect(publishedStaticMeshMaterial(material)).toMatchObject({
      diffuseUrl: '/versions/c1/Textures/a.webp',
      emissiveUrl: '/versions/c1/Textures/e.webp',
      blendMode: 'masked',
      windMode: 'foliage'
    })
  })

  it('does not reinterpret normal glTF materials as L2 materials', () => {
    expect(publishedStaticMeshMaterial(new MeshStandardMaterial())).toBeUndefined()
  })

  it('emits floating-point GLSL literals for integer material values', async () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {})
    const fallback = new MeshStandardMaterial()
    const mesh = meshWithMaterial()
    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )
    const material = mesh.material as MeshStandardMaterial
    const shader = {
      uniforms: {},
      vertexShader: `void main() {
#include <uv_vertex>
#include <begin_vertex>
}`,
      fragmentShader: 'void main() {}'
    }

    material.onBeforeCompile(shader as never, {} as never)

    expect(shader.vertexShader).toContain('vec2(0.0 * l2Time, 0.0 * l2Time)')
    expect(material.emissiveMap).toBeNull()
    expect(warn).not.toHaveBeenCalled()
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('updates prepared shader material state for each render', async () => {
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'water' })
    source.userData.l2 = { panRate: 0.5 }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)
    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/water.glb'
    )
    const material = mesh.material as MeshStandardMaterial
    const shader = {
      uniforms: {} as Record<string, { value: unknown }>,
      vertexShader: `void main() {
#include <uv_vertex>
#include <begin_vertex>
}`,
      fragmentShader: 'void main() {}'
    }

    material.onBeforeCompile(shader as never, {} as never)
    preparation.update(2.5)

    expect(shader.uniforms.l2Time?.value).toBe(2.5)
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('loads root-relative textures from the GLB asset origin and caches the result', async () => {
    const load = vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const modelUrl = 'http://localhost:5300/versions/c1/Meshes/town/hash/town/house.glb'
    const textureUrl = '/versions/c1/Textures/town/hash/town/roof-cache-test.webp'

    const first = await prepareStaticMeshMaterials(
      meshWithMaterial(textureUrl),
      fallback,
      modelUrl
    )
    const second = await prepareStaticMeshMaterials(
      meshWithMaterial(textureUrl),
      fallback,
      modelUrl
    )

    expect(load).toHaveBeenCalledTimes(1)
    expect(load).toHaveBeenCalledWith(
      'http://localhost:5300/versions/c1/Textures/town/hash/town/roof-cache-test.webp',
      expect.any(Function),
      undefined,
      expect.any(Function)
    )
    expect(first.warnings).toEqual([])
    expect(second.warnings).toEqual([])
    first.dispose()
    second.dispose()
    fallback.dispose()
  })

  it('uses the glTF texture orientation for every published texture role', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        expect(texture.flipY).toBe(true)
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'atlas' })
    source.userData.l2 = {
      diffuseUrl: '/versions/c1/Textures/town/atlas-orientation-diffuse.webp',
      opacityUrl: '/versions/c1/Textures/town/atlas-orientation-opacity.webp'
    }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)

    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )
    const material = mesh.material as MeshStandardMaterial
    const opacityFrames = material.userData.l2OpacityFrames as Texture[]

    expect(material.map?.flipY).toBe(false)
    expect(opacityFrames[0]?.flipY).toBe(false)
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('applies authored clamp modes to the primary texture', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'clamped' })
    source.userData.l2 = {
      diffuseUrl: '/versions/c1/Textures/town/clamped.webp',
      clampU: true,
      clampV: true
    }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)

    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )

    const material = mesh.material as MeshStandardMaterial
    expect(material.map?.wrapS).toBe(ClampToEdgeWrapping)
    expect(material.map?.wrapT).toBe(ClampToEdgeWrapping)
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('evicts failed texture loads so a later material preparation can retry', async () => {
    let attempts = 0
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((url, onLoad, _onProgress, onError) => {
        const texture = new Texture()
        attempts++
        if (attempts === 1) onError?.(new Error(`Failed ${url}`))
        else onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const modelUrl = 'https://assets.test/versions/c1/Meshes/town/house.glb'
    const textureUrl = '/versions/c1/Textures/town/roof-retry-test.webp'

    const failed = await prepareStaticMeshMaterials(
      meshWithMaterial(textureUrl),
      fallback,
      modelUrl
    )
    const retried = await prepareStaticMeshMaterials(
      meshWithMaterial(textureUrl),
      fallback,
      modelUrl
    )

    expect(attempts).toBe(2)
    expect(failed.warnings).toEqual([
      'roof: Unable to load texture https://assets.test/versions/c1/Textures/town/roof-retry-test.webp.'
    ])
    expect(retried.warnings).toEqual([])
    failed.dispose()
    retried.dispose()
    fallback.dispose()
  })
})

function meshWithMaterial(diffuseUrl?: string) {
  const material = new MeshStandardMaterial({ name: 'roof' })
  material.userData.l2 = diffuseUrl ? { diffuseUrl } : {}
  return new Mesh(new PlaneGeometry(1, 1), material)
}
