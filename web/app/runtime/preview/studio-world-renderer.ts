import type {
  MapManifest,
  MapRotation,
  MapVector,
  SceneManifest
} from '~/types/studio'
import {
  AmbientLight,
  Box3,
  Box3Helper,
  Color,
  DirectionalLight,
  Group,
  Mesh,
  MeshStandardMaterial,
  OrthographicCamera,
  PerspectiveCamera,
  Raycaster,
  Scene,
  Sphere,
  SphereGeometry,
  SRGBColorSpace,
  Vector2,
  Vector3,
  WebGLRenderer,
  type Material,
  type Object3D
} from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { loadPublishedGltf } from '../core/published-gltf.js'
import {
  unrealForward,
  unrealNodeTransform,
  unrealVector
} from '../core/unreal-transform.js'
import {
  createTerrainMaterial,
  type TerrainMaterialController
} from '../materials/terrain-material.js'

export interface StudioWorldRendererOptions {
  interactive?: boolean
  preserveDrawingBuffer?: boolean
  onLightSelect?: (name: string) => void
}

export interface StudioWorldLoadOptions {
  includeSkyZoneBsp?: boolean
  includeWorldBaseBsp?: boolean
  failOnTerrainMaterialError?: boolean
  onMaterialError?: (message: string) => void
}

type Manifest = MapManifest | SceneManifest
type ObjectIndex = Map<string, Object3D[]>

const colors = {
  actor: 0xaab7c8,
  bsp: 0x75879b,
  sky: 0x776d99,
  worldBase: 0x576475,
  waterSurface: 0x268eb3,
  waterVolume: 0x22c7d6,
  terrainFallback: 0x596a57,
  selected: 0xff8c1a
}

export class StudioWorldRenderer {
  private readonly renderer: WebGLRenderer
  private readonly scene = new Scene()
  private readonly perspectiveCamera: PerspectiveCamera
  private readonly orthographicCamera: OrthographicCamera
  private readonly controls?: OrbitControls

  private readonly content = new Group()
  private readonly actorMeshes: ObjectIndex = new Map()
  private readonly bspMeshes: ObjectIndex = new Map()
  private readonly skyZoneMeshes: ObjectIndex = new Map()
  private readonly worldBaseMeshes: ObjectIndex = new Map()
  private readonly waterSurfaceMeshes: ObjectIndex = new Map()
  private readonly waterMeshes: ObjectIndex = new Map()
  private readonly lightMarkers: ObjectIndex = new Map()
  private readonly terrainMeshes: Object3D[] = []
  private readonly terrainControllers = new Map<string, TerrainMaterialController>()
  private readonly templates = new Map<string, Promise<Object3D>>()
  private readonly raycaster = new Raycaster()
  private readonly pointer = new Vector2()
  private readonly materials = {
    actor: diagnosticMaterial(colors.actor),
    bsp: diagnosticMaterial(colors.bsp),
    sky: diagnosticMaterial(colors.sky),
    worldBase: diagnosticMaterial(colors.worldBase),
    waterSurface: diagnosticMaterial(colors.waterSurface, true),
    waterVolume: diagnosticMaterial(colors.waterVolume, true),
    terrainFallback: diagnosticMaterial(colors.terrainFallback)
  }

  private selection?: Box3Helper
  private loadVersion = 0
  private actorsVisible = true
  private bspVisible = true
  private skyZoneVisible = false
  private worldBaseVisible = false
  private waterSurfacesVisible = true
  private waterVolumesVisible = true
  private lightHelpersVisible = false
  private selectedActorName?: string
  private selectedBspName?: string
  private selectedWaterSurfaceName?: string
  private selectedWaterName?: string
  private selectedLightName?: string
  private skyZoneChunkVisibility: Record<string, boolean> = {}
  private animationActive = false

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly options: StudioWorldRendererOptions = {}
  ) {
    this.renderer = new WebGLRenderer({
      canvas,
      antialias: true,
      preserveDrawingBuffer: options.preserveDrawingBuffer ?? false
    })
    this.renderer.outputColorSpace = SRGBColorSpace
    this.renderer.setClearColor(0x33475f, 1)
    this.renderer.setPixelRatio(options.interactive === false ? 1 : window.devicePixelRatio)
    this.scene.add(this.content)
    this.scene.add(new AmbientLight(0xffffff, 1.35))
    const light = new DirectionalLight(0xffffff, 1.8)
    light.position.set(0.4, 1, 0.55)
    this.scene.add(light)

    this.perspectiveCamera = new PerspectiveCamera(55, 1, 0.1, 1_000_000)
    this.perspectiveCamera.position.set(10_000, 8_000, 10_000)
    this.orthographicCamera = new OrthographicCamera(-1, 1, 1, -1, 0.1, 1_000_000)
    if (options.interactive !== false) {
      this.controls = new OrbitControls(this.perspectiveCamera, canvas)
      this.controls.enableDamping = true
      this.controls.dampingFactor = 0.08
      this.controls.zoomToCursor = true
      canvas.addEventListener('pointerup', this.selectLightAtPointer)
      this.start()
    }
    this.resize()
  }

  private readonly selectLightAtPointer = (event: PointerEvent) => {
    if (!this.lightHelpersVisible || !this.options.onLightSelect) return
    const bounds = this.canvas.getBoundingClientRect()
    this.pointer.set(
      ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
      -((event.clientY - bounds.top) / bounds.height) * 2 + 1
    )
    this.raycaster.setFromCamera(this.pointer, this.perspectiveCamera)
    const markers = [...this.lightMarkers.values()].flat()
    const hit = this.raycaster.intersectObjects(markers, true)[0]?.object
    const name = hit?.userData.lightName as string | undefined
    if (name) this.options.onLightSelect(name)
  }

  start() {
    if (this.animationActive) return
    this.animationActive = true
    this.renderer.setAnimationLoop(() => {
      this.controls?.update()
      this.renderer.render(this.scene, this.perspectiveCamera)
    })
  }

  setInteractionEnabled(enabled: boolean) {
    if (this.controls) this.controls.enabled = enabled
  }

  resize() {
    const width = Math.max(this.canvas.clientWidth || this.canvas.width, 1)
    const height = Math.max(this.canvas.clientHeight || this.canvas.height, 1)
    this.renderer.setSize(width, height, false)
    this.perspectiveCamera.aspect = width / height
    this.perspectiveCamera.updateProjectionMatrix()
  }

  async loadManifest(manifest: Manifest, options: StudioWorldLoadOptions = {}) {
    const version = ++this.loadVersion
    this.clearContent()
    const activeSkyZone = [...manifest.skyZones]
      .filter(zone =>
        manifest.bspMeshes.some(
          bsp => bsp.role === 'sky-zone' && bsp.skyZone === zone.name
        )
      )
      .sort((left, right) => right.order - left.order)[0]

    const bspEntries = manifest.bspMeshes.filter(entry => {
      if (!entry.meshUrl) return false
      if (entry.role === 'world-base') return options.includeWorldBaseBsp !== false
      if (entry.role === 'sky-zone')
        return options.includeSkyZoneBsp !== false && entry.skyZone === activeSkyZone?.name
      return true
    })
    for (const entry of bspEntries) {
      const instance = await this.loadInstance(entry.meshUrl!, version)
      if (!instance) return
      const objects = renderableObjects(instance)
      const material = entry.role === 'sky-zone'
        ? this.materials.sky
        : entry.role === 'world-base'
          ? this.materials.worldBase
          : entry.role === 'water-surface'
            ? this.materials.waterSurface
            : this.materials.bsp
      assignMaterial(instance, material)
      if (entry.role === 'sky-zone') this.skyZoneMeshes.set(entry.name, objects)
      else if (entry.role === 'world-base') this.worldBaseMeshes.set(entry.name, objects)
      else if (entry.role === 'water-surface') this.waterSurfaceMeshes.set(entry.name, objects)
      else this.bspMeshes.set(entry.name, objects)
      this.content.add(instance)
    }

    for (const terrain of manifest.terrains.filter(entry => entry.meshUrl)) {
      const instance = await this.loadInstance(terrain.meshUrl!, version)
      if (!instance) return
      applyTransform(instance, terrain.location, terrain.rotation)
      let controller: TerrainMaterialController | undefined
      try {
        if (renderableObjects(instance).some(object =>
          object instanceof Mesh && !object.geometry.attributes.uv
        ))
          throw new Error('Terrain geometry has no texture coordinates.')
        controller = createTerrainMaterial(terrain, this.renderer)
        await controller.ready
        if (version !== this.loadVersion) {
          controller.dispose()
          return
        }
        this.terrainControllers.set(terrain.name, controller)
        assignMaterial(instance, controller.material)
      } catch (error) {
        controller?.dispose()
        const message = `${terrain.name}: ${errorMessage(error)}`
        options.onMaterialError?.(message)
        if (options.failOnTerrainMaterialError) throw new Error(message)
        assignMaterial(instance, this.materials.terrainFallback)
      }
      this.terrainMeshes.push(...renderableObjects(instance))
      this.content.add(instance)
    }

    const actors = manifest.actors.filter(entry => entry.meshUrl)
    for (let index = 0; index < actors.length; index += 12) {
      await Promise.all(
        actors.slice(index, index + 12).map(async actor => {
          try {
            const instance = await this.loadInstance(actor.meshUrl!, version)
            if (!instance) return
            applyTransform(
              instance,
              actor.location,
              actor.rotation,
              actor.drawScale,
              actor.drawScale3D,
              actor.prePivot
            )
            assignMaterial(instance, this.materials.actor)
            this.actorMeshes.set(actor.name, renderableObjects(instance))
            this.content.add(instance)
          } catch (error) {
            console.warn(`Unable to load map actor ${actor.name}.`, error)
          }
        })
      )
      if (version !== this.loadVersion) return
    }

    for (const volume of manifest.waterVolumes.filter(
      entry => entry.status === 'resolved' && entry.meshUrl
    )) {
      const instance = await this.loadInstance(volume.meshUrl!, version)
      if (!instance) return
      applyTransform(
        instance,
        volume.location,
        volume.rotation,
        volume.drawScale,
        volume.drawScale3D,
        volume.prePivot
      )
      assignMaterial(instance, this.materials.waterVolume)
      this.waterMeshes.set(volume.name, renderableObjects(instance))
      this.content.add(instance)
    }

    this.createLightMarkers(manifest)
    this.applyVisibility()
    this.applySelection()
    if (version === this.loadVersion) this.frameMap()
  }

  private async loadInstance(url: string, version: number) {
    let pending = this.templates.get(url)
    if (!pending) {
      pending = loadPublishedGltf(url).then(template => {
        replaceSourceMaterials(template, this.materials.actor)
        return template
      })
      this.templates.set(url, pending)
    }
    const template = await pending
    if (version !== this.loadVersion) return
    const instance = template.clone(true)
    instance.name = url
    return instance
  }

  private createLightMarkers(manifest: Manifest) {
    for (const entry of manifest.lights) {
      const geometry = new SphereGeometry(
        Math.max(Math.min(entry.radius * 2, 512), 64),
        10,
        8
      )
      const material = new MeshStandardMaterial({
        color: lightColor(entry.hue, entry.saturation),
        emissive: lightColor(entry.hue, entry.saturation),
        emissiveIntensity: 0.65,
        transparent: true,
        opacity: 0.8
      })
      const marker = new Mesh(geometry, material)
      marker.position.copy(unrealVector(entry.location))
      marker.userData.lightName = entry.name
      this.lightMarkers.set(entry.name, [marker])
      this.content.add(marker)
    }
  }

  setSelection(selection: {
    actor?: string
    bsp?: string
    light?: string
    waterSurface?: string
    water?: string
  }) {
    this.selectedActorName = selection.actor
    this.selectedBspName = selection.bsp
    this.selectedLightName = selection.light
    this.selectedWaterSurfaceName = selection.waterSurface
    this.selectedWaterName = selection.water
    this.applySelection()
  }

  setVisibility(visibility: {
    actors: boolean
    bsp: boolean
    skyZone: boolean
    skyZoneChunks: Record<string, boolean>
    worldBase: boolean
    waterSurfaces: boolean
    waterVolumes: boolean
    lightHelpers: boolean
  }) {
    this.actorsVisible = visibility.actors
    this.bspVisible = visibility.bsp
    this.skyZoneVisible = visibility.skyZone
    this.skyZoneChunkVisibility = visibility.skyZoneChunks
    this.worldBaseVisible = visibility.worldBase
    this.waterSurfacesVisible = visibility.waterSurfaces
    this.waterVolumesVisible = visibility.waterVolumes
    this.lightHelpersVisible = visibility.lightHelpers
    this.applyVisibility()
    this.applySelection()
  }

  setTerrainLayerVisibility(visibility: Record<string, boolean[]>) {
    for (const [name, controller] of this.terrainControllers) {
      const enabled = visibility[name]
      if (!enabled) controller.setAllLayersEnabled(true)
      else enabled.forEach((value, index) => controller.setLayerEnabled(index, value))
    }
  }

  private applyVisibility() {
    setIndexVisibility(this.actorMeshes, () => this.actorsVisible)
    setIndexVisibility(this.bspMeshes, () => this.bspVisible)
    setIndexVisibility(
      this.skyZoneMeshes,
      name => this.skyZoneVisible && this.skyZoneChunkVisibility[name] !== false
    )
    setIndexVisibility(this.worldBaseMeshes, () => this.worldBaseVisible)
    setIndexVisibility(this.waterSurfaceMeshes, () => this.waterSurfacesVisible)
    setIndexVisibility(this.waterMeshes, () => this.waterVolumesVisible)
    setIndexVisibility(this.lightMarkers, () => this.lightHelpersVisible)
  }

  private applySelection() {
    if (this.selection) {
      this.scene.remove(this.selection)
      this.selection.geometry.dispose()
      ;(this.selection.material as Material).dispose()
      this.selection = undefined
    }
    const selected = [
      ...(this.selectedActorName && this.actorsVisible
        ? this.actorMeshes.get(this.selectedActorName) ?? [] : []),
      ...(this.selectedBspName
        ? this.bspMeshes.get(this.selectedBspName) ??
          this.worldBaseMeshes.get(this.selectedBspName) ?? [] : []),
      ...(this.selectedWaterSurfaceName && this.waterSurfacesVisible
        ? this.waterSurfaceMeshes.get(this.selectedWaterSurfaceName) ?? [] : []),
      ...(this.selectedWaterName && this.waterVolumesVisible
        ? this.waterMeshes.get(this.selectedWaterName) ?? [] : [])
    ].filter(object => object.visible)
    if (selected.length) {
      const bounds = boundsFor(selected)
      if (!bounds.isEmpty()) {
        this.selection = new Box3Helper(bounds, colors.selected)
        this.scene.add(this.selection)
      }
    }
    for (const [name, markers] of this.lightMarkers) {
      const selectedLight = name === this.selectedLightName
      markers.forEach(marker => marker.scale.setScalar(selectedLight ? 1.5 : 1))
    }
  }

  focusActor(name: string) {
    if (this.actorsVisible) this.focus(this.actorMeshes.get(name))
  }

  focusBsp(name: string) {
    this.focus(this.bspMeshes.get(name) ?? this.worldBaseMeshes.get(name))
  }

  focusLight(name: string) {
    const marker = this.lightMarkers.get(name)?.[0]
    if (!marker) return
    this.controls?.target.copy(marker.position)
    this.perspectiveCamera.position.copy(
      marker.position.clone().add(new Vector3(800, 650, 800))
    )
    this.controls?.update()
  }

  focusWater(name: string) {
    if (this.waterVolumesVisible) this.focus(this.waterMeshes.get(name))
  }

  focusWaterSurface(name: string) {
    if (this.waterSurfacesVisible) this.focus(this.waterSurfaceMeshes.get(name))
  }

  focusPosition(location: MapVector, radius = 1024) {
    const target = unrealVector(location)
    this.controls?.target.copy(target)
    this.perspectiveCamera.position.copy(
      target.clone().add(new Vector3(radius, radius * 0.75, radius))
    )
    this.controls?.update()
  }

  setCameraPose(location: MapVector, rotation: MapRotation) {
    const position = unrealVector(location)
    this.perspectiveCamera.position.copy(position)
    this.controls?.target.copy(position.clone().add(unrealForward(rotation).multiplyScalar(1024)))
    this.controls?.update()
  }

  frameMap(topDown = false) {
    const actors = [...this.actorMeshes.values()].flat()
    const objects = this.terrainMeshes.length
      ? this.terrainMeshes
      : actors.length
        ? actors
        : this.bspMeshes.size
          ? [...this.bspMeshes.values()].flat()
          : [...this.waterSurfaceMeshes.values()].flat()
    this.frame(objects, topDown)
  }

  frameBsp() {
    this.frame([...this.bspMeshes.values()].flat(), false)
  }

  async renderTopDown() {
    const objects = this.terrainMeshes.length
      ? this.terrainMeshes
      : this.actorMeshes.size
        ? [...this.actorMeshes.values()].flat()
        : this.bspMeshes.size
          ? [...this.bspMeshes.values()].flat()
          : [...this.waterSurfaceMeshes.values()].flat()
    const bounds = boundsFor(objects)
    if (bounds.isEmpty()) throw new Error('The map contains no renderable geometry.')
    const center = bounds.getCenter(new Vector3())
    const size = bounds.getSize(new Vector3())
    const extent = Math.max(size.x, size.z, 1) * 1.04
    const elevation = Math.max(size.y, extent, 1)
    this.orthographicCamera.left = -extent / 2
    this.orthographicCamera.right = extent / 2
    this.orthographicCamera.top = extent / 2
    this.orthographicCamera.bottom = -extent / 2
    this.orthographicCamera.near = 0.1
    this.orthographicCamera.far = elevation * 3 + size.y
    this.orthographicCamera.position.set(center.x, bounds.max.y + elevation, center.z)
    this.orthographicCamera.up.set(0, 0, -1)
    this.orthographicCamera.lookAt(center)
    this.orthographicCamera.updateProjectionMatrix()
    await this.renderer.compileAsync(this.scene, this.orthographicCamera)
    this.renderer.render(this.scene, this.orthographicCamera)
    this.renderer.render(this.scene, this.orthographicCamera)
  }

  private focus(objects?: Object3D[]) {
    if (objects?.length) this.frame(objects, false)
  }

  private frame(objects: Object3D[], topDown: boolean) {
    const bounds = boundsFor(objects.filter(object => object.visible))
    if (bounds.isEmpty()) return
    const center = bounds.getCenter(new Vector3())
    const sphere = bounds.getBoundingSphere(new Sphere())
    const radius = Math.max(sphere.radius, 1)
    const direction = topDown
      ? new Vector3(0, 1, 0.001)
      : new Vector3(1, 0.75, 1).normalize()
    const distance = radius / Math.sin((this.perspectiveCamera.fov * Math.PI) / 360) * 1.15
    this.perspectiveCamera.position.copy(center.clone().add(direction.multiplyScalar(distance)))
    this.perspectiveCamera.near = Math.max(distance / 20_000, 0.1)
    this.perspectiveCamera.far = Math.max(distance * 20, 100_000)
    this.perspectiveCamera.updateProjectionMatrix()
    this.controls?.target.copy(center)
    this.controls?.update()
  }

  private clearContent() {
    if (this.selection) {
      this.scene.remove(this.selection)
      this.selection.geometry.dispose()
      ;(this.selection.material as Material).dispose()
    }
    this.selection = undefined
    this.content.clear()
    this.actorMeshes.clear()
    this.bspMeshes.clear()
    this.skyZoneMeshes.clear()
    this.worldBaseMeshes.clear()
    this.waterSurfaceMeshes.clear()
    this.waterMeshes.clear()
    this.lightMarkers.forEach(objects => disposeOwnedObjects(objects))
    this.lightMarkers.clear()
    this.terrainMeshes.length = 0
    this.terrainControllers.forEach(controller => controller.dispose())
    this.terrainControllers.clear()
    const templates = [...this.templates.values()]
    this.templates.clear()
    void Promise.allSettled(templates).then(results => {
      results.forEach(result => {
        if (result.status === 'fulfilled') disposeObjects([result.value])
      })
    })
  }

  dispose() {
    this.loadVersion++
    this.renderer.setAnimationLoop(null)
    this.canvas.removeEventListener('pointerup', this.selectLightAtPointer)
    this.controls?.dispose()
    this.clearContent()
    Object.values(this.materials).forEach(material => material.dispose())
    this.renderer.dispose()
  }
}

function diagnosticMaterial(color: number, transparent = false) {
  return new MeshStandardMaterial({
    color,
    roughness: 0.9,
    metalness: 0,
    transparent,
    opacity: transparent ? 0.38 : 1,
    depthWrite: !transparent
  })
}

function applyTransform(
  object: Object3D,
  location: MapVector,
  rotation: MapRotation,
  drawScale = 1,
  drawScale3D: MapVector = { x: 1, y: 1, z: 1 },
  prePivot: MapVector = { x: 0, y: 0, z: 0 }
) {
  const transform = unrealNodeTransform(
    location,
    rotation,
    drawScale,
    drawScale3D,
    prePivot
  )
  object.position.copy(transform.position)
  object.quaternion.copy(transform.rotation)
  object.scale.copy(transform.scaling)
  object.updateMatrixWorld(true)
}

function renderableObjects(root: Object3D) {
  const objects: Object3D[] = []
  root.traverse(object => {
    if (object instanceof Mesh && object.geometry.attributes.position)
      objects.push(object)
  })
  return objects
}

function assignMaterial(root: Object3D, material: Material) {
  root.traverse(object => {
    if (object instanceof Mesh) object.material = material
  })
}

function replaceSourceMaterials(root: Object3D, replacement: Material) {
  const materials = new Set<Material>()
  root.traverse(object => {
    if (!(object instanceof Mesh)) return
    const source = Array.isArray(object.material) ? object.material : [object.material]
    source.forEach(material => materials.add(material))
    object.material = replacement
  })
  materials.forEach(disposeMaterial)
}

function disposeMaterial(material: Material) {
  for (const value of Object.values(material)) {
    if (value && typeof value === 'object' && 'isTexture' in value)
      (value as { dispose(): void }).dispose()
  }
  material.dispose()
}

function disposeObjects(objects: Object3D[]) {
  const geometries = new Set<{ dispose(): void }>()
  objects.forEach(root => root.traverse(object => {
    if (object instanceof Mesh) geometries.add(object.geometry)
  }))
  geometries.forEach(geometry => geometry.dispose())
}

function disposeOwnedObjects(objects: Object3D[]) {
  objects.forEach(root => root.traverse(object => {
    if (!(object instanceof Mesh)) return
    object.geometry.dispose()
    const materials = Array.isArray(object.material)
      ? object.material
      : [object.material]
    materials.forEach(disposeMaterial)
  }))
}

function setIndexVisibility(index: ObjectIndex, visible: (name: string) => boolean) {
  for (const [name, objects] of index)
    objects.forEach(object => { object.visible = visible(name) })
}

function boundsFor(objects: Object3D[]) {
  const bounds = new Box3()
  objects.forEach(object => bounds.expandByObject(object, true))
  return bounds
}

function lightColor(hue: number, saturation: number) {
  return new Color().setHSL(
    hue / 255,
    Math.max(0, Math.min(1, 1 - saturation / 255)),
    0.55
  )
}

function errorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Terrain material could not be loaded.'
}
