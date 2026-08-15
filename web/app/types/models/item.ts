export type ItemLookupKind = 'item-types' | 'item-actions' | 'item-body-parts' | 'item-materials' | 'item-crystal-types'

export interface ItemLookupRecord { name: string; displayName: string }
export interface ItemStatsRecord {
  accuracyCombat: number | null; criticalRate: number | null; magicalAttack: number | null
  magicalDefence: number | null; maximumMp: number | null; physicalAttack: number | null
  physicalAttackRange: number | null; physicalAttackSpeed: number | null; physicalDefence: number | null
  evasion: number | null; shieldRate: number | null; randomDamage: number | null; shieldDefence: number | null
}
export interface ItemRecord {
  id: number; name: string; itemTypeName: string; itemTypeDisplayName: string
  itemActionName: string | null; itemActionDisplayName: string | null
  itemBodyPartName: string | null; itemBodyPartDisplayName: string | null
  itemMaterialName: string | null; itemMaterialDisplayName: string | null
  itemCrystalTypeName: string | null; itemCrystalTypeDisplayName: string | null
  icon: string | null; weight: number | null; price: number | null; weaponType: string | null
  armorType: string | null; etcItemType: string | null; damageRange: string | null
  stats: ItemStatsRecord | null
}
export interface ItemPage { items: ItemRecord[]; total: number; page: number; pageSize: number }
