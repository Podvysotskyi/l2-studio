import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ItemRecipeDirectory from '../../app/components/pages/content/ItemRecipeDirectory.vue'

describe('crafting recipe directory', () => {
  it('does not repeat stat use in expanded recipe details', async () => {
    const wrapper = await mountSuspended(ItemRecipeDirectory, {
      props: {
        items: [{
          id: 1,
          name: 'Craft Dagger',
          itemRecipeTypeName: 'Dwarven',
          craftLevel: 1,
          successRate: 100,
          statUse: { mp: 45, hp: null },
          ingredients: [{ itemId: 100, itemName: 'Iron Ore', count: 10 }],
          productions: [{ itemId: 101, itemName: 'Dagger', count: 1 }]
        }],
        total: 1,
        loading: false,
        query: '',
        page: 1,
        pageSize: 25
      }
    })

    await wrapper.get('[aria-label="Expand recipe #1"]').trigger('click')

    expect(wrapper.text()).toContain('Iron Ore')
    expect(wrapper.text()).toContain('Dagger')
    expect(wrapper.text()).not.toContain('Stat use: 45 MP')
  })
})
