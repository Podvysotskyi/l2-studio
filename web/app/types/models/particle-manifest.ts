import type { MapVector } from './map-manifest'

export interface ParticleNumberRange {
  min: number
  max: number
}

export interface ParticleVectorRange {
  min: MapVector
  max: MapVector
}

export interface ParticleColorCurveKey {
  time: number
  color: { r: number; g: number; b: number; a: number }
}

export interface ParticleSizeCurveKey {
  time: number
  relativeSize: number
}

export type ParticleDrawStyle =
  'alpha-blend' | 'translucent' | 'darken' | 'brighten'

export interface ParticleEmitterManifestBase {
  kind: 'sprite' | 'beam'
  enabled: boolean
  capacity: number
  drawStyle: ParticleDrawStyle
  opacity: number
  lifetime: ParticleNumberRange
  startSize: ParticleVectorRange
  startVelocity: ParticleVectorRange
  startLocation: ParticleVectorRange
  startLocationOffset: MapVector
  acceleration: MapVector
  particlesPerSecond: number
  spinParticles: boolean
  spin: ParticleNumberRange
  spinDirection: MapVector
  textureSubdivisions: { u: number; v: number; random: boolean }
  sizeCurve: ParticleSizeCurveKey[]
  colorCurve: ParticleColorCurveKey[]
  warmupTime: number
  warmupTicksPerSecond: number
  diagnostics: string[]
}

export interface ParticleSpriteSettings {
  directionMode: 'none' | 'up' | 'normal' | 'unsupported'
  startLocationShape: 'box' | 'sphere' | 'unsupported'
  sphereRadius: ParticleNumberRange
  rotationSource: 'none' | 'normal' | 'unsupported'
  colorScaleRepeats: number
}

export interface ParticleBeamEndPoint {
  offset: ParticleVectorRange
  weight: number
}

export interface ParticleBeamSettings {
  endPointMode: 'offset' | 'unsupported'
  endPoints: ParticleBeamEndPoint[]
  textureUScale: number
  textureVScale: number
  rotatingSheets: number
}

export type ParticleEmitterManifestEntry =
  | (ParticleEmitterManifestBase & {
      kind: 'sprite'
      sprite: ParticleSpriteSettings
      beam: null
    })
  | (ParticleEmitterManifestBase & {
      kind: 'beam'
      sprite: null
      beam: ParticleBeamSettings
    })
