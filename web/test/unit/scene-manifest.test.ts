import { describe, expect, it } from 'vitest'
import {
  isSupportedSceneManifestSchema,
  sceneManifestSchemaVersion
} from '../../app/utils/scene-manifest'

describe('scene manifest schema', () => {
  it('accepts the current published scene manifest schema only', () => {
    expect(sceneManifestSchemaVersion).toBe(12)
    expect(isSupportedSceneManifestSchema(12)).toBe(true)
    expect(isSupportedSceneManifestSchema(11)).toBe(false)
    expect(isSupportedSceneManifestSchema(13)).toBe(false)
  })
})
