import type { LevelActorManifestEntry } from '~/types/studio'

export function filterLevelActors(
  actors: LevelActorManifestEntry[],
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
