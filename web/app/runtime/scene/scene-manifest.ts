import '@babylonjs/loaders/glTF/index.js'
import type {
  LevelActorManifestEntry,
  LevelLightManifestEntry,
  LevelManifest,
  LevelRotation,
  LevelVector,
  SceneManifest,
  SceneObjectManifestEntry
} from '~/types/studio'
import {
  AbstractMesh,
  AssetContainer,
  Color3,
  DirectionalLight,
  LoadAssetContainerAsync,
  type Material,
  PointLight,
  Scene,
  TransformNode,
  Vector3,
  type Light,
  type Observer
} from '@babylonjs/core'
import {
  unrealForward,
  unrealNodeTransform,
  unrealVector
} from '../core/unreal-transform.js'
import { applyL2MaterialMetadata } from '../materials/material-metadata.js'
import {
  createTerrainMaterial,
  type TerrainMaterialController
} from '../materials/terrain-material.js'
import { applyVertexLighting } from '../materials/vertex-lighting.js'
import { applyLevelEnvironment } from './environment.js'
import {
  composeParticleEffects,
  type ComposedParticleEffects
} from '../effects/particle-effects.js'
import {
  composeAmbientSounds,
  type ComposedAmbientSounds
} from '../effects/ambient-sounds.js'
import {
  composeAuthoredEffects,
  type ComposedAuthoredEffects
} from '../effects/authored-effects.js'
import {
  configureManifestRenderingPipeline,
  configurePortalMesh,
  configureSkyMesh,
  configureWorldMesh,
  createSkyPortalMaterial
} from './rendering-pipeline.js'
export {
  LEVEL_GEOMETRY_RENDERING_GROUP_ID,
  SKY_PORTAL_RENDERING_GROUP_ID,
  SKY_ZONE_RENDERING_GROUP_ID
} from './rendering-pipeline.js'

export interface ManifestLoadProgress {
  loaded: number
  total: number
}

export interface ComposeManifestOptions {
  batchSize?: number
  includeSkyZoneBsp?: boolean
  includeWaterSurfaceBsp?: boolean
  includeWorldBaseBsp?: boolean
  signal?: AbortSignal
  onProgress?: (progress: ManifestLoadProgress) => void
  onMaterialError?: (message: string) => void
}

export interface ComposedManifestScene {
  actorMeshes: Map<string, AbstractMesh[]>
  terrainMeshes: AbstractMesh[]
  bspMeshes: AbstractMesh[]
  waterSurfaceMeshes: AbstractMesh[]
  skyZoneMeshes: AbstractMesh[]
  worldBaseBspMeshes: AbstractMesh[]
  skyBackdropMeshes: AbstractMesh[]
  terrainControllers: Map<string, TerrainMaterialController>
  particleEffects: ComposedParticleEffects | null
  ambientSounds: ComposedAmbientSounds | null
  authoredEffects: ComposedAuthoredEffects | null
  dispose(): void
}

export interface ScenePose {
  location: LevelVector
  rotation: LevelRotation
}

function throwIfAborted(signal?: AbortSignal) {
  if (signal?.aborted)
    throw new DOMException('Scene loading was aborted.', 'AbortError')
}

function instanceMeshes(rootNodes: TransformNode[]) {
  return [
    ...new Set(
      rootNodes.flatMap((root) => [
        ...(root instanceof AbstractMesh && root.getTotalVertices() > 0
          ? [root]
          : []),
        ...root.getChildMeshes(false)
      ])
    )
  ]
}

function place(
  node: TransformNode,
  location: LevelVector,
  rotation: LevelRotation,
  drawScale = 1,
  drawScale3D: LevelVector = { x: 1, y: 1, z: 1 },
  prePivot: LevelVector = { x: 0, y: 0, z: 0 }
) {
  const transform = unrealNodeTransform(
    location,
    rotation,
    drawScale,
    drawScale3D,
    prePivot
  )
  node.position.copyFrom(transform.position)
  node.rotationQuaternion = transform.rotation
  node.scaling.copyFrom(transform.scaling)
}

export async function composeLevelManifest(
  scene: Scene,
  manifest: LevelManifest | SceneManifest,
  options: ComposeManifestOptions = {}
): Promise<ComposedManifestScene> {
  assertManifestSchema(manifest)
  applyLevelEnvironment(scene, manifest.environment)
  configureManifestRenderingPipeline(scene)
  const containers = new Map<string, Promise<AssetContainer>>()
  const placements: TransformNode[] = []
  const lights: Light[] = []
  const terrainMeshes: AbstractMesh[] = []
  const bspMeshes: AbstractMesh[] = []
  const waterSurfaceMeshes: AbstractMesh[] = []
  const skyZoneMeshes: AbstractMesh[] = []
  const worldBaseBspMeshes: AbstractMesh[] = []
  const skyBackdropMeshes: AbstractMesh[] = []
  const terrainControllers = new Map<string, TerrainMaterialController>()
  const ownedMaterials: Material[] = []
  const actorMeshes = new Map<string, AbstractMesh[]>()
  let particleEffects: ComposedParticleEffects | null = null
  let ambientSounds: ComposedAmbientSounds | null = null
  let authoredEffects: ComposedAuthoredEffects | null = null
  const actors = manifest.actors.filter(
    (actor): actor is LevelActorManifestEntry & { meshUrl: string } =>
      Boolean(actor.meshUrl)
  )
  const activeSkyZone = [...manifest.skyZones]
    .filter((zone) =>
      manifest.bspMeshes.some(
        (bsp) => bsp.role === 'sky-zone' && bsp.skyZone === zone.name
      )
    )
    .sort((a, b) => b.order - a.order)[0]
  const portalClipped =
    'skyBackdrops' in manifest &&
    Boolean(activeSkyZone) &&
    manifest.skyBackdrops.some((backdrop) => backdrop.meshUrl)
  const eligibleBspEntries = manifest.bspMeshes.filter(
    (bsp) =>
      (bsp.role !== 'world-base' || options.includeWorldBaseBsp !== false) &&
      (bsp.role !== 'water-surface' ||
        options.includeWaterSurfaceBsp !== false) &&
      (bsp.role !== 'sky-zone' ||
        (options.includeSkyZoneBsp !== false &&
          bsp.skyZone === activeSkyZone?.name))
  )
  const bspEntries = eligibleBspEntries.filter((bsp) => bsp.meshUrl)
  const total =
    manifest.terrains.filter((terrain) => terrain.meshUrl).length +
    actors.length +
    bspEntries.length +
    ('skyBackdrops' in manifest
      ? manifest.skyBackdrops.filter((backdrop) => backdrop.meshUrl).length
      : 0)
  let loaded = 0
  let skyObserver: Observer<Scene> | null = null

  const containerFor = (url: string) => {
    const existing = containers.get(url)
    if (existing) return existing
    const rootRelative = url.startsWith('/')
    const pending = LoadAssetContainerAsync(
      rootRelative ? url.slice(1) : url,
      scene,
      {
        rootUrl: rootRelative ? '/' : undefined,
        pluginExtension: '.glb'
      }
    ).then((container) => {
      applyL2MaterialMetadata(container, scene)
      return container
    })
    containers.set(url, pending)
    return pending
  }

  const reportLoaded = () => {
    loaded++
    options.onProgress?.({ loaded, total })
  }

  try {
    options.onProgress?.({ loaded, total })
    {
      const skyPlacement = activeSkyZone
        ? new TransformNode(`${activeSkyZone.name}:sky-zone-placement`, scene)
        : undefined
      const renderedSkyZone = activeSkyZone
      if (
        skyPlacement &&
        renderedSkyZone &&
        options.includeSkyZoneBsp !== false
      ) {
        placements.push(skyPlacement)
        const updateSkyPlacement = () => {
          const camera = scene.activeCamera
          if (!camera) return
          skyPlacement.position.copyFrom(camera.globalPosition)
          skyPlacement.position.subtractInPlace(
            unrealVector(renderedSkyZone.location)
          )
        }
        skyObserver = scene.onBeforeRenderObservable.add(updateSkyPlacement)
        updateSkyPlacement()
      }
      for (const bsp of eligibleBspEntries) {
        if (bsp.error && !bsp.meshUrl)
          options.onMaterialError?.(`${bsp.name}: ${bsp.error}`)
        if (!bsp.meshUrl) continue
        throwIfAborted(options.signal)
        const container = await containerFor(bsp.meshUrl)
        throwIfAborted(options.signal)
        const instance = container.instantiateModelsToScene(
          (name) => `${bsp.name}:${name}`,
          false
        )
        const placement =
          bsp.role === 'sky-zone' && skyPlacement
            ? skyPlacement
            : new TransformNode(`${bsp.name}:placement`, scene)
        for (const root of instance.rootNodes) root.parent = placement
        if (placement !== skyPlacement) placements.push(placement)
        const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
        for (const mesh of meshes) {
          mesh.checkCollisions = false
          mesh.isPickable = false
          if (bsp.role === 'sky-zone') {
            configureSkyMesh(mesh, portalClipped)
          } else configureWorldMesh(mesh)
        }
        if (bsp.role === 'sky-zone') skyZoneMeshes.push(...meshes)
        else if (bsp.role === 'water-surface')
          waterSurfaceMeshes.push(...meshes)
        else if (bsp.role === 'world-base') worldBaseBspMeshes.push(...meshes)
        else bspMeshes.push(...meshes)
        reportLoaded()
      }
    }
    for (const terrain of manifest.terrains) {
      if (!terrain.meshUrl) continue
      throwIfAborted(options.signal)
      const container = await containerFor(terrain.meshUrl)
      throwIfAborted(options.signal)
      const instance = container.instantiateModelsToScene(
        (name) => `${terrain.name}:${name}`,
        false
      )
      const placement = new TransformNode(`${terrain.name}:placement`, scene)
      place(placement, terrain.location, terrain.rotation)
      for (const root of instance.rootNodes) root.parent = placement
      placements.push(placement)
      const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
      for (const mesh of meshes) configureWorldMesh(mesh)
      const terrainMaterial = createTerrainMaterial(terrain, scene)
      if (terrainMaterial.material) {
        try {
          await terrainMaterial.ready
        } catch (error) {
          terrainMaterial.material.dispose(true, true)
          options.onMaterialError?.(
            `${terrain.name}: ${error instanceof Error ? error.message : 'Terrain texture arrays failed to load.'}`
          )
          terrainMeshes.push(...meshes)
          reportLoaded()
          continue
        }
        ownedMaterials.push(terrainMaterial.material)
        for (const mesh of meshes) mesh.material = terrainMaterial.material
        if (terrainMaterial.controller)
          terrainControllers.set(terrain.name, terrainMaterial.controller)
      } else if (terrainMaterial.error) {
        options.onMaterialError?.(`${terrain.name}: ${terrainMaterial.error}`)
      }
      terrainMeshes.push(...meshes)
      reportLoaded()
    }

    const batchSize = Math.max(options.batchSize ?? 12, 1)
    for (let index = 0; index < actors.length; index += batchSize) {
      throwIfAborted(options.signal)
      await Promise.all(
        actors.slice(index, index + batchSize).map(async (actor) => {
          const container = await containerFor(actor.meshUrl)
          throwIfAborted(options.signal)
          const instance = container.instantiateModelsToScene(
            (name) => `${actor.name}:${name}`,
            false
          )
          const placement = new TransformNode(`${actor.name}:placement`, scene)
          place(
            placement,
            actor.location,
            actor.rotation,
            actor.drawScale,
            actor.drawScale3D,
            actor.prePivot
          )
          for (const root of instance.rootNodes) root.parent = placement
          placements.push(placement)
          const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
          for (const mesh of meshes) configureWorldMesh(mesh)
          if (actor.vertexLighting && vertexLightingIsVisible(scene, actor)) {
            for (const mesh of meshes) {
              const material = applyVertexLighting(mesh, actor.vertexLighting)
              if (material && !ownedMaterials.includes(material))
                ownedMaterials.push(material)
            }
          }
          actorMeshes.set(actor.name, meshes)
          reportLoaded()
        })
      )
    }

    for (const light of renderableLights(scene, manifest.lights)) {
      const color = Color3.FromHSV(
        (light.hue / 255) * 360,
        1 - light.saturation / 255,
        1
      )
      if (
        light.className === 'NMovableSunLight' ||
        light.className === 'Sunlight'
      ) {
        const rendered = new DirectionalLight(
          light.name,
          unrealForward(light.rotation),
          scene
        )
        rendered.diffuse = color
        rendered.intensity = light.brightness / 64
        lights.push(rendered)
      } else {
        const rendered = new PointLight(
          light.name,
          unrealVector(light.location),
          scene
        )
        rendered.diffuse = color
        rendered.intensity = light.brightness / 64
        rendered.range = light.radius * 64
        lights.push(rendered)
      }
    }
    if ('skyBackdrops' in manifest) {
      const portalMaterial = createSkyPortalMaterial(scene)
      ownedMaterials.push(portalMaterial)
      for (const backdrop of manifest.skyBackdrops) {
        if (!backdrop.meshUrl) {
          if (backdrop.error)
            options.onMaterialError?.(`${backdrop.name}: ${backdrop.error}`)
          continue
        }
        const container = await containerFor(backdrop.meshUrl)
        throwIfAborted(options.signal)
        const instance = container.instantiateModelsToScene(
          (name) => `${backdrop.name}:${name}`,
          false
        )
        const placement = new TransformNode(`${backdrop.name}:placement`, scene)
        for (const root of instance.rootNodes) root.parent = placement
        placements.push(placement)
        const meshes = instanceMeshes(instance.rootNodes as TransformNode[])
        for (const mesh of meshes) {
          configurePortalMesh(mesh, portalMaterial)
        }
        skyBackdropMeshes.push(...meshes)
        reportLoaded()
      }
    }
    if ('effects' in manifest) {
      particleEffects = composeParticleEffects(scene, manifest.effects)
      particleEffects.diagnostics.forEach((message) =>
        options.onMaterialError?.(message)
      )
      ambientSounds = composeAmbientSounds(scene, manifest.ambientSounds)
      ambientSounds.diagnostics.forEach((message) =>
        options.onMaterialError?.(message)
      )
      authoredEffects = composeAuthoredEffects(
        scene,
        manifest.effects,
        manifest.skyZones,
        { portalClipped }
      )
      authoredEffects.diagnostics.forEach((message) =>
        options.onMaterialError?.(message)
      )
    }
  } catch (error) {
    if (skyObserver) scene.onBeforeRenderObservable.remove(skyObserver)
    placements.forEach((placement) => placement.dispose())
    lights.forEach((light) => light.dispose())
    ownedMaterials.forEach((material) => material.dispose(true, true))
    particleEffects?.dispose()
    ambientSounds?.dispose()
    authoredEffects?.dispose()
    const settled = await Promise.allSettled(containers.values())
    settled.forEach((result) => {
      if (result.status === 'fulfilled') result.value.dispose()
    })
    throw error
  }

  let disposed = false
  return {
    actorMeshes,
    terrainMeshes,
    bspMeshes,
    waterSurfaceMeshes,
    skyZoneMeshes,
    worldBaseBspMeshes,
    skyBackdropMeshes,
    terrainControllers,
    particleEffects,
    ambientSounds,
    authoredEffects,
    dispose() {
      if (disposed) return
      disposed = true
      if (skyObserver) scene.onBeforeRenderObservable.remove(skyObserver)
      placements.forEach((placement) => placement.dispose())
      lights.forEach((light) => light.dispose())
      ownedMaterials.forEach((material) => material.dispose(true, true))
      particleEffects?.dispose()
      ambientSounds?.dispose()
      authoredEffects?.dispose()
      void Promise.allSettled(containers.values()).then((settled) => {
        settled.forEach((result) => {
          if (result.status === 'fulfilled') result.value.dispose()
        })
      })
    }
  }
}

function renderableLights(
  scene: Scene,
  lights: LevelLightManifestEntry[]
): LevelLightManifestEntry[] {
  const cameraPosition = scene.activeCamera?.position
  return [...lights]
    .sort((a, b) => {
      const aSun =
        a.className === 'NMovableSunLight' || a.className === 'Sunlight'
      const bSun =
        b.className === 'NMovableSunLight' || b.className === 'Sunlight'
      if (aSun !== bSun) return aSun ? -1 : 1
      if (!cameraPosition) return b.brightness - a.brightness
      const aDistance = Vector3.DistanceSquared(
        unrealVector(a.location),
        cameraPosition
      )
      const bDistance = Vector3.DistanceSquared(
        unrealVector(b.location),
        cameraPosition
      )
      return (
        b.brightness / Math.max(bDistance, 1) -
        a.brightness / Math.max(aDistance, 1)
      )
    })
    .slice(0, 4)
}

function vertexLightingIsVisible(scene: Scene, actor: LevelActorManifestEntry) {
  if (!scene.activeCamera || scene.fogMode === Scene.FOGMODE_NONE) return true
  const lightingDistance = Math.max((scene.fogEnd - scene.fogStart) / 2, 1)
  return (
    Vector3.DistanceSquared(
      unrealVector(actor.location),
      scene.activeCamera.position
    ) <=
    lightingDistance * lightingDistance
  )
}

function assertManifestSchema(manifest: LevelManifest | SceneManifest) {
  const isScene = 'cameras' in manifest
  const expected = isScene ? 11 : 12
  if (manifest.schemaVersion !== expected)
    throw new Error(
      `${isScene ? 'Scene' : 'Level'} manifest schema ${manifest.schemaVersion} is unsupported; expected ${expected}.`
    )
}

function shortName(name: string) {
  return name.split('.').at(-1) ?? name
}

function lookup(objects: SceneObjectManifestEntry[]) {
  return new Map(
    objects.flatMap((item) => [
      [item.name, item] as const,
      [shortName(item.name), item] as const
    ])
  )
}

export function resolveSceneManagerFrames(
  manifest: SceneManifest,
  tag: string
): SceneObjectManifestEntry[] {
  const manager = manifest.sceneManagers.find(
    (candidate) => candidate.properties.Tag === tag
  )
  const actionNames = manager?.properties.Actions?.split(',').filter(Boolean)
  if (!actionNames?.length) return []

  const actions = lookup(manifest.actions)
  const points = lookup(manifest.interpolationPoints)
  return actionNames.flatMap((name) => {
    const action = actions.get(name) ?? actions.get(shortName(name))
    const target = action?.target
      ? (points.get(action.target) ?? points.get(shortName(action.target)))
      : undefined
    return action && target ? [{ ...target, className: action.className }] : []
  })
}
