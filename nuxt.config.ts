import { fileURLToPath } from 'node:url'

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  app: {
    head: {
      link: [
        {
          rel: 'icon',
          href: '/favicon.ico',
          type: 'image/x-icon',
          sizes: '16x16 32x32'
        }
      ]
    }
  },
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },
  icon: {
    serverBundle: {
      collections: ['lucide']
    }
  },
  modules: ['@nuxt/ui', '@pinia/nuxt'],
  dir: {
    public: fileURLToPath(new URL('../../assets', import.meta.url))
  },
  runtimeConfig: {
    public: {
      apiBase:
        process.env.NUXT_PUBLIC_STUDIO_API_BASE ?? 'http://localhost:5101'
    }
  },
  typescript: { typeCheck: true }
})
