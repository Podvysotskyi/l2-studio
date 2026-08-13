import {
  publishedAssetUrl,
  resolvePublishedAssetUrls
} from '../utils/published-asset-url'

export async function getPublishedManifest<T>(
  url: string,
  assetBaseUrl = String(useRuntimeConfig().public.assetBaseUrl)
): Promise<T> {
  const manifest = await $fetch<T>(publishedAssetUrl(url, assetBaseUrl)) as T
  return resolvePublishedAssetUrls(manifest, assetBaseUrl)
}
