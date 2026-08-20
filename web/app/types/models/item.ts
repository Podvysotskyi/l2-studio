export type ItemLookupKind = 'item-types' | 'item-actions' | 'item-body-parts' | 'item-materials' | 'item-crystal-types' | 'item-handlers' | 'item-skill-types'

export interface ItemLookupRecord {
  name: string
  displayName: string
  parentTypeName?: string | null
  parentTypeDisplayName?: string | null
}
export interface ItemStatsRecord {
  accuracyCombat: number | null; criticalRate: number | null; magicalAttack: number | null
  magicalDefence: number | null; maximumMp: number | null; physicalAttack: number | null
  physicalAttackRange: number | null; physicalAttackSpeed: number | null; physicalDefence: number | null
  evasion: number | null; shieldRate: number | null; randomDamage: number | null; shieldDefence: number | null
}
export interface ItemAttackGeometryRecord {
  offsetX: number; offsetY: number; radius: number; length: number
}
export interface ItemSkillRecord {
  skillId: number; skillLevel: number; skillName: string | null
  itemSkillTypeName: string | null; itemSkillTypeDisplayName: string | null; chance: number | null
}
export interface ItemPrimarySkillRecord {
  value: string; skillId: number | null; skillLevel: number | null; skillName: string | null
}
export interface ItemPropertiesRecord {
  displayId: number | null; crystalCount: number | null; soulshots: number | null; spiritshots: number | null
  mpConsume: number | null; reducedMpConsume: string | null; reuseDelay: number | null; recipeId: number | null
  itemSkill: string | null; useCondition: string | null; elementEnabled: boolean | null; isAttackWeapon: boolean | null
  isForceEquip: boolean | null; isMagicWeapon: boolean | null; isQuestItem: boolean | null; useWeaponSkillsOnly: boolean | null
}
export interface ItemBehaviorAvailabilityRecord {
  enchantEnabled: boolean | null; forNpc: boolean | null; immediateEffect: boolean | null
  isDepositable: boolean | null; isDestroyable: boolean | null; isDropable: boolean | null
  isOlyRestricted: boolean | null; isSellable: boolean | null; isStackable: boolean | null; isTradable: boolean | null
}
export interface ItemConditionRecord {
  messageId: number; addName: boolean; isPvpFlagged: boolean | null
  playerRaces: string[]; playerCategoryTypes: string[]
}
export interface ItemRecord {
  id: number; name: string; itemTypeName: string; itemTypeDisplayName: string
  itemParentTypeName: string | null; itemParentTypeDisplayName: string | null
  itemActionName: string | null; itemActionDisplayName: string | null
  itemBodyPartName: string | null; itemBodyPartDisplayName: string | null
  itemMaterialName: string | null; itemMaterialDisplayName: string | null
  itemCrystalTypeName: string | null; itemCrystalTypeDisplayName: string | null
  icon: string | null; weight: number | null; price: number | null
  handlerName: string | null; handlerDisplayName: string | null
  skills: ItemSkillRecord[]; attackGeometry: ItemAttackGeometryRecord | null
  stats: ItemStatsRecord | null
}
export interface ItemIconRecord {
  itemId: number
  url: string
}
export interface ItemIconReference {
  itemId: number
  icon: string
  itemBodyPartName: string | null
}
export interface ItemDetailRecord {
  item: ItemRecord
  properties: ItemPropertiesRecord
  behaviorAvailability: ItemBehaviorAvailabilityRecord | null
  primarySkill: ItemPrimarySkillRecord | null
  condition: ItemConditionRecord | null
}
export interface ItemPage { items: ItemRecord[]; total: number; page: number; pageSize: number }
