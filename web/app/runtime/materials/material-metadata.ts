import {
  AssetContainer,
  Constants,
  Material,
  PBRMaterial,
  Texture,
  type Scene
} from '@babylonjs/core'
import { browserDecodedTextureUrl } from '../core/texture-url.js'
import { sceneAnimationClock } from '../core/animation-clock.js'
import { WindMaterialPlugin, type WindMode } from './wind-material.js'
import {
  LegacyMaterialPlugin,
  type LegacyMaterialComposite,
  type LegacyMaterialFade
} from './legacy-material.js'

interface L2TextureAnimation {
  frameUrls: string[]
  frameRate: number
}

interface L2UvOscillation {
  uType: number
  vType: number
  uRate: number
  vRate: number
  uAmplitude: number
  vAmplitude: number
  uPhase: number
  vPhase: number
}

interface L2MaterialMetadata {
  blendMode?: string
  unlit?: boolean
  opacityUrl?: string
  opacitySource?: 'none' | 'texture'
  opacityChannel?: 'alpha' | 'luminance'
  depthWrite?: boolean
  depthTest?: boolean
  panRate?: number
  panRateV?: number
  rotationRate?: number
  detailUrl?: string
  detailScale?: number
  diffuseAnimation?: L2TextureAnimation
  opacityAnimation?: L2TextureAnimation
  emissiveAnimation?: L2TextureAnimation
  uvOscillation?: L2UvOscillation
  windMode?: WindMode
  fade?: LegacyMaterialFade
  composite?: LegacyMaterialComposite
  selfIlluminationMaskUrl?: string
  specularUrl?: string
  specularityMaskUrl?: string
  performLightingOnSpecularPass?: boolean
}

function metadata(material: Material): L2MaterialMetadata | undefined {
  const value = material.metadata as
    | {
        gltf?: { extras?: { l2?: L2MaterialMetadata } }
        extras?: { l2?: L2MaterialMetadata }
      }
    | undefined
  return value?.gltf?.extras?.l2 ?? value?.extras?.l2
}

export function applyL2MaterialMetadata(
  container: AssetContainer,
  scene: Scene
) {
  let applied = 0
  for (const material of container.materials) {
    if (!(material instanceof PBRMaterial)) continue
    material.maxSimultaneousLights = 4
    const l2 = metadata(material)
    if (!l2) continue
    applied++
    material.metallic = 0
    material.roughness = 1
    material.environmentIntensity = 0
    material.specularIntensity = 0.15
    material.unlit = l2.unlit === true
    material.disableDepthWrite = l2.depthWrite === false
    if (l2.depthTest === false) material.depthFunction = Constants.ALWAYS
    if (l2.opacityUrl && l2.opacitySource === 'texture') {
      const opacity = new Texture(
        browserDecodedTextureUrl(l2.opacityUrl),
        scene,
        false,
        false
      )
      opacity.gammaSpace = false
      opacity.hasAlpha = true
      opacity.getAlphaFromRGB = l2.opacityChannel === 'luminance'
      material.opacityTexture = opacity
    }
    if (l2.detailUrl) {
      const detail = new Texture(
        browserDecodedTextureUrl(l2.detailUrl),
        scene,
        false,
        false
      )
      const scale = l2.detailScale && l2.detailScale > 0 ? l2.detailScale : 8
      detail.uScale = scale
      detail.vScale = scale
      detail.gammaSpace = true
      material.detailMap.texture = detail
      material.detailMap.diffuseBlendLevel = 0.35
      material.detailMap.isEnabled = true
    }
    if (l2.blendMode === 'additive') {
      material.transparencyMode = Material.MATERIAL_ALPHABLEND
      material.alphaMode = Constants.ALPHA_ADD
    } else if (l2.blendMode === 'modulate') {
      material.transparencyMode = Material.MATERIAL_ALPHABLEND
      material.alphaMode = Constants.ALPHA_MULTIPLY
    } else if (l2.blendMode === 'invisible') {
      material.transparencyMode = Material.MATERIAL_ALPHABLEND
      material.alpha = 0
    }
    if (l2.fade || l2.composite || l2.selfIlluminationMaskUrl || l2.specularUrl)
      new LegacyMaterialPlugin(material, l2)
    const animatedTextures = [
      material.albedoTexture,
      material.opacityTexture,
      material.emissiveTexture
    ].filter((texture): texture is Texture => texture instanceof Texture)
    const flipbooks = [
      createFlipbook(
        material.albedoTexture,
        l2.diffuseAnimation,
        scene,
        (texture) => {
          material.albedoTexture = texture
        }
      ),
      createFlipbook(
        material.opacityTexture,
        l2.opacityAnimation,
        scene,
        (texture) => {
          texture.hasAlpha = true
          texture.getAlphaFromRGB = l2.opacityChannel === 'luminance'
          material.opacityTexture = texture
        }
      ),
      createFlipbook(
        material.emissiveTexture,
        l2.emissiveAnimation,
        scene,
        (texture) => {
          material.emissiveTexture = texture
        }
      )
    ].filter((flipbook): flipbook is Flipbook => flipbook !== null)
    if (
      flipbooks.length > 0 ||
      ((l2.panRate || l2.panRateV || l2.rotationRate || l2.uvOscillation) &&
        animatedTextures.length > 0)
    ) {
      const clock = sceneAnimationClock(scene)
      const unsubscribe = clock.subscribe((elapsedSeconds) => {
        for (const flipbook of flipbooks) flipbook.update(elapsedSeconds)
        const textures = [
          material.albedoTexture,
          material.opacityTexture,
          material.emissiveTexture
        ].filter((texture): texture is Texture => texture instanceof Texture)
        for (const texture of textures) {
          if (l2.panRate || l2.uvOscillation)
            texture.uOffset = (l2.panRate ?? 0) * elapsedSeconds
          if (l2.panRateV || l2.uvOscillation)
            texture.vOffset = (l2.panRateV ?? 0) * elapsedSeconds
          if (l2.rotationRate) texture.wAng = l2.rotationRate * elapsedSeconds
          if (l2.uvOscillation) {
            texture.uScale = 1
            texture.vScale = 1
            applyUvOscillation(texture, l2.uvOscillation, elapsedSeconds)
          }
        }
      })
      material.onDisposeObservable.addOnce(() => {
        unsubscribe()
        flipbooks.forEach((flipbook) => flipbook.dispose())
      })
    }
    if (l2.windMode === 'grass' || l2.windMode === 'foliage') {
      const bounds = container.meshes
        .filter(
          (mesh) =>
            mesh.getTotalVertices() > 0 &&
            (mesh.material === material ||
              mesh.subMeshes?.some(
                (subMesh) => subMesh.getMaterial() === material
              ))
        )
        .map((mesh) => mesh.getBoundingInfo().boundingBox)
      if (bounds.length > 0) {
        new WindMaterialPlugin(material, sceneAnimationClock(scene), {
          mode: l2.windMode,
          minY: Math.min(...bounds.map((bound) => bound.minimum.y)),
          maxY: Math.max(...bounds.map((bound) => bound.maximum.y))
        })
      }
    }
  }
  return applied
}

function applyUvOscillation(
  texture: Texture,
  oscillator: L2UvOscillation,
  elapsedSeconds: number
) {
  applyUvAxis(
    oscillator.uType,
    oscillator.uRate,
    oscillator.uAmplitude,
    oscillator.uPhase,
    elapsedSeconds,
    (offset) => (texture.uOffset += offset),
    (scale) => {
      texture.uScale = scale
      texture.uOffset += (1 - scale) / 2
    }
  )
  applyUvAxis(
    oscillator.vType,
    oscillator.vRate,
    oscillator.vAmplitude,
    oscillator.vPhase,
    elapsedSeconds,
    (offset) => (texture.vOffset += offset),
    (scale) => {
      texture.vScale = scale
      texture.vOffset += (1 - scale) / 2
    }
  )
}

function applyUvAxis(
  type: number,
  rate: number,
  amplitude: number,
  phase: number,
  elapsedSeconds: number,
  offset: (value: number) => void,
  scale: (value: number) => void
) {
  if (!Number.isFinite(rate) || !Number.isFinite(amplitude)) return
  const wave = Math.sin(Math.PI * 2 * (elapsedSeconds * rate + phase))
  if (type === 0) offset(wave * amplitude)
  else if (type === 1) scale(Math.max(0.001, 1 + wave * amplitude))
  else if (type === 2) scale(Math.max(0.001, 1 + Math.abs(wave) * amplitude))
}

interface Flipbook {
  update(elapsedSeconds: number): void
  dispose(): void
}

function createFlipbook(
  source: unknown,
  animation: L2TextureAnimation | undefined,
  scene: Scene,
  apply: (texture: Texture) => void
): Flipbook | null {
  if (
    !(source instanceof Texture) ||
    !animation ||
    animation.frameUrls.length < 2 ||
    !Number.isFinite(animation.frameRate) ||
    animation.frameRate <= 0
  )
    return null

  const frames = animation.frameUrls.map((url) => {
    const texture = new Texture(
      browserDecodedTextureUrl(url),
      scene,
      false,
      false
    )
    texture.gammaSpace = source.gammaSpace
    texture.hasAlpha = source.hasAlpha
    texture.getAlphaFromRGB = source.getAlphaFromRGB
    texture.wrapU = source.wrapU
    texture.wrapV = source.wrapV
    texture.uScale = source.uScale
    texture.vScale = source.vScale
    return texture
  })
  let current = -1
  return {
    update(elapsedSeconds) {
      const next =
        Math.floor(elapsedSeconds * animation.frameRate) % frames.length
      if (next === current) return
      current = next
      apply(frames[next]!)
    },
    dispose() {
      frames.forEach((texture) => texture.dispose())
    }
  }
}
