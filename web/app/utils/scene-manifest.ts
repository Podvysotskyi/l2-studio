export const sceneManifestSchemaVersion = 12

export function isSupportedSceneManifestSchema(schemaVersion: number) {
  return schemaVersion === sceneManifestSchemaVersion
}
