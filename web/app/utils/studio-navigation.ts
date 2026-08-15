import type { NavigationMenuItem } from '@nuxt/ui'

export const studioNavigationGroups = {
  players: 'players',
  npcs: 'npcs',
  items: 'items',
  skills: 'skills',
  library: 'library',
  pipeline: 'pipeline',
  storage: 'storage'
} as const

export type StudioNavigationGroup =
  typeof studioNavigationGroups[keyof typeof studioNavigationGroups]

export const studioNavigation: NavigationMenuItem[] = [
  {
    label: 'Overview',
    type: 'label'
  },
  {
    label: 'Dashboard',
    icon: 'i-lucide-layout-dashboard',
    to: '/',
    exact: true
  },
  {
    label: 'Authoring',
    type: 'label'
  },
  {
    label: 'Players',
    value: studioNavigationGroups.players,
    icon: 'i-lucide-user-round',
    children: [
      {
        label: 'Classes',
        icon: 'i-lucide-git-branch',
        to: '/authoring/players/classes'
      },
      {
        label: 'Races',
        icon: 'i-lucide-orbit',
        to: '/authoring/players/races'
      },
      {
        label: 'Sexes',
        icon: 'i-lucide-tags',
        to: '/authoring/players/sexes'
      },
      {
        label: 'Faces',
        icon: 'i-lucide-smile',
        to: '/authoring/players/faces'
      },
      {
        label: 'Hair styles',
        icon: 'i-lucide-scan-face',
        to: '/authoring/players/hair-styles'
      },
      {
        label: 'Hair colors',
        icon: 'i-lucide-palette',
        to: '/authoring/players/hair-colors'
      }
    ]
  },
  {
    label: 'NPCs',
    value: studioNavigationGroups.npcs,
    icon: 'i-lucide-users-round',
    children: [
      {
        label: 'Definitions',
        icon: 'i-lucide-list',
        to: '/authoring/npcs'
      },
      {
        label: 'Races',
        icon: 'i-lucide-orbit',
        to: '/authoring/npcs/races'
      },
      {
        label: 'Sexes',
        icon: 'i-lucide-tags',
        to: '/authoring/npcs/sexes'
      },
      {
        label: 'Types',
        icon: 'i-lucide-workflow',
        to: '/authoring/npcs/types'
      }
    ]
  },
  {
    label: 'Skills',
    value: studioNavigationGroups.skills,
    icon: 'i-lucide-sparkles',
    children: [
      {
        label: 'Definitions',
        icon: 'i-lucide-list',
        to: '/authoring/skills'
      },
      {
        label: 'Operate types',
        icon: 'i-lucide-play',
        to: '/authoring/skills/operate-types'
      },
      {
        label: 'Target types',
        icon: 'i-lucide-crosshair',
        to: '/authoring/skills/target-types'
      }
    ]
  },
  {
    label: 'Items',
    value: studioNavigationGroups.items,
    icon: 'i-lucide-swords',
    children: [
      { label: 'Definitions', icon: 'i-lucide-list', to: '/authoring/items' },
      { label: 'Types', icon: 'i-lucide-workflow', to: '/authoring/items/types' },
      { label: 'Actions', icon: 'i-lucide-play', to: '/authoring/items/actions' },
      { label: 'Body parts', icon: 'i-lucide-shirt', to: '/authoring/items/body-parts' },
      { label: 'Materials', icon: 'i-lucide-gem', to: '/authoring/items/materials' },
      { label: 'Crystal types', icon: 'i-lucide-sparkles', to: '/authoring/items/crystal-types' }
    ]
  },
  {
    label: 'Delivery',
    type: 'label'
  },
  {
    label: 'Asset library',
    value: studioNavigationGroups.library,
    icon: 'i-lucide-library',
    children: [
      {
        label: 'Textures',
        icon: 'i-lucide-images',
        to: '/library/textures'
      },
      {
        label: 'Music',
        icon: 'i-lucide-music-2',
        to: '/library/music'
      },
      {
        label: 'Static meshes',
        icon: 'i-lucide-box',
        to: '/library/static-meshes'
      },
      {
        label: 'Animations',
        icon: 'i-lucide-person-standing',
        to: '/library/animations'
      },
      {
        label: 'Maps',
        icon: 'i-lucide-map',
        to: '/library/maps'
      },
      {
        label: 'Scenes',
        icon: 'i-lucide-clapperboard',
        to: '/library/scenes'
      }
    ]
  },
  {
    label: 'Pipeline',
    value: studioNavigationGroups.pipeline,
    icon: 'i-lucide-workflow',
    children: [
      {
        label: 'Import jobs',
        icon: 'i-lucide-history',
        to: '/pipeline/imports'
      },
      {
        label: 'Stale resources',
        icon: 'i-lucide-triangle-alert',
        to: '/pipeline/stale-resources'
      },
      {
        label: 'Releases',
        icon: 'i-lucide-rocket',
        to: '/pipeline/releases'
      }
    ]
  },
  {
    label: 'Storage',
    value: studioNavigationGroups.storage,
    icon: 'i-lucide-hard-drive',
    children: [
      {
        label: 'Original resources',
        icon: 'i-lucide-archive',
        to: '/storage/original-resources'
      },
      {
        label: 'Generated assets',
        icon: 'i-lucide-package-open',
        to: '/storage/generated-assets'
      },
      {
        label: 'Artifact registry',
        icon: 'i-lucide-library-big',
        to: '/storage/artifact-registry'
      }
    ]
  }
]

export function studioRouteGroup(path: string): StudioNavigationGroup | undefined {
  if (path.startsWith('/authoring/players/')) return studioNavigationGroups.players
  if (path === '/authoring/npcs' || path.startsWith('/authoring/npcs/'))
    return studioNavigationGroups.npcs
  if (path === '/authoring/items' || path.startsWith('/authoring/items/')) return studioNavigationGroups.items
  if (path === '/authoring/skills' || path.startsWith('/authoring/skills/'))
    return studioNavigationGroups.skills
  if (path.startsWith('/library/')) return studioNavigationGroups.library
  if (path.startsWith('/pipeline/')) return studioNavigationGroups.pipeline
  if (path.startsWith('/storage/')) return studioNavigationGroups.storage
  return undefined
}

export function withStudioRouteGroup(
  expandedGroups: StudioNavigationGroup[],
  path: string
): StudioNavigationGroup[] {
  const routeGroup = studioRouteGroup(path)

  if (!routeGroup || expandedGroups.includes(routeGroup)) return expandedGroups

  return [...expandedGroups, routeGroup]
}

export function studioRouteTitle(path: string) {
  if (path === '/') return 'Dashboard'
  if (path === '/authoring/npcs') return 'NPC definitions'
  if (/^\/authoring\/npcs\/\d+$/.test(path)) return 'NPC definition'
  if (path === '/authoring/npcs/races') return 'NPC races'
  if (path === '/authoring/npcs/sexes') return 'NPC sexes'
  if (path === '/authoring/npcs/types') return 'NPC types'
  if (path === '/authoring/items') return 'Item definitions'
  if (/^\/authoring\/items\/\d+$/.test(path)) return 'Item definition'
  if (path === '/authoring/items/types') return 'Item types'
  if (path === '/authoring/items/actions') return 'Item actions'
  if (path === '/authoring/items/body-parts') return 'Item body parts'
  if (path === '/authoring/items/materials') return 'Item materials'
  if (path === '/authoring/items/crystal-types') return 'Item crystal types'
  if (path === '/authoring/players/classes') return 'Player classes'
  if (path === '/authoring/players/races') return 'Player races'
  if (path === '/authoring/players/sexes') return 'Player sexes'
  if (path === '/authoring/players/faces') return 'Player faces'
  if (path === '/authoring/players/hair-styles') return 'Player hair styles'
  if (path === '/authoring/players/hair-colors') return 'Player hair colors'
  if (path === '/authoring/skills') return 'Skill definitions'
  if (path === '/authoring/skills/operate-types') return 'Skill operate types'
  if (path === '/authoring/skills/target-types') return 'Skill target types'
  if (path === '/library/textures') return 'Textures'
  if (path === '/library/music') return 'Music assets'
  if (path === '/library/static-meshes') return 'Static meshes'
  if (path === '/library/animations') return 'Animations'
  if (path === '/library/maps') return 'Maps'
  if (path.startsWith('/library/maps/')) return 'Map'
  if (path === '/library/scenes') return 'Scenes'
  if (path.startsWith('/library/scenes/')) return 'Client scene'
  if (path === '/pipeline/imports') return 'Asset import jobs'
  if (path === '/pipeline/stale-resources') return 'Stale resources'
  if (path === '/pipeline/releases') return 'Asset releases'
  if (path === '/storage/original-resources') return 'Original resources'
  if (path === '/storage/generated-assets') return 'Generated assets'
  if (path === '/storage/artifact-registry') return 'Generated-asset registry'
  return 'Studio'
}
