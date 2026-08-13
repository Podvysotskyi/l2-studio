import { describe, expect, it } from 'vitest'
import {
  publishedAssetUrl,
  resolvePublishedAssetUrls
} from '../../app/utils/published-asset-url'

describe('published asset URLs', () => {
  it('uses the configured Nginx origin for generated assets', () => {
    expect(publishedAssetUrl('/versions/c1/Textures/a.webp', 'http://localhost:5300/'))
      .toBe('http://localhost:5300/versions/c1/Textures/a.webp')
  })

  it('resolves nested manifest URLs without altering display paths', () => {
    expect(resolvePublishedAssetUrls({
      manifestUrl: '/versions/c1/Maps/a/manifest.json',
      path: '/Textures/Package/Object',
      terrain: { heightmap: '/versions/c1/Maps/a/height.webp' },
      frameUrls: ['/versions/c1/Textures/a/0.webp']
    }, 'https://assets.example')).toEqual({
      manifestUrl: 'https://assets.example/versions/c1/Maps/a/manifest.json',
      path: '/Textures/Package/Object',
      terrain: { heightmap: 'https://assets.example/versions/c1/Maps/a/height.webp' },
      frameUrls: ['https://assets.example/versions/c1/Textures/a/0.webp']
    })
  })
})
