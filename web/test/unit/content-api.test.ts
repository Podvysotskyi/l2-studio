import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createContentApi } from '../../app/services/content-api'
import { createStudioVersionClient } from '../../app/services/studio-version-client'

describe('content API client', () => {
  const fetchMock = vi.fn()

  beforeEach(() => vi.stubGlobal('$fetch', fetchMock))
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('uses the explicitly supplied game version and named item-condition body', async () => {
    fetchMock.mockResolvedValue({})
    const api = createContentApi(createStudioVersionClient('interlude'))
    const request = {
      messageId: 1518,
      addName: false,
      isPvpFlagged: null,
      playerRaces: ['HUMAN'],
      playerCategoryTypes: ['WOLF']
    }

    await api.updateItemCondition('etc', 57, request)

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/game-versions/interlude/content/items/etc/57/condition',
      { method: 'PUT', body: request }
    )
  })
})
