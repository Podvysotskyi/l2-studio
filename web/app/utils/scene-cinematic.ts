import type {
  LevelRotation,
  LevelVector,
  SceneManifest,
  SceneObjectManifestEntry
} from '~/types/studio'

export interface ScenePose {
  location: LevelVector
  rotation: LevelRotation
}

export function scenePlaybackFrames(
  manifest: SceneManifest,
  managerName?: string
): SceneObjectManifestEntry[] {
  const manager = manifest.sceneManagers.find(
    (item) => item.name === managerName
  )
  const actionNames = manager?.properties.Actions?.split(',').filter(Boolean)
  if (actionNames?.length) {
    const pointsByName = objectLookup(manifest.interpolationPoints)
    const actionsByName = objectLookup(manifest.actions)
    return actionNames.flatMap((name, index) => {
      const action =
        actionsByName.get(name) ?? actionsByName.get(shortName(name))
      const target = action?.target
        ? (pointsByName.get(action.target) ??
          pointsByName.get(shortName(action.target)))
        : undefined
      return action && target
        ? [
            {
              ...target,
              order: index,
              name: action.name,
              className: action.className,
              duration: action.duration,
              target: action.target,
              properties: action.properties
            }
          ]
        : []
    })
  }

  return [...manifest.cameras, ...manifest.interpolationPoints].sort(
    (left, right) => left.order - right.order
  )
}

export function sceneManagerLabel(manager: SceneObjectManifestEntry): string {
  return manager.properties.Tag || manager.name
}

function shortName(name: string): string {
  return name.split('.').at(-1) ?? name
}

function objectLookup(objects: SceneObjectManifestEntry[]) {
  return new Map(
    objects.flatMap((item) => [
      [item.name, item] as const,
      [shortName(item.name), item] as const
    ])
  )
}

export function interpolateScenePose(
  from: SceneObjectManifestEntry,
  to: SceneObjectManifestEntry,
  amount: number
): ScenePose {
  const t = Math.min(Math.max(amount, 0), 1)
  return {
    location: {
      x: from.location.x + (to.location.x - from.location.x) * t,
      y: from.location.y + (to.location.y - from.location.y) * t,
      z: from.location.z + (to.location.z - from.location.z) * t
    },
    rotation: {
      pitch: Math.round(
        from.rotation.pitch + (to.rotation.pitch - from.rotation.pitch) * t
      ),
      yaw: Math.round(
        from.rotation.yaw + (to.rotation.yaw - from.rotation.yaw) * t
      ),
      roll: Math.round(
        from.rotation.roll + (to.rotation.roll - from.rotation.roll) * t
      )
    }
  }
}
