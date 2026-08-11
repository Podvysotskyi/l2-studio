import {
  MaterialPluginBase,
  PBRMaterial,
  type UniformBuffer
} from '@babylonjs/core'
import type { SceneAnimationClock } from '../core/animation-clock.js'

export type WindMode = 'grass' | 'foliage'

export interface WindMaterialOptions {
  mode: WindMode
  minY: number
  maxY: number
}

export function windVertexShader() {
  return `
float l2WindHeight = max(l2WindMaxY - l2WindMinY, 0.0001);
float l2WindWeight = l2WindMode < 1.5
  ? smoothstep(0.05, 0.85, (positionUpdated.y - l2WindMinY) / l2WindHeight)
  : 1.0;
vec2 l2WindWorldXZ;
#ifdef INSTANCES
l2WindWorldXZ = world3.xz + positionUpdated.xz;
#else
l2WindWorldXZ = (world * vec4(positionUpdated, 1.0)).xz;
#endif
float l2WindPhase = dot(l2WindWorldXZ, vec2(0.017, 0.011));
float l2WindPrimary = sin(l2WindTime * l2WindSpeed + l2WindPhase);
float l2WindSecondary = sin(l2WindTime * l2WindSpeed * 1.73 + l2WindPhase * 1.41) * 0.35;
float l2WindOffset = (l2WindPrimary + l2WindSecondary) * l2WindAmplitude * l2WindWeight;
positionUpdated.x += l2WindOffset;
positionUpdated.z += l2WindOffset * 0.35;
`.trim()
}

export class WindMaterialPlugin extends MaterialPluginBase {
  private readonly amplitude: number
  private readonly speed: number

  constructor(
    material: PBRMaterial,
    private readonly clock: SceneAnimationClock,
    private readonly options: WindMaterialOptions
  ) {
    super(material, 'L2Wind', 180)
    const height = Math.max(options.maxY - options.minY, 0)
    this.amplitude =
      options.mode === 'grass'
        ? Math.min(height * 0.025, 8)
        : Math.min(height * 0.008, 12)
    this.speed = options.mode === 'grass' ? 1.1 : 0.7
    this._enable(this.amplitude > 0)
  }

  override getUniforms() {
    return {
      ubo: [
        { name: 'l2WindTime', size: 1, type: 'float' },
        { name: 'l2WindMinY', size: 1, type: 'float' },
        { name: 'l2WindMaxY', size: 1, type: 'float' },
        { name: 'l2WindAmplitude', size: 1, type: 'float' },
        { name: 'l2WindSpeed', size: 1, type: 'float' },
        { name: 'l2WindMode', size: 1, type: 'float' }
      ]
    }
  }

  override bindForSubMesh(uniformBuffer: UniformBuffer) {
    uniformBuffer.updateFloat('l2WindTime', this.clock.elapsedSeconds)
    uniformBuffer.updateFloat('l2WindMinY', this.options.minY)
    uniformBuffer.updateFloat('l2WindMaxY', this.options.maxY)
    uniformBuffer.updateFloat('l2WindAmplitude', this.amplitude)
    uniformBuffer.updateFloat('l2WindSpeed', this.speed)
    uniformBuffer.updateFloat(
      'l2WindMode',
      this.options.mode === 'grass' ? 1 : 2
    )
  }

  override getCustomCode(shaderType: string): Record<string, string> | null {
    return shaderType === 'vertex'
      ? { CUSTOM_VERTEX_UPDATE_POSITION: windVertexShader() }
      : null
  }
}
