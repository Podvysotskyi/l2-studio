import {
  AmbientLight,
  Box3,
  DoubleSide,
  DirectionalLight,
  Mesh,
  MeshStandardMaterial,
  PerspectiveCamera,
  Scene,
  Sphere,
  SRGBColorSpace,
  Timer,
  Vector3,
  WebGLRenderer,
  type Material,
  type MeshStandardMaterialParameters,
  type Object3D
} from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { loadPublishedGltf } from '../core/published-gltf.js'
import {
  studioPreviewBackgroundColor,
  studioPreviewBackgrounds,
  type StudioPreviewBackground
} from './studio-preview-background.js'
import {
  prepareStaticMeshMaterials,
  type StaticMeshMaterialBehavior,
  type StaticMeshMaterialInspection,
  type StaticMeshMaterialPreparation,
  type StaticMeshTextureRole
} from '../materials/static-mesh-material.js'

export const studioStaticMeshPreviewBackgrounds = studioPreviewBackgrounds

export type StudioStaticMeshPreviewBackground = StudioPreviewBackground

export const studioStaticMeshMaterialOptions = {
  color: 0xaab7c8,
  roughness: 0.9,
  metalness: 0,
  side: DoubleSide
} satisfies MeshStandardMaterialParameters

export const studioStaticMeshBackFaceBrightness = 0.82

const colorFragmentInclude = '#include <color_fragment>'

export function applyStudioStaticMeshBackFaceTint(shader: {
  fragmentShader: string
}) {
  if (!shader.fragmentShader.includes(colorFragmentInclude))
    throw new Error('Three.js static-mesh shader has no color fragment hook.')
  shader.fragmentShader = shader.fragmentShader.replace(
    colorFragmentInclude,
    `${colorFragmentInclude}\nif (!gl_FrontFacing) diffuseColor.rgb *= ${studioStaticMeshBackFaceBrightness};`
  )
}

export class StudioStaticMeshRenderer {
  private readonly renderer: WebGLRenderer
  private readonly scene = new Scene()
  private readonly camera = new PerspectiveCamera(45, 1, 0.01, 1_000_000)
  private readonly controls: OrbitControls
  private readonly material = createStudioStaticMeshMaterial()
  private readonly timer = new Timer()
  private object?: Object3D
  private materials?: StaticMeshMaterialPreparation
  private loadVersion = 0

  constructor(private readonly canvas: HTMLCanvasElement) {
    this.timer.connect(document)
    this.renderer = new WebGLRenderer({ canvas, antialias: true })
    this.renderer.outputColorSpace = SRGBColorSpace
    this.renderer.setClearColor(studioPreviewBackgroundColor('dark'), 1)
    this.renderer.setPixelRatio(window.devicePixelRatio)
    this.scene.add(new AmbientLight(0xffffff, 1.4))
    const light = new DirectionalLight(0xffffff, 2)
    light.position.set(0.4, 1, 0.6)
    this.scene.add(light)
    this.controls = new OrbitControls(this.camera, canvas)
    this.controls.enableDamping = true
    this.controls.zoomToCursor = true
    this.renderer.setAnimationLoop(timestamp => {
      this.timer.update(timestamp)
      this.controls.update()
      this.materials?.update(this.timer.getElapsed())
      this.renderer.render(this.scene, this.camera)
    })
    this.resize()
  }

  resize() {
    const width = Math.max(this.canvas.clientWidth || 1, 1)
    const height = Math.max(this.canvas.clientHeight || 1, 1)
    this.renderer.setSize(width, height, false)
    this.camera.aspect = width / height
    this.camera.updateProjectionMatrix()
  }

  setBackground(background: StudioStaticMeshPreviewBackground) {
    this.renderer.setClearColor(studioPreviewBackgroundColor(background), 1)
  }

  materialInspections(): StaticMeshMaterialInspection[] {
    return this.materials?.materials ?? []
  }

  setMaterialEnabled(id: string, enabled: boolean) {
    return this.materials?.setMaterialEnabled(id, enabled) ?? []
  }

  setTextureEnabled(id: string, role: StaticMeshTextureRole, enabled: boolean) {
    return this.materials?.setTextureEnabled(id, role, enabled) ?? []
  }

  setBehaviorEnabled(
    id: string,
    behavior: StaticMeshMaterialBehavior,
    enabled: boolean
  ) {
    return this.materials?.setBehaviorEnabled(id, behavior, enabled) ?? []
  }

  resetMaterialInspections() {
    return this.materials?.reset() ?? []
  }

  async load(url: string): Promise<string[]> {
    const version = ++this.loadVersion
    this.removeObject()
    const object = await loadPublishedGltf(url)
    if (version !== this.loadVersion) {
      disposeObject(object, true)
      return []
    }
    const materials = await prepareStaticMeshMaterials(object, this.material, url)
    if (version !== this.loadVersion) {
      materials.dispose()
      disposeObject(object, true)
      return []
    }
    this.materials = materials
    this.object = object
    this.scene.add(object)
    const bounds = new Box3().setFromObject(object, true)
    if (bounds.isEmpty()) throw new Error('The mesh contains no renderable geometry.')
    const center = bounds.getCenter(new Vector3())
    const sphere = bounds.getBoundingSphere(new Sphere())
    const distance = Math.max(
      sphere.radius / Math.sin((this.camera.fov * Math.PI) / 360) * 1.2,
      1
    )
    this.controls.target.copy(center)
    this.camera.position.copy(center.clone().add(new Vector3(distance, distance * 0.7, distance)))
    this.camera.near = Math.max(distance / 10_000, 0.01)
    this.camera.far = Math.max(distance * 20, 100)
    this.camera.updateProjectionMatrix()
    this.controls.update()
    return materials.warnings
  }

  private removeObject() {
    if (!this.object) return
    this.scene.remove(this.object)
    this.materials?.dispose()
    this.materials = undefined
    disposeObject(this.object, false)
    this.object = undefined
  }

  dispose() {
    this.loadVersion++
    this.renderer.setAnimationLoop(null)
    this.timer.dispose()
    this.controls.dispose()
    this.removeObject()
    this.material.dispose()
    this.renderer.dispose()
  }
}

function createStudioStaticMeshMaterial() {
  const material = new MeshStandardMaterial(studioStaticMeshMaterialOptions)
  material.onBeforeCompile = applyStudioStaticMeshBackFaceTint
  material.customProgramCacheKey = () =>
    `studio-static-mesh-back-face-${studioStaticMeshBackFaceBrightness}`
  return material
}

function disposeObject(root: Object3D, disposeMaterials: boolean) {
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    object.geometry.dispose()
    if (!disposeMaterials) return
    const materials = Array.isArray(object.material)
      ? object.material
      : [object.material]
    materials.forEach(disposeMaterial)
  })
}

function disposeMaterial(material: Material) {
  for (const value of Object.values(material)) {
    if (value && typeof value === 'object' && 'isTexture' in value)
      (value as { dispose(): void }).dispose()
  }
  material.dispose()
}
