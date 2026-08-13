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
})
