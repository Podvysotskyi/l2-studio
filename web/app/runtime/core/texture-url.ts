const fallbackMarker = 'gpu=none'

export function browserDecodedTextureUrl(url: string) {
  if (url.startsWith('data:') || url.includes(fallbackMarker)) return url
  return `${url}${url.includes('?') ? '&' : '?'}${fallbackMarker}`
}
