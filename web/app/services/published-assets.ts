import {
  publishedAssetUrl,
  resolvePublishedAssetUrls
} from '../utils/published-asset-url'

export async function getPublishedManifest<T>(
  url: string,
  assetBaseUrl = String(useRuntimeConfig().public.assetBaseUrl)
): Promise<T> {
  return (await getPublishedManifestWithRaw<T>(url, assetBaseUrl)).resolved
}

export async function getPublishedManifestWithRaw<T>(
  url: string,
  assetBaseUrl = String(useRuntimeConfig().public.assetBaseUrl)
): Promise<{ raw: T, resolved: T }> {
  const raw = await $fetch<T>(publishedAssetUrl(url, assetBaseUrl)) as T
  return { raw, resolved: resolvePublishedAssetUrls(raw, assetBaseUrl) }
}
