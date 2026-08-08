import type {
  LevelTerrainLayerManifestEntry,
  LevelTerrainManifestEntry
} from '@l2/ui'
import {
  MaterialPluginBase,
  PBRMaterial,
  Texture,
  type BaseTexture,
  type MaterialDefines,
  type Scene,
  type UniformBuffer
} from '@babylonjs/core'

export interface TerrainMaterialResult {
  material?: PBRMaterial
  controller?: TerrainMaterialController
  error?: string
}

export interface TerrainMaterialController {
  setLayerEnabled(index: number, enabled: boolean): void
  setAllLayersEnabled(enabled: boolean): void
}

const channelComponents = ['r', 'g', 'b', 'a'] as const
export function terrainSamplerCount(terrain: LevelTerrainManifestEntry) {
  return terrain.layers.length + terrain.controlMapUrls.length
}

export function createTerrainMaterial(
  terrain: LevelTerrainManifestEntry,
  scene: Scene
): TerrainMaterialResult {
  if (terrain.materialStatus !== 'resolved') {
    return { error: terrain.materialError ?? 'Terrain material is unresolved.' }
  }
  if (!terrain.layers.length || !terrain.controlMapUrls.length) {
    return { error: 'Terrain material has no layers or control maps.' }
  }
  if (
    terrain.layers.some(
      (layer) =>
        !layer.textureUrl ||
        !['xy', 'xz', 'yz'].includes(layer.textureMapAxis) ||
        !finiteUvTransform(layer) ||
        layer.controlMapChannel < 0 ||
        layer.controlMapChannel > 3 ||
        layer.controlMapIndex < 0 ||
        layer.controlMapIndex >= terrain.controlMapUrls.length
    )
  ) {
    return { error: 'Terrain contains an unsupported or incomplete layer.' }
  }

  const requiredSamplers = terrainSamplerCount(terrain)
  const availableSamplers = scene.getEngine().getCaps().maxTexturesImageUnits
  if (requiredSamplers > availableSamplers) {
    return {
      error: `Terrain requires ${requiredSamplers} texture samplers; this device supports ${availableSamplers}.`
    }
  }

  const material = new PBRMaterial(`${terrain.name}:material`, scene)
  material.metallic = 0
  material.roughness = 1
  material.environmentIntensity = 0
  material.specularIntensity = 0
  material.maxSimultaneousLights = 4
  const controller = new TerrainLayerPlugin(material, terrain)
  return { material, controller }
}

class TerrainLayerPlugin extends MaterialPluginBase {
  private readonly diffuseTextures: Texture[]
  private readonly controlTextures: Texture[]
  private readonly fragmentDefinitions: string
  private readonly fragmentBlend: string
  private readonly enabledLayers: boolean[]

  constructor(material: PBRMaterial, terrain: LevelTerrainManifestEntry) {
    super(material, 'L2TerrainLayers', 200)
    const scene = material.getScene()
    this.diffuseTextures = terrain.layers.map((layer, index) => {
      const texture = new Texture(
        layer.textureUrl!,
        scene,
        false,
        false,
        Texture.TRILINEAR_SAMPLINGMODE
      )
      texture.name = `${terrain.name}:diffuse-${index}`
      texture.wrapU = Texture.WRAP_ADDRESSMODE
      texture.wrapV = Texture.WRAP_ADDRESSMODE
      texture.gammaSpace = true
      return texture
    })
    this.controlTextures = terrain.controlMapUrls.map((url, index) => {
      const texture = new Texture(
        url,
        scene,
        false,
        false,
        Texture.BILINEAR_SAMPLINGMODE
      )
      texture.name = `${terrain.name}:control-${index}`
      texture.wrapU = Texture.CLAMP_ADDRESSMODE
      texture.wrapV = Texture.CLAMP_ADDRESSMODE
      texture.gammaSpace = false
      return texture
    })
    this.enabledLayers = terrain.layers.map(() => true)
    this.fragmentDefinitions = [
      'varying vec2 vTerrainUV;',
      'varying vec3 vTerrainPosition;',
      ...this.diffuseTextures.map(
        (_, index) => `uniform sampler2D terrainDiffuse${index};`
      ),
      ...this.controlTextures.map(
        (_, index) => `uniform sampler2D terrainControl${index};`
      )
    ].join('\n')
    this.fragmentBlend = blendShader(terrain)
    this._enable(true)
  }

  override prepareDefines(defines: MaterialDefines) {
    const uvDefines = defines as MaterialDefines & {
      _needUVs: boolean
      UV1: boolean
    }
    uvDefines._needUVs = true
    uvDefines.UV1 = true
  }

  override getAttributes(attributes: string[]) {
    attributes.push('uv')
  }

  override getSamplers(samplers: string[]) {
    this.diffuseTextures.forEach((_, index) =>
      samplers.push(`terrainDiffuse${index}`)
    )
    this.controlTextures.forEach((_, index) =>
      samplers.push(`terrainControl${index}`)
    )
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
      fragment: this.fragmentDefinitions
    }
  }

  override bindForSubMesh(uniformBuffer: UniformBuffer) {
    this.diffuseTextures.forEach((texture, index) =>
      uniformBuffer.setTexture(`terrainDiffuse${index}`, texture)
    )
    this.controlTextures.forEach((texture, index) =>
      uniformBuffer.setTexture(`terrainControl${index}`, texture)
    )
    this.enabledLayers.forEach((enabled, index) =>
      uniformBuffer.updateFloat(`terrainLayerEnabled${index}`, enabled ? 1 : 0)
    )
    uniformBuffer.updateFloat(
      'terrainAnyLayerEnabled',
      this.enabledLayers.some(Boolean) ? 1 : 0
    )
  }

  setLayerEnabled(index: number, enabled: boolean) {
    if (index < 0 || index >= this.enabledLayers.length) return
    this.enabledLayers[index] = enabled
  }

  setAllLayersEnabled(enabled: boolean) {
    this.enabledLayers.fill(enabled)
  }

  override getActiveTextures(activeTextures: BaseTexture[]) {
    activeTextures.push(...this.diffuseTextures, ...this.controlTextures)
  }

  override hasTexture(texture: BaseTexture) {
    return (
      this.diffuseTextures.includes(texture as Texture) ||
      this.controlTextures.includes(texture as Texture)
    )
  }

  override dispose(forceDisposeTextures?: boolean) {
    if (forceDisposeTextures) {
      this.diffuseTextures.forEach((texture) => texture.dispose())
      this.controlTextures.forEach((texture) => texture.dispose())
    }
  }

  override getCustomCode(shaderType: string): Record<string, string> | null {
    if (shaderType === 'vertex') {
      return {
        CUSTOM_VERTEX_DEFINITIONS:
          'varying vec2 vTerrainUV;\nvarying vec3 vTerrainPosition;',
        CUSTOM_VERTEX_MAIN_END:
          'vTerrainUV = uvUpdated;\nvTerrainPosition = positionUpdated;'
      }
    }
    if (shaderType === 'fragment') {
      return {
        CUSTOM_FRAGMENT_DEFINITIONS: this.fragmentDefinitions,
        CUSTOM_FRAGMENT_UPDATE_ALBEDO: this.fragmentBlend
      }
    }
    return null
  }
}

function blendShader(terrain: LevelTerrainManifestEntry) {
  const lines = ['vec3 terrainColor = vec3(0.0);']
  terrain.layers.forEach((layer, index) => {
    const uv = layerUv(layer)
    const component = channelComponents[layer.controlMapChannel]
    lines.push(
      `vec2 terrainLayerUV${index} = ${uv};`,
      `vec3 terrainLayerColor${index} = texture2D(terrainDiffuse${index}, terrainLayerUV${index}).rgb;`,
      `float terrainLayerWeight${index} = texture2D(terrainControl${layer.controlMapIndex}, vTerrainUV).${component} * terrainLayerEnabled${index};`,
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
  const transform = layer.uvTransform
  if (!transform) return false
  const { u, v } = transform
  return [u.x, u.y, u.z, u.offset, v.x, v.y, v.z, v.offset].every(
    Number.isFinite
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

export { blendShader }
