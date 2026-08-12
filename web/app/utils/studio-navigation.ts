import type { NavigationMenuItem } from '@nuxt/ui'

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
    label: 'Content',
    type: 'label'
  },
  {
    label: 'Players',
    icon: 'i-lucide-user-round',
    defaultOpen: true,
    children: [
      {
        label: 'Classes',
        icon: 'i-lucide-git-branch',
        to: '/content/player-classes'
      },
      { label: 'Races', icon: 'i-lucide-orbit', to: '/content/player-races' },
      { label: 'Sexes', icon: 'i-lucide-tags', to: '/content/player-sexes' }
    ]
  },
  {
    label: 'NPCs',
    icon: 'i-lucide-users-round',
    defaultOpen: true,
    children: [
      { label: 'Definitions', icon: 'i-lucide-list', to: '/content/npcs' },
      { label: 'Races', icon: 'i-lucide-orbit', to: '/content/races' },
      { label: 'Sexes', icon: 'i-lucide-tags', to: '/content/sexes' },
      { label: 'Types', icon: 'i-lucide-workflow', to: '/content/types' }
    ]
  },
  {
    label: 'Skills',
    icon: 'i-lucide-sparkles',
    defaultOpen: true,
    children: [
      { label: 'Definitions', icon: 'i-lucide-list', to: '/content/skills' },
      {
        label: 'Operate types',
        icon: 'i-lucide-play',
        to: '/content/skill-operate-types'
      },
      {
        label: 'Target types',
        icon: 'i-lucide-crosshair',
        to: '/content/skill-target-types'
      }
    ]
  },
  {
    label: 'Assets',
    type: 'label'
  },
  {
    label: 'Asset library',
    icon: 'i-lucide-package-open',
    defaultOpen: true,
    children: [
      {
        label: 'Textures',
        icon: 'i-lucide-images',
        to: '/assets/textures'
      },
      {
        label: 'Music',
        icon: 'i-lucide-music-2',
        to: '/assets/music'
      },
      {
        label: 'Static meshes',
        icon: 'i-lucide-box',
        to: '/assets/staticmeshes'
      },
      {
        label: 'Maps',
        icon: 'i-lucide-map',
        to: '/assets/maps'
      },
      {
        label: 'Scenes',
        icon: 'i-lucide-clapperboard',
        to: '/assets/scenes'
      }
    ]
  },
  {
    label: 'Operations',
    type: 'label'
  },
  {
    label: 'File storage',
    icon: 'i-lucide-hard-drive',
    to: '/storage'
  },
  {
    label: 'Artifact registry',
    icon: 'i-lucide-library-big',
    to: '/assets/artifacts'
  },
  {
    label: 'Releases',
    icon: 'i-lucide-rocket',
    to: '/releases'
  },
  {
    label: 'Background jobs',
    icon: 'i-lucide-activity',
    defaultOpen: true,
    children: [
      {
        label: 'Import jobs',
        icon: 'i-lucide-history',
        to: '/assets/jobs'
      }
    ]
  }
]

export function studioRouteTitle(path: string) {
  if (path === '/') return 'Dashboard'
  if (path === '/content/npcs') return 'NPC definitions'
  if (path === '/content/races') return 'NPC races'
  if (path === '/content/sexes') return 'NPC sexes'
  if (path === '/content/types') return 'NPC types'
  if (path === '/content/player-classes') return 'Player classes'
  if (path === '/content/player-races') return 'Player races'
  if (path === '/content/player-sexes') return 'Player sexes'
  if (path === '/content/skills') return 'Skill definitions'
  if (path === '/content/skill-operate-types') return 'Skill operate types'
  if (path === '/content/skill-target-types') return 'Skill target types'
  if (path === '/assets/imports' || path === '/assets/systextures' || path === '/assets/textures')
    return 'Textures'
  if (path === '/assets/music') return 'Music assets'
  if (path === '/assets/staticmeshes') return 'Static meshes'
  if (path === '/assets/maps') return 'Maps'
  if (path.startsWith('/assets/maps/')) return 'Map'
  if (path === '/assets/scenes') return 'Scenes'
  if (path.startsWith('/assets/scenes/')) return 'Client scene'
  if (path === '/assets/jobs') return 'Asset import jobs'
  if (path === '/assets/artifacts') return 'Generated-asset registry'
  if (path === '/releases') return 'Asset releases'
  if (path === '/storage') return 'File storage'
  return 'Studio'
}
