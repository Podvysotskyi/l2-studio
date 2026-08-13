import type {
  MapTerrainLayerManifestEntry,
  MapTerrainManifestEntry
} from '~/types/studio'
import {
  ClampToEdgeWrapping,
  DataArrayTexture,
  DoubleSide,
  GLSL3,
  LinearFilter,
  LinearMipmapLinearFilter,
  RGBAFormat,
  RepeatWrapping,
  ShaderMaterial,
  UnsignedByteType,
  type WebGLRenderer
} from 'three'

export interface TerrainMaterialController {
  material: ShaderMaterial
  ready: Promise<void>
  setLayerEnabled(index: number, enabled: boolean): void
  setAllLayersEnabled(enabled: boolean): void
  dispose(): void
}

const channelComponents = ['r', 'g', 'b', 'a'] as const

export function terrainSamplerCount(terrain: MapTerrainManifestEntry) {
  return new Set(terrain.layers.map(layer => layer.textureArrayGroup)).size + 1
}

export function validateTerrainMaterial(
  terrain: MapTerrainManifestEntry,
  renderer?: WebGLRenderer
) {
  if (terrain.materialStatus !== 'resolved')
    return terrain.materialError ?? 'Terrain material is unresolved.'
  if (!terrain.layers.length || !terrain.controlMapUrls.length)
    return 'Terrain material has no layers or control maps.'
  if (terrain.controlMapEncoding !== 'webp-rgb-a-horizontal')
    return 'Terrain control-map encoding is unsupported.'
  if (
    terrain.controlMapWidth <= 0 ||
    terrain.controlMapHeight <= 0 ||
    terrain.layers.some(
      layer =>
        !layer.textureUrl ||
        layer.textureWidth <= 0 ||
        layer.textureHeight <= 0 ||
        !Number.isInteger(layer.textureArrayGroup) ||
        layer.textureArrayGroup < 0 ||
        !Number.isInteger(layer.textureArrayLayer) ||
        layer.textureArrayLayer < 0 ||
        !['xy', 'xz', 'yz'].includes(layer.textureMapAxis) ||
        !finiteUvTransform(layer) ||
        !Number.isInteger(layer.controlMapChannel) ||
        layer.controlMapChannel < 0 ||
        layer.controlMapChannel > 3 ||
        !Number.isInteger(layer.controlMapIndex) ||
        layer.controlMapIndex < 0 ||
        layer.controlMapIndex >= terrain.controlMapUrls.length
    )
  )
    return 'Terrain contains an unsupported or incomplete layer.'

  const groups = groupLayers(terrain.layers)
  if (
    [...groups.values()].some(layers =>
      layers.some(
        layer =>
          layer.textureWidth !== layers[0]!.textureWidth ||
          layer.textureHeight !== layers[0]!.textureHeight
      )
    )
  )
    return 'Terrain texture-array layers have inconsistent dimensions.'

  if (renderer) {
    if (terrainSamplerCount(terrain) > renderer.capabilities.maxTextures)
      return 'Terrain exceeds the available texture sampler limit.'
    const context = renderer.getContext() as WebGL2RenderingContext
    const maximumLayers = context.getParameter(
      context.MAX_ARRAY_TEXTURE_LAYERS
    ) as number
    const requiredLayers = Math.max(
      terrain.controlMapUrls.length,
      ...[...groups.values()].map(
        layers => Math.max(...layers.map(layer => layer.textureArrayLayer)) + 1
      )
    )
    if (requiredLayers > maximumLayers)
      return 'Terrain exceeds the available texture-array layer limit.'
  }
}

export function createTerrainMaterial(
  terrain: MapTerrainManifestEntry,
  renderer: WebGLRenderer
): TerrainMaterialController {
  const validationError = validateTerrainMaterial(terrain, renderer)
  if (validationError) throw new Error(validationError)

  const groups = groupLayers(terrain.layers)
  const diffuseTextures = new Map<number, DataArrayTexture>()
  const uniforms: Record<string, { value: unknown }> = {}
  const loads: Promise<void>[] = []

  for (const [group, layers] of groups) {
    const width = layers[0]!.textureWidth
    const height = layers[0]!.textureHeight
    const depth = Math.max(...layers.map(layer => layer.textureArrayLayer)) + 1
    const texture = dataArrayTexture(width, height, depth, true)
    texture.wrapS = RepeatWrapping
    texture.wrapT = RepeatWrapping
    texture.minFilter = LinearMipmapLinearFilter
    texture.magFilter = LinearFilter
    texture.generateMipmaps = true
    diffuseTextures.set(group, texture)
    uniforms[`terrainDiffuseArray${group}`] = { value: texture }
    loads.push(
      loadTextureArray(
        texture,
        width,
        height,
        layers.map(layer => ({
          url: layer.textureUrl!,
          layer: layer.textureArrayLayer
        }))
      )
    )
  }

  const controlTexture = dataArrayTexture(
    terrain.controlMapWidth,
    terrain.controlMapHeight,
    terrain.controlMapUrls.length,
    false
  )
  controlTexture.wrapS = ClampToEdgeWrapping
  controlTexture.wrapT = ClampToEdgeWrapping
  controlTexture.minFilter = LinearFilter
  controlTexture.magFilter = LinearFilter
  controlTexture.generateMipmaps = false
  uniforms.terrainControlArray = { value: controlTexture }
  loads.push(
    loadControlTextureArray(
      controlTexture,
      terrain.controlMapWidth,
      terrain.controlMapHeight,
      terrain.controlMapUrls.map((url, layer) => ({ url, layer }))
    )
  )

  const enabledLayers = terrain.layers.map(() => true)
  enabledLayers.forEach((enabled, index) => {
    uniforms[`terrainLayerEnabled${index}`] = { value: enabled ? 1 : 0 }
  })
  uniforms.terrainAnyLayerEnabled = { value: 1 }

  const material = new ShaderMaterial({
    name: `${terrain.name}:material`,
    uniforms,
    vertexShader: terrainVertexShader(),
    fragmentShader: terrainFragmentShader(terrain),
    glslVersion: GLSL3,
    side: DoubleSide
  })

  const syncVisibility = () => {
    enabledLayers.forEach((enabled, index) => {
      uniforms[`terrainLayerEnabled${index}`]!.value = enabled ? 1 : 0
    })
    uniforms.terrainAnyLayerEnabled!.value = enabledLayers.some(Boolean) ? 1 : 0
  }

  return {
    material,
    ready: Promise.all(loads).then(() => undefined),
    setLayerEnabled(index, enabled) {
      if (index < 0 || index >= enabledLayers.length) return
      enabledLayers[index] = enabled
      syncVisibility()
    },
    setAllLayersEnabled(enabled) {
      enabledLayers.fill(enabled)
      syncVisibility()
    },
    dispose() {
      material.dispose()
      diffuseTextures.forEach(texture => texture.dispose())
      controlTexture.dispose()
    }
  }
}

function dataArrayTexture(
  width: number,
  height: number,
  depth: number,
  diffuse: boolean
) {
  const texture = new DataArrayTexture(
    new Uint8Array(width * height * depth * 4),
    width,
    height,
    depth
  )
  texture.format = RGBAFormat
  texture.type = UnsignedByteType
  texture.flipY = false
  texture.unpackAlignment = 1
  texture.userData.diffuse = diffuse
  return texture
}

function groupLayers(layers: MapTerrainLayerManifestEntry[]) {
  const groups = new Map<number, MapTerrainLayerManifestEntry[]>()
  for (const layer of layers) {
    const group = groups.get(layer.textureArrayGroup) ?? []
    group.push(layer)
    groups.set(layer.textureArrayGroup, group)
  }
  return groups
}

async function loadTextureArray(
  texture: DataArrayTexture,
  width: number,
  height: number,
  sources: { url: string; layer: number }[]
) {
  const pixels = texture.image.data as Uint8Array
  await Promise.all(
    sources.map(async ({ url, layer }) => {
      const decoded = await loadImagePixels(url, width, height)
      pixels.set(decoded, layer * width * height * 4)
    })
  )
  texture.needsUpdate = true
}

async function loadControlTextureArray(
  texture: DataArrayTexture,
  width: number,
  height: number,
  sources: { url: string; layer: number }[]
) {
  texture.image.data = await assembleTerrainControlArray(
    width,
    height,
    texture.image.depth,
    sources,
    url => loadImagePixels(url, width * 2, height)
  )
  texture.needsUpdate = true
}

async function loadImagePixels(url: string, width: number, height: number) {
  const response = await fetch(url)
  if (!response.ok) throw new Error(`Unable to load terrain texture ${url}.`)
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
  if (!context) {
    bitmap.close()
    throw new Error('A 2D canvas is required for terrain textures.')
  }
  context.drawImage(bitmap, 0, 0)
  bitmap.close()
  return context.getImageData(0, 0, width, height).data
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

export function blendShader(terrain: MapTerrainManifestEntry) {
  const lines = ['vec3 terrainColor = vec3(0.0);']
  terrain.layers.forEach((layer, index) => {
    const component = channelComponents[layer.controlMapChannel]
    lines.push(
      `vec2 terrainLayerUV${index} = ${layerUv(layer)};`,
      `vec3 terrainLayerColor${index} = srgbToLinear(texture(terrainDiffuseArray${layer.textureArrayGroup}, vec3(terrainLayerUV${index}, ${glsl(layer.textureArrayLayer)})).rgb);`,
      `float terrainLayerWeight${index} = texture(terrainControlArray, vec3(vTerrainUV, ${glsl(layer.controlMapIndex)})).${component} * terrainLayerEnabled${index};`,
      `terrainColor = mix(terrainColor, terrainLayerColor${index}, terrainLayerWeight${index});`
    )
  })
  lines.push(
    'terrainColor = terrainAnyLayerEnabled > 0.5 ? terrainColor : vec3(0.18);'
  )
  return lines.join('\n')
}

function terrainVertexShader() {
  return `
    varying vec2 vTerrainUV;
    varying vec3 vTerrainPosition;
    varying vec3 vTerrainNormal;
    void main() {
      vTerrainUV = uv;
      vTerrainPosition = position;
      vTerrainNormal = normalize(normalMatrix * normal);
      gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
    }
  `
}

function terrainFragmentShader(terrain: MapTerrainManifestEntry) {
  const samplers = [...new Set(
    terrain.layers.map(layer => layer.textureArrayGroup)
  )].map(group => `uniform highp sampler2DArray terrainDiffuseArray${group};`)
  const toggles = terrain.layers.map(
    (_, index) => `uniform float terrainLayerEnabled${index};`
  )
  return `
    precision highp float;
    precision highp sampler2DArray;
    varying vec2 vTerrainUV;
    varying vec3 vTerrainPosition;
    varying vec3 vTerrainNormal;
    ${samplers.join('\n')}
    uniform highp sampler2DArray terrainControlArray;
    ${toggles.join('\n')}
    uniform float terrainAnyLayerEnabled;
    vec3 srgbToLinear(vec3 value) {
      return mix(value / 12.92, pow((value + 0.055) / 1.055, vec3(2.4)), step(vec3(0.04045), value));
    }
    void main() {
      ${blendShader(terrain)}
      vec3 lightDirection = normalize(vec3(0.35, 0.8, 0.45));
      float diffuse = max(dot(normalize(vTerrainNormal), lightDirection), 0.0);
      gl_FragColor = vec4(terrainColor * (0.55 + diffuse * 0.65), 1.0);
      #include <tonemapping_fragment>
      #include <colorspace_fragment>
    }
  `
}

function layerUv(layer: MapTerrainLayerManifestEntry) {
  const { u, v } = layer.uvTransform
  return `vec2(dot(vTerrainPosition, vec3(${glsl(u.x)}, ${glsl(u.y)}, ${glsl(u.z)})) + ${glsl(u.offset)}, dot(vTerrainPosition, vec3(${glsl(v.x)}, ${glsl(v.y)}, ${glsl(v.z)})) + ${glsl(v.offset)})`
}

function finiteUvTransform(layer: MapTerrainLayerManifestEntry) {
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
