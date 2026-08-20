import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import {
  getAssetCatalogDiagnostics,
  getAssetImportDiagnostics,
  getAssetImportJob,
  getAssetArtifact,
  getAssetArtifacts,
  getAssetImportJobs,
  getImportJob,
  getImportJobs,
  getStaleAssetSources,
  getAssetImportWorkItems,
  getNpcDirectory,
  getNpcSpawnWorldMap,
  getItemDirectory,
  resolveItemIcons,
  getItemSet,
  getItemSetDirectory,
  getItemRecipeDirectory,
  getItemRecipeTypeDirectory,
  getNpcDefinition,
  getItemDefinition,
  getItemLookups,
  getNpcAppearanceManifest,
  getWorldMapOverview,
  getNpcLookupDirectory,
  getSkillLookupDirectory,
  getSkillDefinition,
  getStudioServiceInfo,
  startAssetFileImport,
  startAssetResourceImport,
  startAssetImport,
  startContentImport,
  updateNpcLookupDisplayName,
  updateSkillLookupDisplayName,
  updateNpcDefinition,
  updateItemDefinition,
  updateItemSet,
  setItemPrimarySkill,
  clearItemPrimarySkill,
  createItemSkill,
  updateItemSkill,
  deleteItemSkill,
  deleteItemCondition,
  updateItemCondition,
  rebuildStaleAssetSources,
  verifyAssetArtifact
} from '../../app/services/studio-api'

describe('Studio API service', () => {
  const fetchMock = vi.fn()

  beforeEach(() => vi.stubGlobal('$fetch', fetchMock))
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('loads service information through the Nuxt proxy', async () => {
    fetchMock.mockResolvedValue({})
    await getStudioServiceInfo()
    expect(fetchMock).toHaveBeenCalledWith('/api/system/info')
  })

  it('normalizes directory requests through the service boundary', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 2, pageSize: 50 })
    await getNpcDirectory({
      query: ' Goblin ',
      page: 2,
      pageSize: 50,
      npcTypeName: ' Monster ',
      npcRaceName: ' HUMANOID ',
      withoutRace: true,
      npcSexName: ' MALE ',
      hasVisuals: false
    })
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/content/npcs', {
      query: {
        query: 'Goblin',
        page: 2,
        pageSize: 50,
        npcTypeName: 'Monster',
        npcRaceName: 'HUMANOID',
        withoutRace: true,
        npcSexName: 'MALE',
        hasVisuals: false
      }
    })
  })

  it('loads and updates an NPC definition through the content API', async () => {
    fetchMock.mockResolvedValue({})
    await getNpcDefinition(100)
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/content/npcs/100')

    const request = {
      name: 'Goblin', level: 10, npcTypeName: 'Monster', npcRaceName: null, npcSexName: 'MALE'
    }
    await updateNpcDefinition(100, request)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/npcs/100', {
      method: 'PATCH', body: request
    })
  })

  it('loads NPC spawn data and the terrain overview through version-scoped APIs', async () => {
    fetchMock.mockResolvedValue({})

    await getNpcSpawnWorldMap()
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/npc-spawns/world-map')

    await getWorldMapOverview()
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/world-overview')
  })

  it('loads and updates structured item attack geometry through the content API', async () => {
    fetchMock.mockResolvedValue({})
    await getItemDefinition('weapon', 3028)
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/content/items/weapon/3028')

    const request = {
      name: 'Crescent Moon Bow', attackGeometry: { offsetX: 0, offsetY: 0, radius: 10, length: 0 }
    }
    await updateItemDefinition('weapon', 3028, request)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/weapon/3028', {
      method: 'PATCH', body: request
    })
  })

  it('loads a skill definition through the content API', async () => {
    fetchMock.mockResolvedValue({})
    await getSkillDefinition(3006)
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/content/skills/3006')
  })

  it('filters item definitions and loads item-handler lookups through the content API', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })

    await getItemDirectory('weapon', { handlerName: ' ItemSkills ' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/weapon', {
      query: { page: 1, pageSize: 25, handlerName: 'ItemSkills' }
    })

    await getItemLookups('item-skill-types', { query: ' critical ' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-skill-types', {
      query: { query: 'critical', page: 1, pageSize: 25 }
    })
  })

  it('resolves item icon artwork through the content API', async () => {
    vi.stubGlobal('useRuntimeConfig', () => ({ public: { assetBaseUrl: 'https://assets.example' } }))
    fetchMock.mockResolvedValue([{ itemId: 12, url: '/versions/c1/textures/icon.png' }])

    const icons = await resolveItemIcons([{ itemId: 12, icon: 'icon.weapon_sword_i00', itemBodyPartName: null }])

    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-icons/resolve', {
      method: 'POST', body: { items: [{ itemId: 12, icon: 'icon.weapon_sword_i00', itemBodyPartName: null }] }
    })
    expect(icons).toEqual([{ itemId: 12, url: 'https://assets.example/versions/c1/textures/icon.png' }])
  })

  it('manages primary and attached item skills through the content API', async () => {
    fetchMock.mockResolvedValue({})

    await setItemPrimarySkill('etc', 3028, { skillId: 3005, skillLevel: 1 })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/etc/3028/primary-skill', {
      method: 'PUT', body: { skillId: 3005, skillLevel: 1 }
    })

    await createItemSkill('weapon', 3028, { skillId: 3005, skillLevel: 1, itemSkillTypeName: 'ON_CRITICAL_SKILL', chance: 50 })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/weapon/3028/skills', {
      method: 'POST', body: { skillId: 3005, skillLevel: 1, itemSkillTypeName: 'ON_CRITICAL_SKILL', chance: 50 }
    })

    await updateItemSkill('weapon', 3028, 3005, 1, { itemSkillTypeName: null, chance: null })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/weapon/3028/skills/3005/1', {
      method: 'PATCH', body: { itemSkillTypeName: null, chance: null }
    })

    await deleteItemSkill('weapon', 3028, 3005, 1)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/weapon/3028/skills/3005/1', {
      method: 'DELETE'
    })

    await clearItemPrimarySkill('etc', 3028)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/etc/3028/primary-skill', {
      method: 'DELETE'
    })
  })

  it('manages item conditions through the content API', async () => {
    fetchMock.mockResolvedValue({})
    const request = {
      messageId: 1518, addName: false, isPvpFlagged: null,
      playerRaces: ['HUMAN'], playerCategoryTypes: ['WOLF']
    }
    await updateItemCondition('etc', 57, request)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/etc/57/condition', {
      method: 'PUT', body: request
    })
    await deleteItemCondition('etc', 57)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/items/etc/57/condition', { method: 'DELETE' })
  })

  it('loads and updates item sets through the content API', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })
    await getItemSetDirectory({ query: '  mithril  ' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-sets', {
      query: { query: 'mithril', page: 1, pageSize: 25 }
    })

    await getItemSet(1)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-sets/1')

    const request = { skillId: 3006, skillLevel: 1, str: 1, dex: null, con: null, int: null, wit: null, men: null }
    await updateItemSet(1, request)
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-sets/1', {
      method: 'PATCH', body: request
    })
  })

  it('loads crafting recipes and recipe types through the content API', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })

    await getItemRecipeDirectory({ query: '  mithril  ', page: 2, pageSize: 50 })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-recipes', {
      query: { query: 'mithril', page: 2, pageSize: 50 }
    })

    await getItemRecipeTypeDirectory({ query: ' dwarven ' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/item-recipe-types', {
      query: { query: 'dwarven', page: 1, pageSize: 25 }
    })
  })

  it('loads and updates name-keyed skill lookups', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })

    await getSkillLookupDirectory('skill-target-types', { query: 'area', pageSize: 25 })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/skill-target-types',
      { query: { query: 'area', page: 1, pageSize: 25 } }
    )

    await updateSkillLookupDisplayName('skill-target-types', 'AREA_CORPSE_MOB', 'Area Corpse Mob')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/skill-target-types/AREA_CORPSE_MOB',
      { method: 'PATCH', body: { displayName: 'Area Corpse Mob' } }
    )
  })

  it('loads one NPC appearance manifest reference through the asset API', async () => {
    fetchMock.mockResolvedValue({ manifestUrl: '/versions/c1/npc/manifest.json' })

    await getNpcAppearanceManifest(100)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/game-versions/c1/assets/npcappearances/npcs/100/manifest'
    )
  })

  it('loads and starts import jobs through same-origin URLs', async () => {
    fetchMock.mockResolvedValue([])
    await getAssetImportJobs('textures', 100)
    expect(fetchMock).toHaveBeenCalledWith('/api/game-versions/c1/assets/textures/imports', {
      query: { limit: 100 }
    })

    await startAssetImport('textures')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/textures/imports', {
      method: 'POST',
      body: {}
    })

    await startAssetImport('npcappearances')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/npcappearances/imports', {
      method: 'POST',
      body: {}
    })

    await getAssetImportJob('mappreviews', 'preview run')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/mappreviews/imports/preview%20run'
    )
  })

  it('loads and starts universal import jobs', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })

    await getImportJobs({ category: 'content', target: 'item-materials', status: 'running' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/imports', {
      query: {
        category: 'content', target: 'item-materials', status: 'running', page: 1, pageSize: 25
      }
    })

    await getImportJob('run id')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/imports/run%20id')

    await startContentImport('item-materials', 'restore_defaults')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/imports/content/item-materials', {
      method: 'POST', body: { mode: 'restore_defaults' }
    })
  })

  it('loads per-file progress and filtered diagnostics', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })
    await getAssetImportWorkItems('maps', 'run-id', {
      sourceKey: '17_25.unr',
      status: 'failed',
      query: 'terrain',
      diagnosticSeverity: 'error',
      pageSize: 25
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/work-items',
      {
        query: {
          sourceKey: '17_25.unr',
          status: 'failed',
          query: 'terrain',
          diagnosticSeverity: 'error',
          page: 1,
          pageSize: 25
        }
      }
    )

    await getAssetImportDiagnostics('maps', 'run-id', {
      severity: 'error',
      code: 'conversion.failed',
      query: 'terrain',
      scope: 'run',
      page: 2
    })
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/imports/run-id/diagnostics',
      {
        query: {
          severity: 'error',
          code: 'conversion.failed',
          query: 'terrain',
          scope: 'run',
          page: 2,
          pageSize: 50
        }
      }
    )
  })

  it('loads diagnostics for the exact published catalog item', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 2, pageSize: 25 })

    await getAssetCatalogDiagnostics('maps', '16 25', {
      sourceKey: 'Maps/16_25.unr',
      severity: 'warning',
      query: ' BSP ',
      page: 2
    })

    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/maps/catalog/16%2025/diagnostics',
      {
        query: {
          sourceKey: 'Maps/16_25.unr',
          severity: 'warning',
          query: 'BSP',
          page: 2,
          pageSize: 25
        }
      }
    )
  })

  it('preserves single-file route separators while encoding each path segment', async () => {
    fetchMock.mockResolvedValue({})
    await startAssetFileImport('textures', 'systextures/Lineage Effects.utx')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/files/systextures/Lineage%20Effects.utx',
      { method: 'POST', query: { force: false } }
    )

    await startAssetFileImport('scenes', 'Maps/Entry.unr', true)
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/scenes/imports/files/Maps/Entry.unr',
      { method: 'POST', query: { force: true } }
    )

    await startAssetFileImport('mappreviews', 'Maps/Entry.unr', true)
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/mappreviews/imports/files/Maps/Entry.unr',
      { method: 'POST', query: { force: true } }
    )
  })

  it('starts resource re-imports through the import API', async () => {
    fetchMock.mockResolvedValue({})
    await startAssetResourceImport('textures', 'Texture', 'Package')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/resources',
      { method: 'POST', body: { resourceName: 'Texture', packageName: 'Package', force: false } }
    )

    await startAssetResourceImport('textures', 'Texture', 'Package', 'Textures/Package.utx')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/textures/imports/resources',
      {
        method: 'POST',
        body: {
          resourceName: 'Texture',
          packageName: 'Package',
          sourceKey: 'Textures/Package.utx',
          force: false
        }
      }
    )
  })

  it('supports forced and stale rebuild controls', async () => {
    fetchMock.mockResolvedValue([])
    await startAssetImport('maps', { force: true })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports', {
      method: 'POST',
      body: { force: true }
    })

    await getStaleAssetSources('maps')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports/stale')
    await rebuildStaleAssetSources('maps')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/maps/imports/stale', {
      method: 'POST'
    })
  })

  it('loads and verifies generated artifacts through version-scoped APIs', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 50 })
    await getAssetArtifacts({ kind: 'maps', current: true, integrityStatus: 'healthy' })
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/assets/artifacts', {
      query: {
        kind: 'maps',
        current: true,
        integrityStatus: 'healthy',
        page: 1,
        pageSize: 50
      }
    })

    await getAssetArtifact('artifact id')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/artifacts/artifact%20id'
    )
    await verifyAssetArtifact('artifact id')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/assets/artifacts/artifact%20id/verify',
      { method: 'POST' }
    )
  })

  it('reads and edits NPC lookups through version-scoped APIs', async () => {
    fetchMock.mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 })
    await getNpcLookupDirectory('npc-types')
    expect(fetchMock).toHaveBeenLastCalledWith('/api/game-versions/c1/content/npc-types', {
      query: { page: 1, pageSize: 25 }
    })

    await updateNpcLookupDisplayName('npc-races', 'DARK_ELF', 'Dark Elf')
    expect(fetchMock).toHaveBeenLastCalledWith(
      '/api/game-versions/c1/content/npc-races/DARK_ELF',
      { method: 'PATCH', body: { displayName: 'Dark Elf' } }
    )

  })
})
