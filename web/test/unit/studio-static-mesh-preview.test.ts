import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Studio static-mesh preview inspector', () => {
  it('unmounts the preview when the slideover closes', async () => {
    const directory = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/assets/StudioStaticMeshDirectory.vue'
      ),
      'utf8'
    )

    expect(directory).toContain('<USlideover')
    expect(directory).toContain(':open="Boolean(selectedMesh)"')
    expect(directory).toContain(
      '@update:open="open => { if (!open) closePreview() }"'
    )
    expect(directory).toContain('v-if="selectedMesh?.url"')
    expect(directory).toContain('function closePreview()')
    expect(directory).toContain('selectedMesh.value = undefined')
  })
})
