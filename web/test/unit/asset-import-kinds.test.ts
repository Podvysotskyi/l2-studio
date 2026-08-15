import { describe, expect, it } from 'vitest'
import {
  assetImportKindLabel,
  assetImportKindOptions,
  assetImportKinds
} from '../../app/utils/asset-import-kinds'

describe('Asset import kinds', () => {
  it('provides every supported collection with its display label', () => {
    expect(assetImportKinds).toEqual([
      'textures',
      'music',
      'sounds',
      'staticmeshes',
      'animations',
      'npcappearances',
      'maps',
      'mappreviews',
      'scenes'
    ])
    expect(assetImportKindOptions).toEqual([
      { label: 'All collections', value: 'all' },
      ...assetImportKinds.map((kind) => ({
        label: assetImportKindLabel(kind),
        value: kind
      }))
    ])
  })
})
