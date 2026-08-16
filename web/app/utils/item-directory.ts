import { directoryRouteQuery, directoryRouteState } from './directory'

export interface ItemDirectoryRouteState {
  query: string
  page: number
  pageSize: number
  itemTypeName?: string
  itemActionName?: string
  itemBodyPartName?: string
  itemMaterialName?: string
  itemCrystalTypeName?: string
  handlerName?: string
}

export function itemDirectoryRouteState(query: Record<string, unknown>): ItemDirectoryRouteState {
  return {
    ...directoryRouteState(query),
    itemTypeName: queryValue(query.itemTypeName),
    itemActionName: queryValue(query.itemActionName),
    itemBodyPartName: queryValue(query.itemBodyPartName),
    itemMaterialName: queryValue(query.itemMaterialName),
    itemCrystalTypeName: queryValue(query.itemCrystalTypeName),
    handlerName: queryValue(query.handlerName)
  }
}

export function itemDirectoryRouteQuery(state: ItemDirectoryRouteState) {
  return {
    ...directoryRouteQuery(state.query, state.page, state.pageSize),
    ...(state.itemTypeName ? { itemTypeName: state.itemTypeName } : {}),
    ...(state.itemActionName ? { itemActionName: state.itemActionName } : {}),
    ...(state.itemBodyPartName ? { itemBodyPartName: state.itemBodyPartName } : {}),
    ...(state.itemMaterialName ? { itemMaterialName: state.itemMaterialName } : {}),
    ...(state.itemCrystalTypeName ? { itemCrystalTypeName: state.itemCrystalTypeName } : {}),
    ...(state.handlerName ? { handlerName: state.handlerName } : {})
  }
}

function queryValue(value: unknown): string | undefined {
  return typeof value === 'string' && value ? value : undefined
}
