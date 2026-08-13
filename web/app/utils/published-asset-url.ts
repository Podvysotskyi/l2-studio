const assetUrlKeys = new Set([
  'url',
  'heightmap'
])

export function publishedAssetUrl(url: string, assetBaseUrl: string) {
  if (!url.startsWith('/versions/')) return url
  return `${assetBaseUrl.replace(/\/$/, '')}${url}`
}

export function resolvePublishedAssetUrls<T>(
  value: T,
  assetBaseUrl: string,
  key?: string
): T {
  if (Array.isArray(value)) {
    return value.map(item =>
      typeof item === 'string' && key && isAssetUrlKey(key)
        ? publishedAssetUrl(item, assetBaseUrl)
        : resolvePublishedAssetUrls(item, assetBaseUrl, key)
    ) as T
  }
  if (!value || typeof value !== 'object') return value

  return Object.fromEntries(
    Object.entries(value).map(([key, item]) => [
      key,
      typeof item === 'string' && isAssetUrlKey(key)
        ? publishedAssetUrl(item, assetBaseUrl)
        : resolvePublishedAssetUrls(item, assetBaseUrl, key)
    ])
  ) as T
}

function isAssetUrlKey(key: string) {
  return assetUrlKeys.has(key) || key.endsWith('Url') || key.endsWith('Urls')
}
