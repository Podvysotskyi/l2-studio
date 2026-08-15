import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Map preview capture', () => {
  it('includes water surfaces without loading or rendering water volumes at an optional fixed C1 phase', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/MapPreviewCapture.client.vue'
      ),
      'utf8'
    )

    expect(component).toContain('includeWaterVolumes: false')
    expect(component).toContain('waterSurfaces: true')
    expect(component).toContain('waterVolumes: false')
    expect(component).toContain('animationTimeSeconds?: number')
    expect(component).toContain('preview.renderTopDown(props.animationTimeSeconds)')
  })
})
