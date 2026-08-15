import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getLookupDirectory,
  getPlayerClasses
} from '../../app/services/studio-api'
import { useLookupDirectoryStore } from '../../app/stores/lookup-directory'
import { usePlayerClassDirectoryStore } from '../../app/stores/player-class-directory'

vi.mock('../../app/services/studio-api', () => ({
  getLookupDirectory: vi.fn(),
  getPlayerClasses: vi.fn()
}))

describe('Lookup and player-class stores', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getLookupDirectory).mockReset()
    vi.mocked(getPlayerClasses).mockReset()
  })

  it('loads a lookup catalog and clears its loading state', async () => {
    vi.mocked(getLookupDirectory).mockResolvedValue({
      items: [{ id: 0, name: 'Human' }], total: 1, page: 1, pageSize: 100
    })
    const store = useLookupDirectoryStore()

    const load = store.load('player-races', 'Player race')
    expect(store.isLoading('player-races')).toBe(true)
    await load

    expect(getLookupDirectory).toHaveBeenCalledWith('player-races', { page: 1, pageSize: 100 })
    expect(store.records['player-races']).toEqual([{ id: 0, name: 'Human' }])
    expect(store.isLoading('player-races')).toBe(false)
    expect(store.errors['player-races']).toBeUndefined()
  })

  it('uses the supplied label in lookup loading failures', async () => {
    vi.mocked(getLookupDirectory).mockRejectedValue(new Error('Unavailable'))
    const store = useLookupDirectoryStore()

    await store.load('npc-types', 'NPC type')

    expect(store.errors['npc-types']).toBe(
      'The npc type catalog could not be loaded.'
    )
    expect(store.isLoading('npc-types')).toBe(false)
  })

  it('loads the player class variants', async () => {
    vi.mocked(getPlayerClasses).mockResolvedValue([
      {
        id: 0,
        name: 'Human Fighter',
        parentClassId: null,
        isMage: false,
        allowedRaces: []
      }
    ])
    const store = usePlayerClassDirectoryStore()

    await store.load()

    expect(store.records[0]?.name).toBe('Human Fighter')
    expect(store.loading).toBe(false)
    expect(store.error).toBeUndefined()
  })

  it('reports player class loading failures', async () => {
    vi.mocked(getPlayerClasses).mockRejectedValue(new Error('Unavailable'))
    const store = usePlayerClassDirectoryStore()

    await store.load()

    expect(store.error).toBe(
      'The player class hierarchy could not be loaded from the Studio API.'
    )
    expect(store.loading).toBe(false)
  })
})
