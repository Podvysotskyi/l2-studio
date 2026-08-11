import type { SceneObjectManifestEntry, SkyZoneManifestEntry } from '~/types/studio'
import {
  Color3,
  Constants,
  CreateDecal,
  CreatePlane,
  LensFlare,
  LensFlareSystem,
  Mesh,
  Ray,
  Scene,
  StandardMaterial,
  Texture,
  Vector3
} from '@babylonjs/core'
import { unrealForward, unrealVector } from '../core/unreal-transform.js'
import { browserDecodedTextureUrl } from '../core/texture-url.js'
import {
  configureSkyMaterial,
  configureSkyMesh,
  configurePortalLensFlare,
  configureWorldMesh
} from '../scene/rendering-pipeline.js'

const sunTexture =
  'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMjgiIGhlaWdodD0iMTI4Ij48ZGVmcz48cmFkaWFsR3JhZGllbnQgaWQ9ImciPjxzdG9wIG9mZnNldD0iMCIgc3RvcC1jb2xvcj0id2hpdGUiLz48c3RvcCBvZmZzZXQ9Ii4zNSIgc3RvcC1jb2xvcj0iI2ZmZjZiZCIgc3RvcC1vcGFjaXR5PSIuOTUiLz48c3RvcCBvZmZzZXQ9IjEiIHN0b3AtY29sb3I9IiNmZmQ5NzAiIHN0b3Atb3BhY2l0eT0iMCIvPjwvcmFkaWFsR3JhZGllbnQ+PC9kZWZzPjxyZWN0IHdpZHRoPSIxMjgiIGhlaWdodD0iMTI4IiBmaWxsPSJ1cmwoI2cpIi8+PC9zdmc+'
const moonTexture =
  'data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxMjgiIGhlaWdodD0iMTI4Ij48ZGVmcz48cmFkaWFsR3JhZGllbnQgaWQ9ImciPjxzdG9wIG9mZnNldD0iMCIgc3RvcC1jb2xvcj0iI2VlZjVmZiIvPjxzdG9wIG9mZnNldD0iLjU1IiBzdG9wLWNvbG9yPSIjY2RkY2ZmIiBzdG9wLW9wYWNpdHk9Ii45Ii8+PHN0b3Agb2Zmc2V0PSIxIiBzdG9wLWNvbG9yPSIjOWJiY2ZmIiBzdG9wLW9wYWNpdHk9IjAiLz48L3JhZGlhbEdyYWRpZW50PjwvZGVmcz48cmVjdCB3aWR0aD0iMTI4IiBoZWlnaHQ9IjEyOCIgZmlsbD0idXJsKCNnKSIvPjwvc3ZnPg=='

export interface ComposedAuthoredEffects {
  celestialMeshes: Mesh[]
  projectorMeshes: Mesh[]
  lensFlareSystems: LensFlareSystem[]
  diagnostics: string[]
  dispose(): void
}

export interface ComposeAuthoredEffectsOptions {
  portalClipped?: boolean
}

function numberProperty(
  effect: SceneObjectManifestEntry,
  name: string,
  fallback: number
) {
  const value = Number(effect.properties[name])
  return Number.isFinite(value) ? value : fallback
}

function effectMaterial(
  name: string,
  url: string,
  scene: Scene,
  additive: boolean
) {
  const material = new StandardMaterial(name, scene)
  const texture = new Texture(
    browserDecodedTextureUrl(url),
    scene,
    false,
    false
  )
  texture.hasAlpha = true
  material.diffuseTexture = texture
  material.opacityTexture = texture
  material.emissiveTexture = texture
  material.disableLighting = true
  material.useAlphaFromDiffuseTexture = true
  if (additive) material.alphaMode = Constants.ALPHA_ADD
  return material
}

export function composeAuthoredEffects(
  scene: Scene,
  effects: SceneObjectManifestEntry[],
  skyZones: SkyZoneManifestEntry[] = [],
  options: ComposeAuthoredEffectsOptions = {}
): ComposedAuthoredEffects {
  const celestialMeshes: Mesh[] = []
  const projectorMeshes: Mesh[] = []
  const materials: StandardMaterial[] = []
  const lensFlareSystems: LensFlareSystem[] = []
  const diagnostics: string[] = []
  const activeSkyZone = skyZones.reduce<SkyZoneManifestEntry | undefined>(
    (active, candidate) =>
      !active || candidate.order > active.order ? candidate : active,
    undefined
  )

  for (const effect of effects) {
    if (effect.className === 'NSun' || effect.className === 'NMoon') {
      const textureUrl =
        effect.resourceUrl ??
        (effect.className === 'NSun' ? sunTexture : moonTexture)
      const size = Math.max(numberProperty(effect, 'Radius', 256) * 2, 32)
      const mesh = CreatePlane(effect.name, { size }, scene)
      mesh.position.copyFrom(unrealVector(effect.location))
      mesh.billboardMode = Mesh.BILLBOARDMODE_ALL
      mesh.isPickable = false
      const material = effectMaterial(
        `${effect.name}:material`,
        textureUrl,
        scene,
        true
      )
      mesh.material = material
      configureSkyMaterial(material, options.portalClipped === true)
      configureSkyMesh(mesh, options.portalClipped === true)
      celestialMeshes.push(mesh)
      materials.push(material)
      if (effect.className === 'NSun') {
        const system = new LensFlareSystem(`${effect.name}:flare`, mesh, scene)
        system.borderLimit = 80
        system.meshesSelectionPredicate = (candidate) =>
          candidate.isVisible && candidate !== mesh
        if (activeSkyZone?.lensFlares.length) {
          for (const flare of activeSkyZone.lensFlares) {
            if (!flare.textureUrl) {
              diagnostics.push(
                `${activeSkyZone.name}: lens flare ${flare.index} texture is unavailable.`
              )
              continue
            }
            if (!Number.isFinite(flare.scale) || flare.scale <= 0) {
              diagnostics.push(
                `${activeSkyZone.name}: lens flare ${flare.index} scale is invalid.`
              )
              continue
            }
            if (!Number.isFinite(flare.offset)) {
              diagnostics.push(
                `${activeSkyZone.name}: lens flare ${flare.index} offset is invalid.`
              )
              continue
            }
            const size = Math.min(flare.scale * 0.1, 1)
            LensFlare.AddFlare(
              size,
              flare.offset,
              Color3.White(),
              browserDecodedTextureUrl(flare.textureUrl),
              system
            )
          }
        } else {
          LensFlare.AddFlare(0.18, 0, Color3.White(), textureUrl, system)
          LensFlare.AddFlare(
            0.08,
            0.45,
            new Color3(0.65, 0.8, 1),
            textureUrl,
            system
          )
        }
        if (options.portalClipped) configurePortalLensFlare(system)
        lensFlareSystems.push(system)
      }
      continue
    }

    if (effect.className !== 'Projector') continue
    if (effect.properties.bProjectStaticMesh?.toLocaleLowerCase() === 'false')
      continue
    if (!effect.resourceUrl) {
      diagnostics.push(`${effect.name}: projector material is unavailable.`)
      continue
    }
    const origin = unrealVector(effect.location)
    const direction = unrealForward(effect.rotation).normalize()
    const length = Math.max(
      numberProperty(effect, 'MaxTraceDistance', 1_024),
      1
    )
    const pick = scene.pickWithRay(
      new Ray(origin, direction, length),
      (mesh) => mesh.isEnabled() && mesh.isVisible && mesh.isPickable
    )
    if (!pick?.hit || !pick.pickedMesh || !pick.pickedPoint) {
      diagnostics.push(
        `${effect.name}: projector did not hit supported geometry.`
      )
      continue
    }
    const normal = pick.getNormal(true) ?? direction.scale(-1)
    const fov = numberProperty(effect, 'FOV', 45)
    const distance = Vector3.Distance(origin, pick.pickedPoint)
    const size = Math.max(
      Math.tan((fov * Math.PI) / 360) * distance * 2,
      numberProperty(effect, 'DrawScale', 1)
    )
    const decal = CreateDecal(effect.name, pick.pickedMesh, {
      position: pick.pickedPoint,
      normal,
      size: new Vector3(size, size, Math.max(size * 0.2, 1))
    })
    const material = effectMaterial(
      `${effect.name}:material`,
      effect.resourceUrl,
      scene,
      false
    )
    decal.material = material
    decal.isPickable = false
    configureWorldMesh(decal)
    projectorMeshes.push(decal)
    materials.push(material)
  }

  return {
    celestialMeshes,
    projectorMeshes,
    lensFlareSystems,
    diagnostics,
    dispose() {
      lensFlareSystems.forEach((flare) => flare.dispose())
      celestialMeshes.forEach((mesh) => mesh.dispose())
      projectorMeshes.forEach((mesh) => mesh.dispose())
      materials.forEach((material) => material.dispose(true, true))
    }
  }
}
