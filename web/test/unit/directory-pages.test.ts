import { describe, expect, it, vi } from 'vitest'
import { loadDirectoryOptions } from '../../app/utils/directory-pages'

describe('loadDirectoryOptions', () => {
  it('collects every page while retaining the first response order', async () => {
    const load = vi.fn(async (page: number, pageSize: number) => {
      expect(pageSize).toBe(2)
      const items = {
        1: ['one', 'two'],
        2: ['three', 'four'],
        3: ['five']
      }[page] ?? []
      return { items, total: 5, page, pageSize }
    })

    await expect(loadDirectoryOptions(load, 2)).resolves.toEqual([
      'one', 'two', 'three', 'four', 'five'
    ])
    expect(load).toHaveBeenCalledTimes(3)
    expect(load).toHaveBeenNthCalledWith(1, 1, 2)
    expect(load).toHaveBeenNthCalledWith(2, 2, 2)
    expect(load).toHaveBeenNthCalledWith(3, 3, 2)
  })
})
