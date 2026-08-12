import { describe, expect, it } from 'vitest'
import {
  studioNavigation,
  studioRouteTitle
} from '../../app/utils/studio-navigation'

describe('Studio navigation', () => {
  it('uses maps as the canonical map-asset route and terminology', () => {
    const navigation = JSON.stringify(studioNavigation)

    expect(navigation).toContain('"label":"Maps"')
    expect(navigation).toContain('"to":"/assets/maps"')
    expect(navigation).not.toContain('/assets/levels')
    expect(studioRouteTitle('/assets/maps')).toBe('Maps')
    expect(studioRouteTitle('/assets/maps/17_25')).toBe('Map')
  })
})
