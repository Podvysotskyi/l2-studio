import {
  AdditiveBlending,
  ClampToEdgeWrapping,
  Color,
  DataTexture,
  DoubleSide,
  FrontSide,
  LinearFilter,
  Mesh,
  MeshStandardMaterial,
  MultiplyBlending,
  NoColorSpace,
  RepeatWrapping,
  SRGBColorSpace,
  TextureLoader,
  Vector4,
  type Material,
  type Object3D,
  type Texture
} from 'three'
import { resolvePublishedGltfMaterialUrl } from '../core/published-gltf.js'

export interface PublishedStaticMeshMaterial {
  doubleSided?: boolean
  diffuseUrl?: string | null
  emissiveUrl?: string | null
  opacityUrl?: string | null
  opacitySource?: 'none' | 'texture'
  opacityChannel?: 'alpha' | 'luminance'
  blendMode?: 'opaque' | 'masked' | 'alphablend' | 'additive' | 'modulate' | 'invisible'
  unlit?: boolean
  depthWrite?: boolean
  depthTest?: boolean
  panRate?: number
  panRateV?: number
  rotationRate?: number
  detailUrl?: string | null
  detailScale?: number
  diffuseAnimation?: PublishedTextureAnimation | null
  opacityAnimation?: PublishedTextureAnimation | null
  emissiveAnimation?: PublishedTextureAnimation | null
  uvOscillation?: PublishedUvOscillation | null
  fade?: PublishedMaterialFade | null
  composite?: PublishedMaterialComposite | null
  selfIlluminationMaskUrl?: string | null
  specularUrl?: string | null
  specularityMaskUrl?: string | null
  performLightingOnSpecularPass?: boolean
  clampU?: boolean
  clampV?: boolean
  windMode?: 'grass' | 'foliage' | null
}

export interface PublishedTextureAnimation {
  frameUrls: string[]
  frameRate: number
}

export interface PublishedUvOscillation {
  uType: number
  vType: number
  uRate: number
  vRate: number
  uAmplitude: number
  vAmplitude: number
  uPhase: number
  vPhase: number
}

export interface PublishedMaterialTint {
  r: number
  g: number
  b: number
  a: number
}

export interface PublishedMaterialFade {
  color1: PublishedMaterialTint
  color2: PublishedMaterialTint
  type: number
  period: number
  phase: number
}

export interface PublishedMaterialComposite {
  secondaryUrl?: string | null
  secondaryTint?: PublishedMaterialTint | null
  secondaryFade?: PublishedMaterialFade | null
  maskUrl?: string | null
  colorOperation: number
  alphaOperation: number
  invertMask: boolean
  modulateScale: number
}

export interface StaticMeshMaterialPreparation {
  warnings: string[]
  update(elapsedSeconds: number): void
  dispose(): void
}

type Shader = {
  uniforms: Record<string, { value: unknown }>
  vertexShader: string
  fragmentShader: string
}

type ManagedMaterial = {
  material: MeshStandardMaterial
  shader?: Shader
  diffuseFrames?: Texture[]
  opacityFrames?: Texture[]
  emissiveFrames?: Texture[]
  diffuseAnimation?: PublishedTextureAnimation
  opacityAnimation?: PublishedTextureAnimation
  emissiveAnimation?: PublishedTextureAnimation
}

const transparentPixel = new DataTexture(new Uint8Array([255, 255, 255, 255]), 1, 1)
transparentPixel.colorSpace = SRGBColorSpace
transparentPixel.needsUpdate = true

const textureCache = new Map<string, Promise<Texture>>()

export async function prepareStaticMeshMaterials(
  root: Object3D,
  fallback: Material,
  modelUrl: string
): Promise<StaticMeshMaterialPreparation> {
  const warnings = new Set<string>()
  const managed: ManagedMaterial[] = []
  const replacements = new Map<Material, Promise<MeshStandardMaterial>>()
  const ownedMaterials = new Set<Material>()

  const prepare = async (source: Material): Promise<MeshStandardMaterial> => {
    const definition = publishedStaticMeshMaterial(source)
    if (!definition) return source as MeshStandardMaterial
    try {
      const material = await createMaterial(source, definition, modelUrl)
      managed.push({
        material,
        shader: material.userData.l2Shader as Shader | undefined,
        diffuseFrames: material.userData.l2DiffuseFrames as Texture[] | undefined,
        opacityFrames: material.userData.l2OpacityFrames as Texture[] | undefined,
        emissiveFrames: material.userData.l2EmissiveFrames as Texture[] | undefined,
        diffuseAnimation: definition.diffuseAnimation ?? undefined,
        opacityAnimation: definition.opacityAnimation ?? undefined,
        emissiveAnimation: definition.emissiveAnimation ?? undefined
      })
      ownedMaterials.add(material)
      return material
    } catch (error) {
      warnings.add(`${source.name || 'Unnamed material'}: ${errorMessage(error)}`)
      return fallback as MeshStandardMaterial
    }
  }

  const meshes: Mesh[] = []
  root.traverse(object => {
    if (object instanceof Mesh) meshes.push(object)
  })
  for (const mesh of meshes) {
    const source = Array.isArray(mesh.material) ? mesh.material : [mesh.material]
    const next = await Promise.all(source.map(material => {
      if (publishedStaticMeshMaterial(material) && !mesh.geometry.attributes.uv) {
        warnings.add(`${material.name || 'Unnamed material'}: The mesh has no texture coordinates.`)
        return Promise.resolve(fallback as MeshStandardMaterial)
      }
      let replacement = replacements.get(material)
      if (!replacement) {
        replacement = prepare(material)
        replacements.set(material, replacement)
      }
      return replacement
    }))
    mesh.material = Array.isArray(mesh.material) ? next : next[0]!
  }

  return {
    warnings: [...warnings],
    update(elapsedSeconds) {
      for (const entry of managed) {
        const shader = entry.shader ?? (entry.material.userData.l2Shader as Shader | undefined)
        if (!shader) continue
        entry.shader = shader
        shader.uniforms.l2Time!.value = elapsedSeconds
        updateAnimation(shader, 'l2DiffuseMap', entry.diffuseFrames, entry.diffuseAnimation, elapsedSeconds)
        updateAnimation(shader, 'l2OpacityMap', entry.opacityFrames, entry.opacityAnimation, elapsedSeconds)
        updateAnimation(shader, 'l2EmissiveMap', entry.emissiveFrames, entry.emissiveAnimation, elapsedSeconds)
      }
    },
    dispose() {
      ownedMaterials.forEach(material => material.dispose())
      const textures = new Set<Texture>()
      managed.forEach(entry => {
        entry.diffuseFrames?.forEach(texture => textures.add(texture))
        entry.opacityFrames?.forEach(texture => textures.add(texture))
        entry.emissiveFrames?.forEach(texture => textures.add(texture))
      })
      textures.forEach(texture => texture.dispose())
    }
  }
}

export function publishedStaticMeshMaterial(material: Material) {
  const value = material.userData.l2
  return value && typeof value === 'object'
    ? value as PublishedStaticMeshMaterial
    : undefined
}

async function createMaterial(source: Material, definition: PublishedStaticMeshMaterial, modelUrl: string) {
  const diffuseFrames = await loadFrames(
    definition.diffuseAnimation,
    definition.diffuseUrl,
    true,
    modelUrl,
    definition.clampU,
    definition.clampV
  )
  const opacityFrames = await loadFrames(definition.opacityAnimation, definition.opacityUrl, false, modelUrl)
  const emissiveFrames = await loadFrames(definition.emissiveAnimation, definition.emissiveUrl, true, modelUrl)
  const detail = await loadTexture(definition.detailUrl, true, modelUrl)
  const secondary = await loadTexture(definition.composite?.secondaryUrl, true, modelUrl)
  const mask = await loadTexture(definition.composite?.maskUrl, false, modelUrl)
  const illuminationMask = await loadTexture(definition.selfIlluminationMaskUrl, false, modelUrl)
  const specular = await loadTexture(definition.specularUrl, true, modelUrl)
  const specularityMask = await loadTexture(definition.specularityMaskUrl, false, modelUrl)
  const sourceStandard = source as MeshStandardMaterial
  const material = new MeshStandardMaterial({
    name: source.name,
    color: sourceStandard.color?.clone() ?? new Color(0xffffff),
    roughness: 0.82,
    metalness: 0,
    map: diffuseFrames[0] ?? transparentPixel,
    emissiveMap: emissiveFrames[0] ?? null,
    emissive: sourceStandard.emissive?.clone() ?? new Color(0x000000),
    vertexColors: sourceStandard.vertexColors,
    side: definition.doubleSided ? DoubleSide : sourceStandard.side ?? FrontSide,
    transparent: isTransparent(definition.blendMode),
    alphaTest: definition.blendMode === 'masked'
      ? Math.max(finite(sourceStandard.alphaTest), 0.001)
      : 0,
    depthWrite: definition.depthWrite ?? !isTransparent(definition.blendMode),
    depthTest: definition.depthTest ?? true
  })
  if (definition.blendMode === 'additive') material.blending = AdditiveBlending
  if (definition.blendMode === 'modulate') material.blending = MultiplyBlending
  if (definition.blendMode === 'invisible') material.opacity = 0
  material.userData.l2 = definition

  material.onBeforeCompile = shader => {
    const typedShader = shader as unknown as Shader
    typedShader.uniforms.l2Time = { value: 0 }
    typedShader.uniforms.l2DiffuseMap = { value: diffuseFrames[0] ?? transparentPixel }
    typedShader.uniforms.l2OpacityMap = { value: opacityFrames[0] ?? transparentPixel }
    typedShader.uniforms.l2EmissiveMap = { value: emissiveFrames[0] ?? transparentPixel }
    typedShader.uniforms.l2DetailMap = { value: detail ?? transparentPixel }
    typedShader.uniforms.l2SecondaryMap = { value: secondary ?? transparentPixel }
    typedShader.uniforms.l2MaskMap = { value: mask ?? transparentPixel }
    typedShader.uniforms.l2IlluminationMask = { value: illuminationMask ?? transparentPixel }
    typedShader.uniforms.l2SpecularMap = { value: specular ?? transparentPixel }
    typedShader.uniforms.l2SpecularityMask = { value: specularityMask ?? transparentPixel }
    typedShader.uniforms.l2FadeColor1 = { value: tint(definition.fade?.color1) }
    typedShader.uniforms.l2FadeColor2 = { value: tint(definition.fade?.color2) }
    typedShader.vertexShader = injectVertexShader(typedShader.vertexShader, definition)
    typedShader.fragmentShader = injectFragmentShader(typedShader.fragmentShader, definition)
    material.userData.l2Shader = typedShader
  }
  material.customProgramCacheKey = () => `l2-static-${materialKey(definition)}`
  material.userData.l2DiffuseFrames = diffuseFrames
  material.userData.l2OpacityFrames = opacityFrames
  material.userData.l2EmissiveFrames = emissiveFrames
  return material
}

function injectVertexShader(source: string, material: PublishedStaticMeshMaterial) {
  const header = `
uniform float l2Time;
varying vec2 vL2Uv;
float l2Oscillation(float kind, float rate, float amplitude, float phase) {
  float value = l2Time * rate + phase;
  return kind == 1.0 ? fract(value) * amplitude : sin(value) * amplitude;
}`
  const oscillation = material.uvOscillation
  const panU = finite(material.panRate)
  const panV = finite(material.panRateV)
  const rotation = finite(material.rotationRate)
  const wind = material.windMode
  return source
    .replace('void main() {', `${header}\nvoid main() {`)
    .replace('#include <uv_vertex>', `#include <uv_vertex>
vL2Uv = uv;
vL2Uv += vec2(${glsl(panU)} * l2Time, ${glsl(panV)} * l2Time);
${oscillation ? `vL2Uv += vec2(l2Oscillation(${glsl(oscillation.uType)}, ${glsl(oscillation.uRate)}, ${glsl(oscillation.uAmplitude)}, ${glsl(oscillation.uPhase)}), l2Oscillation(${glsl(oscillation.vType)}, ${glsl(oscillation.vRate)}, ${glsl(oscillation.vAmplitude)}, ${glsl(oscillation.vPhase)}));` : ''}
${rotation ? `vL2Uv -= 0.5; vL2Uv = mat2(cos(l2Time * ${glsl(rotation)}), -sin(l2Time * ${glsl(rotation)}), sin(l2Time * ${glsl(rotation)}), cos(l2Time * ${glsl(rotation)})) * vL2Uv; vL2Uv += 0.5;` : ''}`)
    .replace('#include <begin_vertex>', `#include <begin_vertex>
${wind ? `float l2Wind = sin(l2Time * ${wind === 'grass' ? '2.8' : '1.4'} + position.x * 0.025 + position.z * 0.018) * ${wind === 'grass' ? '0.16' : '0.06'}; transformed.x += l2Wind * max(position.y, 0.0); transformed.z += l2Wind * 0.45 * max(position.y, 0.0);` : ''}`)
}

function injectFragmentShader(source: string, material: PublishedStaticMeshMaterial) {
  const composite = material.composite
  const header = `
uniform float l2Time;
uniform sampler2D l2DiffuseMap;
uniform sampler2D l2OpacityMap;
uniform sampler2D l2EmissiveMap;
uniform sampler2D l2DetailMap;
uniform sampler2D l2SecondaryMap;
uniform sampler2D l2MaskMap;
uniform sampler2D l2IlluminationMask;
uniform sampler2D l2SpecularMap;
uniform sampler2D l2SpecularityMask;
uniform vec4 l2FadeColor1;
uniform vec4 l2FadeColor2;
varying vec2 vL2Uv;
float l2Luminance(vec3 color) { return dot(color, vec3(0.299, 0.587, 0.114)); }
vec4 l2Composite(vec4 primary, vec4 secondary, float mask) {
  ${composite ? compositeColorOperation(composite) : 'return primary;'}
}`
  const opacity = material.opacityUrl && material.opacitySource !== 'none'
    ? `float l2Opacity = ${material.opacityChannel === 'luminance' ? 'l2Luminance(texture2D(l2OpacityMap, vL2Uv).rgb)' : 'texture2D(l2OpacityMap, vL2Uv).a'}; diffuseColor.a *= l2Opacity;`
    : ''
  const detail = material.detailUrl
    ? `diffuseColor.rgb *= mix(vec3(1.0), texture2D(l2DetailMap, vL2Uv * ${glsl(material.detailScale ?? 8)}).rgb, 0.5);`
    : ''
  const combine = composite?.secondaryUrl
    ? `float l2Mask = texture2D(l2MaskMap, vL2Uv).a;
${composite.invertMask ? 'l2Mask = 1.0 - l2Mask;' : ''}
diffuseColor = l2Composite(diffuseColor, texture2D(l2SecondaryMap, vL2Uv), l2Mask);`
    : ''
  const fadeDefinition = material.fade
  const fadeEffect = fadeDefinition
    ? `float l2FadePhase = ${fadeDefinition.period > 0 ? (fadeDefinition.type === 1
      ? `sin(l2Time / ${glsl(fadeDefinition.period)} * 6.2831853 + ${glsl(fadeDefinition.phase)}) * 0.5 + 0.5`
      : `fract(l2Time / ${glsl(fadeDefinition.period)} + ${glsl(fadeDefinition.phase)})`) : '0.0'}; diffuseColor *= mix(l2FadeColor1, l2FadeColor2, l2FadePhase);`
    : ''
  const emission = material.emissiveUrl
    ? `totalEmissiveRadiance += texture2D(l2EmissiveMap, vL2Uv).rgb * ${material.selfIlluminationMaskUrl ? 'texture2D(l2IlluminationMask, vL2Uv).a' : '1.0'};`
    : ''
  const specular = material.specularUrl
    ? `reflectedLight.directSpecular += texture2D(l2SpecularMap, vL2Uv).rgb * ${material.specularityMaskUrl ? 'texture2D(l2SpecularityMask, vL2Uv).a' : '1.0'} * ${material.performLightingOnSpecularPass ? '1.0' : '0.5'};`
    : ''
  const unlit = material.unlit
    ? 'reflectedLight.directDiffuse = diffuseColor.rgb; reflectedLight.indirectDiffuse = vec3(0.0); reflectedLight.indirectSpecular = vec3(0.0);'
    : ''
  return source
    .replace('void main() {', `${header}\nvoid main() {`)
    .replace('#include <map_fragment>', `vec4 l2Diffuse = texture2D(l2DiffuseMap, vL2Uv);
diffuseColor *= l2Diffuse;
${detail}
${combine}
${opacity}
${fadeEffect}`)
    .replace('#include <emissivemap_fragment>', `#include <emissivemap_fragment>
${emission}`)
    .replace('#include <lights_fragment_end>', `#include <lights_fragment_end>
${unlit}
${specular}`)
}

function compositeColorOperation(composite: PublishedMaterialComposite) {
  const scale = glsl(composite.modulateScale || 1)
  switch (composite.colorOperation) {
    case 1: return 'return secondary;'
    case 2: return `return primary * secondary * ${scale};`
    case 3: return 'return min(primary + secondary, vec4(1.0));'
    case 4: return 'return max(primary - secondary, vec4(0.0));'
    case 5: return 'return mix(primary, secondary, mask);'
    case 6: return 'return primary + secondary * mask;'
    case 7: return 'return vec4(vec3(mask), mask);'
    default: return 'return primary;'
  }
}

async function loadFrames(
  animation: PublishedTextureAnimation | null | undefined,
  fallbackUrl: string | null | undefined,
  color: boolean,
  modelUrl: string,
  clampU = false,
  clampV = false
) {
  const urls = animation?.frameUrls?.length ? animation.frameUrls : fallbackUrl ? [fallbackUrl] : []
  const textures = await Promise.all(urls.map(url =>
    loadTexture(url, color, modelUrl, clampU, clampV)))
  return textures.filter((texture): texture is Texture => Boolean(texture))
}

async function loadTexture(
  url: string | null | undefined,
  color: boolean,
  modelUrl: string,
  clampU = false,
  clampV = false
): Promise<Texture | undefined> {
  if (!url) return undefined
  const resolvedUrl = resolvePublishedGltfMaterialUrl(url, modelUrl)
  let cached = textureCache.get(resolvedUrl)
  if (!cached) {
    cached = new Promise<Texture>((resolve, reject) => {
      new TextureLoader().load(resolvedUrl, resolve, undefined, () =>
        reject(new Error(`Unable to load texture ${resolvedUrl}.`)))
    })
    textureCache.set(resolvedUrl, cached)
    cached.catch(() => {
      if (textureCache.get(resolvedUrl) === cached) textureCache.delete(resolvedUrl)
    })
  }
  const source = await cached
  const texture = source.clone()
  texture.flipY = false
  texture.colorSpace = color ? SRGBColorSpace : NoColorSpace
  texture.wrapS = clampU ? ClampToEdgeWrapping : RepeatWrapping
  texture.wrapT = clampV ? ClampToEdgeWrapping : RepeatWrapping
  texture.minFilter = LinearFilter
  texture.magFilter = LinearFilter
  texture.needsUpdate = true
  return texture
}

function updateAnimation(shader: Shader, name: string, frames: Texture[] | undefined, animation: PublishedTextureAnimation | undefined, elapsed: number) {
  if (!frames?.length || !animation || animation.frameRate <= 0) return
  shader.uniforms[name]!.value = frames[Math.floor(elapsed * animation.frameRate) % frames.length]!
}

function isTransparent(mode: PublishedStaticMeshMaterial['blendMode']) {
  return mode === 'alphablend' || mode === 'additive' || mode === 'modulate' || mode === 'invisible'
}

function tint(value: PublishedMaterialTint | undefined) {
  return new Vector4(value?.r ?? 1, value?.g ?? 1, value?.b ?? 1, value?.a ?? 1)
}

function finite(value: number | undefined) {
  return Number.isFinite(value) ? value! : 0
}

function glsl(value: number) {
  if (!Number.isFinite(value)) return '0.0'
  return Number.isInteger(value) ? `${value}.0` : `${value}`
}

function materialKey(material: PublishedStaticMeshMaterial) {
  return JSON.stringify(material)
}

function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'The material could not be prepared.'
}
