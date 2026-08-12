import { expect, test } from '@playwright/test'

const versions = [
  {
    key: 'c1',
    displayName: 'Chronicle 1',
    sourceFolder: 'C1',
    sortOrder: 10,
    isDefault: true
  },
  {
    key: 'interlude',
    displayName: 'Interlude',
    sourceFolder: 'Interlude',
    sortOrder: 30,
    isDefault: false
  }
]

test.beforeEach(async ({ page }) => {
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    if (url.pathname === '/api/game-versions') {
      await route.fulfill({ json: versions })
      return
    }
    if (url.pathname === '/api/system/info') {
      await route.fulfill({ json: { service: 'Studio', buildVersion: 'test', environment: 'Testing' } })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })
})

test('stores C1 when no version has been selected', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByLabel('Game version')).toContainText('Chronicle 1')
  await expect.poll(() => page.evaluate(() =>
    window.localStorage.getItem('l2-studio.game-version')
  )).toBe('c1')
})

test('switches versions and uses the selection after reloading', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByLabel('Game version')).toContainText('Chronicle 1')
  const versionedRequest = page.waitForRequest(request =>
    new URL(request.url()).pathname.startsWith('/api/game-versions/interlude/')
  )

  await page.getByLabel('Game version').click()
  await page.getByText('Interlude', { exact: true }).click()
  await versionedRequest

  await expect(page.getByLabel('Game version')).toContainText('Interlude')
  await expect.poll(() => page.evaluate(() =>
    window.localStorage.getItem('l2-studio.game-version')
  )).toBe('interlude')
})

test('replaces an unavailable stored version with C1', async ({ page }) => {
  await page.addInitScript(() => {
    window.localStorage.setItem('l2-studio.game-version', 'unknown')
  })
  await page.goto('/')

  await expect(page.getByLabel('Game version')).toContainText('Chronicle 1')
  await expect.poll(() => page.evaluate(() =>
    window.localStorage.getItem('l2-studio.game-version')
  )).toBe('c1')
})
