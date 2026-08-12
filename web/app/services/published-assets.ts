export function getPublishedManifest<T>(url: string): Promise<T> {
  return $fetch<T>(url) as Promise<T>
}
