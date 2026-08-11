const studioApiBase = process.env.NUXT_STUDIO_API_BASE?.replace(/\/$/, '')

if (!studioApiBase) {
  throw new Error('NUXT_STUDIO_API_BASE is required')
}

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
  components: [{ path: '~/components', pathPrefix: false }],
  routeRules: {
    '/api/**': {
      proxy: `${studioApiBase}/api/**`
    }
  },
  runtimeConfig: {
    public: {
      apiBase: '',
      assetBaseUrl:
        process.env.NUXT_PUBLIC_ASSET_BASE_URL ?? 'http://localhost:5300'
    }
  },
  typescript: { typeCheck: true }
})
