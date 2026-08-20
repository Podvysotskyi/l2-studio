import { defineConfig, devices } from '@playwright/test'

const port = 4173

export default defineConfig({
  testDir: './test/e2e',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  use: {
    baseURL: `http://127.0.0.1:${port}`,
    trace: 'on-first-retry'
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `NUXT_STUDIO_API_BASE=http://studio-api.invalid PORT=${port} node .output/server/index.mjs`,
    url: `http://127.0.0.1:${port}`,
    reuseExistingServer: !process.env.CI
  }
})
