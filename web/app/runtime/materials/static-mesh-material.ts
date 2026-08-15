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
  materials: StaticMeshMaterialInspection[]
  setMaterialEnabled(id: string, enabled: boolean): StaticMeshMaterialInspection[]
  setTextureEnabled(
    id: string,
    role: StaticMeshTextureRole,
    enabled: boolean
  ): StaticMeshMaterialInspection[]
  setBehaviorEnabled(
    id: string,
    behavior: StaticMeshMaterialBehavior,
    enabled: boolean
  ): StaticMeshMaterialInspection[]
  reset(): StaticMeshMaterialInspection[]
  update(elapsedSeconds: number): void
  dispose(): void
}

export type StaticMeshTextureRole =
  | 'diffuse'
  | 'opacity'
  | 'emissive'
  | 'detail'
  | 'compositeSecondary'
  | 'compositeMask'
  | 'selfIlluminationMask'
  | 'specular'
  | 'specularityMask'

export type StaticMeshMaterialBehavior =
  | 'blending'
  | 'twoSided'
  | 'depthWrite'
  | 'depthTest'
  | 'unlit'
  | 'animation'
  | 'uvEffects'
  | 'wind'
  | 'fade'
  | 'composite'

export interface StaticMeshTextureInspection {
  role: StaticMeshTextureRole
  label: string
  url: string
  frameCount: number
  enabled: boolean
}

export interface StaticMeshMaterialBehaviorInspection {
  behavior: StaticMeshMaterialBehavior
  label: string
  available: boolean
  enabled: boolean
}

export interface StaticMeshMaterialInspection {
  id: string
  name: string
  sections: number[]
  blendMode: NonNullable<PublishedStaticMeshMaterial['blendMode']>
  alphaCutoff: number
  doubleSided: boolean
  depthWrite: boolean
  depthTest: boolean
  unlit: boolean
  clampU: boolean
  clampV: boolean
  panRate: number
  panRateV: number
  rotationRate: number
  uvOscillation?: PublishedUvOscillation
  enabled: boolean
  textures: StaticMeshTextureInspection[]
  behaviors: StaticMeshMaterialBehaviorInspection[]
}

type Shader = {
  uniforms: Record<string, { value: unknown }>
  vertexShader: string
  fragmentShader: string
}

type ManagedMaterial = {
  id: string
  material: MeshStandardMaterial
  definition: PublishedStaticMeshMaterial
  modelUrl: string
  sections: number[]
  controls: StaticMeshMaterialControls
  authored: StaticMeshMaterialAuthoredState
  shader?: Shader
  diffuseFrames?: Texture[]
  opacityFrames?: Texture[]
  emissiveFrames?: Texture[]
  diffuseAnimation?: PublishedTextureAnimation
  opacityAnimation?: PublishedTextureAnimation
  emissiveAnimation?: PublishedTextureAnimation
}

type StaticMeshMaterialControls = {
  material: boolean
  textures: Record<StaticMeshTextureRole, boolean>
  behaviors: Record<StaticMeshMaterialBehavior, boolean>
}

type StaticMeshMaterialAuthoredState = {
  transparent: boolean
  alphaTest: number
  blending: MeshStandardMaterial['blending']
  side: MeshStandardMaterial['side']
  depthWrite: boolean
  depthTest: boolean
  opacity: number
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
  const entries = new Map<MeshStandardMaterial, ManagedMaterial>()
  const ownedMaterials = new Set<Material>()
  let materialId = 0

  const prepare = async (source: Material): Promise<MeshStandardMaterial> => {
    const definition = publishedStaticMeshMaterial(source)
    if (!definition) return source as MeshStandardMaterial
    try {
      const controls = materialControls(definition, (source as MeshStandardMaterial).side)
      const material = await createMaterial(source, definition, modelUrl, controls)
      const entry: ManagedMaterial = {
        id: `material-${materialId++}`,
        material,
        definition,
        modelUrl,
        sections: [],
        controls,
        authored: authoredState(material),
        shader: material.userData.l2Shader as Shader | undefined,
        diffuseFrames: material.userData.l2DiffuseFrames as Texture[] | undefined,
        opacityFrames: material.userData.l2OpacityFrames as Texture[] | undefined,
        emissiveFrames: material.userData.l2EmissiveFrames as Texture[] | undefined,
        diffuseAnimation: definition.diffuseAnimation ?? undefined,
        opacityAnimation: definition.opacityAnimation ?? undefined,
        emissiveAnimation: definition.emissiveAnimation ?? undefined
      }
      managed.push(entry)
      entries.set(material, entry)
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
  let section = 0
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
    next.forEach(material => {
      entries.get(material)?.sections.push(section++)
    })
    mesh.material = Array.isArray(mesh.material) ? next : next[0]!
  }

  return {
    warnings: [...warnings],
    get materials() {
      return inspections(managed)
    },
    setMaterialEnabled(id, enabled) {
      const entry = managed.find(item => item.id === id)
      if (!entry) return inspections(managed)
      entry.controls.material = enabled
      entry.material.visible = enabled
      return inspections(managed)
    },
    setTextureEnabled(id, role, enabled) {
      const entry = managed.find(item => item.id === id)
      if (!entry || !textureInspection(entry, role)) return inspections(managed)
      entry.controls.textures[role] = enabled
      syncShaderControls(entry)
      return inspections(managed)
    },
    setBehaviorEnabled(id, behavior, enabled) {
      const entry = managed.find(item => item.id === id)
      if (!entry || !behaviorInspection(entry, behavior).available) return inspections(managed)
      entry.controls.behaviors[behavior] = enabled
      applyBehavior(entry, behavior)
      return inspections(managed)
    },
    reset() {
      managed.forEach(resetEntry)
      return inspections(managed)
    },
    update(elapsedSeconds) {
      for (const entry of managed) {
        const shader = entry.shader ?? (entry.material.userData.l2Shader as Shader | undefined)
        if (!shader) continue
        entry.shader = shader
        shader.uniforms.l2Time!.value = elapsedSeconds
        if (entry.controls.behaviors.animation) {
          updateAnimation(shader, 'l2DiffuseMap', entry.diffuseFrames, entry.diffuseAnimation, elapsedSeconds)
          updateAnimation(shader, 'l2OpacityMap', entry.opacityFrames, entry.opacityAnimation, elapsedSeconds)
          updateAnimation(shader, 'l2EmissiveMap', entry.emissiveFrames, entry.emissiveAnimation, elapsedSeconds)
        }
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

export function applyStaticMeshMaterialFallback(
  root: Object3D,
  fallback: Material
) {
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    const source = Array.isArray(object.material)
      ? object.material
      : [object.material]
    if (source.some(publishedStaticMeshMaterial)) return
    object.material = fallback
  })
}

async function createMaterial(
  source: Material,
  definition: PublishedStaticMeshMaterial,
  modelUrl: string,
  controls: StaticMeshMaterialControls
) {
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
  material.userData.l2Controls = controls

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
    typedShader.uniforms.l2DiffuseEnabled = { value: enabled(controls.textures.diffuse) }
    typedShader.uniforms.l2OpacityEnabled = { value: enabled(controls.textures.opacity) }
    typedShader.uniforms.l2EmissiveEnabled = { value: enabled(controls.textures.emissive) }
    typedShader.uniforms.l2DetailEnabled = { value: enabled(controls.textures.detail) }
    typedShader.uniforms.l2CompositeEnabled = { value: enabled(controls.behaviors.composite) }
    typedShader.uniforms.l2CompositeSecondaryEnabled = { value: enabled(controls.textures.compositeSecondary) }
    typedShader.uniforms.l2CompositeMaskEnabled = { value: enabled(controls.textures.compositeMask) }
    typedShader.uniforms.l2IlluminationMaskEnabled = { value: enabled(controls.textures.selfIlluminationMask) }
    typedShader.uniforms.l2SpecularEnabled = { value: enabled(controls.textures.specular) }
    typedShader.uniforms.l2SpecularityMaskEnabled = { value: enabled(controls.textures.specularityMask) }
    typedShader.uniforms.l2UnlitEnabled = { value: enabled(controls.behaviors.unlit) }
    typedShader.uniforms.l2UvEffectsEnabled = { value: enabled(controls.behaviors.uvEffects) }
    typedShader.uniforms.l2WindEnabled = { value: enabled(controls.behaviors.wind) }
    typedShader.uniforms.l2FadeEnabled = { value: enabled(controls.behaviors.fade) }
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
uniform float l2UvEffectsEnabled;
uniform float l2WindEnabled;
varying vec2 vL2Uv;`
  const oscillation = material.uvOscillation
  const panU = finite(material.panRate)
  const panV = finite(material.panRateV)
  const rotation = finite(material.rotationRate)
  const wind = material.windMode
  const oscillationTransform = oscillation
    ? `float l2UOscillation = sin(6.28318530718 * (l2Time * ${glsl(oscillation.uRate)} + ${glsl(oscillation.uPhase)})) * ${glsl(oscillation.uAmplitude)};
float l2VOscillation = sin(6.28318530718 * (l2Time * ${glsl(oscillation.vRate)} + ${glsl(oscillation.vPhase)})) * ${glsl(oscillation.vAmplitude)};
${uvOscillationAxis('x', 'l2UOscillation', oscillation.uType)}
${uvOscillationAxis('y', 'l2VOscillation', oscillation.vType)}`
    : ''
  return source
    .replace('void main() {', `${header}\nvoid main() {`)
    .replace('#include <uv_vertex>', `#include <uv_vertex>
vL2Uv = uv;
float l2UvTime = l2Time * l2UvEffectsEnabled;
vec2 l2UvOffset = vec2(${glsl(panU)}, ${glsl(panV)}) * l2UvTime;
vec2 l2UvScale = vec2(1.0);
${oscillationTransform}
vL2Uv = (vL2Uv - vec2(0.5)) * l2UvScale + vec2(0.5) + l2UvOffset;
${rotation ? `vL2Uv -= vec2(0.5); vL2Uv = mat2(cos(l2UvTime * ${glsl(rotation)}), -sin(l2UvTime * ${glsl(rotation)}), sin(l2UvTime * ${glsl(rotation)}), cos(l2UvTime * ${glsl(rotation)})) * vL2Uv; vL2Uv += vec2(0.5);` : ''}`)
    .replace('#include <begin_vertex>', `#include <begin_vertex>
${wind ? `float l2Wind = sin(l2Time * ${wind === 'grass' ? '2.8' : '1.4'} + position.x * 0.025 + position.z * 0.018) * ${wind === 'grass' ? '0.16' : '0.06'} * l2WindEnabled; transformed.x += l2Wind * max(position.y, 0.0); transformed.z += l2Wind * 0.45 * max(position.y, 0.0);` : ''}`)
}

function uvOscillationAxis(
  axis: 'x' | 'y',
  value: string,
  type: number
) {
  if (type === 0)
    return `l2UvOffset.${axis} += ${value} * l2UvEffectsEnabled;`
  if (type === 1)
    return `l2UvScale.${axis} = max(0.001, 1.0 + ${value} * l2UvEffectsEnabled);`
  if (type === 2)
    return `l2UvScale.${axis} = max(0.001, 1.0 + abs(${value}) * l2UvEffectsEnabled);`
  return ''
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
uniform float l2DiffuseEnabled;
uniform float l2OpacityEnabled;
uniform float l2EmissiveEnabled;
uniform float l2DetailEnabled;
uniform float l2CompositeEnabled;
uniform float l2CompositeSecondaryEnabled;
uniform float l2CompositeMaskEnabled;
uniform float l2IlluminationMaskEnabled;
uniform float l2SpecularEnabled;
uniform float l2SpecularityMaskEnabled;
uniform float l2UnlitEnabled;
uniform float l2FadeEnabled;
varying vec2 vL2Uv;
float l2Luminance(vec3 color) { return dot(color, vec3(0.299, 0.587, 0.114)); }
vec4 l2Composite(vec4 primary, vec4 secondary, float mask) {
  ${composite ? compositeColorOperation(composite) : 'return primary;'}
}`
  const opacity = material.opacityUrl && material.opacitySource !== 'none'
    ? `float l2Opacity = ${material.opacityChannel === 'luminance' ? 'l2Luminance(texture2D(l2OpacityMap, vL2Uv).rgb)' : 'texture2D(l2OpacityMap, vL2Uv).a'}; diffuseColor.a *= mix(1.0, l2Opacity, l2OpacityEnabled);`
    : ''
  const detail = material.detailUrl
    ? `diffuseColor.rgb *= mix(vec3(1.0), texture2D(l2DetailMap, vL2Uv * ${glsl(material.detailScale ?? 8)}).rgb, 0.5 * l2DetailEnabled);`
    : ''
  const combine = composite?.secondaryUrl
    ? `float l2Mask = mix(1.0, texture2D(l2MaskMap, vL2Uv).a, l2CompositeMaskEnabled);
${composite.invertMask ? 'l2Mask = 1.0 - l2Mask;' : ''}
vec4 l2Secondary = mix(vec4(1.0), texture2D(l2SecondaryMap, vL2Uv), l2CompositeSecondaryEnabled);
diffuseColor = mix(diffuseColor, l2Composite(diffuseColor, l2Secondary, l2Mask), l2CompositeEnabled);`
    : ''
  const fadeDefinition = material.fade
  const fadeEffect = fadeDefinition
    ? `float l2FadePhase = ${fadeDefinition.period > 0 ? (fadeDefinition.type === 1
      ? `sin(l2Time / ${glsl(fadeDefinition.period)} * 6.2831853 + ${glsl(fadeDefinition.phase)}) * 0.5 + 0.5`
      : `fract(l2Time / ${glsl(fadeDefinition.period)} + ${glsl(fadeDefinition.phase)})`) : '0.0'}; diffuseColor *= mix(vec4(1.0), mix(l2FadeColor1, l2FadeColor2, l2FadePhase), l2FadeEnabled);`
    : ''
  const emission = material.emissiveUrl
    ? `totalEmissiveRadiance += texture2D(l2EmissiveMap, vL2Uv).rgb * l2EmissiveEnabled * ${material.selfIlluminationMaskUrl ? 'mix(1.0, texture2D(l2IlluminationMask, vL2Uv).a, l2IlluminationMaskEnabled)' : '1.0'};`
    : ''
  const specular = material.specularUrl
    ? `reflectedLight.directSpecular += texture2D(l2SpecularMap, vL2Uv).rgb * l2SpecularEnabled * ${material.specularityMaskUrl ? 'mix(1.0, texture2D(l2SpecularityMask, vL2Uv).a, l2SpecularityMaskEnabled)' : '1.0'} * ${material.performLightingOnSpecularPass ? '1.0' : '0.5'};`
    : ''
  const unlit = material.unlit
    ? 'if (l2UnlitEnabled > 0.5) { reflectedLight.directDiffuse = diffuseColor.rgb; reflectedLight.indirectDiffuse = vec3(0.0); reflectedLight.indirectSpecular = vec3(0.0); }'
    : ''
  return source
    .replace('void main() {', `${header}\nvoid main() {`)
    .replace('#include <map_fragment>', `vec4 l2Diffuse = mix(vec4(1.0), texture2D(l2DiffuseMap, vL2Uv), l2DiffuseEnabled);
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

function materialControls(
  definition: PublishedStaticMeshMaterial,
  sourceSide: number
): StaticMeshMaterialControls {
  const texture = <T extends StaticMeshTextureRole>(role: T) => true
  return {
    material: true,
    textures: {
      diffuse: texture('diffuse'),
      opacity: texture('opacity'),
      emissive: texture('emissive'),
      detail: texture('detail'),
      compositeSecondary: texture('compositeSecondary'),
      compositeMask: texture('compositeMask'),
      selfIlluminationMask: texture('selfIlluminationMask'),
      specular: texture('specular'),
      specularityMask: texture('specularityMask')
    },
    behaviors: {
      blending: definition.blendMode !== 'opaque',
      twoSided: sourceSide === DoubleSide || definition.doubleSided === true,
      depthWrite: definition.depthWrite ?? !isTransparent(definition.blendMode),
      depthTest: definition.depthTest ?? true,
      unlit: definition.unlit === true,
      animation: hasAnimation(definition),
      uvEffects: hasUvEffects(definition),
      wind: definition.windMode === 'grass' || definition.windMode === 'foliage',
      fade: definition.fade !== null && definition.fade !== undefined,
      composite: definition.composite !== null && definition.composite !== undefined
    }
  }
}

function authoredState(material: MeshStandardMaterial): StaticMeshMaterialAuthoredState {
  return {
    transparent: material.transparent,
    alphaTest: material.alphaTest,
    blending: material.blending,
    side: material.side,
    depthWrite: material.depthWrite,
    depthTest: material.depthTest,
    opacity: material.opacity
  }
}

function inspections(managed: ManagedMaterial[]) {
  return managed.map(entry => ({
    id: entry.id,
    name: entry.material.name || entry.definition.diffuseUrl || entry.id,
    sections: [...entry.sections],
    blendMode: entry.definition.blendMode ?? 'opaque',
    alphaCutoff: entry.authored.alphaTest,
    doubleSided: entry.authored.side === DoubleSide,
    depthWrite: entry.authored.depthWrite,
    depthTest: entry.authored.depthTest,
    unlit: entry.definition.unlit === true,
    clampU: entry.definition.clampU === true,
    clampV: entry.definition.clampV === true,
    panRate: finite(entry.definition.panRate),
    panRateV: finite(entry.definition.panRateV),
    rotationRate: finite(entry.definition.rotationRate),
    uvOscillation: entry.definition.uvOscillation ?? undefined,
    enabled: entry.controls.material,
    textures: textureRoles
      .map(role => textureInspection(entry, role))
      .filter((texture): texture is StaticMeshTextureInspection => texture !== undefined),
    behaviors: behaviorRoles.map(behavior => behaviorInspection(entry, behavior))
  }))
}

const textureRoles: StaticMeshTextureRole[] = [
  'diffuse',
  'opacity',
  'emissive',
  'detail',
  'compositeSecondary',
  'compositeMask',
  'selfIlluminationMask',
  'specular',
  'specularityMask'
]

const behaviorRoles: StaticMeshMaterialBehavior[] = [
  'blending',
  'twoSided',
  'depthWrite',
  'depthTest',
  'unlit',
  'animation',
  'uvEffects',
  'wind',
  'fade',
  'composite'
]

const textureLabels: Record<StaticMeshTextureRole, string> = {
  diffuse: 'Diffuse',
  opacity: 'Opacity',
  emissive: 'Emissive',
  detail: 'Detail',
  compositeSecondary: 'Composite secondary',
  compositeMask: 'Composite mask',
  selfIlluminationMask: 'Self-illumination mask',
  specular: 'Specular',
  specularityMask: 'Specularity mask'
}

const behaviorLabels: Record<StaticMeshMaterialBehavior, string> = {
  blending: 'Authored blending',
  twoSided: 'Two-sided rendering',
  depthWrite: 'Depth write',
  depthTest: 'Depth test',
  unlit: 'Unlit lighting',
  animation: 'Texture animation',
  uvEffects: 'UV effects',
  wind: 'Wind deformation',
  fade: 'Color fade',
  composite: 'Composite material'
}

function textureInspection(
  entry: ManagedMaterial,
  role: StaticMeshTextureRole
): StaticMeshTextureInspection | undefined {
  const { definition } = entry
  const animation = role === 'diffuse'
    ? definition.diffuseAnimation
    : role === 'opacity'
      ? definition.opacityAnimation
      : role === 'emissive'
        ? definition.emissiveAnimation
        : undefined
  const url = role === 'diffuse'
    ? definition.diffuseUrl
    : role === 'opacity'
      ? definition.opacityUrl
      : role === 'emissive'
        ? definition.emissiveUrl
        : role === 'detail'
          ? definition.detailUrl
          : role === 'compositeSecondary'
            ? definition.composite?.secondaryUrl
            : role === 'compositeMask'
              ? definition.composite?.maskUrl
              : role === 'selfIlluminationMask'
                ? definition.selfIlluminationMaskUrl
                : role === 'specular'
                  ? definition.specularUrl
                  : definition.specularityMaskUrl
  const frameUrls = animation?.frameUrls ?? []
  const sourceUrl = url ?? frameUrls[0]
  if (!sourceUrl) return undefined
  return {
    role,
    label: textureLabels[role],
    url: resolvePublishedGltfMaterialUrl(sourceUrl, entry.modelUrl),
    frameCount: frameUrls.length || 1,
    enabled: entry.controls.textures[role]
  }
}

function behaviorInspection(
  entry: ManagedMaterial,
  behavior: StaticMeshMaterialBehavior
): StaticMeshMaterialBehaviorInspection {
  const { definition } = entry
  const available = behavior === 'blending'
    ? definition.blendMode !== undefined && definition.blendMode !== 'opaque'
    : behavior === 'twoSided' || behavior === 'depthWrite' || behavior === 'depthTest'
      ? true
      : behavior === 'unlit'
        ? definition.unlit === true
        : behavior === 'animation'
          ? hasAnimation(definition)
          : behavior === 'uvEffects'
            ? hasUvEffects(definition)
            : behavior === 'wind'
              ? definition.windMode === 'grass' || definition.windMode === 'foliage'
              : behavior === 'fade'
                ? definition.fade !== null && definition.fade !== undefined
                : definition.composite !== null && definition.composite !== undefined
  return { behavior, label: behaviorLabels[behavior], available, enabled: entry.controls.behaviors[behavior] }
}

function hasAnimation(definition: PublishedStaticMeshMaterial) {
  return [definition.diffuseAnimation, definition.opacityAnimation, definition.emissiveAnimation]
    .some(animation => (animation?.frameUrls.length ?? 0) > 1)
}

function hasUvEffects(definition: PublishedStaticMeshMaterial) {
  return Boolean(definition.panRate || definition.panRateV || definition.rotationRate || definition.uvOscillation)
}

function applyBehavior(entry: ManagedMaterial, behavior: StaticMeshMaterialBehavior) {
  const enabled = entry.controls.behaviors[behavior]
  if (behavior === 'blending') {
    entry.material.transparent = enabled ? entry.authored.transparent : false
    entry.material.alphaTest = enabled ? entry.authored.alphaTest : 0
    entry.material.blending = enabled ? entry.authored.blending : entry.authored.blending
    entry.material.opacity = enabled ? entry.authored.opacity : 1
    entry.material.needsUpdate = true
  } else if (behavior === 'twoSided') {
    entry.material.side = enabled ? DoubleSide : FrontSide
    entry.material.needsUpdate = true
  } else if (behavior === 'depthWrite') {
    entry.material.depthWrite = enabled
  } else if (behavior === 'depthTest') {
    entry.material.depthTest = enabled
  } else if (behavior === 'animation' && !enabled) {
    resetAnimation(entry)
  }
  syncShaderControls(entry)
}

function resetEntry(entry: ManagedMaterial) {
  const next = materialControls(entry.definition, entry.authored.side)
  entry.controls.material = next.material
  Object.assign(entry.controls.textures, next.textures)
  Object.assign(entry.controls.behaviors, next.behaviors)
  entry.material.visible = true
  entry.material.transparent = entry.authored.transparent
  entry.material.alphaTest = entry.authored.alphaTest
  entry.material.blending = entry.authored.blending
  entry.material.side = entry.authored.side
  entry.material.depthWrite = entry.authored.depthWrite
  entry.material.depthTest = entry.authored.depthTest
  entry.material.opacity = entry.authored.opacity
  entry.material.needsUpdate = true
  resetAnimation(entry)
  syncShaderControls(entry)
}

function resetAnimation(entry: ManagedMaterial) {
  const shader = entry.shader ?? (entry.material.userData.l2Shader as Shader | undefined)
  if (!shader) return
  shader.uniforms.l2DiffuseMap!.value = entry.diffuseFrames?.[0] ?? transparentPixel
  shader.uniforms.l2OpacityMap!.value = entry.opacityFrames?.[0] ?? transparentPixel
  shader.uniforms.l2EmissiveMap!.value = entry.emissiveFrames?.[0] ?? transparentPixel
}

function syncShaderControls(entry: ManagedMaterial) {
  const shader = entry.shader ?? (entry.material.userData.l2Shader as Shader | undefined)
  if (!shader) return
  entry.shader = shader
  const { textures, behaviors } = entry.controls
  shader.uniforms.l2DiffuseEnabled!.value = enabled(textures.diffuse)
  shader.uniforms.l2OpacityEnabled!.value = enabled(textures.opacity)
  shader.uniforms.l2EmissiveEnabled!.value = enabled(textures.emissive)
  shader.uniforms.l2DetailEnabled!.value = enabled(textures.detail)
  shader.uniforms.l2CompositeEnabled!.value = enabled(behaviors.composite)
  shader.uniforms.l2CompositeSecondaryEnabled!.value = enabled(textures.compositeSecondary)
  shader.uniforms.l2CompositeMaskEnabled!.value = enabled(textures.compositeMask)
  shader.uniforms.l2IlluminationMaskEnabled!.value = enabled(textures.selfIlluminationMask)
  shader.uniforms.l2SpecularEnabled!.value = enabled(textures.specular)
  shader.uniforms.l2SpecularityMaskEnabled!.value = enabled(textures.specularityMask)
  shader.uniforms.l2UnlitEnabled!.value = enabled(behaviors.unlit)
  shader.uniforms.l2UvEffectsEnabled!.value = enabled(behaviors.uvEffects)
  shader.uniforms.l2WindEnabled!.value = enabled(behaviors.wind)
  shader.uniforms.l2FadeEnabled!.value = enabled(behaviors.fade)
}

function enabled(value: boolean) {
  return value ? 1 : 0
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
