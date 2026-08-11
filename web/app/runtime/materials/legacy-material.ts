import {
  BaseTexture,
  MaterialDefines,
  MaterialPluginBase,
  PBRMaterial,
  Scene,
  Texture,
  type UniformBuffer
} from '@babylonjs/core'
import {
  sceneAnimationClock,
  type SceneAnimationClock
} from '../core/animation-clock.js'
import { browserDecodedTextureUrl } from '../core/texture-url.js'

export interface LegacyMaterialColor {
  r: number
  g: number
  b: number
  a: number
}

export interface LegacyMaterialFade {
  color1: LegacyMaterialColor
  color2: LegacyMaterialColor
  type: number
  period: number
  phase: number
}

export interface LegacyMaterialComposite {
  secondaryUrl?: string | null
  secondaryTint?: LegacyMaterialColor | null
  secondaryFade?: LegacyMaterialFade | null
  maskUrl?: string | null
  colorOperation: number
  alphaOperation: number
  invertMask: boolean
  modulateScale: number
}

export interface LegacyMaterialOptions {
  fade?: LegacyMaterialFade | null
  composite?: LegacyMaterialComposite | null
  selfIlluminationMaskUrl?: string | null
  specularUrl?: string | null
  specularityMaskUrl?: string | null
}

const color = (value?: LegacyMaterialColor | null) =>
  value ?? { r: 1, g: 1, b: 1, a: 1 }

export class LegacyMaterialPlugin extends MaterialPluginBase {
  private readonly clock: SceneAnimationClock
  private readonly secondary?: Texture
  private readonly mask?: Texture
  private readonly selfIlluminationMask?: Texture
  private readonly specular?: Texture
  private readonly specularityMask?: Texture

  constructor(
    material: PBRMaterial,
    private readonly options: LegacyMaterialOptions
  ) {
    super(material, 'L2LegacyMaterial', 170)
    const scene = material.getScene()
    this.clock = sceneAnimationClock(scene)
    this.secondary = this.texture(options.composite?.secondaryUrl, scene)
    this.mask = this.texture(options.composite?.maskUrl, scene, false)
    this.selfIlluminationMask = this.texture(
      options.selfIlluminationMaskUrl,
      scene,
      false
    )
    this.specular = this.texture(options.specularUrl, scene)
    this.specularityMask = this.texture(
      options.specularityMaskUrl,
      scene,
      false
    )
    this._enable(
      Boolean(
        options.fade ||
        options.composite ||
        this.selfIlluminationMask ||
        this.specular
      )
    )
  }

  private texture(
    url: string | null | undefined,
    scene: Scene,
    gammaSpace = true
  ) {
    if (!url) return undefined
    const texture = new Texture(
      browserDecodedTextureUrl(url),
      scene,
      false,
      false
    )
    texture.gammaSpace = gammaSpace
    return texture
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
    if (!attributes.includes('uv')) attributes.push('uv')
  }

  override getSamplers(samplers: string[]) {
    samplers.push(
      'l2SecondarySampler',
      'l2CompositeMaskSampler',
      'l2SelfIlluminationMaskSampler',
      'l2SpecularSampler',
      'l2SpecularityMaskSampler'
    )
  }

  override getUniforms() {
    return {
      ubo: [
        { name: 'l2LegacyTime', size: 1, type: 'float' },
        { name: 'l2Fade1', size: 4, type: 'vec4' },
        { name: 'l2Fade2', size: 4, type: 'vec4' },
        { name: 'l2FadeSettings', size: 4, type: 'vec4' },
        { name: 'l2SecondaryTint', size: 4, type: 'vec4' },
        { name: 'l2SecondaryFade1', size: 4, type: 'vec4' },
        { name: 'l2SecondaryFade2', size: 4, type: 'vec4' },
        { name: 'l2SecondaryFadeSettings', size: 4, type: 'vec4' },
        { name: 'l2CompositeSettings', size: 4, type: 'vec4' },
        { name: 'l2LegacySources', size: 4, type: 'vec4' },
        { name: 'l2SpecularMaskPresent', size: 1, type: 'float' }
      ]
    }
  }

  override bindForSubMesh(uniformBuffer: UniformBuffer) {
    const fade = this.options.fade
    const composite = this.options.composite
    const secondaryFade = composite?.secondaryFade
    const fade1 = color(fade?.color1)
    const fade2 = color(fade?.color2)
    const secondaryTint = color(composite?.secondaryTint)
    const secondaryFade1 = color(secondaryFade?.color1)
    const secondaryFade2 = color(secondaryFade?.color2)
    uniformBuffer.updateFloat('l2LegacyTime', this.clock.elapsedSeconds)
    uniformBuffer.updateFloat4('l2Fade1', fade1.r, fade1.g, fade1.b, fade1.a)
    uniformBuffer.updateFloat4('l2Fade2', fade2.r, fade2.g, fade2.b, fade2.a)
    uniformBuffer.updateFloat4(
      'l2FadeSettings',
      fade?.type ?? 0,
      fade?.period ?? 0,
      fade?.phase ?? 0,
      fade ? 1 : 0
    )
    uniformBuffer.updateFloat4(
      'l2SecondaryTint',
      secondaryTint.r,
      secondaryTint.g,
      secondaryTint.b,
      secondaryTint.a
    )
    uniformBuffer.updateFloat4(
      'l2SecondaryFade1',
      secondaryFade1.r,
      secondaryFade1.g,
      secondaryFade1.b,
      secondaryFade1.a
    )
    uniformBuffer.updateFloat4(
      'l2SecondaryFade2',
      secondaryFade2.r,
      secondaryFade2.g,
      secondaryFade2.b,
      secondaryFade2.a
    )
    uniformBuffer.updateFloat4(
      'l2SecondaryFadeSettings',
      secondaryFade?.type ?? 0,
      secondaryFade?.period ?? 0,
      secondaryFade?.phase ?? 0,
      secondaryFade ? 1 : 0
    )
    uniformBuffer.updateFloat4(
      'l2CompositeSettings',
      composite?.colorOperation ?? 0,
      composite?.alphaOperation ?? 0,
      composite?.invertMask ? 1 : 0,
      composite?.modulateScale ?? 1
    )
    uniformBuffer.updateFloat4(
      'l2LegacySources',
      this.secondary ? 1 : 0,
      this.mask ? 1 : 0,
      this.selfIlluminationMask ? 1 : 0,
      this.specular ? 1 : 0
    )
    uniformBuffer.updateFloat(
      'l2SpecularMaskPresent',
      this.specularityMask ? 1 : 0
    )
    if (this.secondary)
      uniformBuffer.setTexture('l2SecondarySampler', this.secondary)
    if (this.mask) uniformBuffer.setTexture('l2CompositeMaskSampler', this.mask)
    if (this.selfIlluminationMask)
      uniformBuffer.setTexture(
        'l2SelfIlluminationMaskSampler',
        this.selfIlluminationMask
      )
    if (this.specular)
      uniformBuffer.setTexture('l2SpecularSampler', this.specular)
    if (this.specularityMask)
      uniformBuffer.setTexture('l2SpecularityMaskSampler', this.specularityMask)
  }

  override getActiveTextures(activeTextures: BaseTexture[]) {
    for (const texture of [
      this.secondary,
      this.mask,
      this.selfIlluminationMask,
      this.specular,
      this.specularityMask
    ])
      if (texture) activeTextures.push(texture)
  }

  override hasTexture(texture: BaseTexture) {
    return [
      this.secondary,
      this.mask,
      this.selfIlluminationMask,
      this.specular,
      this.specularityMask
    ].includes(texture as Texture)
  }

  override dispose(forceDisposeTextures?: boolean) {
    if (!forceDisposeTextures) return
    this.secondary?.dispose()
    this.mask?.dispose()
    this.selfIlluminationMask?.dispose()
    this.specular?.dispose()
    this.specularityMask?.dispose()
  }

  override getCustomCode(shaderType: string): Record<string, string> | null {
    if (shaderType !== 'fragment') return null
    return {
      CUSTOM_FRAGMENT_DEFINITIONS: `
uniform sampler2D l2SecondarySampler;
uniform sampler2D l2CompositeMaskSampler;
uniform sampler2D l2SelfIlluminationMaskSampler;
uniform sampler2D l2SpecularSampler;
uniform sampler2D l2SpecularityMaskSampler;
float l2FadeAmount(float kind, float period, float phase) {
  if (period <= 0.0001) return 0.0;
  float cycle = fract(l2LegacyTime / period + phase);
  return kind > 0.5
    ? 0.5 + 0.5 * sin(cycle * 6.28318530718)
    : 1.0 - abs(cycle * 2.0 - 1.0);
}
vec4 l2FadeColor(vec4 first, vec4 second, vec4 settings) {
  return mix(first, second, l2FadeAmount(settings.x, settings.y, settings.z));
}`,
      CUSTOM_FRAGMENT_UPDATE_ALBEDO: `
if (l2FadeSettings.w > 0.5) {
  vec4 faded = l2FadeColor(l2Fade1, l2Fade2, l2FadeSettings);
  surfaceAlbedo = faded.rgb;
  alpha = faded.a;
}
if (l2CompositeSettings.x > 0.5) {
  vec4 primary = vec4(surfaceAlbedo, alpha);
  vec4 secondary = l2LegacySources.x > 0.5
    ? texture2D(l2SecondarySampler, vMainUV1)
    : l2SecondaryTint;
  if (l2SecondaryFadeSettings.w > 0.5)
    secondary *= l2FadeColor(
      l2SecondaryFade1,
      l2SecondaryFade2,
      l2SecondaryFadeSettings
    );
  float maskValue = l2LegacySources.y > 0.5
    ? texture2D(l2CompositeMaskSampler, vMainUV1).a
    : primary.a;
  if (l2CompositeSettings.z > 0.5) maskValue = 1.0 - maskValue;
  float operation = l2CompositeSettings.x;
  if (operation < 2.5)
    surfaceAlbedo = primary.rgb * secondary.rgb * l2CompositeSettings.w;
  else if (operation < 3.5)
    surfaceAlbedo = primary.rgb + secondary.rgb;
  else if (operation < 6.5)
    surfaceAlbedo = primary.rgb + secondary.rgb * maskValue;
  if (l2CompositeSettings.y < 0.5) alpha = maskValue;
  else alpha = primary.a * secondary.a;
}`,
      CUSTOM_FRAGMENT_BEFORE_FOG: `
if (l2LegacySources.z > 0.5) {
  vec3 illuminationSample = texture2D(
    l2SelfIlluminationMaskSampler,
    vMainUV1
  ).rgb;
  float illuminationMask = dot(
    illuminationSample,
    vec3(0.2126, 0.7152, 0.0722)
  );
  finalColor.rgb -= finalEmissive * (1.0 - illuminationMask);
}
if (l2LegacySources.w > 0.5) {
  vec3 legacySpecular = texture2D(l2SpecularSampler, vMainUV1).rgb;
  float legacySpecularity = 1.0;
  if (l2SpecularMaskPresent > 0.5)
    legacySpecularity = texture2D(l2SpecularityMaskSampler, vMainUV1).r;
  finalColor.rgb += legacySpecular * legacySpecularity * 0.15;
}`
    }
  }
}
