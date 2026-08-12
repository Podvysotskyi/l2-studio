import type { MapActorManifestEntry } from '~/types/studio'

export function filterMapActors(
  actors: MapActorManifestEntry[],
  query: string
) {
  const term = query.trim().toLocaleLowerCase()
  if (!term) return actors

  return actors.filter((actor) =>
    [actor.name, actor.className, actor.meshPackage, actor.meshObject].some(
      (value) => value?.toLocaleLowerCase().includes(term)
    )
  )
}
