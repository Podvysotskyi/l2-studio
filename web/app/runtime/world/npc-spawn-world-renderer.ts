import type {
  NpcSpawnWorldMap,
  NpcSpawnWorldMapPoint,
  NpcSpawnWorldMapZone
} from '~/types/models/npc-spawn-world-map'
import type { WorldMapOverviewManifest } from '~/types/models/world-map-overview'
import {
  AmbientLight,
  Box3,
  Color,
  DirectionalLight,
  DoubleSide,
  Group,
  InstancedMesh,
  LineBasicMaterial,
  LineSegments,
  Matrix4,
  Mesh,
  MeshBasicMaterial,
  MeshStandardMaterial,
  OrthographicCamera,
  Raycaster,
  Scene,
  SphereGeometry,
  Shape,
  ShapeGeometry,
  Vector2,
  Vector3,
  WebGLRenderer,
  BufferGeometry,
  Float32BufferAttribute,
  type Object3D
} from 'three'
import { OrbitControls } from 'three/addons/controls/OrbitControls.js'
import { loadPublishedGltf } from '../core/published-gltf'

export type NpcSpawnWorldSelection =
  | { kind: 'point'; value: NpcSpawnWorldMapPoint }
  | { kind: 'zone'; value: NpcSpawnWorldMapZone }

export interface NpcSpawnWorldRendererOptions {
  onSelect: (selection: NpcSpawnWorldSelection | undefined) => void
}

export class NpcSpawnWorldRenderer {
  private readonly renderer: WebGLRenderer
  private readonly scene = new Scene()
  private readonly camera = new OrthographicCamera(-1, 1, 1, -1, 0.1, 2_000_000)
  private readonly controls: OrbitControls
  private readonly terrain = new Group()
  private readonly overlays = new Group()
  private readonly raycaster = new Raycaster()
  private readonly pointer = new Vector2()
  private readonly pointGeometry = new SphereGeometry(550, 8, 6)
  private readonly pointMaterial = new MeshStandardMaterial({ color: 0x2dd4bf, emissive: 0x0f766e, emissiveIntensity: 0.35 })
  private readonly zoneMaterial = new MeshStandardMaterial({ color: 0xf59e0b, emissive: 0x92400e, emissiveIntensity: 0.25 })
  private readonly terrainMaterial = new MeshStandardMaterial({ color: 0x5f8d63, roughness: 0.9, metalness: 0 })
  private readonly zoneFillMaterial = new MeshBasicMaterial({ color: 0xf59e0b, transparent: true, opacity: 0.16, side: DoubleSide })
  private readonly matrix = new Matrix4()
  private readonly bounds = new Box3()
  private map?: NpcSpawnWorldMap
  private query = ''
  private pointsVisible = true
  private zonesVisible = true
  private pointMarkers?: InstancedMesh
  private zoneMarkers?: InstancedMesh
  private pointValues: NpcSpawnWorldMapPoint[] = []
  private zoneValues: NpcSpawnWorldMapZone[] = []

  constructor(
    private readonly canvas: HTMLCanvasElement,
    private readonly options: NpcSpawnWorldRendererOptions
  ) {
    this.renderer = new WebGLRenderer({ canvas, antialias: true })
    this.renderer.setPixelRatio(window.devicePixelRatio)
    this.renderer.setClearColor(0x09120f)
    this.scene.add(this.terrain, this.overlays)
    this.scene.add(new AmbientLight(0xffffff, 1.25))
    const sunlight = new DirectionalLight(0xfff7d6, 2.1)
    sunlight.position.set(0.4, 1, 0.6)
    this.scene.add(sunlight)
    this.controls = new OrbitControls(this.camera, canvas)
    this.controls.enableDamping = true
    this.controls.dampingFactor = 0.08
    this.controls.maxPolarAngle = Math.PI / 2.05
    this.controls.minPolarAngle = Math.PI / 7
    canvas.addEventListener('pointerup', this.selectAtPointer)
    this.renderer.setAnimationLoop(() => {
      this.controls.update()
      this.renderer.render(this.scene, this.camera)
    })
    this.resize()
  }

  async load(
    map: NpcSpawnWorldMap,
    overview?: WorldMapOverviewManifest
  ) {
    this.map = map
    this.clearTerrain()
    this.setFilters(this.query, this.pointsVisible, this.zonesVisible)
    if (overview) {
      await Promise.all(overview.tiles.map(async tile => {
        const instance = await loadPublishedGltf(tile.meshUrl)
        instance.traverse(object => {
          if (object instanceof Mesh) object.material = this.terrainMaterial
        })
        this.terrain.add(instance)
      }))
    }
    this.fit()
  }

  setFilters(query: string, pointsVisible: boolean, zonesVisible: boolean) {
    this.query = query.trim().toLowerCase()
    this.pointsVisible = pointsVisible
    this.zonesVisible = zonesVisible
    this.rebuildOverlays()
  }

  resize() {
    const width = Math.max(this.canvas.clientWidth || this.canvas.width, 1)
    const height = Math.max(this.canvas.clientHeight || this.canvas.height, 1)
    this.renderer.setSize(width, height, false)
    this.fit()
  }

  fit() {
    if (this.bounds.isEmpty()) return
    const size = this.bounds.getSize(new Vector3())
    const center = this.bounds.getCenter(new Vector3())
    const aspect = Math.max(this.canvas.clientWidth / Math.max(this.canvas.clientHeight, 1), 1)
    const halfHeight = Math.max(size.x / aspect, size.z, size.y) * 0.62 + 2_000
    this.camera.left = -halfHeight * aspect
    this.camera.right = halfHeight * aspect
    this.camera.top = halfHeight
    this.camera.bottom = -halfHeight
    this.camera.near = 1
    this.camera.far = Math.max(size.length() * 8, 100_000)
    this.camera.position.copy(center).add(new Vector3(size.x * 0.55 + 20_000, size.y * 0.8 + 30_000, size.z * 0.55 + 20_000))
    this.camera.updateProjectionMatrix()
    this.controls.target.copy(center)
    this.controls.update()
  }

  dispose() {
    this.canvas.removeEventListener('pointerup', this.selectAtPointer)
    this.renderer.setAnimationLoop(null)
    this.controls.dispose()
    this.pointGeometry.dispose()
    this.pointMaterial.dispose()
    this.zoneMaterial.dispose()
    this.terrainMaterial.dispose()
    this.zoneFillMaterial.dispose()
    this.renderer.dispose()
  }

  private readonly selectAtPointer = (event: PointerEvent) => {
    const bounds = this.canvas.getBoundingClientRect()
    this.pointer.set(
      ((event.clientX - bounds.left) / bounds.width) * 2 - 1,
      -((event.clientY - bounds.top) / bounds.height) * 2 + 1
    )
    this.raycaster.setFromCamera(this.pointer, this.camera)
    const hit = this.raycaster.intersectObjects(
      [this.pointMarkers, this.zoneMarkers].filter(Boolean) as Object3D[],
      false
    )[0]
    if (!hit || hit.instanceId === undefined) return this.options.onSelect(undefined)
    if (hit.object === this.pointMarkers) {
      const value = this.pointValues[hit.instanceId]
      return this.options.onSelect(value ? { kind: 'point', value } : undefined)
    }
    const value = this.zoneValues[hit.instanceId]
    this.options.onSelect(value ? { kind: 'zone', value } : undefined)
  }

  private rebuildOverlays() {
    this.overlays.clear()
    this.pointMarkers = undefined
    this.zoneMarkers = undefined
    this.bounds.makeEmpty()
    if (!this.map) return
    this.pointValues = this.pointsVisible
      ? this.map.points.filter(value => this.matches(value.npcId, value.npcName, value.spawnName))
      : []
    this.zoneValues = this.zonesVisible
      ? this.map.zones.filter(value => this.matchesZone(value))
      : []
    this.pointMarkers = this.createMarkers(this.pointValues, value => new Vector3(value.x, value.z + 350, value.y), this.pointMaterial)
    this.zoneMarkers = this.createMarkers(this.zoneValues, value => zoneCenter(value), this.zoneMaterial)
    if (this.pointMarkers) this.overlays.add(this.pointMarkers)
    if (this.zoneMarkers) this.overlays.add(this.zoneMarkers)
    for (const zone of this.zoneValues) this.addZoneOutline(zone)
    this.extendBounds()
    this.fit()
  }

  private createMarkers<T>(
    values: T[],
    position: (value: T) => Vector3,
    material: MeshStandardMaterial
  ) {
    if (!values.length) return undefined
    const mesh = new InstancedMesh(this.pointGeometry, material, values.length)
    values.forEach((value, index) => {
      this.matrix.makeTranslation(position(value).x, position(value).y, position(value).z)
      mesh.setMatrixAt(index, this.matrix)
    })
    mesh.instanceMatrix.needsUpdate = true
    return mesh
  }

  private addZoneOutline(zone: NpcSpawnWorldMapZone) {
    if (zone.territoryNodes.length < 3) return
    const positions: number[] = []
    const nodes = zone.territoryNodes
    const shape = new Shape(nodes.map(node => new Vector2(node.x, node.y)))
    const fill = new Mesh(new ShapeGeometry(shape), this.zoneFillMaterial)
    fill.rotation.x = Math.PI / 2
    fill.position.y = zone.minZ + 16
    this.overlays.add(fill)
    for (let index = 0; index < nodes.length; index++) {
      const current = nodes[index]!
      const next = nodes[(index + 1) % nodes.length]!
      positions.push(
        current.x, zone.minZ, current.y, next.x, zone.minZ, next.y,
        current.x, zone.maxZ, current.y, next.x, zone.maxZ, next.y,
        current.x, zone.minZ, current.y, current.x, zone.maxZ, current.y
      )
    }
    const geometry = new BufferGeometry()
    geometry.setAttribute('position', new Float32BufferAttribute(positions, 3))
    this.overlays.add(new LineSegments(geometry, new LineBasicMaterial({ color: 0xfbbf24, transparent: true, opacity: 0.72 })))
  }

  private matches(npcId: number, npcName: string | null, source: string) {
    return !this.query || `${npcId} ${npcName ?? ''} ${source}`.toLowerCase().includes(this.query)
  }

  private matchesZone(zone: NpcSpawnWorldMapZone) {
    return !this.query || zone.name.toLowerCase().includes(this.query) || zone.npcs.some(value =>
      this.matches(value.npcId, value.npcName, zone.name))
  }

  private extendBounds() {
    for (const point of this.pointValues)
      this.bounds.expandByPoint(new Vector3(point.x, point.z, point.y))
    for (const zone of this.zoneValues) {
      for (const node of zone.territoryNodes) {
        this.bounds.expandByPoint(new Vector3(node.x, zone.minZ, node.y))
        this.bounds.expandByPoint(new Vector3(node.x, zone.maxZ, node.y))
      }
    }
  }

  private clearTerrain() {
    this.terrain.clear()
  }
}

function zoneCenter(zone: NpcSpawnWorldMapZone) {
  const count = Math.max(zone.territoryNodes.length, 1)
  const x = zone.territoryNodes.reduce((sum, value) => sum + value.x, 0) / count
  const y = zone.territoryNodes.reduce((sum, value) => sum + value.y, 0) / count
  return new Vector3(x, zone.maxZ + 550, y)
}
