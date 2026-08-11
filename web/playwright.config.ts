import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './test/e2e',
  fullyParallel: true,
  forbidOnly: Boolean(process.env.CI),
  retries: process.env.CI ? 2 : 0,
  reporter: 'list',
  use: {
    baseURL: 'http://127.0.0.1:3301',
    trace: 'on-first-retry'
  },
  webServer: {
    command:
      'NUXT_STUDIO_API_BASE=http://127.0.0.1:59999 npm run dev -- --host 127.0.0.1 --port 3301',
    url: 'http://127.0.0.1:3301',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
})
