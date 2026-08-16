import type { NavigationMenuItem } from '@nuxt/ui'
import { describe, expect, it } from 'vitest'
import {
  studioNavigation,
  studioNavigationGroups,
  withStudioRouteGroup,
  studioRouteGroup,
  studioRouteTitle
} from '../../app/utils/studio-navigation'

describe('Studio navigation', () => {
  it('organizes authoring, delivery, pipeline, and storage destinations by workflow', () => {
    expect(groupLabels()).toEqual([
      'Players',
      'NPCs',
      'Skills',
      'Items',
      'Asset library',
      'Pipeline',
      'Storage'
    ])
    expect(group('Players').children?.map(item => item.label)).toEqual([
      'Classes',
      'Races',
      'Sexes',
      'Faces',
      'Hair styles',
      'Hair colors'
    ])
    expect(group('Items').children?.map(item => item.label)).toEqual([
      'Definitions',
      'Lookups'
    ])
    expect(child(group('Items'), 'Definitions').children?.map(item => item.label)).toEqual([
      'Armor', 'Weapons', 'Arrows', 'Materials', 'Potions', 'Recipes',
      'Enchants', 'Scrolls', 'Pet collars', 'Etc items'
    ])
    expect(child(group('Items'), 'Lookups').children?.map(item => item.label)).toEqual([
      'Types', 'Actions', 'Body parts', 'Materials', 'Crystal types', 'Handlers', 'Skill types'
    ])
    expect(group('Asset library').children?.map(item => item.label)).toEqual([
      'Textures',
      'Music',
      'Static meshes',
      'Animations',
      'Maps',
      'Scenes'
    ])
    expect(group('Pipeline').children?.map(item => item.label)).toEqual([
      'Import jobs',
      'Stale resources',
      'Releases'
    ])
    expect(group('Storage').children?.map(item => item.label)).toEqual([
      'Original resources',
      'Generated assets',
      'Artifact registry'
    ])
  })

  it('contains only canonical browser routes', () => {
    const routes = navigationRoutes(studioNavigation)

    expect(routes).toContain('/authoring/players/classes')
    expect(routes).toContain('/authoring/players/hair-styles')
    expect(routes).toContain('/authoring/npcs')
    expect(routes).toContain('/authoring/items/weapon')
    expect(routes).toContain('/authoring/items/armor')
    expect(routes).toContain('/authoring/items/pet-collar')
    expect(routes).toContain('/authoring/items/etc')
    expect(routes).toContain('/authoring/items/lookups/handlers')
    expect(routes).toContain('/authoring/items/lookups/skill-types')
    expect(routes).toContain('/authoring/skills/target-types')
    expect(routes).toContain('/library/static-meshes')
    expect(routes).toContain('/library/animations')
    expect(routes).toContain('/library/maps')
    expect(routes).toContain('/pipeline/stale-resources')
    expect(routes).toContain('/pipeline/releases')
    expect(routes).toContain('/storage/original-resources')
    expect(routes).toContain('/storage/generated-assets')
    expect(routes).toContain('/storage/artifact-registry')
    expect(routes).not.toContain('/content/npcs')
    expect(routes).not.toContain('/authoring/items/body-parts')
    expect(routes).not.toContain('/authoring/equipment/weapons')
    expect(routes).not.toContain('/assets/maps')
    expect(routes).not.toContain('/storage')
    expect(routes).not.toContain('/releases')
    expect(routes).not.toContain('/pipeline/storage')
    expect(routes).not.toContain('/pipeline/artifacts')
    expect(routes).not.toContain('/monitoring/stale-resources')
  })

  it('finds the group containing the current route', () => {
    expect(studioRouteGroup('/')).toBeUndefined()
    expect(studioRouteGroup('/authoring/players/classes'))
      .toBe(studioNavigationGroups.players)
    expect(studioRouteGroup('/authoring/npcs/races'))
      .toBe(studioNavigationGroups.npcs)
    expect(studioRouteGroup('/authoring/items/weapon/1'))
      .toBe(studioNavigationGroups.items)
    expect(studioRouteGroup('/authoring/items/lookups/materials'))
      .toBe(studioNavigationGroups.items)
    expect(studioRouteGroup('/authoring/skills'))
      .toBe(studioNavigationGroups.skills)
    expect(studioRouteGroup('/library/maps/17_25'))
      .toBe(studioNavigationGroups.library)
    expect(studioRouteGroup('/pipeline/imports'))
      .toBe(studioNavigationGroups.pipeline)
    expect(studioRouteGroup('/pipeline/stale-resources'))
      .toBe(studioNavigationGroups.pipeline)
    expect(studioRouteGroup('/storage/original-resources'))
      .toBe(studioNavigationGroups.storage)
    expect(studioRouteGroup('/assets/maps')).toBeUndefined()
  })

  it('adds the current route group without closing existing groups', () => {
    const authoringGroups = withStudioRouteGroup(
      [studioNavigationGroups.players],
      '/authoring/npcs/races'
    )

    expect(authoringGroups).toEqual([
      studioNavigationGroups.players,
      studioNavigationGroups.npcs
    ])
    expect(withStudioRouteGroup(authoringGroups, '/authoring/npcs'))
      .toEqual(authoringGroups)
    expect(withStudioRouteGroup(authoringGroups, '/')).toEqual(authoringGroups)
  })

  it('provides titles for canonical list and detail routes', () => {
    expect(studioRouteTitle('/authoring/npcs')).toBe('NPC definitions')
    expect(studioRouteTitle('/authoring/npcs/100')).toBe('NPC definition')
    expect(studioRouteTitle('/authoring/items')).toBe('Item definitions')
    expect(studioRouteTitle('/authoring/items/etc')).toBe('Etc Item definitions')
    expect(studioRouteTitle('/authoring/items/etc/1')).toBe('Etc Item definition')
    expect(studioRouteTitle('/authoring/items/etc/1/skills')).toBe('Etc Item skills')
    expect(studioRouteTitle('/authoring/items/weapon')).toBe('Weapon definitions')
    expect(studioRouteTitle('/authoring/items/weapon/1')).toBe('Weapon definition')
    expect(studioRouteTitle('/authoring/items/weapon/1/skills')).toBe('Weapon skills')
    expect(studioRouteTitle('/authoring/items/armor/1')).toBe('Armor definition')
    expect(studioRouteTitle('/authoring/items/pet-collar/1')).toBe('Pet Collar definition')
    expect(studioRouteTitle('/authoring/items/lookups/body-parts')).toBe('Item body parts')
    expect(studioRouteTitle('/authoring/items/lookups/crystal-types')).toBe('Item crystal types')
    expect(studioRouteTitle('/authoring/items/lookups/handlers')).toBe('Item handlers')
    expect(studioRouteTitle('/authoring/items/lookups/skill-types')).toBe('Item skill types')
    expect(studioRouteTitle('/authoring/skills/operate-types'))
      .toBe('Skill operate types')
    expect(studioRouteTitle('/authoring/players/hair-colors')).toBe('Player hair colors')
    expect(studioRouteTitle('/library/maps')).toBe('Maps')
    expect(studioRouteTitle('/library/maps/17_25')).toBe('Map')
    expect(studioRouteTitle('/library/scenes/lobby')).toBe('Client scene')
    expect(studioRouteTitle('/pipeline/releases')).toBe('Asset releases')
    expect(studioRouteTitle('/pipeline/stale-resources')).toBe('Stale resources')
    expect(studioRouteTitle('/storage/original-resources')).toBe('Original resources')
    expect(studioRouteTitle('/storage/generated-assets')).toBe('Generated assets')
    expect(studioRouteTitle('/storage/artifact-registry')).toBe('Generated-asset registry')
  })
})

function groupLabels() {
  return studioNavigation
    .filter(item => item.children?.length)
    .map(item => item.label)
}

function group(label: string) {
  const item = studioNavigation.find(candidate => candidate.label === label)
  if (!item) throw new Error(`Missing navigation group: ${label}`)
  return item
}

function child(item: NavigationMenuItem, label: string) {
  const child = item.children?.find(candidate => candidate.label === label)
  if (!child) throw new Error(`Missing ${label} child`)
  return child
}

function navigationRoutes(items: NavigationMenuItem[]): string[] {
  return items.flatMap((item) => [
    ...(typeof item.to === 'string' ? [item.to] : []),
    ...navigationRoutes(item.children ?? [])
  ])
}
