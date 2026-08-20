export interface StudioVersionClient {
  readonly gameVersion: string
  path(path: string): string
}

/**
 * Builds a private-Studio API path for one explicit game-version context.
 * Callers own selection; this client deliberately does not read browser storage.
 */
export function createStudioVersionClient(gameVersion: string): StudioVersionClient {
  const normalizedVersion = gameVersion.trim()
  if (!normalizedVersion) throw new Error('A game version is required.')

  const root = `/api/game-versions/${encodeURIComponent(normalizedVersion)}`
  return {
    gameVersion: normalizedVersion,
    path: (path: string) => `${root}${path.startsWith('/') ? path : `/${path}`}`
  }
}
