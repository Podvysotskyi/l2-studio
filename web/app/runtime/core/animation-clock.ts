import type { Observer, Scene } from '@babylonjs/core'

export interface SceneAnimationClock {
  readonly elapsedSeconds: number
  subscribe(listener: (elapsedSeconds: number) => void): () => void
}

class AnimationClock implements SceneAnimationClock {
  elapsedSeconds = 0
  private readonly listeners = new Set<(elapsedSeconds: number) => void>()
  private readonly observer: Observer<Scene>

  constructor(private readonly scene: Scene) {
    this.observer = scene.onBeforeRenderObservable.add(() => {
      const delta = Math.min(Math.max(scene.getEngine().getDeltaTime(), 0), 100)
      this.elapsedSeconds += delta / 1_000
      for (const listener of this.listeners) listener(this.elapsedSeconds)
    })
    scene.onDisposeObservable.addOnce(() => {
      scene.onBeforeRenderObservable.remove(this.observer)
      this.listeners.clear()
    })
  }

  subscribe(listener: (elapsedSeconds: number) => void) {
    this.listeners.add(listener)
    listener(this.elapsedSeconds)
    return () => this.listeners.delete(listener)
  }
}

const clocks = new WeakMap<Scene, AnimationClock>()

export function sceneAnimationClock(scene: Scene): SceneAnimationClock {
  const existing = clocks.get(scene)
  if (existing) return existing
  const clock = new AnimationClock(scene)
  clocks.set(scene, clock)
  return clock
}
