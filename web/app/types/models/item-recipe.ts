import type { DirectoryPage } from '../responses/content-directory-response'

export interface ItemRecipeItemRecord {
  itemId: number
  itemName: string | null
  count: number
}

export interface ItemRecipeStatUseRecord {
  mp: number | null
  hp: number | null
}

export interface ItemRecipeRecord {
  id: number
  name: string
  itemRecipeTypeName: string
  craftLevel: number
  successRate: number
  statUse: ItemRecipeStatUseRecord | null
  ingredients: ItemRecipeItemRecord[]
  productions: ItemRecipeItemRecord[]
}

export interface ItemRecipeTypeRecord {
  name: string
  recipeCount: number
}

export type ItemRecipePage = DirectoryPage<ItemRecipeRecord>
export type ItemRecipeTypePage = DirectoryPage<ItemRecipeTypeRecord>
