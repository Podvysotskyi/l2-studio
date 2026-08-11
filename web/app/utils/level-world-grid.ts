import type { LevelCatalogEntry } from '~/types/studio'

export interface LevelWorldCoordinate {
  x: number
  y: number
}

export interface LevelWorldCell extends LevelWorldCoordinate {
  key: string
  level?: LevelCatalogEntry
}

export interface LevelWorldGrid {
  minX: number
  maxX: number
  minY: number
  maxY: number
  width: number
  height: number
  cells: LevelWorldCell[]
  unpositioned: LevelCatalogEntry[]
}

export function parseLevelWorldCoordinate(
  name: string
): LevelWorldCoordinate | undefined {
  const match = /^(-?\d+)_(-?\d+)$/.exec(name)
  if (!match) return undefined

  return {
    x: Number.parseInt(match[1]!, 10),
    y: Number.parseInt(match[2]!, 10)
  }
}

export function buildLevelWorldGrid(
  levels: LevelCatalogEntry[]
): LevelWorldGrid {
  const positioned = levels.flatMap((level) => {
    const coordinate = parseLevelWorldCoordinate(level.name)
    return coordinate ? [{ level, ...coordinate }] : []
  })
  const unpositioned = levels.filter(
    (level) => !parseLevelWorldCoordinate(level.name)
  )

  if (!positioned.length) {
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

  const minX = Math.min(...positioned.map(({ x }) => x))
  const maxX = Math.max(...positioned.map(({ x }) => x))
  const minY = Math.min(...positioned.map(({ y }) => y))
  const maxY = Math.max(...positioned.map(({ y }) => y))
  const byCoordinate = new Map(
    positioned.map((entry) => [`${entry.x}_${entry.y}`, entry.level])
  )
  const cells: LevelWorldCell[] = []

  for (let y = minY; y <= maxY; y += 1) {
    for (let x = minX; x <= maxX; x += 1) {
      const key = `${x}_${y}`
      cells.push({ key, x, y, level: byCoordinate.get(key) })
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
