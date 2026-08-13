import type { AssetImportKind } from '~/types/studio'

export const assetImportKinds: AssetImportKind[] = [
  'textures',
  'music',
  'sounds',
  'staticmeshes',
  'maps',
  'mappreviews',
  'scenes'
]

export const assetImportKindOptions = [
  { label: 'All collections', value: 'all' },
  { label: 'Textures', value: 'textures' },
  { label: 'Music', value: 'music' },
  { label: 'Sounds', value: 'sounds' },
  { label: 'Static meshes', value: 'staticmeshes' },
  { label: 'Maps', value: 'maps' },
  { label: 'Map previews', value: 'mappreviews' },
  { label: 'Scenes', value: 'scenes' }
]

export function assetImportKindLabel(kind: AssetImportKind) {
  if (kind === 'textures') return 'Textures'
  if (kind === 'music') return 'Music'
  if (kind === 'sounds') return 'Sounds'
  if (kind === 'staticmeshes') return 'Static meshes'
  if (kind === 'maps') return 'Maps'
  return kind === 'mappreviews' ? 'Map previews' : 'Scenes'
}
