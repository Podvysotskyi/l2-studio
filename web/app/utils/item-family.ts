import type { ItemFamily } from '~/types/requests/directory-request'

export const itemFamilies: ItemFamily[] = [
  'armor', 'weapon', 'arrow', 'material', 'potion', 'recipe', 'enchant', 'scroll', 'pet-collar', 'etc'
]

export const itemFamilyLabels: Record<ItemFamily, string> = {
  armor: 'Armor', weapon: 'Weapon', arrow: 'Arrow', material: 'Material', potion: 'Potion',
  recipe: 'Recipe', enchant: 'Enchant', scroll: 'Scroll', 'pet-collar': 'Pet Collar', etc: 'Etc Item'
}

export const skillItemFamilies: ItemFamily[] = ['weapon', 'potion', 'enchant', 'scroll', 'pet-collar', 'etc']

export function isItemFamily(value: string): value is ItemFamily {
  return itemFamilies.includes(value as ItemFamily)
}
