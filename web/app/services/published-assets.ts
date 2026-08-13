import { resolvePublishedAssetUrls } from '../utils/published-asset-url'

export async function getPublishedManifest<T>(url: string): Promise<T> {
  const manifest = await $fetch<T>(url) as T
  return resolvePublishedAssetUrls(manifest, String(useRuntimeConfig().public.assetBaseUrl))
}
