export const mapWorldMapTileSize = 128
export const mapWorldMapMinimumScale = 0.35
export const mapWorldMapMaximumScale = 3

export interface MapWorldMapPoint {
  x: number
  y: number
}

export interface MapWorldMapSize {
  width: number
  height: number
}

export interface MapWorldMapTransform extends MapWorldMapPoint {
  scale: number
}

export function clampMapWorldMapScale(scale: number) {
  return Math.min(
    mapWorldMapMaximumScale,
    Math.max(mapWorldMapMinimumScale, scale)
  )
}

export function centerMapWorldMap(
  viewport: MapWorldMapSize,
  world: MapWorldMapSize,
  scale = 1
): MapWorldMapTransform {
  return {
    x: (viewport.width - world.width * scale) / 2,
    y: (viewport.height - world.height * scale) / 2,
    scale
  }
}

export function fitMapWorldMap(
  viewport: MapWorldMapSize,
  world: MapWorldMapSize,
  padding = 32
): MapWorldMapTransform {
  const availableWidth = Math.max(1, viewport.width - padding * 2)
  const availableHeight = Math.max(1, viewport.height - padding * 2)
  const scale = clampMapWorldMapScale(
    Math.min(availableWidth / world.width, availableHeight / world.height)
  )
  return centerMapWorldMap(viewport, world, scale)
}

export function zoomMapWorldMapAt(
  transform: MapWorldMapTransform,
  requestedScale: number,
  anchor: MapWorldMapPoint
): MapWorldMapTransform {
  const scale = clampMapWorldMapScale(requestedScale)
  const worldX = (anchor.x - transform.x) / transform.scale
  const worldY = (anchor.y - transform.y) / transform.scale

  return {
    x: anchor.x - worldX * scale,
    y: anchor.y - worldY * scale,
    scale
  }
}

export function constrainMapWorldMapPan(
  transform: MapWorldMapTransform,
  viewport: MapWorldMapSize,
  world: MapWorldMapSize,
  visibleEdge = 64
): MapWorldMapTransform {
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
