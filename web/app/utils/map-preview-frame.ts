export interface MapPreviewBounds {
  minimum: { x: number; y: number; z: number }
  maximum: { x: number; y: number; z: number }
}

export interface MapPreviewFrame {
  center: { x: number; y: number; z: number }
  camera: { x: number; y: number; z: number }
  extent: number
  maxZ: number
  up: { x: 0; y: 0; z: -1 }
}

export function calculateMapPreviewFrame(
  bounds: MapPreviewBounds
): MapPreviewFrame {
  const center = {
    x: (bounds.minimum.x + bounds.maximum.x) / 2,
    y: (bounds.minimum.y + bounds.maximum.y) / 2,
    z: (bounds.minimum.z + bounds.maximum.z) / 2
  }
  const extent =
    Math.max(
      bounds.maximum.x - bounds.minimum.x,
      bounds.maximum.z - bounds.minimum.z,
      1
    ) * 1.04
  const elevation = Math.max(bounds.maximum.y - bounds.minimum.y, extent, 1)
  return {
    center,
    camera: {
      x: center.x,
      y: bounds.maximum.y + elevation,
      z: center.z
    },
    extent,
    maxZ: elevation * 3 + (bounds.maximum.y - bounds.minimum.y),
    up: { x: 0, y: 0, z: -1 }
  }
}
