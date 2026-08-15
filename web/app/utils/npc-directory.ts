import { directoryRouteQuery, directoryRouteState } from './directory'
import type { NpcVisualFilter } from '../types/models/content-directory'

export const npcRaceNoneValue = '__no_race__'

export interface NpcDirectoryRouteState {
  query: string
  page: number
  pageSize: number
  npcTypeName?: string
  npcRaceName?: string
  npcSexName?: string
  visualFilter?: NpcVisualFilter
}

export function npcDirectoryRouteState(query: Record<string, unknown>): NpcDirectoryRouteState {
  const directory = directoryRouteState(query)
  return {
    ...directory,
    npcTypeName: queryValue(query.npcTypeName),
    npcRaceName: query.withoutRace === 'true'
      ? npcRaceNoneValue
      : queryValue(query.npcRaceName),
    npcSexName: queryValue(query.npcSexName),
    visualFilter: visualFilter(query.hasVisuals)
  }
}

export function npcDirectoryRouteQuery(state: NpcDirectoryRouteState) {
  return {
    ...directoryRouteQuery(state.query, state.page, state.pageSize),
    ...(state.npcTypeName ? { npcTypeName: state.npcTypeName } : {}),
    ...(state.npcRaceName && state.npcRaceName !== npcRaceNoneValue
      ? { npcRaceName: state.npcRaceName }
      : {}),
    ...(state.npcRaceName === npcRaceNoneValue ? { withoutRace: 'true' } : {}),
    ...(state.npcSexName ? { npcSexName: state.npcSexName } : {}),
    ...(state.visualFilter ? { hasVisuals: state.visualFilter } : {})
  }
}

function queryValue(value: unknown): string | undefined {
  return typeof value === 'string' && value ? value : undefined
}

function visualFilter(value: unknown): NpcVisualFilter | undefined {
  return value === 'with' || value === 'without' ? value : undefined
}
