import {
  ClampToEdgeWrapping,
  DoubleSide,
  Mesh,
  MeshStandardMaterial,
  PlaneGeometry,
  Texture,
  TextureLoader
} from 'three'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  applyStaticMeshMaterialFallback,
  prepareStaticMeshMaterials,
  publishedStaticMeshMaterial
} from '../../app/runtime/materials/static-mesh-material'

afterEach(() => {
  vi.restoreAllMocks()
})

describe('published static-mesh material', () => {
  it('preserves authored water materials and replaces unresolved fallbacks', () => {
    const authored = new MeshStandardMaterial({ name: 'authored-water' })
    authored.userData.l2 = { panRate: 0.25 }
    const unresolved = new MeshStandardMaterial({ name: 'unresolved-water' })
    const fallback = new MeshStandardMaterial({ name: 'water-fallback' })
    const authoredMesh = new Mesh(new PlaneGeometry(1, 1), authored)
    const unresolvedMesh = new Mesh(new PlaneGeometry(1, 1), unresolved)

    applyStaticMeshMaterialFallback(authoredMesh, fallback)
    applyStaticMeshMaterialFallback(unresolvedMesh, fallback)

    expect(authoredMesh.material).toBe(authored)
    expect(unresolvedMesh.material).toBe(fallback)
    authored.dispose()
    unresolved.dispose()
    fallback.dispose()
    authoredMesh.geometry.dispose()
    unresolvedMesh.geometry.dispose()
  })

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

    expect(shader.vertexShader).toContain('vec2(0.0, 0.0) * l2UvTime')
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

  it('applies authored water UV offset, stretch, and stretch-repeat modes', async () => {
    const fallback = new MeshStandardMaterial()
    const cases = [
      {
        type: 0,
        expected: 'l2UvOffset.x += l2UOscillation * l2UvEffectsEnabled;'
      },
      {
        type: 1,
        expected: 'l2UvScale.x = max(0.001, 1.0 + l2UOscillation * l2UvEffectsEnabled);'
      },
      {
        type: 2,
        expected: 'l2UvScale.x = max(0.001, 1.0 + abs(l2UOscillation) * l2UvEffectsEnabled);'
      }
    ]

    for (const { type, expected } of cases) {
      const source = new MeshStandardMaterial({ name: `water-${type}` })
      source.userData.l2 = {
        panRate: 0.25,
        panRateV: -0.125,
        rotationRate: 0.5,
        uvOscillation: {
          uType: type,
          vType: 0,
          uRate: 1,
          vRate: 2,
          uAmplitude: 0.2,
          vAmplitude: 0.1,
          uPhase: 0.25,
          vPhase: 0.5
        }
      }
      const mesh = new Mesh(new PlaneGeometry(1, 1), source)
      const preparation = await prepareStaticMeshMaterials(
        mesh,
        fallback,
        `https://assets.test/versions/c1/Maps/water-${type}.glb`
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

      expect(shader.vertexShader).toContain(
        'sin(6.28318530718 * (l2Time * 1.0 + 0.25)) * 0.2'
      )
      expect(shader.vertexShader).toContain(expected)
      expect(shader.vertexShader).toContain(
        'vL2Uv = (vL2Uv - vec2(0.5)) * l2UvScale + vec2(0.5) + l2UvOffset;'
      )
      expect(shader.vertexShader).toContain(
        'mat2(cos(l2UvTime * 0.5), -sin(l2UvTime * 0.5)'
      )
      preparation.dispose()
      mesh.geometry.dispose()
    }
    fallback.dispose()
  })

  it('selects authored water flipbook frames from live elapsed time', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'animated-water' })
    source.userData.l2 = {
      diffuseAnimation: {
        frameUrls: ['/water-live-0.webp', '/water-live-1.webp'],
        frameRate: 2
      }
    }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)
    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Maps/animated-water.glb'
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
    const frames = material.userData.l2DiffuseFrames as Texture[]
    preparation.update(0.75)

    expect(shader.uniforms.l2DiffuseMap?.value).toBe(frames[1])
    preparation.update(1)
    expect(shader.uniforms.l2DiffuseMap?.value).toBe(frames[0])
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('samples luminance opacity and keeps it synchronized with animated diffuse frames', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'legacy-flame' })
    const animation = {
      frameUrls: ['/flame-0.webp', '/flame-1.webp'],
      frameRate: 20
    }
    source.userData.l2 = {
      diffuseUrl: '/flame.webp',
      opacityUrl: '/flame.webp',
      opacitySource: 'texture',
      opacityChannel: 'luminance',
      blendMode: 'alphablend',
      diffuseAnimation: animation,
      opacityAnimation: animation
    }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)
    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/flame.glb'
    )
    const material = mesh.material as MeshStandardMaterial
    const shader = {
      uniforms: {} as Record<string, { value: unknown }>,
      vertexShader: `void main() {
#include <uv_vertex>
#include <begin_vertex>
}`,
      fragmentShader: `void main() {
#include <map_fragment>
#include <emissivemap_fragment>
#include <lights_fragment_end>
}`
    }

    material.onBeforeCompile(shader as never, {} as never)
    preparation.update(0.075)

    const diffuseFrames = material.userData.l2DiffuseFrames as Texture[]
    const opacityFrames = material.userData.l2OpacityFrames as Texture[]
    expect(shader.fragmentShader).toContain(
      'l2Luminance(texture2D(l2OpacityMap, vL2Uv).rgb)'
    )
    expect(shader.uniforms.l2DiffuseMap?.value).toBe(diffuseFrames[1])
    expect(shader.uniforms.l2OpacityMap?.value).toBe(opacityFrames[1])
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

  it('inspects and live-toggles material texture and behavior controls', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const source = new MeshStandardMaterial({ name: 'diagnostic', side: DoubleSide })
    source.userData.l2 = {
      diffuseUrl: '/versions/c1/Textures/town/diffuse.webp',
      opacityUrl: '/versions/c1/Textures/town/opacity.webp',
      blendMode: 'alphablend',
      depthWrite: true,
      depthTest: true,
      panRate: 0.5,
      uvOscillation: {
        uType: 0,
        vType: 0,
        uRate: 0.1,
        vRate: 0.1,
        uAmplitude: 0.05,
        vAmplitude: 0.05,
        uPhase: 0.1,
        vPhase: 0.1
      },
      windMode: 'foliage'
    }
    const mesh = new Mesh(new PlaneGeometry(1, 1), source)
    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/town/house.glb'
    )
    const material = mesh.material as MeshStandardMaterial
    const shader = {
      uniforms: {} as Record<string, { value: unknown }>,
      vertexShader: `void main() {
#include <uv_vertex>
#include <begin_vertex>
}`,
      fragmentShader: `void main() {
#include <map_fragment>
#include <emissivemap_fragment>
#include <lights_fragment_end>
}`
    }
    material.onBeforeCompile(shader as never, {} as never)

    expect(preparation.materials).toMatchObject([{
      name: 'diagnostic',
      sections: [0],
      blendMode: 'alphablend',
      panRate: 0.5,
      panRateV: 0,
      rotationRate: 0,
      uvOscillation: {
        uRate: 0.1,
        vRate: 0.1,
        uAmplitude: 0.05,
        vAmplitude: 0.05
      },
      textures: [
        { role: 'diffuse', enabled: true },
        { role: 'opacity', enabled: true }
      ]
    }])
    const [inspection] = preparation.materials
    expect(inspection).toBeDefined()
    preparation.setMaterialEnabled(inspection!.id, false)
    preparation.setTextureEnabled(inspection!.id, 'diffuse', false)
    preparation.setBehaviorEnabled(inspection!.id, 'uvEffects', false)
    preparation.setBehaviorEnabled(inspection!.id, 'twoSided', false)

    expect(material.visible).toBe(false)
    expect(shader.uniforms.l2DiffuseEnabled?.value).toBe(0)
    expect(shader.uniforms.l2UvEffectsEnabled?.value).toBe(0)

    preparation.reset()

    expect(material.visible).toBe(true)
    expect(shader.uniforms.l2DiffuseEnabled?.value).toBe(1)
    expect(shader.uniforms.l2UvEffectsEnabled?.value).toBe(1)
    expect(preparation.materials[0]?.behaviors.find(item => item.behavior === 'twoSided')?.enabled).toBe(true)
    preparation.dispose()
    fallback.dispose()
    mesh.geometry.dispose()
  })

  it('keeps the Cave43 skeleton sections and their authored render modes inspectable', async () => {
    vi.spyOn(TextureLoader.prototype, 'load')
      .mockImplementation((_url, onLoad) => {
        const texture = new Texture()
        onLoad(texture)
        return texture
      })
    const fallback = new MeshStandardMaterial()
    const materials = [
      ['d_vally_skeleton02', 'opaque'],
      ['d_vally_skeleton03', 'opaque'],
      ['d_vally_skeleton06', 'alphablend']
    ].map(([name, blendMode]) => {
      const material = new MeshStandardMaterial({ name })
      material.userData.l2 = {
        diffuseUrl: `/versions/c1/Textures/Giran_antaras_t/${name}.webp`,
        blendMode
      }
      return material
    })
    const mesh = new Mesh(new PlaneGeometry(1, 1), materials)

    const preparation = await prepareStaticMeshMaterials(
      mesh,
      fallback,
      'https://assets.test/versions/c1/Meshes/Giran_antaras_s/Giran_antaras_cave43.glb'
    )

    expect(preparation.materials.map(({ name, sections, blendMode, textures }) => ({
      name,
      sections,
      blendMode,
      diffuse: textures.find(texture => texture.role === 'diffuse')?.url
    }))).toEqual([
      {
        name: 'd_vally_skeleton02',
        sections: [0],
        blendMode: 'opaque',
        diffuse: 'https://assets.test/versions/c1/Textures/Giran_antaras_t/d_vally_skeleton02.webp'
      },
      {
        name: 'd_vally_skeleton03',
        sections: [1],
        blendMode: 'opaque',
        diffuse: 'https://assets.test/versions/c1/Textures/Giran_antaras_t/d_vally_skeleton03.webp'
      },
      {
        name: 'd_vally_skeleton06',
        sections: [2],
        blendMode: 'alphablend',
        diffuse: 'https://assets.test/versions/c1/Textures/Giran_antaras_t/d_vally_skeleton06.webp'
      }
    ])

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
