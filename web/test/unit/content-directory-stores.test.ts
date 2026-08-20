import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getNpcDirectory,
  getItemDirectory,
  resolveItemIcons,
  getSkillDirectory,
  getItemRecipeDirectory,
  getItemRecipeTypeDirectory
} from '../../app/services/studio-api'
import { useItemDirectoryStore } from '../../app/stores/item-directory'
import { useNpcDirectoryStore } from '../../app/stores/npc-directory'
import { useSkillDirectoryStore } from '../../app/stores/skill-directory'
import { useItemRecipeDirectoryStore } from '../../app/stores/item-recipe-directory'
import { useItemRecipeTypeDirectoryStore } from '../../app/stores/item-recipe-type-directory'

vi.mock('../../app/services/studio-api', () => ({
  getNpcDirectory: vi.fn(),
  getItemDirectory: vi.fn(),
  resolveItemIcons: vi.fn(),
  getSkillDirectory: vi.fn(),
  getItemRecipeDirectory: vi.fn(),
  getItemRecipeTypeDirectory: vi.fn()
}))

describe('Content directory stores', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    vi.mocked(getNpcDirectory).mockReset()
    vi.mocked(getItemDirectory).mockReset()
    vi.mocked(resolveItemIcons).mockReset()
    vi.mocked(getSkillDirectory).mockReset()
    vi.mocked(getItemRecipeDirectory).mockReset()
    vi.mocked(getItemRecipeTypeDirectory).mockReset()
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
    store.npcTypeName = 'Monster'
    store.npcRaceName = 'HUMANOID'
    store.npcSexName = 'MALE'
    store.visualFilter = 'without'

    await store.load()

    expect(getNpcDirectory).toHaveBeenCalledWith({
      query: 'Goblin',
      page: 2,
      pageSize: 50,
      npcTypeName: 'Monster',
      npcRaceName: 'HUMANOID',
      withoutRace: undefined,
      npcSexName: 'MALE',
      hasVisuals: false
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

  it('loads item definitions using their family', async () => {
    vi.mocked(getItemDirectory).mockResolvedValue({
      items: [], total: 0, page: 1, pageSize: 25
    })
    const store = useItemDirectoryStore()
    store.family = 'armor'

    await store.load()

    expect(getItemDirectory).toHaveBeenCalledWith('armor', expect.any(Object))
  })

  it('resolves current item-page icons without failing the directory when artwork is unavailable', async () => {
    vi.mocked(getItemDirectory).mockResolvedValue({
      items: [{ ...item('Sword'), icon: 'icon.weapon_sword_i00' }], total: 1, page: 1, pageSize: 25
    })
    vi.mocked(resolveItemIcons).mockRejectedValue(new Error('Texture catalog unavailable'))
    const store = useItemDirectoryStore()

    await store.load()

    expect(resolveItemIcons).toHaveBeenCalledWith([{ itemId: 1, icon: 'icon.weapon_sword_i00', itemBodyPartName: null }])
    expect(store.items[0]?.name).toBe('Sword')
    expect(store.iconUrls).toEqual({})
    expect(store.error).toBeUndefined()
  })

  it('maps resolved item artwork by item ID when rows share an icon identifier', async () => {
    vi.mocked(getItemDirectory).mockResolvedValue({
      items: [
        { ...item('Hard Leather Shirt'), id: 27, icon: 'icon.armor_hard_leather_shirt_i00', itemBodyPartName: 'chest' },
        { ...item('Hard Leather Pants'), id: 28, icon: 'icon.armor_hard_leather_shirt_i00', itemBodyPartName: 'legs' }
      ], total: 2, page: 1, pageSize: 25
    })
    vi.mocked(resolveItemIcons).mockResolvedValue([
      { itemId: 27, url: 'https://assets.test/icons/chest.webp' },
      { itemId: 28, url: 'https://assets.test/icons/legs.webp' }
    ])
    const store = useItemDirectoryStore()

    await store.load()

    expect(resolveItemIcons).toHaveBeenCalledWith([
      { itemId: 27, icon: 'icon.armor_hard_leather_shirt_i00', itemBodyPartName: 'chest' },
      { itemId: 28, icon: 'icon.armor_hard_leather_shirt_i00', itemBodyPartName: 'legs' }
    ])
    expect(store.iconUrls).toEqual({
      27: 'https://assets.test/icons/chest.webp',
      28: 'https://assets.test/icons/legs.webp'
    })
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

  it('loads recipe catalogs using their independent directory state', async () => {
    vi.mocked(getItemRecipeDirectory).mockResolvedValue({
      items: [{ id: 1, name: 'Craft Dagger', itemRecipeTypeName: 'dwarven', craftLevel: 1, successRate: 100, statUse: null, ingredients: [], productions: [] }],
      total: 1, page: 2, pageSize: 50
    })
    vi.mocked(getItemRecipeTypeDirectory).mockResolvedValue({
      items: [{ name: 'dwarven', recipeCount: 1 }], total: 1, page: 1, pageSize: 25
    })
    const recipes = useItemRecipeDirectoryStore()
    const types = useItemRecipeTypeDirectoryStore()
    recipes.query = 'Dagger'
    recipes.page = 2
    recipes.pageSize = 50

    await Promise.all([recipes.load(), types.load()])

    expect(getItemRecipeDirectory).toHaveBeenCalledWith({ query: 'Dagger', page: 2, pageSize: 50 })
    expect(getItemRecipeTypeDirectory).toHaveBeenCalledWith({ query: undefined, page: 1, pageSize: 25 })
    expect(recipes.items[0]?.name).toBe('Craft Dagger')
    expect(types.items[0]?.recipeCount).toBe(1)
  })
})

function npc(name: string) {
  return {
    id: 1,
    appearanceId: 1,
    level: 10,
    name,
    npcTypeName: 'Monster',
    npcTypeDisplayName: 'Monster',
    npcRaceName: 'HUMANOID',
    npcRaceDisplayName: 'Humanoid',
    npcSexName: 'MALE',
    npcSexDisplayName: 'Male',
    hasVisuals: true,
    status: null
  }
}

function item(name: string) {
  return {
    id: 1,
    name,
    itemTypeName: 'Weapon',
    itemTypeDisplayName: 'Weapon',
    itemParentTypeName: null,
    itemParentTypeDisplayName: null,
    itemActionName: null,
    itemActionDisplayName: null,
    itemBodyPartName: null,
    itemBodyPartDisplayName: null,
    itemMaterialName: null,
    itemMaterialDisplayName: null,
    itemCrystalTypeName: null,
    itemCrystalTypeDisplayName: null,
    icon: null,
    weight: null,
    price: null,
    handlerName: null,
    handlerDisplayName: null,
    skills: [],
    attackGeometry: null,
    stats: null
  }
}
