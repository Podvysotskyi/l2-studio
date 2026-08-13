import { readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { describe, expect, it } from 'vitest'

describe('Studio map world inspector', () => {
  it('opens selected tiles in a slideover and clears selection on close', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapWorldMap.client.vue'
      ),
      'utf8'
    )

    expect(component).toContain('<USlideover')
    expect(component).toContain(':open="Boolean(selectedMap)"')
    expect(component).toContain(
      '@update:open="open => { if (!open) closeSelection() }"'
    )
    expect(component).toContain('function closeSelection()')
    expect(component).toContain('selectedMap.value = undefined')
  })

  it('closes the selected-map slideover when the import drawer opens', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapWorldMap.client.vue'
      ),
      'utf8'
    )

    expect(component).toContain('importDrawerOpen: boolean')
    expect(component).toContain('() => props.importDrawerOpen')
    expect(component).toContain('if (open) closeSelection()')
  })

  it('fills the remaining dashboard height without a viewport-height cap', async () => {
    const component = await readFile(
      resolve(
        import.meta.dirname,
        '../../app/components/pages/maps/StudioMapWorldMap.client.vue'
      ),
      'utf8'
    )

    expect(component).toContain('flex min-h-0 flex-1 flex-col overflow-hidden')
    expect(component).toContain('relative min-h-0 min-w-0 flex-1 bg-[#09120f]')
    expect(component).toContain('map-world-viewport relative h-full')
    expect(component).not.toContain('100dvh')

    const pageShell = await readFile(
      resolve(import.meta.dirname, '../../app/assets/css/main.css'),
      'utf8'
    )

    expect(pageShell).toContain('.studio-page {\n  display: flex;\n  flex: 1;')
  })
})
