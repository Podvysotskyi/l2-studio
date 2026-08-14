import { LoadingManager, type Object3D } from 'three'
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'
import type { GLTF } from 'three/addons/loaders/GLTFLoader.js'

const versionPathMarker = '/versions/'
const placeholderImage =
  'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw=='

export function normalizePublishedGltfResourceUrl(url: string) {
  if (url.startsWith(`/${versionPathMarker}`)) return url.slice(1)

  const firstVersionPath = url.indexOf(versionPathMarker)
  const resourceVersionPath = url.lastIndexOf(versionPathMarker)
  if (firstVersionPath < 0 || firstVersionPath === resourceVersionPath)
    return url

  const resourcePath = url.slice(resourceVersionPath)
  try {
    return new URL(resourcePath, url).toString()
  } catch {
    return resourcePath
  }
}

export function resolvePublishedGltfMaterialUrl(url: string, modelUrl: string) {
  const normalized = normalizePublishedGltfResourceUrl(url)
  try {
    return normalizePublishedGltfResourceUrl(new URL(normalized, modelUrl).toString())
  } catch {
    return normalized
  }
}

export function createPublishedGltfLoader() {
  const manager = new LoadingManager()
  manager.setURLModifier(resolveStudioGltfResourceUrl)
  return new GLTFLoader(manager)
}

export function resolveStudioGltfResourceUrl(url: string) {
  return /\.(?:png|jpe?g|webp|ktx2?)(?:[?#]|$)/i.test(url)
    ? placeholderImage
    : normalizePublishedGltfResourceUrl(url)
}

export async function loadPublishedGltf(url: string): Promise<Object3D> {
  return (await loadPublishedGltfAsset(url)).scene
}

export function loadPublishedGltfAsset(url: string): Promise<GLTF> {
  return createPublishedGltfLoader().loadAsync(url)
}
