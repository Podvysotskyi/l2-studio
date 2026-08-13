import type { MapCatalogEntry } from '~/types/studio'

export interface MapWorldCoordinate {
  x: number
  y: number
}

export interface MapWorldCell extends MapWorldCoordinate {
  key: string
  map?: MapCatalogEntry
}

export interface MapWorldGrid {
  minX: number
  maxX: number
  minY: number
  maxY: number
  width: number
  height: number
  cells: MapWorldCell[]
  unpositioned: MapCatalogEntry[]
}

export function parseMapWorldCoordinate(
  name: string
): MapWorldCoordinate | undefined {
  const match = /^(-?\d+)_(-?\d+)$/.exec(name)
  if (!match) return undefined

  return {
    x: Number.parseInt(match[1]!, 10),
    y: Number.parseInt(match[2]!, 10)
  }
}

export function buildMapWorldGrid(
  maps: MapCatalogEntry[]
): MapWorldGrid {
  const positioned = maps.flatMap((map) => {
    const coordinate = parseMapWorldCoordinate(map.name)
    return coordinate ? [{ map, ...coordinate }] : []
  })
  const coordinateCounts = new Map<string, number>()
  for (const entry of positioned) {
    const key = `${entry.x}_${entry.y}`
    coordinateCounts.set(key, (coordinateCounts.get(key) ?? 0) + 1)
  }
  const uniquelyPositioned = positioned.filter(
    entry => coordinateCounts.get(`${entry.x}_${entry.y}`) === 1
  )
  const unpositioned = maps.filter((map) => {
    const coordinate = parseMapWorldCoordinate(map.name)
    return !coordinate || coordinateCounts.get(`${coordinate.x}_${coordinate.y}`)! > 1
  })

  if (!uniquelyPositioned.length) {
    return {
      minX: 0,
      maxX: 0,
      minY: 0,
      maxY: 0,
      width: 0,
      height: 0,
      cells: [],
      unpositioned
    }
  }

  const minX = Math.min(...uniquelyPositioned.map(({ x }) => x))
  const maxX = Math.max(...uniquelyPositioned.map(({ x }) => x))
  const minY = Math.min(...uniquelyPositioned.map(({ y }) => y))
  const maxY = Math.max(...uniquelyPositioned.map(({ y }) => y))
  const byCoordinate = new Map(
    uniquelyPositioned.map((entry) => [`${entry.x}_${entry.y}`, entry.map])
  )
  const cells: MapWorldCell[] = []

  for (let y = minY; y <= maxY; y += 1) {
    for (let x = minX; x <= maxX; x += 1) {
      const key = `${x}_${y}`
      cells.push({ key, x, y, map: byCoordinate.get(key) })
    }
  }

  return {
    minX,
    maxX,
    minY,
    maxY,
    width: maxX - minX + 1,
    height: maxY - minY + 1,
    cells,
    unpositioned
  }
}
