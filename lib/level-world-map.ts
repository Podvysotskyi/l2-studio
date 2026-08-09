export const levelWorldMapTileSize = 128
export const levelWorldMapMinimumScale = 0.35
export const levelWorldMapMaximumScale = 3

export interface LevelWorldMapPoint {
  x: number
  y: number
}

export interface LevelWorldMapSize {
  width: number
  height: number
}

export interface LevelWorldMapTransform extends LevelWorldMapPoint {
  scale: number
}

export function clampLevelWorldMapScale(scale: number) {
  return Math.min(
    levelWorldMapMaximumScale,
    Math.max(levelWorldMapMinimumScale, scale)
  )
}

export function centerLevelWorldMap(
  viewport: LevelWorldMapSize,
  world: LevelWorldMapSize,
  scale = 1
): LevelWorldMapTransform {
  return {
    x: (viewport.width - world.width * scale) / 2,
    y: (viewport.height - world.height * scale) / 2,
    scale
  }
}

export function fitLevelWorldMap(
  viewport: LevelWorldMapSize,
  world: LevelWorldMapSize,
  padding = 32
): LevelWorldMapTransform {
  const availableWidth = Math.max(1, viewport.width - padding * 2)
  const availableHeight = Math.max(1, viewport.height - padding * 2)
  const scale = clampLevelWorldMapScale(
    Math.min(availableWidth / world.width, availableHeight / world.height)
  )
  return centerLevelWorldMap(viewport, world, scale)
}

export function zoomLevelWorldMapAt(
  transform: LevelWorldMapTransform,
  requestedScale: number,
  anchor: LevelWorldMapPoint
): LevelWorldMapTransform {
  const scale = clampLevelWorldMapScale(requestedScale)
  const worldX = (anchor.x - transform.x) / transform.scale
  const worldY = (anchor.y - transform.y) / transform.scale

  return {
    x: anchor.x - worldX * scale,
    y: anchor.y - worldY * scale,
    scale
  }
}

export function constrainLevelWorldMapPan(
  transform: LevelWorldMapTransform,
  viewport: LevelWorldMapSize,
  world: LevelWorldMapSize,
  visibleEdge = 64
): LevelWorldMapTransform {
  const width = world.width * transform.scale
  const height = world.height * transform.scale
  const constrainAxis = (
    position: number,
    content: number,
    available: number
  ) =>
    content <= available
      ? (available - content) / 2
      : Math.min(
          visibleEdge,
          Math.max(available - content - visibleEdge, position)
        )

  return {
    x: constrainAxis(transform.x, width, viewport.width),
    y: constrainAxis(transform.y, height, viewport.height),
    scale: transform.scale
  }
}
