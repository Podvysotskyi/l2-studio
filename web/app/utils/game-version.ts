export const gameVersionStorageKey = 'l2-studio.game-version'

export function selectedGameVersionKey() {
  if (!import.meta.client) return 'interlude'
  return window.localStorage.getItem(gameVersionStorageKey) ?? 'interlude'
}
