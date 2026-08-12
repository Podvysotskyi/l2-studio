import type { GameVersionSummary } from '../types/models/game-version'

export const gameVersionStorageKey = 'l2-studio.game-version'
export const defaultGameVersionKey = 'c1'

export function selectedGameVersionKey() {
  if (!import.meta.client) return defaultGameVersionKey
  return window.localStorage.getItem(gameVersionStorageKey) ?? defaultGameVersionKey
}

export function resolveSelectedGameVersionKey(
  versions: GameVersionSummary[],
  selected: string
) {
  if (versions.some(version => version.key === selected)) return selected
  return versions.find(version => version.isDefault)?.key ??
    versions[0]?.key ??
    defaultGameVersionKey
}
