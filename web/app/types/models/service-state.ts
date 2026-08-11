export const serviceLabels = {
  connected: 'Connected',
  connecting: 'Connecting',
  disconnected: 'Disconnected',
  error: 'Unavailable'
} as const

export type ServiceState = keyof typeof serviceLabels
