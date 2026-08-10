import { fileURLToPath } from 'node:url'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },
  icon: {
    serverBundle: {
      collections: ['lucide']
    }
  },
  modules: ['@nuxt/ui', '@pinia/nuxt'],
  dir: {
    public:
      process.env.L2_PUBLIC_ASSETS_DIR ??
      fileURLToPath(new URL('./assets', import.meta.url))
  },
  runtimeConfig: {
    public: {
      apiBase:
        process.env.NUXT_PUBLIC_STUDIO_API_BASE ?? 'http://localhost:5101'
    }
  },
  typescript: { typeCheck: true }
})
