export interface SystemInfo {
  service: string
  buildVersion: string
  environment: string
}

export function systemInfoUrl(apiBase: string): string {
  return `${apiBase.replace(/\/$/, '')}/api/system/info`
}
