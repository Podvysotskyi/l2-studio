import { describe, expect, it } from 'vitest'
import {
  isSupportedSceneManifestSchema,
  sceneManifestSchemaVersion
} from '../../app/utils/scene-manifest'

describe('scene manifest schema', () => {
  it('accepts the current published scene manifest schema only', () => {
    expect(sceneManifestSchemaVersion).toBe(13)
    expect(isSupportedSceneManifestSchema(13)).toBe(true)
    expect(isSupportedSceneManifestSchema(12)).toBe(false)
    expect(isSupportedSceneManifestSchema(14)).toBe(false)
  })
})
