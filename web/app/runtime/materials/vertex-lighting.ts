import type { MapVertexLightingReference } from '~/types/studio'
import {
  MaterialPluginBase,
  InstancedMesh,
  PBRMaterial,
  Texture,
  type AbstractMesh,
  type BaseTexture,
  type UniformBuffer
} from '@babylonjs/core'
import { browserDecodedTextureUrl } from '../core/texture-url.js'

export function applyVertexLighting(
  mesh: AbstractMesh,
  reference: MapVertexLightingReference
) {
  if (mesh.getTotalVertices() !== reference.vertexCount) return null
  if (!(mesh.material instanceof PBRMaterial)) return null
  if (mesh instanceof InstancedMesh) {
    const source = mesh.sourceMesh
    const plugin = pluginFor(source.material, reference)
    if (!plugin) return null
    source.registerInstancedBuffer('l2LightingTexelOffset', 1)
    source.registerInstancedBuffer('l2LightingEnabled', 1)
    mesh.instancedBuffers.l2LightingTexelOffset = reference.texelOffset
    mesh.instancedBuffers.l2LightingEnabled = 1
    return null
  }
  return null
}

const sharedPlugins = new WeakMap<PBRMaterial, VertexLightingPlugin>()

function pluginFor(material: unknown, reference: MapVertexLightingReference) {
  if (!(material instanceof PBRMaterial)) return null
  const existing = sharedPlugins.get(material)
  if (existing) return existing.accepts(reference) ? existing : null
  const plugin = new VertexLightingPlugin(material, reference)
  sharedPlugins.set(material, plugin)
  return plugin
}

export function vertexLightingShader() {
  return 'int l2LightingTexel = int(l2LightingTexelOffset + 0.5) + gl_VertexID;\nint l2LightingWidth = int(l2LightingTextureWidth + 0.5);\nivec2 l2LightingCoordinate = ivec2(l2LightingTexel % l2LightingWidth, l2LightingTexel / l2LightingWidth);\nvL2BakedLighting = l2LightingEnabled > 0.5 ? texelFetch(l2VertexLightingAtlas, l2LightingCoordinate, 0) : vec4(1.0);'
}

class VertexLightingPlugin extends MaterialPluginBase {
  private readonly texture: Texture
  private readonly textureUrl: string
  private readonly vertexCode: string
  private readonly textureWidth: number

  constructor(material: PBRMaterial, reference: MapVertexLightingReference) {
    super(material, 'L2VertexLighting', 190)
    this.texture = new Texture(
      browserDecodedTextureUrl(reference.url),
      material.getScene(),
      false,
      false
    )
    this.textureUrl = reference.url
    this.texture.gammaSpace = true
    this.texture.wrapU = Texture.CLAMP_ADDRESSMODE
    this.texture.wrapV = Texture.CLAMP_ADDRESSMODE
    this.vertexCode = vertexLightingShader()
    this.textureWidth = reference.textureWidth
    this._enable(true)
  }

  accepts(reference: MapVertexLightingReference) {
    return reference.url === this.textureUrl
  }

  override getAttributes(attributes: string[]) {
    attributes.push('l2LightingTexelOffset', 'l2LightingEnabled')
  }

  override getSamplers(samplers: string[]) {
    samplers.push('l2VertexLightingAtlas')
  }

  override getUniforms() {
    return {
      ubo: [{ name: 'l2LightingTextureWidth', size: 1, type: 'float' }],
      vertex:
        'uniform sampler2D l2VertexLightingAtlas;\nvarying vec4 vL2BakedLighting;',
      fragment: 'varying vec4 vL2BakedLighting;'
    }
  }

  override bindForSubMesh(uniformBuffer: UniformBuffer) {
    uniformBuffer.setTexture('l2VertexLightingAtlas', this.texture)
    uniformBuffer.updateFloat('l2LightingTextureWidth', this.textureWidth)
  }

  override getActiveTextures(activeTextures: BaseTexture[]) {
    activeTextures.push(this.texture)
  }

  override hasTexture(texture: BaseTexture) {
    return texture === this.texture
  }

  override dispose(forceDisposeTextures?: boolean) {
    if (forceDisposeTextures) this.texture.dispose()
  }

  override getCustomCode(shaderType: string): Record<string, string> | null {
    if (shaderType === 'vertex')
      return {
        CUSTOM_VERTEX_DEFINITIONS:
          'uniform sampler2D l2VertexLightingAtlas;\nvarying vec4 vL2BakedLighting;',
        CUSTOM_VERTEX_MAIN_END: this.vertexCode
      }
    if (shaderType === 'fragment')
      return {
        CUSTOM_FRAGMENT_DEFINITIONS: 'varying vec4 vL2BakedLighting;',
        CUSTOM_FRAGMENT_UPDATE_ALBEDO: 'surfaceAlbedo *= vL2BakedLighting.rgb;'
      }
    return null
  }
}
