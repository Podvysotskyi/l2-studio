import { describe, expect, it, vi } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import ItemDefinitionDetail from '../../app/components/pages/content/ItemDefinitionDetail.vue'
import { getItemDefinition, resolveItemIcons } from '../../app/services/studio-api'

vi.mock('../../app/services/studio-api', () => ({
  getItemDefinition: vi.fn(),
  resolveItemIcons: vi.fn()
}))

describe('item definition detail', () => {
  it('shows the resolved item artwork in the header', async () => {
    vi.mocked(getItemDefinition).mockResolvedValue(detail())
    vi.mocked(resolveItemIcons).mockResolvedValue([
      { itemId: 12, url: 'https://assets.test/icons/artisans-sword.webp' }
    ])

    const wrapper = await mountSuspended(ItemDefinitionDetail, {
      props: { family: 'weapon' },
      route: '/authoring/items/weapon/12',
      global: { stubs: { NuxtPage: true } }
    })

    await vi.waitFor(() => {
      expect(wrapper.find('img[alt="Artisan Sword icon"]').attributes('src'))
        .toBe('https://assets.test/icons/artisans-sword.webp')
    })
    expect(resolveItemIcons).toHaveBeenCalledWith([{
      itemId: 12,
      icon: 'icon.weapon_artisans_sword_i00',
      itemBodyPartName: null
    }])
  })
})

function detail() {
  return {
    item: {
      id: 12,
      name: 'Artisan Sword',
      itemTypeName: 'Sword',
      itemTypeDisplayName: 'Sword',
      itemParentTypeName: null,
      itemParentTypeDisplayName: null,
      itemActionName: 'EQUIP',
      itemActionDisplayName: 'Equip',
      itemBodyPartName: null,
      itemBodyPartDisplayName: null,
      itemMaterialName: null,
      itemMaterialDisplayName: null,
      itemCrystalTypeName: null,
      itemCrystalTypeDisplayName: null,
      icon: 'icon.weapon_artisans_sword_i00',
      weight: null,
      price: null,
      handlerName: null,
      handlerDisplayName: null,
      skills: [],
      attackGeometry: null,
      stats: null
    },
    properties: {
      displayId: null,
      crystalCount: null,
      soulshots: null,
      spiritshots: null,
      mpConsume: null,
      reducedMpConsume: null,
      reuseDelay: null,
      recipeId: null,
      itemSkill: null,
      useCondition: null,
      elementEnabled: null,
      isAttackWeapon: null,
      isForceEquip: null,
      isMagicWeapon: null,
      isQuestItem: null,
      useWeaponSkillsOnly: null
    },
    behaviorAvailability: null,
    primarySkill: null,
    condition: null
  }
}
