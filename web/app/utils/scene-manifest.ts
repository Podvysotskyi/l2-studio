export const sceneManifestSchemaVersion = 13

export function isSupportedSceneManifestSchema(schemaVersion: number) {
  return schemaVersion === sceneManifestSchemaVersion
}
