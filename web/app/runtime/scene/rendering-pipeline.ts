import {
  AbstractMesh,
  Constants,
  Material,
  Scene,
  StandardMaterial,
  type LensFlareSystem,
  type ParticleSystem
} from '@babylonjs/core'

export const SKY_PORTAL_RENDERING_GROUP_ID = 0
export const SKY_ZONE_RENDERING_GROUP_ID = 1
export const LEVEL_GEOMETRY_RENDERING_GROUP_ID = 2
export const SKY_PORTAL_STENCIL_REFERENCE = 1

export function configureManifestRenderingPipeline(scene: Scene) {
  scene.setRenderingAutoClearDepthStencil(SKY_ZONE_RENDERING_GROUP_ID, false)
  scene.setRenderingAutoClearDepthStencil(
    LEVEL_GEOMETRY_RENDERING_GROUP_ID,
    true,
    true,
    false
  )
}

export function createSkyPortalMaterial(scene: Scene) {
  const material = new StandardMaterial('sky-portal-mask', scene)
  material.disableColorWrite = true
  material.disableDepthWrite = true
  material.depthFunction = Constants.ALWAYS
  material.backFaceCulling = false
  material.stencil.enabled = true
  material.stencil.func = Constants.ALWAYS
  material.stencil.funcRef = SKY_PORTAL_STENCIL_REFERENCE
  material.stencil.funcMask = 0xff
  material.stencil.mask = 0xff
  material.stencil.opStencilFail = Constants.KEEP
  material.stencil.opDepthFail = Constants.REPLACE
  material.stencil.opStencilDepthPass = Constants.REPLACE
  material.stencil.backFunc = Constants.ALWAYS
  material.stencil.backOpStencilFail = Constants.KEEP
  material.stencil.backOpDepthFail = Constants.REPLACE
  material.stencil.backOpStencilDepthPass = Constants.REPLACE
  return material
}

export function configureSkyMaterial(
  material: Material,
  portalClipped: boolean
) {
  material.disableDepthWrite = true
  material.stencil.enabled = portalClipped
  if (!portalClipped) return
  material.stencil.func = Constants.EQUAL
  material.stencil.funcRef = SKY_PORTAL_STENCIL_REFERENCE
  material.stencil.funcMask = 0xff
  material.stencil.mask = 0
  material.stencil.opStencilFail = Constants.KEEP
  material.stencil.opDepthFail = Constants.KEEP
  material.stencil.opStencilDepthPass = Constants.KEEP
  material.stencil.backFunc = Constants.EQUAL
  material.stencil.backOpStencilFail = Constants.KEEP
  material.stencil.backOpDepthFail = Constants.KEEP
  material.stencil.backOpStencilDepthPass = Constants.KEEP
}

export function configureSkyMesh(mesh: AbstractMesh, portalClipped: boolean) {
  mesh.renderingGroupId = SKY_ZONE_RENDERING_GROUP_ID
  mesh.alwaysSelectAsActiveMesh = true
  if (mesh.material) configureSkyMaterial(mesh.material, portalClipped)
}

export function configurePortalMesh(mesh: AbstractMesh, material: Material) {
  mesh.renderingGroupId = SKY_PORTAL_RENDERING_GROUP_ID
  mesh.material = material
  mesh.alwaysSelectAsActiveMesh = true
  mesh.checkCollisions = false
  mesh.isPickable = false
}

export function configureWorldMesh(mesh: AbstractMesh) {
  mesh.renderingGroupId = LEVEL_GEOMETRY_RENDERING_GROUP_ID
}

export function configureWorldParticles(system: ParticleSystem) {
  system.renderingGroupId = LEVEL_GEOMETRY_RENDERING_GROUP_ID
}

export function configurePortalLensFlare(system: LensFlareSystem) {
  const render = system.render.bind(system)
  system.render = () => {
    const engine = system.getScene().getEngine()
    engine.cacheStencilState()
    engine.setStencilBuffer(true)
    engine.setStencilMask(0)
    engine.setStencilFunction(Constants.EQUAL)
    engine.setStencilFunctionReference(SKY_PORTAL_STENCIL_REFERENCE)
    engine.setStencilFunctionMask(0xff)
    engine.setStencilOperationFail(Constants.KEEP)
    engine.setStencilOperationDepthFail(Constants.KEEP)
    engine.setStencilOperationPass(Constants.KEEP)
    try {
      return render()
    } finally {
      engine.restoreStencilState()
    }
  }
}
