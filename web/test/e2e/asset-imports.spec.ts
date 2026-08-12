import { expect, test } from '@playwright/test'

const baseRun = {
  id: '11111111-1111-1111-1111-111111111111',
  kind: 'textures',
  triggerType: 'full_scan',
  status: 'running',
  requestedSourceKey: null,
  requestedAt: '2026-08-11T12:00:00Z',
  startedAt: '2026-08-11T12:00:01Z',
  discoveryFinishedAt: '2026-08-11T12:00:02Z',
  finishedAt: null,
  discoveredFileCount: 2,
  completedFileCount: 1,
  succeededFileCount: 1,
  warningFileCount: 0,
  failedFileCount: 0,
  error: null
}

test('starts a scan, observes file progress, inspects diagnostics, and retries one file', async ({
  page
}) => {
  let scanStarted = false
  let fileRetried = false
  let diagnosticQuery = ''
  await page.route('**/api/**', async (route) => {
    const request = route.request()
    const url = new URL(request.url())
    if (url.pathname === '/api/game-versions') {
      await route.fulfill({ json: [
        { key: 'interlude', displayName: 'Interlude', sourceFolder: 'Interlude', sortOrder: 30, isDefault: true }
      ] })
      return
    }
    if (url.pathname === '/api/game-versions/interlude/assets/textures/imports' && request.method() === 'POST') {
      scanStarted = true
      await route.fulfill({ json: baseRun })
      return
    }
    if (url.pathname.endsWith('/imports') && request.method() === 'GET') {
      const kind = url.pathname.split('/')[5]
      await route.fulfill({ json: kind === 'textures' && scanStarted ? [baseRun] : [] })
      return
    }
    if (url.pathname.endsWith('/catalog')) {
      await route.fulfill({
        json: {
          summary: {
            kind: 'textures',
            sourceFolder: 'textures',
            sourceHash: '0'.repeat(64),
            schemaVersion: 7,
            protocol: 121,
            total: 0,
            resolved: 0,
            skipped: 0,
            groups: 0,
            publishedAt: '2026-08-11T12:00:00Z'
          },
          groups: [],
          items: [],
          total: 0,
          page: 1,
          pageSize: 100
        }
      })
      return
    }
    if (url.pathname.endsWith('/work-items')) {
      await route.fulfill({
        json: {
          items: [
            {
              id: '22222222-2222-2222-2222-222222222222',
              runId: baseRun.id,
              importKind: 'textures',
              sourceKey: 'broken.utx',
              sourceHash: '1'.repeat(64),
              status: 'failed',
              attemptCount: 1,
              createdAt: '2026-08-11T12:00:02Z',
              startedAt: '2026-08-11T12:00:03Z',
              finishedAt: '2026-08-11T12:00:04Z',
              totalResourceCount: 3,
              processedResourceCount: 1,
              skippedResourceCount: 0,
              warningCount: 0,
              error: 'Texture payload is invalid.',
              unpublishedAt: '2026-08-11T12:00:04Z'
            }
          ],
          total: 1,
          page: 1,
          pageSize: 100
        }
      })
      return
    }
    if (url.pathname.endsWith('/diagnostics')) {
      diagnosticQuery = url.searchParams.get('query') ?? ''
      await route.fulfill({
        json: {
          items: [
            {
              id: 1,
              runId: baseRun.id,
              workItemId: '22222222-2222-2222-2222-222222222222',
              severity: 'error',
              code: 'conversion.failed',
              stage: 'conversion',
              sourceKey: 'broken.utx',
              objectName: null,
              message: 'Texture payload is invalid.',
              createdAt: '2026-08-11T12:00:04Z'
            }
          ],
          total: 1,
          page: 1,
          pageSize: 100
        }
      })
      return
    }
    if (url.pathname.endsWith('/imports/files/broken.utx')) {
      fileRetried = true
      await route.fulfill({
        json: {
          ...baseRun,
          id: '33333333-3333-3333-3333-333333333333',
          triggerType: 'single_file',
          status: 'queued',
          requestedSourceKey: 'broken.utx',
          discoveredFileCount: 1,
          completedFileCount: 0,
          succeededFileCount: 0
        }
      })
      return
    }
    await route.fulfill({ status: 404, json: {} })
  })

  await page.goto('/assets/textures')
  await page.waitForLoadState('networkidle')
  await page.getByRole('button', { name: 'Import textures' }).click()
  await expect.poll(() => scanStarted).toBe(true)

  await page.goto('/assets/jobs')
  await expect(page.getByText('1 / 2 completed')).toBeVisible()
  const runCard = page.locator('article').filter({ hasText: '1 / 2 completed' })
  await runCard.locator('button').first().click()
  await expect(page.getByText('broken.utx').first()).toBeVisible()
  await expect(page.getByText('Unpublished')).toBeVisible()
  await expect(page.getByText('conversion.failed')).toBeVisible()

  await page.getByPlaceholder('Message, source, or object').fill('terrain')
  await page.getByRole('button', { name: 'Filter' }).click()
  await expect.poll(() => diagnosticQuery).toBe('terrain')

  await page.getByRole('button', { name: 'Re-import' }).click()
  await expect.poll(() => fileRetried).toBe(true)
})
