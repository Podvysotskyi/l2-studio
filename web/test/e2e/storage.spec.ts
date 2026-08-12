import { expect, test } from '@playwright/test'

test('browses version storage and keeps generated assets read-only', async ({ page }) => {
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    if (url.pathname === '/api/game-versions') {
      await route.fulfill({
        json: [
          {
            key: 'interlude',
            displayName: 'Interlude',
            sourceFolder: 'Interlude',
            sortOrder: 30,
            isDefault: true
          }
        ]
      })
      return
    }
    if (url.pathname === '/api/system/info') {
      await route.fulfill({ json: { name: 'Studio', description: 'Ready' } })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })
  await page.route('**/storage-api/**', async route => {
    const url = new URL(route.request().url())
    const assets = url.pathname.includes('/assets/')
    await route.fulfill({
      json: {
        path: '',
        entries: assets
          ? [
              {
                name: 'textures',
                path: 'textures',
                type: 'directory',
                size: null,
                modifiedAt: '2026-08-11T12:00:00Z'
              }
            ]
          : [
              {
                name: 'maps',
                path: 'maps',
                type: 'directory',
                size: null,
                modifiedAt: '2026-08-11T12:00:00Z'
              },
              {
                name: 'logo.utx',
                path: 'logo.utx',
                type: 'file',
                size: 1024,
                modifiedAt: '2026-08-11T12:00:00Z'
              }
            ]
      }
    })
  })

  await page.goto('/storage')
  await expect(page.getByText('maps')).toBeVisible()
  await expect(page.getByText('logo.utx')).toBeVisible()
  await expect(page.getByRole('button', { name: /^Move / })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Upload folder' })).toBeVisible()
  await page.getByRole('button', { name: 'Generated assets' }).click()
  await expect(page.getByText('Generated assets are read-only')).toBeVisible()
  await expect(page.getByRole('button', { name: 'textures', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Upload folder' })).toBeHidden()
})

test('replaces stale entries with a loader while opening a folder', async ({ page }) => {
  let fulfillFolderListing: () => Promise<void>
  let markFolderRequested: () => void
  const folderRequested = new Promise<void>(resolve => {
    markFolderRequested = resolve
  })

  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    if (url.pathname === '/api/game-versions') {
      await route.fulfill({
        json: [{
          key: 'interlude',
          displayName: 'Interlude',
          sourceFolder: 'Interlude',
          sortOrder: 30,
          isDefault: true
        }]
      })
      return
    }
    if (url.pathname === '/api/system/info') {
      await route.fulfill({ json: { name: 'Studio', description: 'Ready' } })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })
  await page.route('**/storage-api/**', async route => {
    const url = new URL(route.request().url())
    if (url.searchParams.get('path') === 'maps') {
      fulfillFolderListing = () => route.fulfill({
        json: {
          path: 'maps',
          entries: [{
            name: 'gludio.unr',
            path: 'maps/gludio.unr',
            type: 'file',
            size: 1024,
            modifiedAt: '2026-08-11T12:00:00Z'
          }]
        }
      })
      markFolderRequested()
      return
    }
    await route.fulfill({
      json: {
        path: '',
        entries: [
          {
            name: 'maps',
            path: 'maps',
            type: 'directory',
            size: null,
            modifiedAt: '2026-08-11T12:00:00Z'
          },
          {
            name: 'logo.utx',
            path: 'logo.utx',
            type: 'file',
            size: 1024,
            modifiedAt: '2026-08-11T12:00:00Z'
          }
        ]
      }
    })
  })

  await page.goto('/storage')
  await expect(page.getByText('logo.utx')).toBeVisible()

  await page.getByRole('button', { name: 'maps' }).click()
  await folderRequested
  await expect(page.getByText('Loading directory…')).toBeVisible()
  await expect(page.getByText('logo.utx')).toHaveCount(0)

  await fulfillFolderListing!()
  await expect(page.getByText('gludio.unr')).toBeVisible()
})
