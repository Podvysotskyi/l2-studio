import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Studio map directory actions', () => {
  it('replaces the import-jobs link with a full-page refresh action', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapDirectory.vue'
      ),
      'utf8'
    )

    expect(component).toContain('function refreshPage()')
    expect(component).toContain('window.location.reload()')
    expect(component).toContain('label="Refresh"')
    expect(component).not.toContain('label="Import jobs"')
    expect(component).not.toContain('to="/pipeline/imports"')
    expect(component).toContain(':import-drawer-open="importDrawerOpen"')
  })
})
