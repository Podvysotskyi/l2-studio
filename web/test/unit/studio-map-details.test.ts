import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Studio map details Sky Zone preview', () => {
  it('uses an isolated modal preview instead of inline Sky Zone visibility controls', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapDetails.vue'
      ),
      'utf8'
    )

    expect(component).toContain('label="Preview Sky Zone"')
    expect(component).toContain('title="Sky Zone preview"')
    expect(component).toContain('v-model="selectedSkyZoneName"')
    expect(component).toContain(':manifest="skyZonePreviewManifest"')
    expect(component).toContain(':sky-zone-visible="true"')
    expect(component).not.toContain('skyZoneVisible')
    expect(component).not.toContain('skyZoneChunkVisibility')
    expect(component).not.toContain('setSkyZoneChunkVisible')
  })
})
