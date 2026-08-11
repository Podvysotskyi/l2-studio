import type { SceneObjectManifestEntry } from '~/types/studio'

export function filterSceneObjects(
  objects: SceneObjectManifestEntry[],
  query: string
) {
  const normalized = query.trim().toLocaleLowerCase()
  if (!normalized) return objects

  return objects.filter((object) =>
    [
      object.name,
      object.className,
      object.owner,
      object.target,
      object.resourceUrl,
      object.diagnostic,
      object.properties.Tag
    ].some((value) => value?.toLocaleLowerCase().includes(normalized))
  )
}

export function sceneObjectStatus(object: SceneObjectManifestEntry) {
  if (object.diagnostic) return 'diagnostic'
  if (object.resourceUrl) return 'resolved'
  return 'metadata'
}
