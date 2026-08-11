import type {
  LevelTerrainLayerManifestEntry,
  LevelTerrainManifestEntry
} from '~/types/studio'
import {
  MaterialPluginBase,
  PBRMaterial,
  RawTexture2DArray,
  Texture,
  type BaseTexture,
  type MaterialDefines,
  type Scene,
  type UniformBuffer
} from '@babylonjs/core'

export interface TerrainMaterialResult {
  material?: PBRMaterial
  controller?: TerrainMaterialController
  ready?: Promise<void>
  error?: string
}

export interface TerrainMaterialController {
  setLayerEnabled(index: number, enabled: boolean): void
  setAllLayersEnabled(enabled: boolean): void
}

const channelComponents = ['r', 'g', 'b', 'a'] as const

export function terrainSamplerCount(terrain: LevelTerrainManifestEntry) {
  return (
    new Set(terrain.layers.map((layer) => layer.textureArrayGroup)).size + 1
  )
}

export function createTerrainMaterial(
  terrain: LevelTerrainManifestEntry,
  scene: Scene
): TerrainMaterialResult {
  if (terrain.materialStatus !== 'resolved')
    return { error: terrain.materialError ?? 'Terrain material is unresolved.' }
  if (!terrain.layers.length || !terrain.controlMapUrls.length)
    return { error: 'Terrain material has no layers or control maps.' }
  if (terrain.controlMapEncoding !== 'webp-rgb-a-horizontal')
    return { error: 'Terrain control-map encoding is unsupported.' }
  if (!scene.getEngine().getCaps().texture2DArrayMaxLayerCount)
    return { error: 'Terrain texture arrays require WebGL2.' }
  if (
    terrain.controlMapWidth <= 0 ||
    terrain.controlMapHeight <= 0 ||
    terrain.layers.some(
      (layer) =>
        !layer.textureUrl ||
        layer.textureWidth <= 0 ||
        layer.textureHeight <= 0 ||
        !['xy', 'xz', 'yz'].includes(layer.textureMapAxis) ||
        !finiteUvTransform(layer) ||
        layer.controlMapChannel < 0 ||
        layer.controlMapChannel > 3 ||
        layer.controlMapIndex < 0 ||
        layer.controlMapIndex >= terrain.controlMapUrls.length
    )
  )
    return { error: 'Terrain contains an unsupported or incomplete layer.' }

  const material = new PBRMaterial(`${terrain.name}:material`, scene)
  material.metallic = 0
  material.roughness = 1
  material.environmentIntensity = 0
  material.specularIntensity = 0
  material.maxSimultaneousLights = 4
  const controller = new TerrainLayerPlugin(material, terrain)
  return { material, controller, ready: controller.ready }
}

class TerrainLayerPlugin
  extends MaterialPluginBase
  implements TerrainMaterialController
{
  private readonly diffuseTextures = new Map<number, RawTexture2DArray>()
  private readonly controlTexture: RawTexture2DArray
  private readonly enabledLayers: boolean[]
  private readonly definitions: string
  private readonly fragmentBlend: string
  readonly ready: Promise<void>

  constructor(material: PBRMaterial, terrain: LevelTerrainManifestEntry) {
    super(material, 'L2TerrainTextureArrays', 200)
    const scene = material.getScene()
    const groups = new Map<number, LevelTerrainLayerManifestEntry[]>()
    for (const layer of terrain.layers) {
      const layers = groups.get(layer.textureArrayGroup) ?? []
      layers.push(layer)
      groups.set(layer.textureArrayGroup, layers)
    }
    const loads: Promise<void>[] = []
    for (const [group, layers] of groups) {
      const width = layers[0]!.textureWidth
      const height = layers[0]!.textureHeight
      const depth =
        Math.max(...layers.map((layer) => layer.textureArrayLayer)) + 1
      const texture = RawTexture2DArray.CreateRGBATexture(
        new Uint8Array(width * height * depth * 4),
        width,
        height,
        depth,
        scene,
        true,
        false,
        Texture.TRILINEAR_SAMPLINGMODE
      )
      texture.name = `${terrain.name}:diffuse-array-${group}`
      texture.gammaSpace = true
      this.diffuseTextures.set(group, texture)
      loads.push(
        loadTextureArray(
          texture,
          width,
          height,
          depth,
          layers.map((layer) => ({
            url: layer.textureUrl!,
            layer: layer.textureArrayLayer
          }))
        )
      )
    }
    this.controlTexture = RawTexture2DArray.CreateRGBATexture(
      new Uint8Array(
        terrain.controlMapWidth *
          terrain.controlMapHeight *
          terrain.controlMapUrls.length *
          4
      ),
      terrain.controlMapWidth,
      terrain.controlMapHeight,
      terrain.controlMapUrls.length,
      scene,
      false,
      false,
      Texture.BILINEAR_SAMPLINGMODE
    )
    this.controlTexture.name = `${terrain.name}:control-array-${terrain.controlMapArrayGroup}`
    this.controlTexture.gammaSpace = false
    loads.push(
      loadControlTextureArray(
        this.controlTexture,
        terrain.controlMapWidth,
        terrain.controlMapHeight,
        terrain.controlMapUrls.length,
        terrain.controlMapUrls.map((url, layer) => ({ url, layer }))
      )
    )
    this.ready = Promise.all(loads).then(() => undefined)
    this.enabledLayers = terrain.layers.map(() => true)
    this.definitions = [
      'varying vec2 vTerrainUV;',
      'varying vec3 vTerrainPosition;',
      ...[...this.diffuseTextures].map(
        ([group]) => `uniform highp sampler2DArray terrainDiffuseArray${group};`
      ),
      'uniform highp sampler2DArray terrainControlArray;'
    ].join('\n')
    this.fragmentBlend = blendShader(terrain)
    this._enable(true)
  }

  override prepareDefines(defines: MaterialDefines) {
    const values = defines as MaterialDefines & {
      _needUVs: boolean
      UV1: boolean
    }
    values._needUVs = true
    values.UV1 = true
  }

  override getAttributes(attributes: string[]) {
    attributes.push('uv')
  }

  override getSamplers(samplers: string[]) {
    for (const group of this.diffuseTextures.keys())
      samplers.push(`terrainDiffuseArray${group}`)
    samplers.push('terrainControlArray')
  }

  override getUniforms() {
    return {
      ubo: [
        ...this.enabledLayers.map((_, index) => ({
          name: `terrainLayerEnabled${index}`,
          size: 1,
          type: 'float'
        })),
        { name: 'terrainAnyLayerEnabled', size: 1, type: 'float' }
      ],
      fragment: this.definitions
    }
  }

  override bindForSubMesh(uniformBuffer: UniformBuffer) {
    for (const [group, texture] of this.diffuseTextures)
      uniformBuffer.setTexture(`terrainDiffuseArray${group}`, texture)
    uniformBuffer.setTexture('terrainControlArray', this.controlTexture)
    this.enabledLayers.forEach((enabled, index) =>
      uniformBuffer.updateFloat(`terrainLayerEnabled${index}`, enabled ? 1 : 0)
    )
    uniformBuffer.updateFloat(
      'terrainAnyLayerEnabled',
      this.enabledLayers.some(Boolean) ? 1 : 0
    )
  }

  setLayerEnabled(index: number, enabled: boolean) {
    if (index >= 0 && index < this.enabledLayers.length)
      this.enabledLayers[index] = enabled
  }

  setAllLayersEnabled(enabled: boolean) {
    this.enabledLayers.fill(enabled)
  }

  override getActiveTextures(activeTextures: BaseTexture[]) {
    activeTextures.push(...this.diffuseTextures.values(), this.controlTexture)
  }

  override hasTexture(texture: BaseTexture) {
    return (
      [...this.diffuseTextures.values()].includes(
        texture as RawTexture2DArray
      ) || texture === this.controlTexture
    )
  }

  override dispose(forceDisposeTextures?: boolean) {
    if (!forceDisposeTextures) return
    for (const texture of this.diffuseTextures.values()) texture.dispose()
    this.controlTexture.dispose()
  }

  override getCustomCode(shaderType: string): Record<string, string> | null {
    if (shaderType === 'vertex')
      return {
        CUSTOM_VERTEX_DEFINITIONS:
          'varying vec2 vTerrainUV;\nvarying vec3 vTerrainPosition;',
        CUSTOM_VERTEX_MAIN_END:
          'vTerrainUV = uvUpdated;\nvTerrainPosition = positionUpdated;'
      }
    if (shaderType === 'fragment')
      return {
        CUSTOM_FRAGMENT_DEFINITIONS: this.definitions,
        CUSTOM_FRAGMENT_UPDATE_ALBEDO: this.fragmentBlend
      }
    return null
  }
}

async function loadTextureArray(
  texture: RawTexture2DArray,
  width: number,
  height: number,
  depth: number,
  sources: { url: string; layer: number }[]
) {
  if (
    typeof document === 'undefined' ||
    typeof createImageBitmap === 'undefined'
  )
    return
  const pixels = new Uint8Array(width * height * depth * 4)
  await Promise.all(
    sources.map(async ({ url, layer }) => {
      const response = await fetch(url)
      if (!response.ok)
        throw new Error(`Unable to load terrain texture ${url}.`)
      const bitmap = await createImageBitmap(await response.blob(), {
        colorSpaceConversion: 'none',
        premultiplyAlpha: 'none'
      })
      if (bitmap.width !== width || bitmap.height !== height) {
        bitmap.close()
        throw new Error(`Terrain texture ${url} has unexpected dimensions.`)
      }
      const canvas = document.createElement('canvas')
      canvas.width = width
      canvas.height = height
      const context = canvas.getContext('2d', { willReadFrequently: true })
      if (!context)
        throw new Error('A 2D canvas is required for terrain textures.')
      context.drawImage(bitmap, 0, 0)
      bitmap.close()
      pixels.set(
        context.getImageData(0, 0, width, height).data,
        layer * width * height * 4
      )
    })
  )
  texture.update(pixels)
}

async function loadControlTextureArray(
  texture: RawTexture2DArray,
  width: number,
  height: number,
  depth: number,
  sources: { url: string; layer: number }[]
) {
  if (
    typeof document === 'undefined' ||
    typeof createImageBitmap === 'undefined'
  )
    return
  const pixels = await assembleTerrainControlArray(
    width,
    height,
    depth,
    sources,
    async (url) => {
      const response = await fetch(url)
      if (!response.ok)
        throw new Error(`Unable to load terrain control map ${url}.`)
      const bitmap = await createImageBitmap(await response.blob(), {
        colorSpaceConversion: 'none',
        premultiplyAlpha: 'none'
      })
      if (bitmap.width !== width * 2 || bitmap.height !== height) {
        bitmap.close()
        throw new Error(
          `Terrain control map ${url} has unexpected encoded dimensions.`
        )
      }
      const canvas = document.createElement('canvas')
      canvas.width = bitmap.width
      canvas.height = bitmap.height
      const context = canvas.getContext('2d', { willReadFrequently: true })
      if (!context) {
        bitmap.close()
        throw new Error('A 2D canvas is required for terrain control maps.')
      }
      context.drawImage(bitmap, 0, 0)
      bitmap.close()
      return context.getImageData(0, 0, canvas.width, canvas.height).data
    }
  )
  texture.update(pixels)
}

export async function assembleTerrainControlArray(
  width: number,
  height: number,
  depth: number,
  sources: { url: string; layer: number }[],
  loadPixels: (url: string) => Promise<ArrayLike<number>>
) {
  if (!Number.isInteger(depth) || depth <= 0)
    throw new Error(
      'Terrain control-map array depth must be a positive integer.'
    )
  const pixels = new Uint8Array(width * height * depth * 4)
  await Promise.all(
    sources.map(async ({ url, layer }) => {
      if (!Number.isInteger(layer) || layer < 0 || layer >= depth)
        throw new Error(`Terrain control-map layer ${layer} is out of range.`)
      pixels.set(
        unpackTerrainControlPixels(await loadPixels(url), width, height),
        layer * width * height * 4
      )
    })
  )
  return pixels
}

export function unpackTerrainControlPixels(
  encoded: ArrayLike<number>,
  width: number,
  height: number
) {
  if (
    !Number.isInteger(width) ||
    width <= 0 ||
    !Number.isInteger(height) ||
    height <= 0
  )
    throw new Error('Terrain control-map dimensions must be positive integers.')
  const encodedWidth = width * 2
  if (encoded.length !== encodedWidth * height * 4)
    throw new Error(
      'Terrain control-map pixel data does not match its encoded dimensions.'
    )

  const pixels = new Uint8Array(width * height * 4)
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const left = (y * encodedWidth + x) * 4
      const right = (y * encodedWidth + width + x) * 4
      if (encoded[left + 3] !== 255 || encoded[right + 3] !== 255)
        throw new Error('Terrain control-map transport pixels must be opaque.')
      const target = (y * width + x) * 4
      pixels[target] = encoded[left]!
      pixels[target + 1] = encoded[left + 1]!
      pixels[target + 2] = encoded[left + 2]!
      pixels[target + 3] = encoded[right]!
    }
  }
  return pixels
}

export function blendShader(terrain: LevelTerrainManifestEntry) {
  const lines = ['vec3 terrainColor = vec3(0.0);']
  terrain.layers.forEach((layer, index) => {
    const component = channelComponents[layer.controlMapChannel]
    lines.push(
      `vec2 terrainLayerUV${index} = ${layerUv(layer)};`,
      `vec3 terrainLayerColor${index} = texture(terrainDiffuseArray${layer.textureArrayGroup}, vec3(terrainLayerUV${index}, ${glsl(layer.textureArrayLayer)})).rgb;`,
      `float terrainLayerWeight${index} = texture(terrainControlArray, vec3(vTerrainUV, ${glsl(layer.controlMapIndex)})).${component} * terrainLayerEnabled${index};`,
      `terrainColor = mix(terrainColor, terrainLayerColor${index}, terrainLayerWeight${index});`
    )
  })
  lines.push(
    'surfaceAlbedo = terrainAnyLayerEnabled > 0.5 ? terrainColor : vec3(0.18);'
  )
  return lines.join('\n')
}

function layerUv(layer: LevelTerrainLayerManifestEntry) {
  const { u, v } = layer.uvTransform
  return `vec2(dot(vTerrainPosition, vec3(${glsl(u.x)}, ${glsl(u.y)}, ${glsl(u.z)})) + ${glsl(u.offset)}, dot(vTerrainPosition, vec3(${glsl(v.x)}, ${glsl(v.y)}, ${glsl(v.z)})) + ${glsl(v.offset)})`
}

function finiteUvTransform(layer: LevelTerrainLayerManifestEntry) {
  const { u, v } = layer.uvTransform ?? {}
  return Boolean(
    u &&
    v &&
    [u.x, u.y, u.z, u.offset, v.x, v.y, v.z, v.offset].every(Number.isFinite)
  )
}

function glsl(value: number) {
  if (!Number.isFinite(value)) return '0.0'
  const formatted = value
    .toPrecision(9)
    .replace(/(?:\.0+|(?:(\.\d*?)0+))$/, '$1')
  return formatted.includes('.') || /e/i.test(formatted)
    ? formatted
    : `${formatted}.0`
}
