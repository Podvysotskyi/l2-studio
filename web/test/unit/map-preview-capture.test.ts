import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Map preview capture', () => {
  it('includes water surfaces and resolved water volumes in the generated preview', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/MapPreviewCapture.client.vue'
      ),
      'utf8'
    )

    expect(component).toContain('waterSurfaces: true')
    expect(component).toContain('waterVolumes: true')
  })
})
