import type { ItemConditionRecord, ItemDetailRecord } from '../types/models/item'
import type { ItemFamily } from '../types/requests/directory-request'
import type { UpdateItemConditionRequest } from '../types/requests/update-item-condition-request'
import type { StudioVersionClient } from './studio-version-client'

export interface ContentApiClient {
  getItem(family: ItemFamily, id: number): Promise<ItemDetailRecord>
  updateItemCondition(
    family: ItemFamily,
    id: number,
    request: UpdateItemConditionRequest
  ): Promise<ItemConditionRecord>
  deleteItemCondition(family: ItemFamily, id: number): Promise<void>
}

export function createContentApi(client: StudioVersionClient): ContentApiClient {
  const itemPath = (family: ItemFamily, id: number) =>
    client.path(`/content/items/${family}/${id}`)

  return {
    getItem: (family, id) => $fetch<ItemDetailRecord>(itemPath(family, id)),
    updateItemCondition: (family, id, request) => $fetch<ItemConditionRecord>(
      `${itemPath(family, id)}/condition`,
      { method: 'PUT', body: request }
    ),
    deleteItemCondition: (family, id) => $fetch<void>(
      `${itemPath(family, id)}/condition`,
      { method: 'DELETE' }
    )
  }
}
