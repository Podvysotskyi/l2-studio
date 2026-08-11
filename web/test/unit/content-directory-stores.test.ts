import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getNpcDirectory,
  getSkillDirectory
} from '../../app/services/studio-api'
import { useNpcDirectoryStore } from '../../app/stores/npc-directory'
import { useSkillDirectoryStore } from '../../app/stores/skill-directory'

vi.mock('../../app/services/studio-api', () => ({
  getNpcDirectory: vi.fn(),
  getSkillDirectory: vi.fn()
}))

describe('Content directory stores', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getNpcDirectory).mockReset()
    vi.mocked(getSkillDirectory).mockReset()
  })

  it('loads NPCs using the current directory state', async () => {
    vi.mocked(getNpcDirectory).mockResolvedValue({
      items: [npc('Goblin')],
      total: 1,
      page: 2,
      pageSize: 50
    })
    const store = useNpcDirectoryStore()
    store.query = 'Goblin'
    store.page = 2
    store.pageSize = 50

    await store.load()

    expect(getNpcDirectory).toHaveBeenCalledWith({
      query: 'Goblin',
      page: 2,
      pageSize: 50
    })
    expect(store.items[0]?.name).toBe('Goblin')
    expect(store.total).toBe(1)
    expect(store.loading).toBe(false)
    expect(store.error).toBeUndefined()
  })

  it('keeps the latest NPC result when an earlier request resolves late', async () => {
    let resolveFirst: (value: { items: ReturnType<typeof npc>[]; total: number; page: number; pageSize: number }) => void = () => {}
    let resolveSecond: (value: { items: ReturnType<typeof npc>[]; total: number; page: number; pageSize: number }) => void = () => {}
    vi.mocked(getNpcDirectory)
      .mockReturnValueOnce(new Promise(resolve => { resolveFirst = resolve }))
      .mockReturnValueOnce(new Promise(resolve => { resolveSecond = resolve }))
    const store = useNpcDirectoryStore()

    const first = store.load()
    store.query = 'latest'
    const second = store.load()
    resolveSecond({ items: [npc('Latest')], total: 2, page: 1, pageSize: 25 })
    await second
    resolveFirst({ items: [npc('Stale')], total: 1, page: 1, pageSize: 25 })
    await first

    expect(store.items[0]?.name).toBe('Latest')
    expect(store.total).toBe(2)
  })

  it('reports a stable skill-directory failure', async () => {
    vi.mocked(getSkillDirectory).mockRejectedValue(new Error('Unavailable'))
    const store = useSkillDirectoryStore()

    await store.load()

    expect(store.error).toBe(
      'The skill directory could not be loaded from the Studio API.'
    )
    expect(store.loading).toBe(false)
  })
})

function npc(name: string) {
  return {
    id: 1,
    level: 10,
    name,
    npcTypeId: 29,
    npcType: 'Monster',
    npcRaceId: 17,
    npcRace: 'Humanoid',
    npcSexId: 0,
    npcSex: 'Male'
  }
}
