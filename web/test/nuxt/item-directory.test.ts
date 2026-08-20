import { describe, expect, it, vi } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ItemDirectory from '../../app/components/pages/content/ItemDirectory.vue'

vi.mock('../../app/services/studio-api', () => ({
  deleteItemDefinition: vi.fn(),
  getItemLookups: vi.fn().mockResolvedValue({ items: [], total: 0, page: 1, pageSize: 25 }),
  updateItemDefinition: vi.fn()
}))

describe('item definition directory', () => {
  it('shows the item icon before the ID and falls back when its image cannot load', async () => {
    const wrapper = await mountSuspended(ItemDirectory, {
      props: {
        items: [item()],
        iconUrls: { 1: 'https://assets.test/icons/sword.webp' },
        total: 1,
        loading: false,
        family: 'weapon',
        query: '',
        page: 1,
        pageSize: 25,
        itemTypeName: undefined,
        itemActionName: undefined,
        itemBodyPartName: undefined,
        itemMaterialName: undefined,
        itemCrystalTypeName: undefined,
        handlerName: undefined
      }
    })

    const headers = wrapper.findAll('thead th').map(header => header.text())
    expect(headers.slice(0, 2)).toEqual(['', 'ID'])
    const icon = wrapper.get('img[alt="Sword icon"]')
    expect(icon.attributes('src')).toBe('https://assets.test/icons/sword.webp')

    await icon.trigger('error')

    expect(wrapper.find('[aria-label="Sword icon unavailable"]').exists()).toBe(true)
  })
})

function item() {
  return {
    id: 1,
    name: 'Sword',
    itemTypeName: 'Weapon',
    itemTypeDisplayName: 'Weapon',
    itemParentTypeName: null,
    itemParentTypeDisplayName: null,
    itemActionName: null,
    itemActionDisplayName: null,
    itemBodyPartName: null,
    itemBodyPartDisplayName: null,
    itemMaterialName: null,
    itemMaterialDisplayName: null,
    itemCrystalTypeName: null,
    itemCrystalTypeDisplayName: null,
    icon: 'icon.weapon_sword_i00',
    weight: null,
    price: null,
    handlerName: null,
    handlerDisplayName: null,
    skills: [],
    attackGeometry: null,
    stats: null
  }
}
