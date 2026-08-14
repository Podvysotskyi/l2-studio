import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getPublishedManifestWithRaw } from '../../app/services/published-assets'

describe('published manifests', () => {
  const fetchMock = vi.fn()

  beforeEach(() => vi.stubGlobal('$fetch', fetchMock))
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('returns the stored JSON alongside its browser-resolved form', async () => {
    fetchMock.mockResolvedValue({
      manifestUrl: '/versions/c1/Maps/Aden/manifest.json',
      path: '/Maps/Aden'
    })

    const manifest = await getPublishedManifestWithRaw(
      '/versions/c1/Maps/Aden/manifest.json',
      'https://assets.example'
    )

    expect(fetchMock).toHaveBeenCalledWith(
      'https://assets.example/versions/c1/Maps/Aden/manifest.json'
    )
    expect(manifest.raw).toEqual({
      manifestUrl: '/versions/c1/Maps/Aden/manifest.json',
      path: '/Maps/Aden'
    })
    expect(manifest.resolved).toEqual({
      manifestUrl: 'https://assets.example/versions/c1/Maps/Aden/manifest.json',
      path: '/Maps/Aden'
    })
  })
})
