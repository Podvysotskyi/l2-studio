import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getNpcLookupDirectory,
  updateNpcLookupDisplayName
} from '../../app/services/studio-api'
import { useNpcLookupDirectoryStore } from '../../app/stores/npc-lookup-directory'

vi.mock('../../app/services/studio-api', () => ({
  getNpcLookupDirectory: vi.fn(),
  updateNpcLookupDisplayName: vi.fn()
}))

describe('NPC lookup directory store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getNpcLookupDirectory).mockReset()
    vi.mocked(updateNpcLookupDisplayName).mockReset()
  })

  it('loads name-keyed records', async () => {
    vi.mocked(getNpcLookupDirectory).mockResolvedValue([
      { name: 'DARK_ELF', displayName: 'Dark Elf' }
    ])
    const store = useNpcLookupDirectoryStore()

    await store.load('npc-races', 'Race values')

    expect(store.records['npc-races']).toEqual([
      { name: 'DARK_ELF', displayName: 'Dark Elf' }
    ])
    expect(store.isLoading('npc-races')).toBe(false)
  })

  it('replaces the edited record without changing its canonical name', async () => {
    vi.mocked(getNpcLookupDirectory).mockResolvedValue([
      { name: 'ETC', displayName: 'Etc' }
    ])
    vi.mocked(updateNpcLookupDisplayName).mockResolvedValue({
      name: 'ETC',
      displayName: 'Other'
    })
    const store = useNpcLookupDirectoryStore()
    await store.load('npc-sexes')

    await store.updateDisplayName('npc-sexes', 'ETC', 'Other')

    expect(updateNpcLookupDisplayName).toHaveBeenCalledWith('npc-sexes', 'ETC', 'Other')
    expect(store.records['npc-sexes']).toEqual([{ name: 'ETC', displayName: 'Other' }])
  })
})
