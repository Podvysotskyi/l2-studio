import type {
  ParticleColorCurveKey,
  ParticleEmitterManifestEntry,
  ParticleSizeCurveKey,
  SceneObjectManifestEntry
} from '~/types/studio'
import {
  Color3,
  Color4,
  Constants,
  Material,
  Mesh,
  ParticleSystem,
  Quaternion,
  Scene,
  StandardMaterial,
  SphereDirectedParticleEmitter,
  Texture,
  Vector3,
  VertexBuffer
} from '@babylonjs/core'
import {
  unrealForward,
  unrealRotationQuaternion,
  unrealVector
} from '../core/unreal-transform.js'
import { browserDecodedTextureUrl } from '../core/texture-url.js'
import {
  configureWorldMesh,
  configureWorldParticles
} from '../scene/rendering-pipeline.js'

export interface ComposedParticleEffects {
  systems: ParticleSystem[]
  beams: ComposedBeamEffect[]
  diagnostics: string[]
  dispose(): void
}

export interface ComposedBeamEffect {
  name: string
  origin: Vector3
  mesh: Mesh
  setEnabled(enabled: boolean): void
  dispose(): void
}

function numberProperty(
  effect: SceneObjectManifestEntry,
  name: string,
  fallback: number
) {
  const parsed = Number(effect.properties[name])
  return Number.isFinite(parsed) ? parsed : fallback
}

function vectorProperty(
  effect: SceneObjectManifestEntry,
  name: string,
  fallback = Vector3.Zero()
) {
  const values = effect.properties[name]?.split(',').map(Number)
  return values?.length === 3 && values.every(Number.isFinite)
    ? unrealVector({ x: values[0]!, y: values[1]!, z: values[2]! })
    : fallback
}

function rangeProperty(
  effect: SceneObjectManifestEntry,
  name: string,
  fallback: [number, number]
): [number, number] {
  const values = effect.properties[name]?.split(',').map(Number)
  return values?.length === 2 && values.every(Number.isFinite)
    ? [values[0]!, values[1]!]
    : fallback
}

function vectorRangeProperty(
  effect: SceneObjectManifestEntry,
  name: string
): [Vector3, Vector3] | undefined {
  const values = effect.properties[name]
    ?.split(';')
    .map((part) => part.split(',').map(Number))
  return values?.length === 2 &&
    values.every((part) => part.length === 3 && part.every(Number.isFinite))
    ? [
        unrealVector({
          x: values[0]![0]!,
          y: values[0]![1]!,
          z: values[0]![2]!
        }),
        unrealVector({
          x: values[1]![0]!,
          y: values[1]![1]!,
          z: values[1]![2]!
        })
      ]
    : undefined
}

function enabled(effect: SceneObjectManifestEntry) {
  return (
    effect.particle?.enabled ??
    effect.properties.Disabled?.toLocaleLowerCase() !== 'true'
  )
}

function visible(scene: Scene, effect: SceneObjectManifestEntry) {
  if (!scene.activeCamera || scene.fogMode === Scene.FOGMODE_NONE) return true
  const distance = Math.max(scene.fogEnd - scene.fogStart, 1)
  return (
    Vector3.DistanceSquared(
      unrealVector(effect.location),
      scene.activeCamera.position
    ) <=
    distance * distance
  )
}

function normalizedDrawStyle(effect: SceneObjectManifestEntry) {
  if (effect.particle) return effect.particle.drawStyle
  const drawStyle = numberProperty(effect, 'DrawStyle', 3)
  if (drawStyle === 1) return 'alpha-blend'
  if (drawStyle === 3) return 'translucent'
  if (drawStyle === 6) return 'brighten'
  return 'darken'
}

function particleBlendMode(effect: SceneObjectManifestEntry) {
  const drawStyle = normalizedDrawStyle(effect)
  if (drawStyle === 'brighten' || drawStyle === 'translucent')
    return ParticleSystem.BLENDMODE_ADD
  if (drawStyle === 'darken') return ParticleSystem.BLENDMODE_MULTIPLY
  return ParticleSystem.BLENDMODE_STANDARD
}

function materialBlendMode(effect: SceneObjectManifestEntry) {
  const drawStyle = normalizedDrawStyle(effect)
  if (drawStyle === 'brighten' || drawStyle === 'translucent')
    return Constants.ALPHA_ADD
  if (drawStyle === 'darken') return Constants.ALPHA_MULTIPLY
  return Constants.ALPHA_COMBINE
}

function repeatedColorCurve(curve: ParticleColorCurveKey[], repeats: number) {
  if (repeats <= 1 || curve.length === 0) return curve
  const result: ParticleColorCurveKey[] = []
  for (let repeat = 0; repeat < repeats; repeat++)
    for (const key of curve)
      result.push({ ...key, time: (repeat + key.time) / repeats })
  return result
}

function sampleRange(random: () => number, min: number, max: number) {
  return min + (max - min) * random()
}

function hashSeed(value: string) {
  let hash = 2166136261
  for (let index = 0; index < value.length; index++) {
    hash ^= value.charCodeAt(index)
    hash = Math.imul(hash, 16777619)
  }
  return hash >>> 0
}

function seededRandom(seed: number) {
  let state = seed || 1
  return () => {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0
    return state / 0x1_0000_0000
  }
}

function createSystem(effect: SceneObjectManifestEntry, scene: Scene) {
  const particle = effect.particle
  const capacity = Math.max(
    1,
    Math.min(
      Math.round(
        particle?.capacity ?? numberProperty(effect, 'MaxParticles', 10)
      ),
      2_000
    )
  )
  const subdivisionsU = Math.max(
    1,
    Math.round(
      particle?.textureSubdivisions.u ??
        numberProperty(effect, 'TextureUSubdivisions', 1)
    )
  )
  const subdivisionsV = Math.max(
    1,
    Math.round(
      particle?.textureSubdivisions.v ??
        numberProperty(effect, 'TextureVSubdivisions', 1)
    )
  )
  const system = new ParticleSystem(
    effect.name,
    capacity,
    scene,
    null,
    subdivisionsU * subdivisionsV > 1
  )
  configureWorldParticles(system)
  const particleTexture = new Texture(
    browserDecodedTextureUrl(effect.resourceUrl!),
    scene,
    false,
    false
  )
  particleTexture.hasAlpha = true
  system.particleTexture = particleTexture
  system.emitter = unrealVector(effect.location)
  system.blendMode = particleBlendMode(effect)
  system.color1 = new Color4(
    1,
    1,
    1,
    particle?.opacity ?? numberProperty(effect, 'Opacity', 1)
  )
  system.color2 = system.color1.clone()
  system.colorDead = new Color4(1, 1, 1, 0)
  if (subdivisionsU * subdivisionsV > 1) {
    const configureSheet = () => {
      const size = system.particleTexture?.getSize()
      if (!size?.width || !size.height) return
      system.spriteCellWidth = size.width / subdivisionsU
      system.spriteCellHeight = size.height / subdivisionsV
      system.startSpriteCellID = 0
      system.endSpriteCellID = subdivisionsU * subdivisionsV - 1
      system.spriteCellLoop = true
      system.spriteRandomStartCell =
        particle?.textureSubdivisions.random ??
        effect.properties.UseRandomSubdivision?.toLocaleLowerCase() === 'true'
    }
    configureSheet()
    particleTexture.onLoadObservable.addOnce(configureSheet)
  }

  const lifetime = Math.max(numberProperty(effect, 'Lifetime', 2), 0.05)
  const lifetimeRange: [number, number] = particle
    ? [particle.lifetime.min, particle.lifetime.max]
    : rangeProperty(effect, 'LifetimeRange', [
        numberProperty(effect, 'LifetimeRangeMin', lifetime),
        numberProperty(effect, 'LifetimeRangeMax', lifetime)
      ])
  system.minLifeTime = Math.max(lifetimeRange[0], 0.05)
  system.maxLifeTime = Math.max(lifetimeRange[1], system.minLifeTime)
  const ownerScale = Math.max(numberProperty(effect, 'OwnerDrawScale', 1), 0.01)
  const authoredSize = Math.max(numberProperty(effect, 'StartSize', 32), 0.01)
  const sizeRange = particle
    ? [
        unrealVector(particle.startSize.min),
        unrealVector(particle.startSize.max)
      ]
    : vectorRangeProperty(effect, 'StartSizeRange')
  system.minSize = Math.max(
    (sizeRange?.[0]?.x ??
      numberProperty(effect, 'StartSizeMin', authoredSize)) * ownerScale,
    0.01
  )
  system.maxSize = Math.max(
    (sizeRange?.[1]?.x ??
      numberProperty(effect, 'StartSizeMax', authoredSize)) * ownerScale,
    system.minSize
  )
  system.emitRate = Math.max(
    particle?.particlesPerSecond ??
      numberProperty(
        effect,
        'ParticlesPerSecond',
        capacity / system.maxLifeTime
      ),
    0.1
  )
  system.gravity = particle
    ? unrealVector(particle.acceleration)
    : vectorProperty(effect, 'Acceleration')
  const offset = particle
    ? unrealVector(particle.startLocationOffset)
    : vectorProperty(effect, 'StartLocationOffset')
  system.minEmitBox = particle
    ? unrealVector(particle.startLocation.min).add(offset)
    : offset.clone()
  system.maxEmitBox = particle
    ? unrealVector(particle.startLocation.max).add(offset)
    : offset.clone()
  const forward = unrealForward(effect.rotation)
  const defaultPower = effect.className === 'BeamEmitter' ? 96 : 12
  const velocityRange = particle
    ? [
        unrealVector(particle.startVelocity.min),
        unrealVector(particle.startVelocity.max)
      ]
    : vectorRangeProperty(effect, 'StartVelocityRange')
  system.direction1 =
    velocityRange?.[0] ?? vectorProperty(effect, 'StartVelocityMin', forward)
  system.direction2 =
    velocityRange?.[1] ?? vectorProperty(effect, 'StartVelocityMax', forward)
  system.minEmitPower = velocityRange
    ? 1
    : numberProperty(effect, 'StartVelocity', defaultPower)
  system.maxEmitPower = system.minEmitPower
  if (particle?.kind === 'sprite') {
    if (particle.sprite.startLocationShape === 'sphere') {
      const radius = Math.max(particle.sprite.sphereRadius.max, 0)
      const emitter = new SphereDirectedParticleEmitter(
        radius,
        system.direction1,
        system.direction2
      )
      emitter.radiusRange =
        radius > 0
          ? Math.min(
              Math.max(
                1 - Math.max(particle.sprite.sphereRadius.min, 0) / radius,
                0
              ),
              1
            )
          : 1
      system.particleEmitterType = emitter
      system.emitter = unrealVector(effect.location).add(offset)
    }
    if (particle.sprite.directionMode === 'up')
      system.billboardMode = ParticleSystem.BILLBOARDMODE_Y
    if (
      particle.sprite.directionMode === 'normal' ||
      particle.sprite.rotationSource === 'normal'
    )
      system.billboardMode = ParticleSystem.BILLBOARDMODE_STRETCHED_LOCAL
  }
  if (
    particle?.spinParticles ??
    effect.properties.SpinParticles?.toLocaleLowerCase() === 'true'
  ) {
    const spin: [number, number] = particle
      ? [particle.spin.min, particle.spin.max]
      : rangeProperty(effect, 'SpinsPerSecondRange', [
          numberProperty(effect, 'SpinsPerSecondMin', -0.25),
          numberProperty(effect, 'SpinsPerSecondMax', 0.25)
        ])
    const direction = particle && particle.spinDirection.x > 0.5 ? -1 : 1
    system.minAngularSpeed = spin[0] * Math.PI * 2 * direction
    system.maxAngularSpeed = spin[1] * Math.PI * 2 * direction
  }
  const opacity = particle?.opacity ?? numberProperty(effect, 'Opacity', 1)
  const fadeIn = effect.properties.FadeIn?.toLocaleLowerCase() === 'true'
  const fadeOut = effect.properties.FadeOut?.toLocaleLowerCase() === 'true'
  if (fadeIn) {
    system.addColorGradient(0, new Color4(1, 1, 1, 0))
    system.addColorGradient(
      Math.min(
        numberProperty(effect, 'FadeInEndTime', 0.2) / system.maxLifeTime,
        1
      ),
      new Color4(1, 1, 1, opacity)
    )
  }
  if (fadeOut) {
    system.addColorGradient(
      Math.min(
        numberProperty(effect, 'FadeOutStartTime', system.maxLifeTime * 0.8) /
          system.maxLifeTime,
        1
      ),
      new Color4(1, 1, 1, opacity)
    )
    system.addColorGradient(1, new Color4(1, 1, 1, 0))
  }
  particle?.sizeCurve.forEach((key) =>
    system.addSizeGradient(key.time, key.relativeSize)
  )
  repeatedColorCurve(
    particle?.colorCurve ?? [],
    particle?.kind === 'sprite' ? particle.sprite.colorScaleRepeats : 1
  ).forEach((key) =>
    system.addColorGradient(
      key.time,
      new Color4(key.color.r, key.color.g, key.color.b, key.color.a)
    )
  )
  system.updateSpeed = 1 / 60
  const warmupTicks =
    particle?.warmupTicksPerSecond ??
    numberProperty(effect, 'WarmupTicksPerSecond', 0)
  const warmupTime =
    particle?.warmupTime ?? numberProperty(effect, 'WarmupTime', 2)
  system.preWarmCycles = Math.min(Math.round(warmupTicks * warmupTime), 120)
  system.preWarmStepOffset = 1 / Math.max(warmupTicks, 1)

  system.start()
  return system
}

type BeamParticle = Extract<ParticleEmitterManifestEntry, { kind: 'beam' }>

interface LiveBeam {
  age: number
  lifetime: number
  start: Vector3
  end: Vector3
  width: number
}

function curveFactor(curve: ParticleSizeCurveKey[], time: number) {
  if (curve.length === 0) return 1
  const nextIndex = curve.findIndex((key) => key.time >= time)
  if (nextIndex <= 0) return curve[Math.max(nextIndex, 0)]!.relativeSize
  if (nextIndex < 0) return curve.at(-1)!.relativeSize
  const previous = curve[nextIndex - 1]!
  const next = curve[nextIndex]!
  const amount =
    (time - previous.time) / Math.max(next.time - previous.time, 1e-6)
  return (
    previous.relativeSize + (next.relativeSize - previous.relativeSize) * amount
  )
}

function curveColor(curve: ParticleColorCurveKey[], time: number) {
  if (curve.length === 0) return new Color4(1, 1, 1, 1)
  const nextIndex = curve.findIndex((key) => key.time >= time)
  const value = (key: ParticleColorCurveKey) =>
    new Color4(key.color.r, key.color.g, key.color.b, key.color.a)
  if (nextIndex <= 0) return value(curve[Math.max(nextIndex, 0)]!)
  if (nextIndex < 0) return value(curve.at(-1)!)
  const previous = curve[nextIndex - 1]!
  const next = curve[nextIndex]!
  const amount =
    (time - previous.time) / Math.max(next.time - previous.time, 1e-6)
  return Color4.Lerp(value(previous), value(next), amount)
}

class RibbonBeamEffect implements ComposedBeamEffect {
  readonly name: string
  readonly origin: Vector3
  readonly mesh: Mesh
  private readonly material: StandardMaterial
  private readonly particle: BeamParticle
  private readonly effect: SceneObjectManifestEntry
  private readonly random: () => number
  private readonly rotation: Quaternion
  private readonly ownerScale: number
  private readonly positions: Float32Array
  private readonly colors: Float32Array
  private readonly observer: ReturnType<
    Scene['onBeforeRenderObservable']['add']
  >
  private readonly live: LiveBeam[] = []
  private emissionRemainder = 0
  private running = true

  constructor(
    scene: Scene,
    effect: SceneObjectManifestEntry,
    particle: BeamParticle
  ) {
    this.name = effect.name
    this.origin = unrealVector(effect.location)
    this.effect = effect
    this.particle = particle
    this.random = seededRandom(hashSeed(effect.name))
    this.rotation = unrealRotationQuaternion(effect.rotation)
    this.ownerScale = Math.max(
      numberProperty(effect, 'OwnerDrawScale', 1),
      0.01
    )

    const sheets = particle.beam.rotatingSheets
    const quadCount = particle.capacity * sheets
    this.positions = new Float32Array(quadCount * 12)
    this.colors = new Float32Array(quadCount * 16)
    const uvs = new Float32Array(quadCount * 8)
    const indices = new Array<number>(quadCount * 6)
    for (let quad = 0; quad < quadCount; quad++) {
      const uv = quad * 8
      uvs.set(
        [
          0,
          0,
          particle.beam.textureUScale,
          0,
          particle.beam.textureUScale,
          particle.beam.textureVScale,
          0,
          particle.beam.textureVScale
        ],
        uv
      )
      const vertex = quad * 4
      indices.splice(
        quad * 6,
        6,
        vertex,
        vertex + 1,
        vertex + 2,
        vertex,
        vertex + 2,
        vertex + 3
      )
    }

    const texture = new Texture(
      browserDecodedTextureUrl(effect.resourceUrl!),
      scene,
      false,
      false
    )
    texture.hasAlpha = true
    this.material = new StandardMaterial(`${effect.name}-beam-material`, scene)
    this.material.diffuseTexture = texture
    this.material.diffuseColor = Color3.White()
    this.material.emissiveColor = Color3.White()
    this.material.disableLighting = true
    this.material.backFaceCulling = false
    this.material.disableDepthWrite = true
    this.material.useAlphaFromDiffuseTexture = true
    this.material.transparencyMode = Material.MATERIAL_ALPHABLEND
    this.material.alphaMode = materialBlendMode(effect)

    this.mesh = new Mesh(`${effect.name}-beam`, scene)
    this.mesh.material = this.material
    this.mesh.isPickable = false
    this.mesh.alwaysSelectAsActiveMesh = true
    this.mesh.hasVertexAlpha = true
    configureWorldMesh(this.mesh)
    this.mesh.setVerticesData(VertexBuffer.PositionKind, this.positions, true)
    this.mesh.setVerticesData(VertexBuffer.UVKind, uvs, false)
    this.mesh.setVerticesData(VertexBuffer.ColorKind, this.colors, true, 4)
    this.mesh.setIndices(indices)

    const warmup = Math.min(particle.warmupTime, particle.lifetime.max)
    const warmCount = Math.min(
      particle.capacity,
      Math.floor(particle.particlesPerSecond * warmup)
    )
    for (let index = 0; index < warmCount; index++) {
      const beam = this.spawn()
      beam.age = (index / Math.max(warmCount, 1)) * warmup
      this.live.push(beam)
    }
    this.updateGeometry(scene)
    this.observer = scene.onBeforeRenderObservable.add(() => this.update(scene))
  }

  setEnabled(enabled: boolean) {
    this.running = enabled
    this.mesh.setEnabled(enabled)
  }

  dispose() {
    this.mesh.getScene().onBeforeRenderObservable.remove(this.observer)
    this.mesh.dispose()
    this.material.dispose(false, true)
  }

  private sampleVector(range: BeamParticle['startLocation']) {
    return unrealVector({
      x: sampleRange(this.random, range.min.x, range.max.x),
      y: sampleRange(this.random, range.min.y, range.max.y),
      z: sampleRange(this.random, range.min.z, range.max.z)
    })
  }

  private spawn() {
    const endpointTotal = this.particle.beam.endPoints.reduce(
      (total, endpoint) => total + endpoint.weight,
      0
    )
    let selection = this.random() * Math.max(endpointTotal, 1)
    const endpoint =
      this.particle.beam.endPoints.find((candidate) => {
        selection -=
          endpointTotal > 0
            ? candidate.weight
            : 1 / this.particle.beam.endPoints.length
        return selection <= 0
      }) ?? this.particle.beam.endPoints.at(-1)!
    const localStart = this.sampleVector(this.particle.startLocation)
      .addInPlace(unrealVector(this.particle.startLocationOffset))
      .scaleInPlace(this.ownerScale)
      .rotateByQuaternionToRef(this.rotation, new Vector3())
    const localEnd = this.sampleVector(endpoint.offset)
      .scaleInPlace(this.ownerScale)
      .rotateByQuaternionToRef(this.rotation, new Vector3())
    return {
      age: 0,
      lifetime: Math.max(
        sampleRange(
          this.random,
          this.particle.lifetime.min,
          this.particle.lifetime.max
        ),
        0.05
      ),
      start: this.origin.add(localStart),
      end: this.origin.add(localEnd),
      width:
        Math.max(
          sampleRange(
            this.random,
            this.particle.startSize.min.x,
            this.particle.startSize.max.x
          ),
          0.01
        ) * this.ownerScale
    } satisfies LiveBeam
  }

  private update(scene: Scene) {
    if (!this.running) return
    const elapsed = Math.min(scene.getEngine().getDeltaTime() / 1000, 0.1)
    for (const beam of this.live) beam.age += elapsed
    for (let index = this.live.length - 1; index >= 0; index--)
      if (this.live[index]!.age >= this.live[index]!.lifetime)
        this.live.splice(index, 1)
    this.emissionRemainder += elapsed * this.particle.particlesPerSecond
    while (
      this.emissionRemainder >= 1 &&
      this.live.length < this.particle.capacity
    ) {
      this.live.push(this.spawn())
      this.emissionRemainder--
    }
    this.updateGeometry(scene)
  }

  private updateGeometry(scene: Scene) {
    this.positions.fill(0)
    this.colors.fill(0)
    const sheets = this.particle.beam.rotatingSheets
    for (let beamIndex = 0; beamIndex < this.live.length; beamIndex++) {
      const beam = this.live[beamIndex]!
      const progress = Math.min(beam.age / beam.lifetime, 1)
      const direction = beam.end.subtract(beam.start).normalize()
      const midpoint = Vector3.Center(beam.start, beam.end)
      const view = (scene.activeCamera?.position ?? Vector3.Up()).subtract(
        midpoint
      )
      let side = Vector3.Cross(direction, view).normalize()
      if (side.lengthSquared() < 1e-6)
        side = Vector3.Cross(direction, Vector3.Up()).normalize()
      if (side.lengthSquared() < 1e-6) side = Vector3.Right()
      const halfWidth =
        (beam.width * curveFactor(this.particle.sizeCurve, progress)) / 2
      const color = curveColor(this.particle.colorCurve, progress)
      color.a *= this.particle.opacity * this.fade(progress, beam.lifetime)
      for (let sheet = 0; sheet < sheets; sheet++) {
        const rotatedSide = side.rotateByQuaternionToRef(
          Quaternion.RotationAxis(direction, (sheet * Math.PI) / sheets),
          new Vector3()
        )
        const edge = rotatedSide.scale(halfWidth)
        const vertices = [
          beam.start.subtract(edge),
          beam.end.subtract(edge),
          beam.end.add(edge),
          beam.start.add(edge)
        ]
        const quad = beamIndex * sheets + sheet
        for (let vertex = 0; vertex < 4; vertex++) {
          const positionOffset = quad * 12 + vertex * 3
          this.positions.set(vertices[vertex]!.asArray(), positionOffset)
          const colorOffset = quad * 16 + vertex * 4
          this.colors.set(color.asArray(), colorOffset)
        }
      }
    }
    this.mesh.updateVerticesData(
      VertexBuffer.PositionKind,
      this.positions,
      true,
      false
    )
    this.mesh.updateVerticesData(
      VertexBuffer.ColorKind,
      this.colors,
      false,
      false
    )
  }

  private fade(progress: number, lifetime: number) {
    let alpha = 1
    if (this.effect.properties.FadeIn?.toLocaleLowerCase() === 'true')
      alpha *= Math.min(
        (progress * lifetime) /
          Math.max(numberProperty(this.effect, 'FadeInEndTime', 0.2), 1e-6),
        1
      )
    if (this.effect.properties.FadeOut?.toLocaleLowerCase() === 'true') {
      const start = numberProperty(
        this.effect,
        'FadeOutStartTime',
        lifetime * 0.8
      )
      if (progress * lifetime > start)
        alpha *= Math.max(
          1 - (progress * lifetime - start) / Math.max(lifetime - start, 1e-6),
          0
        )
    }
    return alpha
  }
}

export function composeParticleEffects(
  scene: Scene,
  effects: SceneObjectManifestEntry[]
): ComposedParticleEffects {
  const systems: ParticleSystem[] = []
  const beams: ComposedBeamEffect[] = []
  const diagnostics: string[] = []
  for (const effect of effects) {
    if (
      effect.className !== 'SpriteEmitter' &&
      effect.className !== 'BeamEmitter'
    )
      continue
    effect.particle?.diagnostics.forEach((message) =>
      diagnostics.push(`${effect.name}: ${message}`)
    )
    if (!enabled(effect) || !visible(scene, effect)) continue
    if (!effect.owner) {
      diagnostics.push(`${effect.name}: authored emitter owner is missing.`)
      continue
    }
    if (!effect.resourceUrl) {
      diagnostics.push(`${effect.name}: particle texture is unavailable.`)
      continue
    }
    if (effect.className === 'BeamEmitter') {
      if (effect.particle?.kind !== 'beam') {
        diagnostics.push(`${effect.name}: typed beam settings are unavailable.`)
        continue
      }
      if (
        effect.particle.beam.endPointMode !== 'offset' ||
        effect.particle.beam.endPoints.length === 0
      ) {
        diagnostics.push(
          `${effect.name}: no supported beam offset endpoint is available.`
        )
        continue
      }
      beams.push(new RibbonBeamEffect(scene, effect, effect.particle))
      continue
    }
    systems.push(createSystem(effect, scene))
  }
  return {
    systems,
    beams,
    diagnostics,
    dispose() {
      systems.forEach((system) => system.dispose(true))
      beams.forEach((beam) => beam.dispose())
    }
  }
}
