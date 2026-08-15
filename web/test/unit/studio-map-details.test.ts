import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Studio map details Sky Zone preview', () => {
  it('defaults diagnostic water volumes to hidden while retaining the visibility control', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapDetails.vue'
      ),
      'utf8'
    )

    expect(component).toContain('const waterVolumesVisible = ref(false)')
    expect(component).toContain('waterVolumesVisible.value = false')
    expect(component).toContain('v-model="waterVolumesVisible"')
  })

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

  it('shows generated previews and the raw manifest tree in the Summary tab', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapDetails.vue'
      ),
      'utf8'
    )

    expect(component).toContain('getPublishedManifestWithRaw')
    expect(component).toContain("'mappreviews'")
    expect(component).toContain("entry.sourceKey,\n      true")
    expect(component).toContain('label="Generated map preview"')
    expect(component).toContain("'Regenerate preview' : 'Generate preview'")
    expect(component).toContain('<StudioJsonTree :value="rawManifest" />')
  })
})
