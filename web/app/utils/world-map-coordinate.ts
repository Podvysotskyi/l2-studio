const worldMapTileSize = 32768
const worldMapTileXOffset = 20
const worldMapTileYOffset = 18

export function worldMapTileName(x: number, y: number) {
  return `${Math.floor(x / worldMapTileSize) + worldMapTileXOffset}_${Math.floor(y / worldMapTileSize) + worldMapTileYOffset}`
}
