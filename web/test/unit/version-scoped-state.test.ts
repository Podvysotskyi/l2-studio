import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useItemDirectoryStore } from '../../app/stores/item-directory'
import { useNpcDirectoryStore } from '../../app/stores/npc-directory'
import { resetVersionScopedState } from '../../app/stores/version-scoped-state'

describe('Version-scoped state', () => {
  beforeEach(() => setActivePinia(createPinia()))

  it('clears cached directory results and invalidates their active requests', () => {
    const items = useItemDirectoryStore()
    const npcs = useNpcDirectoryStore()
    items.items = [{ id: 57 } as never]
    items.total = 1
    items.error = 'Old version failed'
    npcs.items = [{ id: 100 } as never]
    npcs.total = 1

    resetVersionScopedState()

    expect(items.items).toEqual([])
    expect(items.total).toBe(0)
    expect(items.error).toBeUndefined()
    expect(npcs.items).toEqual([])
    expect(npcs.total).toBe(0)
  })
})
