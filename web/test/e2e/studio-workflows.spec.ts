import { expect, test, type Page, type Route } from '@playwright/test'

const versions = [
  { key: 'c1', displayName: 'Chronicle 1', isDefault: true },
  { key: 'interlude', displayName: 'Interlude', isDefault: false }
]

test.beforeEach(async ({ page }) => {
  await mockStudioApi(page)
})

test('selecting a game version reloads version-scoped content', async ({ page }) => {
  await page.goto('/authoring/items/etc')

  await expect(page.getByText('Etc Item definitions')).toBeVisible()
  await expect(page.getByText('Adena C1')).toBeVisible()
  await page.getByLabel('Game version').click()
  await page.getByRole('option', { name: 'Interlude' }).click()

  await expect.poll(() => page.evaluate(() => window.localStorage.getItem('l2-studio.game-version')))
    .toBe('interlude')
  await expect(page.getByText('Adena Interlude')).toBeVisible()
  await expect(page.getByText('Adena C1')).toHaveCount(0)
})

test('a directory search owns its query string', async ({ page }) => {
  await page.goto('/authoring/items/etc')
  const search = page.getByLabel('Search item names')
  await search.fill('wolf')

  await expect(page).toHaveURL(/query=wolf/)
})

test('authoring mutation errors remain visible to the form', async ({ page }) => {
  await page.goto('/authoring/items/etc')

  await page.getByRole('button', { name: 'Edit' }).first().click()
  await page.getByRole('button', { name: 'Save changes' }).click()

  await expect(page.getByText('The item could not be saved.')).toBeVisible()
  await expect(page.getByText('Name is required.')).toBeVisible()
})

test('original resources expose mutations while generated assets remain read-only', async ({ page }) => {
  await page.goto('/storage/original-resources')

  await expect(page.getByRole('heading', { name: 'Original resources' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'New folder' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Upload' })).toBeVisible()

  await page.goto('/storage/generated-assets')

  await expect(page.getByRole('heading', { name: 'Generated assets' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'New folder' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: /upload/i })).toHaveCount(0)
})

test('the map inspector lazy-loads its inspection surface', async ({ page }) => {
  await page.goto('/library/maps/test-map')

  await expect(page.getByText('Map inspector')).toBeVisible()
  await expect(page.getByRole('tab', { name: 'Actors' })).toBeVisible()
})

test('the scene inspector lazy-loads its inspection surface', async ({ page }) => {
  await page.goto('/library/scenes/test-scene')

  await expect(page.getByRole('heading', { name: 'Test scene' })).toBeVisible()
  await expect(page.getByText('Structural BSP')).toBeVisible()
})

async function mockStudioApi(page: Page) {
  await page.route('http://localhost:5300/versions/**', route =>
    fulfill(route, sceneManifest(), 200, { 'access-control-allow-origin': '*' })
  )
  await page.route('**/api/**', async route => {
    const url = new URL(route.request().url())
    const path = url.pathname
    const method = route.request().method()

    if (path === '/api/game-versions') return fulfill(route, versions)
    if (path === '/api/system/info') return fulfill(route, { name: 'Studio', version: 'test' })
    if (method === 'PUT' && path.endsWith('/content/items/etc/57')) {
      return fulfill(route, {
        title: 'One or more validation errors occurred.',
        detail: 'The item could not be saved.',
        status: 400,
        errors: { definition: ['Name is required.'] }
      }, 400)
    }
    if (path.includes('/content/items/etc/lookups/'))
      return fulfill(route, { items: itemLookups(path), total: 1, page: 1, pageSize: 500 })
    if (path.endsWith('/content/items/etc'))
      return fulfill(route, {
        items: [item(path.includes('/interlude/') ? 'Interlude' : 'C1')],
        total: 1,
        page: 1,
        pageSize: 25
      })
    if (path.includes('/assets/catalog/maps/test-map'))
      return fulfill(route, mapCatalogEntry())
    if (path.includes('/assets/catalog/scenes/test-scene'))
      return fulfill(route, sceneCatalogEntry())
    if (path.includes('/assets/catalog'))
      return fulfill(route, { items: [], total: 0, page: 1, pageSize: 500 })
    if (path.includes('/imports')) return fulfill(route, { items: [], total: 0, page: 1, pageSize: 25 })

    return fulfill(route, { items: [], total: 0, page: 1, pageSize: 25 })
  })
}

function fulfill(
  route: Route,
  body: unknown,
  status = 200,
  headers: Record<string, string> = {}
) {
  return route.fulfill({
    status,
    contentType: 'application/json',
    headers,
    body: JSON.stringify(body)
  })
}

function itemLookups(path: string) {
  const name = path.endsWith('/item-types') ? 'EtcItem' : 'None'
  return [{ name, displayName: name, parentTypeName: name === 'EtcItem' ? 'EtcItem' : undefined, parentTypeDisplayName: 'Etc Item' }]
}

function item(version: string) {
  return {
    id: 57,
    name: `Adena ${version}`,
    itemTypeName: 'EtcItem',
    itemTypeDisplayName: 'Etc Item',
    itemParentTypeName: null,
    itemParentTypeDisplayName: null,
    itemActionName: null,
    itemActionDisplayName: null,
    itemBodyPartName: null,
    itemBodyPartDisplayName: null,
    itemMaterialName: 'None',
    itemMaterialDisplayName: 'None',
    itemCrystalTypeName: null,
    itemCrystalTypeDisplayName: null,
    handlerName: null,
    handlerDisplayName: null,
    icon: null,
    weight: 0,
    price: 0,
    attackGeometry: null
  }
}

function mapCatalogEntry() {
  return {
    name: 'test-map',
    sourceKey: 'maps/test-map.unr',
    objectName: 'test-map',
    importStatus: 'completed',
    url: 'http://assets.invalid/maps/test-map.json'
  }
}

function sceneCatalogEntry() {
  return {
    name: 'test-scene',
    fileName: 'test-scene.unr',
    manifestUrl: '/versions/c1/scenes/test-scene.json',
    terrainCount: 0,
    actorCount: 0,
    cinematicObjectCount: 0,
    sha256: 'test',
    status: 'resolved',
    error: null,
    sourceKey: 'scenes/test-scene.unr'
  }
}

function sceneManifest() {
  return {
    schemaVersion: 13,
    name: 'Test scene',
    fileName: 'test-scene.unr',
    sourceHash: 'test',
    protocol: 0,
    environment: {
      ambientColor: { r: 0, g: 0, b: 0 },
      ambientBrightness: 0,
      distanceFog: null
    },
    terrains: [],
    actors: [],
    lights: [],
    waterVolumes: [],
    skyZones: [],
    bspMeshes: [],
    skyBackdrops: [],
    cameras: [],
    interpolationPoints: [],
    sceneManagers: [],
    actions: [],
    ambientSounds: [],
    effects: [],
    unrepresentedObjectClasses: {}
  }
}
