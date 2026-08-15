import {
  AnimationClip,
  Bone,
  DoubleSide,
  Group,
  Mesh,
  MeshBasicMaterial,
  PlaneGeometry,
  QuaternionKeyframeTrack
} from 'three'
import { describe, expect, it } from 'vitest'
import {
  bindAnimationClips,
  applyAppearanceMaterials,
  hasBoundAnimationTrack,
  studioAnimationPreviewBackgrounds,
  studioAnimationPreviewMaterialOptions,
  studioAnimationCameraDistance
} from '../../app/runtime/preview/studio-animation-renderer'

describe('Studio animation renderer', () => {
  it('fits meter-scale assets without a one-unit distance floor', () => {
    expect(studioAnimationCameraDistance(0.1, 45)).toBeCloseTo(0.3266, 3)
    expect(studioAnimationCameraDistance(0, 45)).toBe(0.01)
  })

  it('renders animation preview materials on both sides', () => {
    expect(studioAnimationPreviewMaterialOptions.side).toBe(DoubleSide)
  })

  it('uses the static-mesh contrast background presets', () => {
    expect(studioAnimationPreviewBackgrounds).toEqual([
      { id: 'dark', label: 'Dark slate', color: 0x09101d },
      { id: 'neutral', label: 'Neutral gray', color: 0x6b7280 },
      { id: 'light', label: 'Warm light', color: 0xe4e1da }
    ])
  })

  it('recognizes reusable clips that target mesh bones by name', () => {
    const root = new Group()
    const bone = new Bone()
    bone.name = 'Bip01'
    root.add(bone)
    const compatible = new AnimationClip('idle', 1, [
      new QuaternionKeyframeTrack('Bip01.quaternion', [0], [0, 0, 0, 1])
    ])
    const incompatible = new AnimationClip('idle', 1, [
      new QuaternionKeyframeTrack('Missing.quaternion', [0], [0, 0, 0, 1])
    ])

    expect(hasBoundAnimationTrack(root, [compatible])).toBe(true)
    expect(hasBoundAnimationTrack(root, [incompatible])).toBe(false)
  })

  it('removes unbound accessory tracks before playback', () => {
    const root = new Group()
    const bone = new Bone()
    bone.name = 'Bip01'
    root.add(bone)
    const clip = new AnimationClip('idle', 1, [
      new QuaternionKeyframeTrack('Bip01.quaternion', [0], [0, 0, 0, 1]),
      new QuaternionKeyframeTrack('Missing.quaternion', [0], [0, 0, 0, 1])
    ])

    const [bound] = bindAnimationClips(root, [clip])

    expect(bound?.tracks.map(track => track.name)).toEqual(['Bip01.quaternion'])
    expect(clip.tracks).toHaveLength(2)
  })

  it('assigns appearance material bindings in skeletal section order', () => {
    const root = new Group()
    root.add(
      new Mesh(new PlaneGeometry(), new MeshBasicMaterial()),
      new Mesh(new PlaneGeometry(), new MeshBasicMaterial())
    )

    const sectionCount = applyAppearanceMaterials(root, [
      { sectionIndex: 0, name: 'Body', diffuseUrl: '/textures/body.webp' },
      { sectionIndex: 1, name: 'Face', diffuseUrl: '/textures/face.webp' },
      { sectionIndex: 2, name: 'Unused', diffuseUrl: '/textures/unused.webp' }
    ])
    const materials = root.children.map(item => (item as Mesh).material as MeshBasicMaterial)

    expect(sectionCount).toBe(2)
    expect(materials.map(material => material.name)).toEqual(['Body', 'Face'])
    expect(materials.map(material => material.userData.l2.diffuseUrl)).toEqual([
      '/textures/body.webp',
      '/textures/face.webp'
    ])
  })

  it('preserves embedded defaults for sections without a resolved override', () => {
    const root = new Group()
    const body = new MeshBasicMaterial()
    body.name = 'Embedded body'
    const armor = new MeshBasicMaterial()
    armor.name = 'Embedded armor'
    root.add(new Mesh(new PlaneGeometry(), body), new Mesh(new PlaneGeometry(), armor))

    applyAppearanceMaterials(root, [
      { sectionIndex: 0, name: 'Body override', diffuseUrl: '/textures/body.webp' }
    ])

    const materials = root.children.map(item => (item as Mesh).material as MeshBasicMaterial)
    expect(materials[0]).not.toBe(body)
    expect(materials[0]?.name).toBe('Body override')
    expect(materials[1]).toBe(armor)
  })
})
