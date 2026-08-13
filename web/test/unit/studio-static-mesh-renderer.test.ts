import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { DoubleSide } from 'three'
import { describe, expect, it } from 'vitest'
import {
  applyStudioStaticMeshBackFaceTint,
  studioStaticMeshBackFaceBrightness,
  studioStaticMeshMaterialOptions
} from '../../app/runtime/preview/studio-static-mesh-renderer'

describe('Studio static-mesh renderer', () => {
  it('renders both sides with the shared diagnostic material', () => {
    expect(studioStaticMeshMaterialOptions.side).toBe(DoubleSide)
  })

  it('darkens only the interior-facing fragments', () => {
    const shader = { fragmentShader: 'void main() {\n#include <color_fragment>\n}' }

    applyStudioStaticMeshBackFaceTint(shader)

    expect(shader.fragmentShader).toContain('!gl_FrontFacing')
    expect(shader.fragmentShader).toContain(
      `diffuseColor.rgb *= ${studioStaticMeshBackFaceBrightness}`
    )
  })

  it('uses the supported Three.js timer lifecycle in preview renderers', async () => {
    const runtimeRoot = resolve(import.meta.dirname, '../../app/runtime/preview')
    const sources = await Promise.all([
      'studio-static-mesh-renderer.ts',
      'studio-world-renderer.ts'
    ].map(file => readFile(resolve(runtimeRoot, file), 'utf8')))

    sources.forEach(source => {
      expect(source).not.toMatch(/\bClock\b/)
      expect(source).toContain('new Timer()')
      expect(source).toContain('this.timer.update(timestamp)')
      expect(source).toContain('this.timer.getElapsed()')
      expect(source).toContain('this.timer.dispose()')
    })
  })
})
