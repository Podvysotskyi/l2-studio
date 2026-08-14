import {
  AmbientLight,
  AnimationMixer,
  Box3,
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
  type AnimationClip,
  type Material,
  type Object3D
} from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { loadPublishedGltfAsset } from '../core/published-gltf.js'

export interface StudioAnimationState {
  clipNames: string[]
  clipName?: string
  duration: number
  time: number
  playing: boolean
}

export class StudioAnimationRenderer {
  private readonly renderer: WebGLRenderer
  private readonly scene = new Scene()
  private readonly camera = new PerspectiveCamera(45, 1, 0.01, 1_000_000)
  private readonly controls: OrbitControls
  private object?: Object3D
  private mixer?: AnimationMixer
  private clips: AnimationClip[] = []
  private action?: AnimationAction
  private lastTimestamp?: number
  private loadVersion = 0
  private speed = 1

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly changed: (state: StudioAnimationState) => void
  ) {
    this.renderer = new WebGLRenderer({ canvas, antialias: true })
    this.renderer.outputColorSpace = SRGBColorSpace
    this.renderer.setClearColor(0x09101d, 1)
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

  async load(url: string, animationUrl?: string | null) {
    const version = ++this.loadVersion
    this.removeObject()
    const [gltf, animationGltf] = await Promise.all([
      loadPublishedGltfAsset(url),
      animationUrl ? loadPublishedGltfAsset(animationUrl) : undefined
    ])
    if (version !== this.loadVersion) {
      disposeObject(gltf.scene)
      if (animationGltf) disposeObject(animationGltf.scene)
      return
    }
    this.object = gltf.scene
    this.clips = animationGltf?.animations ?? gltf.animations
    gltf.scene.traverse(object => {
      if (!(object instanceof Mesh)) return
      const materials = Array.isArray(object.material) ? object.material : [object.material]
      materials.forEach(disposeMaterial)
      object.material = new MeshStandardMaterial({ color: 0xaab7c8, roughness: 0.9 })
    })
    this.scene.add(gltf.scene)
    this.mixer = new AnimationMixer(gltf.scene)
    this.lastTimestamp = undefined
    const bounds = new Box3().setFromObject(gltf.scene, true)
    if (bounds.isEmpty()) throw new Error('The animation contains no renderable geometry.')
    const center = bounds.getCenter(new Vector3())
    const sphere = bounds.getBoundingSphere(new Sphere())
    const distance = Math.max(sphere.radius / Math.sin((this.camera.fov * Math.PI) / 360) * 1.25, 1)
    this.controls.target.copy(center)
    this.camera.position.copy(center.clone().add(new Vector3(distance, distance * 0.65, distance)))
    this.camera.near = Math.max(distance / 10_000, 0.01)
    this.camera.far = Math.max(distance * 20, 100)
    this.camera.updateProjectionMatrix()
    if (this.clips[0]) this.select(this.clips[0].name)
    else this.emit()
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
    if (!this.object) return
    this.scene.remove(this.object)
    disposeObject(this.object)
    this.mixer?.stopAllAction()
    this.object = undefined
    this.mixer = undefined
    this.action = undefined
    this.clips = []
  }

  dispose() {
    this.loadVersion++
    this.renderer.setAnimationLoop(null)
    this.controls.dispose()
    this.removeObject()
    this.renderer.dispose()
  }
}

function disposeObject(root: Object3D) {
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    object.geometry.dispose()
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
