import type { MapPlayerStartManifestEntry } from '~/types/studio'

export function filterMapPlayerStarts(
  playerStarts: MapPlayerStartManifestEntry[],
  query: string
) {
  const term = query.trim().toLocaleLowerCase()
  if (!term) return playerStarts

  return playerStarts.filter((playerStart) =>
    playerStart.name.toLocaleLowerCase().includes(term)
  )
}
