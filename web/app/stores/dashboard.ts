import { defineStore } from 'pinia'
import { ref } from 'vue'
import {
  getAssetCatalogs,
  getAssetImportJobs,
  getLookupDirectory,
  getNpcLookupDirectory,
  getNpcDirectory,
  getPlayerClasses,
  getSkillDirectory
} from '../services/studio-api'
import type {
  AssetCatalogSummary,
  AssetImportKind
} from '../types/models/asset-catalog'
import type { AssetImportJob } from '../types/models/asset-import-job'
import type { LookupKind } from '../types/models/content-directory'

export interface DashboardAssetSummary {
  kind: AssetImportKind
  label: string
  description: string
  icon: string
  to: string
  total: number
  resolved: number
  skipped: number
  groups: number | null
  groupLabel: string
  available: boolean
}

export const useDashboardStore = defineStore('dashboard', () => {
  const loading = ref(true)
  const contentError = ref(false)
  const assetError = ref(false)
  const jobsError = ref(false)
  const counts = ref(emptyContentCounts())
  const assets = ref<DashboardAssetSummary[]>(createEmptyAssetSummaries())
  const jobs = ref<AssetImportJob[]>([])

  async function load() {
    loading.value = true
    await Promise.all([loadContentSummary(), loadAssetSummary(), loadJobs()])
    loading.value = false
  }

  async function loadContentSummary() {
    try {
      const lookupCount = async (kind: LookupKind) =>
        (await getLookupDirectory(kind)).length
      const [
        npcs,
        skills,
        playerClasses,
        playerRaces,
        playerSexes,
        npcRaces,
        npcSexes,
        npcTypes,
        skillOperateTypes,
        skillTargetTypes
      ] = await Promise.all([
        getNpcDirectory({ page: 1, pageSize: 1 }),
        getSkillDirectory({ page: 1, pageSize: 1 }),
        getPlayerClasses(),
        lookupCount('player-races'),
        lookupCount('player-sexes'),
        getNpcLookupDirectory('npc-races').then(items => items.length),
        getNpcLookupDirectory('npc-sexes').then(items => items.length),
        getNpcLookupDirectory('npc-types').then(items => items.length),
        lookupCount('skill-operate-types'),
        lookupCount('skill-target-types')
      ])
      counts.value = {
        npcs: npcs.total,
        skills: skills.total,
        playerClasses: playerClasses.length,
        playerRaces,
        playerSexes,
        npcRaces,
        npcSexes,
        npcTypes,
        skillOperateTypes,
        skillTargetTypes
      }
      contentError.value = false
    } catch {
      contentError.value = true
    }
  }

  async function loadAssetSummary() {
    const summaries = createEmptyAssetSummaries()
    try {
      const records = await getAssetCatalogs()
      applyAssetCatalogs(summaries, records)
      assetError.value = false
    } catch {
      assetError.value = true
    }
    assets.value = summaries
  }

  async function loadJobs() {
    try {
      const kinds = [
        'textures',
        'music',
        'staticmeshes',
        'animations',
        'maps',
        'scenes'
      ] as const
      jobs.value = (
        await Promise.all(
          kinds.map((kind) => getAssetImportJobs(kind, 10))
        )
      )
        .flat()
        .sort(
          (left, right) =>
            new Date(right.requestedAt).getTime() -
            new Date(left.requestedAt).getTime()
        )
      jobsError.value = false
    } catch {
      jobsError.value = true
    }
  }

  return {
    loading,
    contentError,
    assetError,
    jobsError,
    counts,
    assets,
    jobs,
    load
  }
})

function emptyContentCounts() {
  return {
    npcs: 0,
    skills: 0,
    playerClasses: 0,
    playerRaces: 0,
    playerSexes: 0,
    npcRaces: 0,
    npcSexes: 0,
    npcTypes: 0,
    skillOperateTypes: 0,
    skillTargetTypes: 0
  }
}

function applyAssetCatalogs(
  summaries: DashboardAssetSummary[],
  records: AssetCatalogSummary[]
) {
  for (const record of records) {
    const index = summaries.findIndex((item) => item.kind === record.kind)
    if (index < 0) continue
    summaries[index] = {
      ...summaries[index]!,
      total: record.total,
      resolved: record.resolved,
      skipped: record.skipped,
      groups: record.groupCount || record.total,
      available: true
    }
  }
}

function createEmptyAssetSummaries(): DashboardAssetSummary[] {
  return [
    asset(
      'textures',
      'Textures',
      'System and world texture packages from the original client folders.',
      'i-lucide-images',
      '/library/textures',
      'packages'
    ),
    asset(
      'music',
      'Music',
      'Validated browser-playable Ogg Vorbis soundtrack assets.',
      'i-lucide-music-2',
      '/library/music',
      'tracks'
    ),
    asset(
      'staticmeshes',
      'Static meshes',
      'UE2 world geometry converted to interactive GLB previews.',
      'i-lucide-box',
      '/library/static-meshes',
      'packages'
    ),
    asset(
      'animations',
      'Animations',
      'C1 skeletal meshes and playable UE2 animation clips.',
      'i-lucide-person-standing',
      '/library/animations',
      'packages'
    ),
    asset(
      'maps',
      'Maps',
      'Coordinate-named geographic world tiles and placements.',
      'i-lucide-map',
      '/library/maps',
      'maps'
    ),
    asset(
      'scenes',
      'Scenes',
      'Client entry, lobby, sky, and support scene packages.',
      'i-lucide-clapperboard',
      '/library/scenes',
      'scenes'
    )
  ]
}

function asset(
  kind: AssetImportKind,
  label: string,
  description: string,
  icon: string,
  to: string,
  groupLabel: string
): DashboardAssetSummary {
  return {
    kind,
    label,
    description,
    icon,
    to,
    total: 0,
    resolved: 0,
    skipped: 0,
    groups: null,
    groupLabel,
    available: false
  }
}
