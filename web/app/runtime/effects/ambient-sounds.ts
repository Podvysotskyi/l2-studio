import type { SceneObjectManifestEntry } from '~/types/studio'
import { Scene, Vector3 } from '@babylonjs/core'
import { unrealVector } from '../core/unreal-transform.js'

export interface ComposedAmbientSounds {
  activeCount: number
  diagnostics: string[]
  start(): void
  setVolume(value: number): void
  setMuted(value: boolean): void
  dispose(): void
}

interface Track {
  source: SceneObjectManifestEntry
  audio: HTMLAudioElement
  radius: number
  volume: number
  randomDelay: number
  timer?: ReturnType<typeof setTimeout>
}

function numberProperty(
  source: SceneObjectManifestEntry,
  name: string,
  fallback: number
) {
  const value = Number(source.properties[name])
  return Number.isFinite(value) ? value : fallback
}

function attenuation(scene: Scene, track: Track) {
  if (!scene.activeCamera) return 1
  const distance = Vector3.Distance(
    scene.activeCamera.position,
    unrealVector(track.source.location)
  )
  return Math.max(0, 1 - distance / track.radius)
}

export function composeAmbientSounds(
  scene: Scene,
  sources: SceneObjectManifestEntry[]
): ComposedAmbientSounds {
  const diagnostics: string[] = []
  const tracks: Track[] = []
  if (typeof Audio === 'undefined') {
    return {
      activeCount: 0,
      diagnostics: ['Browser audio is unavailable.'],
      start() {},
      setVolume() {},
      setMuted() {},
      dispose() {}
    }
  }

  for (const source of sources) {
    if (!source.resourceUrl) {
      if (source.properties.AmbientSound)
        diagnostics.push(
          `${source.name}: ${source.diagnostic ?? 'ambient sound asset is unavailable.'}`
        )
      continue
    }
    const radius = Math.max(numberProperty(source, 'SoundRadius', 64) * 64, 1)
    if (
      scene.activeCamera &&
      Vector3.DistanceSquared(
        scene.activeCamera.position,
        unrealVector(source.location)
      ) >
        radius * radius
    )
      continue
    const audio = new Audio(source.resourceUrl)
    audio.preload = 'none'
    const randomDelay = Math.max(numberProperty(source, 'AmbientRandom', 0), 0)
    audio.loop = randomDelay === 0
    tracks.push({
      source,
      audio,
      radius,
      volume: Math.min(numberProperty(source, 'SoundVolume', 255) / 255, 1),
      randomDelay
    })
  }

  let started = false
  let disposed = false
  let masterVolume = 1
  let muted = false
  const schedule = (track: Track) => {
    if (disposed || track.randomDelay <= 0) return
    const delay = track.randomDelay * (0.5 + Math.random()) * 1_000
    track.timer = setTimeout(async () => {
      if (disposed) return
      track.audio.currentTime = 0
      try {
        await track.audio.play()
      } catch {
        // A later interaction can retry; autoplay rejection is not a content failure.
      }
      schedule(track)
    }, delay)
  }
  const start = () => {
    if (started || disposed) return
    started = true
    for (const track of tracks) {
      if (track.randomDelay > 0) schedule(track)
      else void track.audio.play().catch(() => {})
    }
  }
  const unlock = () => start()
  if (typeof window !== 'undefined') {
    window.addEventListener('pointerdown', unlock, { once: true })
    window.addEventListener('keydown', unlock, { once: true })
  }
  const observer = scene.onBeforeRenderObservable.add(() => {
    for (const track of tracks)
      track.audio.volume = muted
        ? 0
        : track.volume * masterVolume * attenuation(scene, track)
  })

  return {
    activeCount: tracks.length,
    diagnostics,
    start,
    setVolume(value) {
      masterVolume = Math.max(0, Math.min(value, 1))
    },
    setMuted(value) {
      muted = value
    },
    dispose() {
      if (disposed) return
      disposed = true
      if (observer) scene.onBeforeRenderObservable.remove(observer)
      if (typeof window !== 'undefined') {
        window.removeEventListener('pointerdown', unlock)
        window.removeEventListener('keydown', unlock)
      }
      for (const track of tracks) {
        if (track.timer) clearTimeout(track.timer)
        track.audio.pause()
        track.audio.src = ''
      }
    }
  }
}
