import { LoadingManager, type Object3D } from 'three'
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js'

const versionPathMarker = '/versions/'
const transparentPixel =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+3MxZ5wAAAABJRU5ErkJggg=='

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

export function createPublishedGltfLoader() {
  const manager = new LoadingManager()
  manager.setURLModifier(resolveStudioGltfResourceUrl)
  return new GLTFLoader(manager)
}

export function resolveStudioGltfResourceUrl(url: string) {
  return /\.(?:png|jpe?g|webp|ktx2?)(?:[?#]|$)/i.test(url)
    ? transparentPixel
    : normalizePublishedGltfResourceUrl(url)
}

export async function loadPublishedGltf(url: string): Promise<Object3D> {
  return (await createPublishedGltfLoader().loadAsync(url)).scene
}
