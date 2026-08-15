import {
  AmbientLight,
  AnimationClip,
  AnimationMixer,
  Box3,
  DoubleSide,
  DirectionalLight,
  Mesh,
  MeshStandardMaterial,
  PerspectiveCamera,
  Scene,
  Sphere,
  SRGBColorSpace,
  Vector3,
  WebGLRenderer,
  type AnimationAction,
  type Material,
  type MeshStandardMaterialParameters,
  type Object3D
} from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { loadPublishedGltfAsset } from '../core/published-gltf.js'
import {
  studioPreviewBackgroundColor,
  studioPreviewBackgrounds,
  type StudioPreviewBackground
} from './studio-preview-background.js'
import {
  prepareStaticMeshMaterials,
  type PublishedStaticMeshMaterial,
  type StaticMeshMaterialInspection,
  type StaticMeshMaterialPreparation
} from '../materials/static-mesh-material.js'

export interface StudioAnimationState {
  clipNames: string[]
  clipName?: string
  duration: number
  time: number
  playing: boolean
}

export interface StudioAnimationMaterialBinding {
  sectionIndex: number
  name: string
  diffuseUrl?: string | null
  material?: PublishedStaticMeshMaterial | null
}

export const studioAnimationPreviewBackgrounds = studioPreviewBackgrounds

export type StudioAnimationPreviewBackground = StudioPreviewBackground

export function studioAnimationCameraDistance(radius: number, fieldOfView: number) {
  const halfFieldOfView = fieldOfView * Math.PI / 360
  return Math.max(radius / Math.sin(halfFieldOfView) * 1.25, 0.01)
}

export const studioAnimationPreviewMaterialOptions = {
  color: 0xaab7c8,
  roughness: 0.9,
  side: DoubleSide
} satisfies MeshStandardMaterialParameters

export function hasBoundAnimationTrack(root: Object3D, clips: AnimationClip[]) {
  return bindAnimationClips(root, clips).length > 0
}

export function bindAnimationClips(root: Object3D, clips: AnimationClip[]) {
  const names = new Set<string>()
  root.traverse(object => {
    if (object.name) names.add(object.name)
    names.add(object.uuid)
  })
  return clips.map(clip => {
    const tracks = clip.tracks.filter(track => {
      const separator = track.name.lastIndexOf('.')
      return separator > 0 && names.has(track.name.slice(0, separator))
    })
    return tracks.length === clip.tracks.length
      ? clip
      : new AnimationClip(clip.name, clip.duration, tracks, clip.blendMode)
  }).filter(clip => clip.tracks.length > 0)
}

export class StudioAnimationRenderer {
  private readonly renderer: WebGLRenderer
  private readonly scene = new Scene()
  private readonly camera = new PerspectiveCamera(45, 1, 0.01, 1_000_000)
  private readonly controls: OrbitControls
  private readonly fallbackMaterial = new MeshStandardMaterial(studioAnimationPreviewMaterialOptions)
  private object?: Object3D
  private materials?: StaticMeshMaterialPreparation
  private readonly sourceMaterials = new Set<Material>()
  private mixer?: AnimationMixer
  private clips: AnimationClip[] = []
  private action?: AnimationAction
  private lastTimestamp?: number
  private elapsedSeconds = 0
  private loadVersion = 0
  private speed = 1

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly changed: (state: StudioAnimationState) => void
  ) {
    this.renderer = new WebGLRenderer({ canvas, antialias: true })
    this.renderer.outputColorSpace = SRGBColorSpace
    this.renderer.setClearColor(studioPreviewBackgroundColor('dark'), 1)
    this.renderer.setPixelRatio(window.devicePixelRatio)
    this.scene.add(new AmbientLight(0xffffff, 1.5))
    const light = new DirectionalLight(0xffffff, 2.2)
    light.position.set(0.5, 1, 0.7)
    this.scene.add(light)
    this.controls = new OrbitControls(this.camera, canvas)
    this.controls.enableDamping = true
    this.renderer.setAnimationLoop(timestamp => this.render(timestamp))
    this.resize()
  }

  materialInspections(): StaticMeshMaterialInspection[] {
    return this.materials?.materials ?? []
  }

  async load(
    url: string,
    animationUrl?: string | null,
    materialBindings: StudioAnimationMaterialBinding[] = []
  ): Promise<string[]> {
    const version = ++this.loadVersion
    this.removeObject()
    let gltf: Awaited<ReturnType<typeof loadPublishedGltfAsset>>
    let animationGltf: Awaited<ReturnType<typeof loadPublishedGltfAsset>> | undefined
    try {
      [gltf, animationGltf] = await Promise.all([
        loadPublishedGltfAsset(url),
        animationUrl ? loadPublishedGltfAsset(animationUrl) : undefined
      ])
    } catch (error) {
      if (version !== this.loadVersion) return []
      throw error
    }
    if (version !== this.loadVersion) {
      disposeObject(gltf.scene)
      if (animationGltf) disposeObject(animationGltf.scene)
      return []
    }
    const loadedClips = animationGltf?.animations ?? gltf.animations
    const clips = bindAnimationClips(gltf.scene, loadedClips)
    if (loadedClips.length > 0 && clips.length === 0) {
      disposeObject(gltf.scene)
      if (animationGltf) disposeObject(animationGltf.scene)
      throw new Error('The animation clips do not target any bones in this skeletal mesh.')
    }
    const bounds = new Box3().setFromObject(gltf.scene, true)
    if (bounds.isEmpty()) {
      disposeObject(gltf.scene)
      if (animationGltf) disposeObject(animationGltf.scene)
      throw new Error('The animation contains no renderable geometry.')
    }
    this.object = gltf.scene
    this.clips = clips
    const sectionCount = applyAppearanceMaterials(gltf.scene, materialBindings, this.sourceMaterials)
    const materials = await prepareStaticMeshMaterials(gltf.scene, this.fallbackMaterial, url)
    if (version !== this.loadVersion) {
      materials.dispose()
      disposeObject(gltf.scene, false)
      if (animationGltf) disposeObject(animationGltf.scene)
      return []
    }
    this.materials = materials
    this.scene.add(gltf.scene)
    this.mixer = new AnimationMixer(gltf.scene)
    this.lastTimestamp = undefined
    this.elapsedSeconds = 0
    const center = bounds.getCenter(new Vector3())
    const sphere = bounds.getBoundingSphere(new Sphere())
    const distance = studioAnimationCameraDistance(sphere.radius, this.camera.fov)
    this.controls.target.copy(center)
    this.camera.position.copy(center.clone().add(new Vector3(distance, distance * 0.65, distance)))
    this.camera.near = Math.max(distance / 10_000, 0.0001)
    this.camera.far = Math.max(distance * 20, 10)
    this.camera.updateProjectionMatrix()
    if (animationGltf) disposeObject(animationGltf.scene)
    if (this.clips[0]) this.select(this.clips[0].name)
    else this.emit()
    const unassignedBindings = materialBindings.filter(binding => binding.sectionIndex >= sectionCount).length
    return unassignedBindings > 0
      ? [...materials.warnings, `${unassignedBindings} appearance material slot${unassignedBindings === 1 ? '' : 's'} could not be assigned to the skeletal mesh.`]
      : materials.warnings
  }

  select(name: string) {
    const clip = this.clips.find(item => item.name === name)
    if (!clip || !this.mixer) return
    this.action?.stop()
    this.action = this.mixer.clipAction(clip)
    this.action.play()
    this.action.paused = false
    this.emit()
  }

  setPlaying(value: boolean) {
    if (!this.action) return
    this.action.paused = !value
    this.emit()
  }

  seek(time: number) {
    if (!this.action || !this.mixer) return
    this.action.time = Math.max(0, Math.min(time, this.action.getClip().duration))
    this.mixer.update(0)
    this.emit()
  }

  setSpeed(value: number) { this.speed = value }

  setBackground(background: StudioAnimationPreviewBackground) {
    this.renderer.setClearColor(studioPreviewBackgroundColor(background), 1)
  }

  resize() {
    const width = Math.max(this.canvas.clientWidth || 1, 1)
    const height = Math.max(this.canvas.clientHeight || 1, 1)
    this.renderer.setSize(width, height, false)
    this.camera.aspect = width / height
    this.camera.updateProjectionMatrix()
  }

  private render(timestamp: number) {
    const delta = this.lastTimestamp === undefined ? 0 : Math.min((timestamp - this.lastTimestamp) / 1000, 0.1)
    this.lastTimestamp = timestamp
    this.mixer?.update(delta * this.speed)
    this.elapsedSeconds += delta
    this.materials?.update(this.elapsedSeconds)
    this.controls.update()
    this.renderer.render(this.scene, this.camera)
    if (this.action && !this.action.paused) this.emit()
  }

  private emit() {
    this.changed({
      clipNames: this.clips.map(item => item.name),
      clipName: this.action?.getClip().name,
      duration: this.action?.getClip().duration ?? 0,
      time: this.action?.time ?? 0,
      playing: Boolean(this.action && !this.action.paused)
    })
  }

  private removeObject() {
    if (this.object) {
      this.scene.remove(this.object)
      this.materials?.dispose()
      disposeObject(this.object, false)
    }
    this.sourceMaterials.forEach(disposeMaterial)
    this.sourceMaterials.clear()
    this.mixer?.stopAllAction()
    this.object = undefined
    this.materials = undefined
    this.mixer = undefined
    this.action = undefined
    this.clips = []
    this.lastTimestamp = undefined
    this.elapsedSeconds = 0
    this.emit()
  }

  dispose() {
    this.loadVersion++
    this.renderer.setAnimationLoop(null)
    this.controls.dispose()
    this.removeObject()
    this.fallbackMaterial.dispose()
    this.renderer.dispose()
  }
}

export function applyAppearanceMaterials(
  root: Object3D,
  materialBindings: StudioAnimationMaterialBinding[],
  sourceMaterials = new Set<Material>()
) {
  let section = 0
  const bindingsBySection = new Map(materialBindings.map(binding => [binding.sectionIndex, binding]))
  const loadedMaterials = new Set<Material>()
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    const source = Array.isArray(object.material) ? object.material : [object.material]
    const replacements = source.map(material => {
      const binding = bindingsBySection.get(section++)
      if (!binding) {
        sourceMaterials.add(material)
        return material
      }
      loadedMaterials.add(material)
      const replacement = new MeshStandardMaterial(studioAnimationPreviewMaterialOptions)
      replacement.name = binding?.name || material.name || `Section ${section}`
      const definition = binding?.material ?? (binding?.diffuseUrl ? { diffuseUrl: binding.diffuseUrl } : undefined)
      if (definition) replacement.userData.l2 = definition
      sourceMaterials.add(replacement)
      return replacement
    })
    object.material = Array.isArray(object.material) ? replacements : replacements[0]!
  })
  loadedMaterials.forEach(disposeMaterial)
  return section
}

function disposeObject(root: Object3D, disposeMaterials = true) {
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    object.geometry.dispose()
    if (!disposeMaterials) return
    const materials = Array.isArray(object.material) ? object.material : [object.material]
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
