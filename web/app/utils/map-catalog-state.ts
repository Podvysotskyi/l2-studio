import type { AssetCatalogPage, MapCatalogEntry } from '~/types/studio'

export function hasImportedMaps(
  catalog: AssetCatalogPage<MapCatalogEntry> | undefined
) {
  return Boolean(catalog?.items.length)
}
