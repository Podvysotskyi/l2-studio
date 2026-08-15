export const studioPreviewBackgrounds = [
  { id: 'dark', label: 'Dark slate', color: 0x09101d },
  { id: 'neutral', label: 'Neutral gray', color: 0x6b7280 },
  { id: 'light', label: 'Warm light', color: 0xe4e1da }
] as const

export type StudioPreviewBackground = (typeof studioPreviewBackgrounds)[number]['id']

export function studioPreviewBackgroundColor(background: StudioPreviewBackground) {
  return studioPreviewBackgrounds.find(item => item.id === background)?.color
    ?? studioPreviewBackgrounds[0].color
}
