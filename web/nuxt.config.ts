const studioApiBase = process.env.NUXT_STUDIO_API_BASE?.replace(/\/$/, '')

if (!studioApiBase) {
  throw new Error('NUXT_STUDIO_API_BASE is required')
}

export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  css: ['~/assets/css/main.css'],
  devtools: { enabled: true },
  icon: {
    clientBundle: {
      scan: {
        globInclude: [
          '**/*.{vue,js,jsx,ts,tsx,md,mdc,mdx,yml,yaml}'
        ]
      }
    },
    localApiEndpoint: '/_nuxt_icon',
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
    studioApiBase,
    storageResourcesRoot:
      process.env.NUXT_STORAGE_RESOURCES_ROOT ?? '/workspace/resources',
    storageAssetsRoot:
      process.env.NUXT_STORAGE_ASSETS_ROOT ?? '/workspace/assets/public',
    public: {
      apiBase: '',
      assetBaseUrl:
        process.env.NUXT_PUBLIC_ASSET_BASE_URL ?? 'http://localhost:5300'
    }
  },
  typescript: { typeCheck: true }
})
