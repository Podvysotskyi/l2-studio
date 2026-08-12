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
              }
            ]
      }
    })
  })

  await page.goto('/storage')
  await expect(page.getByText('maps')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Upload folder' })).toBeVisible()
  await page.getByRole('button', { name: 'Generated assets' }).click()
  await expect(page.getByText('Generated assets are read-only')).toBeVisible()
  await expect(page.getByRole('button', { name: 'textures', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Upload folder' })).toBeHidden()
})
