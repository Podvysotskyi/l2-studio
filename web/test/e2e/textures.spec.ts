import { expect, test } from '@playwright/test'

const groups = [
  {
    name: 'S_Test',
    fileName: 'S_Test.utx',
    sha256: '1'.repeat(64),
    textureCount: 1,
    materialCount: 0,
    originalFolder: 'systextures',
    path: 'systextures/S_Test.utx'
  },
  {
    name: 'T_Empty',
    fileName: 'T_Empty.utx',
    sha256: '2'.repeat(64),
    textureCount: 0,
    materialCount: 0,
    originalFolder: 'textures',
    path: 'textures/T_Empty.utx'
  },
  {
    name: 'T_Test',
    fileName: 'T_Test.utx',
    sha256: '3'.repeat(64),
    textureCount: 1,
    materialCount: 0,
    originalFolder: 'textures',
    path: 'textures/T_Test.utx'
  }
]

test('browses texture folders and clears the preview when the folder changes', async ({ page }) => {
  await page.route('**/api/**', async (route) => {
    const url = new URL(route.request().url())
    if (url.pathname === '/api/game-versions') {
      await route.fulfill({ json: [
        { key: 'interlude', displayName: 'Interlude', sourceFolder: 'Interlude', sortOrder: 30, isDefault: true }
      ] })
      return
    }
    if (url.pathname.endsWith('/imports')) {
      await route.fulfill({ json: [] })
      return
    }
    if (url.pathname.endsWith('/catalog')) {
      const folder = url.searchParams.get('originalFolder')
      const packageName = url.searchParams.get('packageName')
      const item = folder && packageName ? {
        packageName,
        objectName: `${packageName}.Texture`,
        url: '/texture.webp',
        width: 64,
        height: 64,
        format: 'webp',
        sha256: '4'.repeat(64),
        status: 'resolved',
        error: null,
        gpuUrl: null,
        gpuSha256: null,
        gpuCompressed: false,
        mipCount: 1,
        animation: null,
        originalFolder: folder,
        path: `${folder}/${packageName}/${packageName}.Texture.webp`
      } : undefined
      await route.fulfill({ json: {
        summary: { kind: 'textures', sourceFolder: 'textures', sourceHash: '0'.repeat(64), schemaVersion: 7, protocol: 121, total: groups.length, resolved: groups.length, skipped: 0, groupCount: groups.length, publishedAt: '2026-08-12T12:00:00Z' },
        groups,
        items: item ? [item] : [],
        total: item ? 1 : 0,
        page: 1,
        pageSize: 100
      } })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/assets/textures')
  await expect(page.getByText('Select a folder to view its textures.')).toBeVisible()
  await expect(page.getByRole('button', { name: 'T_Empty', exact: true })).toHaveCount(0)

  await page.getByRole('button', { name: 'T_Test', exact: true }).click()
  await expect(page).toHaveURL(/folder=textures&package=T_Test/)
  await page.getByRole('button', { name: /Select textures\/T_Test\/T_Test\.Texture\.webp/ }).click()
  await expect(page.getByRole('heading', { name: 'T_Test.Texture' })).toBeVisible()

  await page.getByRole('button', { name: 'S_Test', exact: true }).click()
  await expect(page).toHaveURL(/folder=systextures&package=S_Test/)
  await expect(page.getByRole('heading', { name: 'T_Test.Texture' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: /Select systextures\/S_Test\/S_Test\.Texture\.webp/ })).toBeVisible()
})
